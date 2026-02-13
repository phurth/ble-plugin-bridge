package com.blemqttbridge.core

import android.bluetooth.BluetoothDevice
import android.content.Context
import android.util.Log
import com.blemqttbridge.core.interfaces.BleDevicePlugin
import com.blemqttbridge.core.interfaces.OutputPluginInterface
import com.blemqttbridge.core.interfaces.PollingDevicePlugin
import com.blemqttbridge.core.interfaces.PluginConfig
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock

/**
 * Registry for managing BLE device plugins and output plugins.
 * All plugins now use the BleDevicePlugin architecture.
 * Supports multi-instance plugins for managing multiple devices of the same type.
 */
class PluginRegistry {
    
    companion object {
        private const val TAG = "PluginRegistry"
        
        @Volatile
        private var instance: PluginRegistry? = null
        
        fun getInstance(): PluginRegistry {
            return instance ?: synchronized(this) {
                instance ?: PluginRegistry().also { instance = it }
            }
        }
    }
    
    private val mutex = Mutex()
    private val loadedDevicePlugins = mutableMapOf<String, BleDevicePlugin>()
    private val pluginInstances = mutableMapOf<String, BleDevicePlugin>()  // v2.6.0+: Multi-instance support
    private var outputPlugin: OutputPluginInterface? = null
    
    // Plugin factory map: pluginId -> factory function
    private val blePluginFactories = mutableMapOf<String, () -> BleDevicePlugin>()
    private val outputPluginFactories = mutableMapOf<String, () -> OutputPluginInterface>()
    private val pollingPluginFactories = mutableMapOf<String, () -> PollingDevicePlugin>()

    // Active polling plugin instances
    private val pollingPluginInstances = mutableMapOf<String, PollingDevicePlugin>()
    
    /**
     * Register a BLE plugin factory.
     * Factory will be called only when plugin is actually needed.
     * All plugins must implement BleDevicePlugin interface.
     */
    fun registerBlePlugin(pluginId: String, factory: () -> BleDevicePlugin) {
        blePluginFactories[pluginId] = factory
        Log.d(TAG, "Registered BLE plugin: $pluginId")
    }
    
    /**
     * Register an output plugin factory.
     */
    fun registerOutputPlugin(pluginId: String, factory: () -> OutputPluginInterface) {
        outputPluginFactories[pluginId] = factory
        Log.d(TAG, "Registered output plugin: $pluginId")
    }

    /**
     * Register a polling plugin factory.
     * Polling plugins use REST APIs instead of BLE for device communication.
     */
    fun registerPollingPlugin(pluginId: String, factory: () -> PollingDevicePlugin) {
        pollingPluginFactories[pluginId] = factory
        Log.d(TAG, "Registered polling plugin: $pluginId")
    }

