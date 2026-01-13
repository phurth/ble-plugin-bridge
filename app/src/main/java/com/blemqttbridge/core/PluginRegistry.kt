package com.blemqttbridge.core

import android.bluetooth.BluetoothDevice
import android.content.Context
import android.util.Log
import com.blemqttbridge.core.interfaces.BlePluginInterface
import com.blemqttbridge.core.interfaces.BleDevicePlugin
import com.blemqttbridge.core.interfaces.OutputPluginInterface
import com.blemqttbridge.core.interfaces.PluginConfig
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock

/**
 * Registry for managing BLE device plugins and output plugins.
 * Implements lazy loading to minimize memory usage.
 * Only one BLE plugin is loaded at a time (memory optimization).
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
    private val loadedBlePlugins = mutableMapOf<String, BlePluginInterface>()
    private val loadedDevicePlugins = mutableMapOf<String, BleDevicePlugin>()
    private val pluginInstances = mutableMapOf<String, BleDevicePlugin>()  // v2.6.0+: Multi-instance support
    private var outputPlugin: OutputPluginInterface? = null
    
    // Plugin factory map: pluginId -> factory function
    // NOTE: Factories can return either BlePluginInterface (legacy) or BleDevicePlugin (new)
    private val blePluginFactories = mutableMapOf<String, () -> Any>()
    private val outputPluginFactories = mutableMapOf<String, () -> OutputPluginInterface>()
    
    /**
     * Register a BLE plugin factory.
     * Factory will be called only when plugin is actually needed.
     * Can return either BlePluginInterface (legacy) or BleDevicePlugin (new architecture).
     */
    fun registerBlePlugin(pluginId: String, factory: () -> Any) {
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
     * Get a BLE plugin (loaded or loads it on-demand).
     * Supports multiple plugins loaded simultaneously.
     * 
     * @param pluginId The plugin to load
     * @param context Android context for initialization
     * @param config Plugin configuration
     * @return The loaded plugin, or null if load failed
     */
    suspend fun getBlePlugin(
        pluginId: String,
        context: Context,
        config: Map<String, String>
    ): BlePluginInterface? = mutex.withLock {
        // If we already have this plugin loaded, return it
        loadedBlePlugins[pluginId]?.let {
            Log.d(TAG, "BLE plugin already loaded: $pluginId")
            return@withLock it
        }
        
        // Load new plugin
        val factory = blePluginFactories[pluginId]
        if (factory == null) {
            Log.e(TAG, "No factory registered for BLE plugin: $pluginId")
            return@withLock null
        }
        
        try {
            Log.i(TAG, "Loading BLE plugin: $pluginId")
            val plugin = factory()
            
            // Check if it's a legacy BlePluginInterface
            if (plugin !is BlePluginInterface) {
                Log.w(TAG, "Plugin $pluginId is not a BlePluginInterface (may be BleDevicePlugin - use getDevicePlugin instead)")
                return@withLock null
            }
            
            val result = plugin.initialize(context, config)
            
            if (result.isSuccess) {
                loadedBlePlugins[pluginId] = plugin
                Log.i(TAG, "BLE plugin loaded successfully: $pluginId v${plugin.getPluginVersion()} (${loadedBlePlugins.size} total)")
                return@withLock plugin
            } else {
                Log.e(TAG, "Failed to initialize BLE plugin $pluginId: ${result.exceptionOrNull()?.message}")
                return@withLock null
            }
        } catch (e: Exception) {
            Log.e(TAG, "Exception loading BLE plugin $pluginId", e)
            return@withLock null
        }
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
        
        // Check already loaded plugins first (fast path)
        for ((pluginId, plugin) in loadedBlePlugins) {
            // Skip if not enabled (when context is available)
            if (enabledPlugins != null && !enabledPlugins.contains(pluginId)) {
                continue
            }
            
            if (plugin.canHandleDevice(device, scanRecord)) {
                Log.d(TAG, "Device ${device.address} matches loaded plugin: $pluginId")
                return pluginId
            }
        }
        
        // Check other registered plugins by creating temporary instances
        for ((pluginId, factory) in blePluginFactories) {
            if (loadedBlePlugins.containsKey(pluginId)) continue // already checked
            
            // Skip if not enabled (when context is available)
            if (enabledPlugins != null && !enabledPlugins.contains(pluginId)) {
                Log.d(TAG, "Skipping disabled plugin: $pluginId")
                continue
            }
            
            try {
                val tempPlugin = factory()
                
                // Check if it's a new-style BleDevicePlugin
                if (tempPlugin is BleDevicePlugin) {
                    // Initialize with config if context available (for MAC matching)
                    if (context != null) {
                        try {
                            val config = PluginConfig(AppConfig.getBlePluginConfig(context, pluginId))
                            tempPlugin.initialize(context, config)
                        } catch (e: Exception) {
                            Log.w(TAG, "Could not initialize temp plugin $pluginId with config: ${e.message}")
                        }
                    }
                    
                    // For now, pass null for scanRecord - matching is done by MAC/name
                    // TODO: Convert ByteArray to ScanRecord when needed
                    if (tempPlugin.matchesDevice(device, null)) {
                        Log.d(TAG, "Device ${device.address} matches device plugin: $pluginId (not loaded yet)")
                        return pluginId
                    }
                }
                // Check if it's a legacy BlePluginInterface
                else if (tempPlugin is BlePluginInterface) {
                    if (tempPlugin.canHandleDevice(device, scanRecord)) {
                        Log.d(TAG, "Device ${device.address} matches legacy plugin: $pluginId (not loaded yet)")
                        return pluginId
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
     * Get a specific loaded BLE plugin.
     */
    fun getLoadedBlePlugin(pluginId: String): BlePluginInterface? = loadedBlePlugins[pluginId]
    
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
     * Get all currently loaded BLE plugins.
     */
    fun getLoadedBlePlugins(): Map<String, BlePluginInterface> = loadedBlePlugins.toMap()
    
    /**
     * Get the currently loaded output plugin (if any).
     */
    fun getCurrentOutputPlugin(): OutputPluginInterface? = outputPlugin
    
    /**
     * Unload a specific BLE plugin.
     * Called when last device of this type disconnects.
     */
    suspend fun unloadBlePlugin(pluginId: String) = mutex.withLock {
        loadedBlePlugins[pluginId]?.let { plugin ->
            Log.i(TAG, "Unloading BLE plugin: $pluginId")
            plugin.cleanup()
            loadedBlePlugins.remove(pluginId)
            System.gc()
        }
    }
    
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
            
            // Add device MAC to config with plugin-specific key names
            when (instance.pluginType) {
                "easytouch" -> configWithDevice["thermostat_mac"] = instance.deviceMac
                "onecontrol" -> configWithDevice["gateway_mac"] = instance.deviceMac
                "onecontrol_v2" -> configWithDevice["gateway_mac"] = instance.deviceMac
                "gopower" -> configWithDevice["controller_mac"] = instance.deviceMac
                // Add other plugin types as needed
            }
            
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
        Log.i(TAG, "Cleaning up all plugins (${loadedBlePlugins.size} BLE plugins, ${loadedDevicePlugins.size} device plugins, ${pluginInstances.size} instances)")
        
        for ((pluginId, plugin) in loadedBlePlugins) {
            Log.i(TAG, "Cleaning up BLE plugin: $pluginId")
            plugin.cleanup()
        }
        loadedBlePlugins.clear()
        
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
        
        System.gc()
    }
    
    /**
     * Get list of registered BLE plugin IDs.
     */
    fun getRegisteredBlePlugins(): List<String> = blePluginFactories.keys.toList()
    
    /**
     * Get list of registered output plugin IDs.
     */
    fun getRegisteredOutputPlugins(): List<String> = outputPluginFactories.keys.toList()
}
