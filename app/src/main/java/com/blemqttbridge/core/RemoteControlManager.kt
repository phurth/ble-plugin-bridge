package com.blemqttbridge.core

import android.content.Context
import android.content.Intent
import android.util.Log
import com.blemqttbridge.core.interfaces.OutputPluginInterface
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.launch
import org.json.JSONObject

/**
 * Manages remote control of the BLE bridge service via MQTT commands.
 * 
 * Supports commands:
 * - Service control: start, stop, restart
 * - Plugin management: load, unload, reload
 * - Status queries: list_plugins, service_status
 * 
 * Command format (JSON):
 * {
 *   "command": "load_plugin",
 *   "plugin_id": "onecontrol",
 *   "config": { ... }
 * }
 */
class RemoteControlManager(
    private val context: Context,
    private val serviceScope: CoroutineScope,
    private val pluginRegistry: PluginRegistry
) {
    companion object {
        private const val TAG = "RemoteControlManager"
        private const val CONTROL_TOPIC = "bridge/control"
        private const val STATUS_TOPIC = "bridge/status"
    }
    
    private var outputPlugin: OutputPluginInterface? = null
    
    /**
     * Initialize remote control by subscribing to control topics.
     */
    suspend fun initialize(output: OutputPluginInterface) {
        outputPlugin = output
        
        try {
            // Subscribe to control commands
            output.subscribeToCommands(CONTROL_TOPIC) { topic, payload ->
                serviceScope.launch {
                    handleControlCommand(payload)
                }
            }
            
            Log.i(TAG, "Remote control initialized - listening on $CONTROL_TOPIC")
            publishStatus("Remote control active")
            
        } catch (e: Exception) {
            Log.e(TAG, "Failed to initialize remote control", e)
        }
    }
    
    /**
     * Handle incoming control command.
     */
    private suspend fun handleControlCommand(payload: String) {
        try {
            val json = JSONObject(payload)
            val command = json.optString("command", "")
            
            Log.i(TAG, "Received command: $command")
            
            when (command) {
                "start_service" -> handleStartService(json)
                "stop_service" -> handleStopService()
                "restart_service" -> handleRestartService(json)
                "load_plugin" -> handleLoadPlugin(json)
                "unload_plugin" -> handleUnloadPlugin(json)
                "reload_plugin" -> handleReloadPlugin(json)
                "list_plugins" -> handleListPlugins()
                "service_status" -> handleServiceStatus()
                else -> {
                    Log.w(TAG, "Unknown command: $command")
                    publishStatus("Error: Unknown command '$command'")
                }
            }
            
        } catch (e: Exception) {
            Log.e(TAG, "Error handling control command", e)
            publishStatus("Error: ${e.message}")
        }
    }
    
    /**
     * Start the BLE service with specified plugin.
     */
    private fun handleStartService(json: JSONObject) {
        val blePluginId = json.optString("ble_plugin", "onecontrol_v2")
        val outputPluginId = json.optString("output_plugin", "mqtt")
        
        Log.i(TAG, "Starting service with BLE plugin: $blePluginId")
        
        val intent = Intent(context, BaseBleService::class.java).apply {
            action = BaseBleService.ACTION_START_SCAN
            putExtra(BaseBleService.EXTRA_BLE_PLUGIN_ID, blePluginId)
            putExtra(BaseBleService.EXTRA_OUTPUT_PLUGIN_ID, outputPluginId)
        }
        
        context.startForegroundService(intent)
        publishStatus("Service starting with plugin: $blePluginId")
    }
    
    /**
     * Stop the BLE service.
     */
    private fun handleStopService() {
        Log.i(TAG, "Stopping service")
        
        val intent = Intent(context, BaseBleService::class.java).apply {
            action = BaseBleService.ACTION_STOP_SERVICE
        }
        
        context.startService(intent)
        publishStatus("Service stopping")
    }
    
    /**
     * Restart the service with new configuration.
     */
    private fun handleRestartService(json: JSONObject) {
        Log.i(TAG, "Restarting service")
        handleStopService()
        
        // Wait briefly, then start
        serviceScope.launch {
            kotlinx.coroutines.delay(1000)
            handleStartService(json)
        }
    }
    
    /**
     * Load a BLE plugin with configuration.
     */
    private suspend fun handleLoadPlugin(json: JSONObject) {
        val pluginId = json.optString("plugin_id", "")
        
        if (pluginId.isEmpty()) {
            publishStatus("Error: plugin_id required")
            return
        }
        
        Log.i(TAG, "Loading plugin: $pluginId")
        
        // Parse config if provided
        val config = mutableMapOf<String, String>()
        if (json.has("config")) {
            val configJson = json.getJSONObject("config")
            configJson.keys().forEach { key ->
                config[key] = configJson.getString(key)
            }
        } else {
            // Load from SharedPreferences
            config.putAll(AppConfig.getBlePluginConfig(context, pluginId))
        }
        
        // Load plugin
        val plugin = pluginRegistry.getBlePlugin(pluginId, context, config)
        
        if (plugin != null) {
            publishStatus("Plugin loaded: $pluginId v${plugin.getPluginVersion()}")
        } else {
            publishStatus("Error: Failed to load plugin '$pluginId'")
        }
    }
    
    /**
     * Unload a BLE plugin.
     */
    private suspend fun handleUnloadPlugin(json: JSONObject) {
        val pluginId = json.optString("plugin_id", "")
        
        if (pluginId.isEmpty()) {
            publishStatus("Error: plugin_id required")
            return
        }
        
        Log.i(TAG, "Unloading plugin: $pluginId")
        
        pluginRegistry.unloadBlePlugin(pluginId)
        publishStatus("Plugin unloaded: $pluginId")
    }
    
    /**
     * Reload a plugin (unload + load).
     */
    private suspend fun handleReloadPlugin(json: JSONObject) {
        val pluginId = json.optString("plugin_id", "")
        
        if (pluginId.isEmpty()) {
            publishStatus("Error: plugin_id required")
            return
        }
        
        Log.i(TAG, "Reloading plugin: $pluginId")
        
        // Unload
        pluginRegistry.unloadBlePlugin(pluginId)
        
        // Wait briefly
        kotlinx.coroutines.delay(500)
        
        // Load with stored config
        val config = AppConfig.getBlePluginConfig(context, pluginId)
        val plugin = pluginRegistry.getBlePlugin(pluginId, context, config)
        
        if (plugin != null) {
            publishStatus("Plugin reloaded: $pluginId v${plugin.getPluginVersion()}")
        } else {
            publishStatus("Error: Failed to reload plugin '$pluginId'")
        }
    }
    
    /**
     * List all registered and loaded plugins.
     */
    private fun handleListPlugins() {
        val registered = pluginRegistry.getRegisteredBlePlugins()
        val loaded = pluginRegistry.getLoadedBlePlugins()
        
        val response = JSONObject().apply {
            put("registered_plugins", registered.joinToString(", "))
            put("loaded_plugins", loaded.keys.joinToString(", "))
            put("loaded_count", loaded.size)
        }
        
        publishStatus(response.toString())
    }
    
    /**
     * Report service status.
     */
    private fun handleServiceStatus() {
        val isRunning = ServiceStateManager.wasServiceRunning(context)
        val loaded = pluginRegistry.getLoadedBlePlugins()
        
        val response = JSONObject().apply {
            put("service_running", isRunning)
            put("loaded_plugins", loaded.keys.joinToString(", "))
            put("mqtt_connected", outputPlugin?.isConnected() ?: false)
        }
        
        publishStatus(response.toString())
    }
    
    /**
     * Publish status message to status topic.
     */
    private fun publishStatus(message: String) {
        serviceScope.launch {
            try {
                outputPlugin?.publishState(STATUS_TOPIC, message, retained = false)
                Log.d(TAG, "Status published: $message")
            } catch (e: Exception) {
                Log.e(TAG, "Failed to publish status", e)
            }
        }
    }
    
    /**
     * Cleanup resources.
     */
    fun cleanup() {
        outputPlugin = null
        Log.i(TAG, "Remote control manager cleaned up")
    }
}