    /**
     * Find which plugin can handle a discovered device.
     * Checks loaded plugins first (fast), then factory creates temporary instances.
     * Only checks plugins that are enabled in ServiceStateManager.
     * 
     * @param device The discovered Bluetooth device
     * @param scanRecord Raw scan record data
     * @param context Context for loading plugin config (required for enabled check)
     * @return Plugin ID if a match is found, null otherwise
     */
    fun findPluginForDevice(device: BluetoothDevice, scanRecord: ByteArray?, context: Context? = null): String? {
        // Get enabled plugins - if no context, fall back to checking all (legacy behavior)
        val enabledPlugins = if (context != null) {
            ServiceStateManager.getEnabledBlePlugins(context)
        } else {
            null
        }
        
        // Check other registered plugins by creating temporary instances
        for ((pluginId, factory) in blePluginFactories) {
            // Skip if not enabled (when context is available)
            // For multi-instance plugins, check if ANY instance of this plugin type is enabled
            if (enabledPlugins != null) {
                val pluginOrInstanceEnabled = enabledPlugins.contains(pluginId) || 
                    enabledPlugins.any { it.startsWith("${pluginId}_") }
                if (!pluginOrInstanceEnabled) {
                    Log.d(TAG, "Skipping disabled plugin: $pluginId (device: ${device.address})")
                    continue
                } else {
                    Log.d(TAG, "Plugin $pluginId is enabled, will check device ${device.address}")
                }
            }
            
            try {
                val tempPlugin = factory()
                
                // For multi-instance plugins, check each instance's config
                if (context != null && tempPlugin.supportsMultipleInstances) {
                    val allInstances = ServiceStateManager.getAllInstances(context)
                    val matchingInstances = allInstances.filter { (_, instance) -> 
                        instance.pluginType == pluginId 
                    }
                    
                    for ((instanceId, instance) in matchingInstances) {
                        try {
                            val instancePlugin = factory()
                            Log.d(TAG, "Instance $instanceId config map: ${instance.config}")
                            
                            // Build config from instance data
                            val configMap = if (instance.config.isNotEmpty()) {
                                instance.config.toMutableMap()
                            } else {
                                mutableMapOf()
                            }
                            
                            // Add device MAC to config based on plugin type
                            when (pluginId) {
                                "easytouch" -> {
                                    configMap["thermostat_mac"] = instance.deviceMac
                                    instance.config["password"]?.let { configMap["thermostat_password"] = it }
                                }
                                "onecontrol", "onecontrol_v2" -> configMap["gateway_mac"] = instance.deviceMac
                                "gopower" -> configMap["controller_mac"] = instance.deviceMac
                                "hughes_watchdog" -> configMap["watchdog_mac"] = instance.deviceMac
                                "hughes_gen2" -> configMap["watchdog_gen2_mac"] = instance.deviceMac
                                "mopeka" -> configMap["sensor_mac"] = instance.deviceMac
                            }
                            
                            // Use new initialize method with instance config
                            instancePlugin.initializeWithConfig(instanceId, configMap)
                            
                            Log.d(TAG, "Checking if device ${device.address} matches instance: $instanceId")
                            // Convert ByteArray back to ScanRecord for matching
                            val scanRecordObj = scanRecord?.let { bytes ->
                                try {
                                    // Use the bytes directly to create a new ScanRecord via the builder pattern
                                    val parser = Class.forName("android.bluetooth.le.ScanRecord")
                                        .getDeclaredMethod("parseFromBytes", ByteArray::class.java)
                                    parser.invoke(null, bytes) as? android.bluetooth.le.ScanRecord
                                } catch (e: Exception) {
                                    Log.w(TAG, "Failed to parse ScanRecord: ${e.message}")
                                    null
                                }
                            }
                            if (instancePlugin.matchesDevice(device, scanRecordObj)) {
                                Log.d(TAG, "Device ${device.address} matches instance: $instanceId")
                                return instanceId  // Return instance ID, not plugin type
                            }
                        } catch (e: Exception) {
                            Log.w(TAG, "Error checking instance $instanceId: ${e.message}")
                        }
                    }
                } else {
                    // Single-instance plugin: Still need to match against stored instances
                    // These plugins should already be loaded via createPluginInstance,
                    // but we still need to check if the device MAC matches the configured instance
                    if (context != null) {
                        val allInstances = ServiceStateManager.getAllInstances(context)
                        val matchingInstances = allInstances.filter { (_, instance) -> 
                            instance.pluginType == pluginId 
                        }
                        
                        for ((instanceId, instance) in matchingInstances) {
                            // For single-instance plugins, match by device MAC address
                            if (device.address.equals(instance.deviceMac, ignoreCase = true)) {
                                Log.d(TAG, "Device ${device.address} matches single-instance plugin: $instanceId (plugin: $pluginId)")
                                return instanceId  // Return instance ID for single-instance plugin
                            }
                        }
                        
                        Log.d(TAG, "Skipping single-instance plugin $pluginId - no MAC match for device ${device.address}")
                    } else {
                        Log.d(TAG, "Skipping single-instance plugin $pluginId - no context available for instance lookup")
                    }
                }
            } catch (e: Exception) {
                Log.e(TAG, "Error checking plugin $pluginId for device", e)
            }
        }
        
        return null
    }
    
    /**
     * Get the output plugin.
     * Loads it if not already loaded.
     */
    suspend fun getOutputPlugin(
        pluginId: String,
        context: Context,
        config: Map<String, String>
    ): OutputPluginInterface? = mutex.withLock {
        // If already loaded, return it
        if (outputPlugin != null) {
            Log.d(TAG, "Output plugin already loaded: $pluginId")
            return@withLock outputPlugin
        }
        
        val factory = outputPluginFactories[pluginId]
        if (factory == null) {
            Log.e(TAG, "No factory registered for output plugin: $pluginId")
            return@withLock null
        }
        
        try {
            Log.i(TAG, "Loading output plugin: $pluginId")
            val plugin = factory()
            val result = plugin.initialize(config)
            
            if (result.isSuccess) {
                outputPlugin = plugin
                Log.i(TAG, "Output plugin loaded successfully: $pluginId")
                return@withLock plugin
            } else {
                Log.e(TAG, "Failed to initialize output plugin $pluginId: ${result.exceptionOrNull()?.message}")
                return@withLock null
            }
        } catch (e: Exception) {
            Log.e(TAG, "Exception loading output plugin $pluginId", e)
            return@withLock null
        }
    }
    
