package com.blemqttbridge.core

import android.bluetooth.BluetoothDevice
import android.content.Context
import android.util.Log
import com.blemqttbridge.core.interfaces.BlePluginInterface
import com.blemqttbridge.core.interfaces.OutputPluginInterface
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
    private var currentBlePlugin: BlePluginInterface? = null
    private var currentBlePluginId: String? = null
    private var outputPlugin: OutputPluginInterface? = null
    
    // Plugin factory map: pluginId -> factory function
    private val blePluginFactories = mutableMapOf<String, () -> BlePluginInterface>()
    private val outputPluginFactories = mutableMapOf<String, () -> OutputPluginInterface>()
    
    /**
     * Register a BLE plugin factory.
     * Factory will be called only when plugin is actually needed.
     */
    fun registerBlePlugin(pluginId: String, factory: () -> BlePluginInterface) {
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
     * Get the currently loaded BLE plugin.
     * Loads it if not already loaded (lazy initialization).
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
        if (currentBlePluginId == pluginId && currentBlePlugin != null) {
            Log.d(TAG, "BLE plugin already loaded: $pluginId")
            return@withLock currentBlePlugin
        }
        
        // Unload current plugin if different
        if (currentBlePlugin != null && currentBlePluginId != pluginId) {
            Log.i(TAG, "Unloading BLE plugin: $currentBlePluginId")
            currentBlePlugin?.cleanup()
            currentBlePlugin = null
            currentBlePluginId = null
            // Suggest GC to reclaim memory
            System.gc()
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
            val result = plugin.initialize(context, config)
            
            if (result.isSuccess) {
                currentBlePlugin = plugin
                currentBlePluginId = pluginId
                Log.i(TAG, "BLE plugin loaded successfully: $pluginId v${plugin.getPluginVersion()}")
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
     * 
     * @param device The discovered Bluetooth device
     * @param scanRecord Raw scan record data
     * @return Plugin ID if a match is found, null otherwise
     */
    fun findPluginForDevice(device: BluetoothDevice, scanRecord: ByteArray?): String? {
        // Check currently loaded plugin first (fast path)
        if (currentBlePlugin?.canHandleDevice(device, scanRecord) == true) {
            return currentBlePluginId
        }
        
        // Check other registered plugins
        // Note: We create temporary instances just to check canHandleDevice
        // This is lightweight since we don't initialize them
        for ((pluginId, factory) in blePluginFactories) {
            if (pluginId == currentBlePluginId) continue // already checked
            
            try {
                val tempPlugin = factory()
                if (tempPlugin.canHandleDevice(device, scanRecord)) {
                    Log.d(TAG, "Device ${device.address} matches plugin: $pluginId")
                    return pluginId
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
     * Get the currently loaded BLE plugin (if any).
     */
    fun getCurrentBlePlugin(): BlePluginInterface? = currentBlePlugin
    
    /**
     * Get the currently loaded output plugin (if any).
     */
    fun getCurrentOutputPlugin(): OutputPluginInterface? = outputPlugin
    
    /**
     * Unload all plugins and cleanup resources.
     */
    suspend fun cleanup() = mutex.withLock {
        Log.i(TAG, "Cleaning up all plugins")
        
        currentBlePlugin?.cleanup()
        currentBlePlugin = null
        currentBlePluginId = null
        
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