    /**
     * Get a BLE device plugin (new architecture) if it implements the interface.
     * Returns the same instance for repeated calls (singleton per plugin ID).
     * 
     * @param pluginId The plugin to check
     * @param context Android context for initialization (will load plugin if needed)
     * @return The plugin if it implements BleDevicePlugin, null otherwise
     */
    fun getDevicePlugin(pluginId: String, context: Context): BleDevicePlugin? {
        // Check if already loaded
        loadedDevicePlugins[pluginId]?.let {
            return it
        }
        
        // Check if this plugin is registered and creates a BleDevicePlugin
        val factory = blePluginFactories[pluginId] ?: return null
        
        try {
            val plugin = factory()
            if (plugin is BleDevicePlugin) {
                // Cache the instance so we return the same one
                loadedDevicePlugins[pluginId] = plugin
                Log.d(TAG, "Created and cached device plugin: $pluginId")
                return plugin
            } else {
                return null
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error creating device plugin $pluginId", e)
            return null
        }
    }
    
    /**
     * Get the currently loaded output plugin (if any).
     */
    fun getCurrentOutputPlugin(): OutputPluginInterface? = outputPlugin
    
    /**
     * Unload a specific device plugin.
     * Called when plugin is being removed/disabled.
     */
    suspend fun unloadDevicePlugin(pluginId: String) = mutex.withLock {
        loadedDevicePlugins[pluginId]?.let { plugin ->
            Log.i(TAG, "Unloading device plugin: $pluginId")
            // Device plugins don't have a cleanup method in the interface yet
            // But we should remove the instance to allow garbage collection
            loadedDevicePlugins.remove(pluginId)
            Log.i(TAG, "Device plugin $pluginId removed from cache")
            System.gc()
        }
    }

    // ============================================================================
    // Multi-Instance Plugin Management (v2.6.0+)
    // ============================================================================

    /**
     * Create a new plugin instance from a PluginInstance descriptor.
     * Used for multi-instance scenarios where multiple devices of the same type
     * are managed independently.
     * 
     * @param instance The PluginInstance containing device-specific config
     * @param context Android context for initialization
     * @return The initialized plugin instance, or null if creation failed
     */
    fun createPluginInstance(instance: PluginInstance, context: Context): BleDevicePlugin? {
        val pluginType = instance.pluginType
        val factory = blePluginFactories[pluginType]
        
        if (factory == null) {
            Log.e(TAG, "No factory registered for plugin type: $pluginType")
            return null
        }
        
        return try {
            Log.i(TAG, "Creating plugin instance: ${instance.instanceId} (type: $pluginType)")
            val plugin = factory()
            
            if (plugin !is BleDevicePlugin) {
                Log.e(TAG, "Plugin factory for $pluginType does not return BleDevicePlugin")
                return null
            }
            
            // Build config map with device-specific fields
            val configWithDevice = instance.config.toMutableMap()
            
            // Map generic config keys to plugin-specific key names
            when (instance.pluginType) {
                "easytouch" -> {
                    configWithDevice["thermostat_mac"] = instance.deviceMac
                    // Map generic "password" to "thermostat_password"
                    instance.config["password"]?.let {
                        configWithDevice["thermostat_password"] = it
                        Log.d(TAG, "Mapped password -> thermostat_password for ${instance.instanceId}")
                    }
                }
                "onecontrol" -> {
                    configWithDevice["gateway_mac"] = instance.deviceMac
                    // "gateway_pin" already uses correct key name
                }
                "onecontrol_v2" -> {
                    configWithDevice["gateway_mac"] = instance.deviceMac
                    // "gateway_pin" already uses correct key name
                }
                "gopower" -> {
                    configWithDevice["controller_mac"] = instance.deviceMac
                }
                "hughes_watchdog" -> {
                    configWithDevice["watchdog_mac"] = instance.deviceMac
                }
                "hughes_gen2" -> {
                    configWithDevice["watchdog_gen2_mac"] = instance.deviceMac
                }
                "mopeka" -> {
                    configWithDevice["sensor_mac"] = instance.deviceMac
                }
                // Add other plugin types as needed
            }
            
            Log.d(TAG, "Instance ${instance.instanceId} config keys: ${configWithDevice.keys}")
            
            // Initialize with instance-specific configuration (including device MAC)
            plugin.initializeWithConfig(instance.instanceId, configWithDevice)
            
            // Cache the instance
            pluginInstances[instance.instanceId] = plugin
            
            Log.i(TAG, "Plugin instance created successfully: ${instance.instanceId}")
            plugin
        } catch (e: Exception) {
            Log.e(TAG, "Error creating plugin instance ${instance.instanceId}", e)
            null
        }
    }

    /**
     * Get a previously created plugin instance by ID.
     * Returns null if the instance hasn't been created yet.
     * 
     * @param instanceId The unique instance ID (e.g., "easytouch_b1241e")
     * @return The plugin instance, or null if not found
     */
    fun getPluginInstance(instanceId: String): BleDevicePlugin? {
        return pluginInstances[instanceId]
    }

    /**
     * Get all created plugin instances.
     * 
     * @return Map of instanceId -> BleDevicePlugin
     */
    fun getAllPluginInstances(): Map<String, BleDevicePlugin> {
        return pluginInstances.toMap()
    }

    /**
     * Get plugin instances filtered by plugin type.
     * 
     * @param pluginType The plugin type (e.g., "easytouch")
     * @return List of plugin instances of this type
     */
    fun getPluginInstancesOfType(pluginType: String): List<BleDevicePlugin> {
        return pluginInstances.values.filter { 
            it.pluginId == pluginType 
        }
    }

    /**
     * Remove and unload a plugin instance.
     * Called when instance is being deleted/disabled.
     * 
     * @param instanceId The instance to remove
     */
    suspend fun removePluginInstance(instanceId: String) = mutex.withLock {
        pluginInstances[instanceId]?.let { plugin ->
            Log.i(TAG, "Removing plugin instance: $instanceId")
            plugin.destroy()
            pluginInstances.remove(instanceId)
            Log.i(TAG, "Plugin instance removed: $instanceId")
            System.gc()
        }
    }
    
    /**
     * Unload all plugins and cleanup resources.
     */
    suspend fun cleanup() = mutex.withLock {
        Log.i(TAG, "Cleaning up all plugins (${loadedDevicePlugins.size} device plugins, ${pluginInstances.size} instances)")
        
        // Clear device plugins
        loadedDevicePlugins.clear()
        
        // Clear plugin instances
        for ((instanceId, plugin) in pluginInstances) {
            Log.i(TAG, "Cleaning up plugin instance: $instanceId")
            plugin.destroy()
        }
        pluginInstances.clear()

        outputPlugin?.disconnect()
        outputPlugin = null

        // Don't clear polling plugins - they are managed independently by the polling service
        // Polling plugins should continue running even when BLE service stops

        System.gc()
    }

    // ============================================================================
    // Polling Plugin Management
    // ============================================================================

    /**
     * Create and initialize a polling plugin instance.
     *
     * @param pluginType The plugin type (e.g., "peplink")
     * @param instanceId Unique instance identifier (e.g., "peplink_main")
     * @param context Android context
     * @param config Plugin configuration
     * @return The initialized plugin instance, or null if creation failed
     */
    fun createPollingPluginInstance(
        pluginType: String,
        instanceId: String,
        context: Context,
        config: PluginConfig
    ): PollingDevicePlugin? {
        val factory = pollingPluginFactories[pluginType]

        if (factory == null) {
            Log.e(TAG, "No factory registered for polling plugin type: $pluginType")
            return null
        }

        return try {
            Log.i(TAG, "Creating polling plugin instance: $instanceId (type: $pluginType)")
            val plugin = factory()
            plugin.instanceId = instanceId  // Set the instanceId BEFORE initialize
            plugin.initialize(context, config)

            // Cache the instance
            pollingPluginInstances[instanceId] = plugin

            Log.i(TAG, "Polling plugin instance created successfully: $instanceId")
            plugin
        } catch (e: Exception) {
            Log.e(TAG, "Error creating polling plugin instance $instanceId", e)
            null
        }
    }

    /**
     * Get a polling plugin instance by ID.
     *
     * @param instanceId The instance ID (e.g., "peplink_main")
     * @return The plugin instance, or null if not found
     */
    fun getPollingPluginInstance(instanceId: String): PollingDevicePlugin? {
        return pollingPluginInstances[instanceId]
    }

    /**
     * Get all polling plugin instances.
     *
     * @return Map of instanceId -> PollingDevicePlugin
     */
    fun getAllPollingPluginInstances(): Map<String, PollingDevicePlugin> {
        return pollingPluginInstances.toMap()
    }

    /**
     * Remove and destroy a polling plugin instance.
     *
     * @param instanceId The instance to remove
     */
    suspend fun removePollingPluginInstance(instanceId: String) = mutex.withLock {
        pollingPluginInstances[instanceId]?.let { plugin ->
            Log.i(TAG, "Removing polling plugin instance: $instanceId")
            plugin.destroy()
            pollingPluginInstances.remove(instanceId)
            Log.i(TAG, "Polling plugin instance removed: $instanceId")
            System.gc()
        }
    }

    /**
     * Get list of registered BLE plugin IDs.
     */
    fun getRegisteredBlePlugins(): List<String> = blePluginFactories.keys.toList()

    /**
     * Get list of registered output plugin IDs.
     */
    fun getRegisteredOutputPlugins(): List<String> = outputPluginFactories.keys.toList()

    /**
     * Get list of registered polling plugin IDs.
     */
    fun getRegisteredPollingPlugins(): List<String> = pollingPluginFactories.keys.toList()
}
