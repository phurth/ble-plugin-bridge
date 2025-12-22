package com.blemqttbridge.receivers

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.util.Log
import com.blemqttbridge.core.BaseBleService
import com.blemqttbridge.core.PluginRegistry
import com.blemqttbridge.core.AppConfig
import com.blemqttbridge.core.ServiceStateManager
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch

/**
 * BroadcastReceiver for complete service and plugin control via ADB.
 * 
 * Provides the same functionality as MQTT remote control but via ADB broadcasts.
 * All responses are logged to logcat for capture via `adb logcat`.
 * 
 * USAGE EXAMPLES:
 * 
 * # Start service with OneControl plugin
 * adb shell am broadcast -a com.blemqttbridge.CONTROL_COMMAND \
 *   --es command "start_service" \
 *   --es ble_plugin "onecontrol" \
 *   --es output_plugin "mqtt"
 * 
 * # Stop service
 * adb shell am broadcast -a com.blemqttbridge.CONTROL_COMMAND \
 *   --es command "stop_service"
 * 
 * # Restart service
 * adb shell am broadcast -a com.blemqttbridge.CONTROL_COMMAND \
 *   --es command "restart_service" \
 *   --es ble_plugin "onecontrol"
 * 
 * # Load plugin
 * adb shell am broadcast -a com.blemqttbridge.CONTROL_COMMAND \
 *   --es command "load_plugin" \
 *   --es plugin_id "onecontrol"
 * 
 * # Unload plugin
 * adb shell am broadcast -a com.blemqttbridge.CONTROL_COMMAND \
 *   --es command "unload_plugin" \
 *   --es plugin_id "onecontrol"
 * 
 * # Reload plugin (unload + load)
 * adb shell am broadcast -a com.blemqttbridge.CONTROL_COMMAND \
 *   --es command "reload_plugin" \
 *   --es plugin_id "onecontrol"
 * 
 * # List plugins
 * adb shell am broadcast -a com.blemqttbridge.CONTROL_COMMAND \
 *   --es command "list_plugins"
 * 
 * # Get service status
 * adb shell am broadcast -a com.blemqttbridge.CONTROL_COMMAND \
 *   --es command "service_status"
 * 
 * Monitor responses:
 *   adb logcat | grep "ControlCmd:"
 */
class ControlCommandReceiver : BroadcastReceiver() {
    
    companion object {
        private const val TAG = "ControlCommandReceiver"
        const val ACTION_CONTROL_COMMAND = "com.blemqttbridge.CONTROL_COMMAND"
        const val EXTRA_COMMAND = "command"
        const val EXTRA_BLE_PLUGIN = "ble_plugin"
        const val EXTRA_OUTPUT_PLUGIN = "output_plugin"
        const val EXTRA_PLUGIN_ID = "plugin_id"
        
        // For logcat filtering - use prefix that's easy to grep
        private const val RESPONSE_PREFIX = "ControlCmd:"
    }
    
    override fun onReceive(context: Context, intent: Intent) {
        Log.i(TAG, "$RESPONSE_PREFIX onReceive() called - action: ${intent.action}")
        
        if (intent.action != ACTION_CONTROL_COMMAND) {
            Log.w(TAG, "$RESPONSE_PREFIX Ignoring action: ${intent.action}")
            return
        }
        
        val command = intent.getStringExtra(EXTRA_COMMAND)
        
        if (command.isNullOrEmpty()) {
            logResponse("❌ ERROR: Missing 'command' parameter")
            return
        }
        
        Log.i(TAG, "$RESPONSE_PREFIX Received command: $command")
        
        when (command) {
            "start_service" -> handleStartService(context, intent)
            "stop_service" -> handleStopService(context)
            "restart_service" -> handleRestartService(context, intent)
            "load_plugin" -> handleLoadPlugin(context, intent)
            "unload_plugin" -> handleUnloadPlugin(context, intent)
            "reload_plugin" -> handleReloadPlugin(context, intent)
            "list_plugins" -> handleListPlugins(context)
            "service_status" -> handleServiceStatus(context)
            else -> logResponse("❌ ERROR: Unknown command '$command'")
        }
    }
    
    /**
     * Start the BLE service with specified plugins.
     */
    private fun handleStartService(context: Context, intent: Intent) {
        val blePlugin = intent.getStringExtra(EXTRA_BLE_PLUGIN) ?: "onecontrol_v2"
        val outputPlugin = intent.getStringExtra(EXTRA_OUTPUT_PLUGIN) ?: "mqtt"
        
        logResponse("🚀 Starting service with BLE plugin: $blePlugin, output: $outputPlugin")
        
        val serviceIntent = Intent(context, BaseBleService::class.java).apply {
            action = BaseBleService.ACTION_START_SCAN
            putExtra(BaseBleService.EXTRA_BLE_PLUGIN_ID, blePlugin)
            putExtra(BaseBleService.EXTRA_OUTPUT_PLUGIN_ID, outputPlugin)
        }
        
        try {
            context.startForegroundService(serviceIntent)
            logResponse("✅ SUCCESS: Service start command sent")
        } catch (e: Exception) {
            logResponse("❌ ERROR: Failed to start service - ${e.message}")
        }
    }
    
    /**
     * Stop the BLE service.
     */
    private fun handleStopService(context: Context) {
        logResponse("🛑 Stopping service")
        
        val serviceIntent = Intent(context, BaseBleService::class.java).apply {
            action = BaseBleService.ACTION_STOP_SERVICE
        }
        
        try {
            context.startService(serviceIntent)
            logResponse("✅ SUCCESS: Service stop command sent")
        } catch (e: Exception) {
            logResponse("❌ ERROR: Failed to stop service - ${e.message}")
        }
    }
    
    /**
     * Restart the service with new configuration.
     */
    private fun handleRestartService(context: Context, intent: Intent) {
        logResponse("🔄 Restarting service")
        
        handleStopService(context)
        
        // Schedule restart after brief delay
        CoroutineScope(Dispatchers.Main).launch {
            kotlinx.coroutines.delay(1000)
            handleStartService(context, intent)
        }
    }
    
    /**
     * Load a BLE plugin with configuration.
     */
    private fun handleLoadPlugin(context: Context, intent: Intent) {
        val pluginId = intent.getStringExtra(EXTRA_PLUGIN_ID)
        
        if (pluginId.isNullOrEmpty()) {
            logResponse("❌ ERROR: Missing 'plugin_id' parameter")
            return
        }
        
        logResponse("📥 Loading plugin: $pluginId")
        
        CoroutineScope(Dispatchers.IO).launch {
            try {
                val registry = PluginRegistry.getInstance()
                val config = AppConfig.getBlePluginConfig(context, pluginId)
                val plugin = registry.getBlePlugin(pluginId, context, config)
                
                if (plugin != null) {
                    logResponse("✅ SUCCESS: Plugin loaded - $pluginId v${plugin.getPluginVersion()}")
                } else {
                    logResponse("❌ ERROR: Failed to load plugin '$pluginId'")
                }
            } catch (e: Exception) {
                logResponse("❌ ERROR: Exception loading plugin - ${e.message}")
            }
        }
    }
    
    /**
     * Unload a BLE plugin.
     */
    private fun handleUnloadPlugin(context: Context, intent: Intent) {
        val pluginId = intent.getStringExtra(EXTRA_PLUGIN_ID)
        
        if (pluginId.isNullOrEmpty()) {
            logResponse("❌ ERROR: Missing 'plugin_id' parameter")
            return
        }
        
        logResponse("📤 Unloading plugin: $pluginId")
        
        CoroutineScope(Dispatchers.IO).launch {
            try {
                val registry = PluginRegistry.getInstance()
                registry.unloadBlePlugin(pluginId)
                logResponse("✅ SUCCESS: Plugin unloaded - $pluginId")
            } catch (e: Exception) {
                logResponse("❌ ERROR: Exception unloading plugin - ${e.message}")
            }
        }
    }
    
    /**
     * Reload a plugin (unload + load).
     */
    private fun handleReloadPlugin(context: Context, intent: Intent) {
        val pluginId = intent.getStringExtra(EXTRA_PLUGIN_ID)
        
        if (pluginId.isNullOrEmpty()) {
            logResponse("❌ ERROR: Missing 'plugin_id' parameter")
            return
        }
        
        logResponse("🔄 Reloading plugin: $pluginId")
        
        CoroutineScope(Dispatchers.IO).launch {
            try {
                val registry = PluginRegistry.getInstance()
                
                // Unload
                registry.unloadBlePlugin(pluginId)
                logResponse("   Unloaded $pluginId")
                
                // Wait briefly
                kotlinx.coroutines.delay(500)
                
                // Reload
                val config = AppConfig.getBlePluginConfig(context, pluginId)
                val plugin = registry.getBlePlugin(pluginId, context, config)
                
                if (plugin != null) {
                    logResponse("✅ SUCCESS: Plugin reloaded - $pluginId v${plugin.getPluginVersion()}")
                } else {
                    logResponse("❌ ERROR: Failed to reload plugin '$pluginId'")
                }
            } catch (e: Exception) {
                logResponse("❌ ERROR: Exception reloading plugin - ${e.message}")
            }
        }
    }
    
    /**
     * List all registered and loaded plugins.
     */
    private fun handleListPlugins(context: Context) {
        logResponse("📋 Listing plugins")
        
        CoroutineScope(Dispatchers.IO).launch {
            try {
                val registry = PluginRegistry.getInstance()
                val registered = registry.getRegisteredBlePlugins()
                val loaded = registry.getLoadedBlePlugins()
                
                logResponse("   Registered plugins: ${registered.joinToString(", ")}")
                logResponse("   Loaded plugins: ${loaded.keys.joinToString(", ")}")
                logResponse("   Loaded count: ${loaded.size}")
                logResponse("✅ SUCCESS: Plugin list complete")
            } catch (e: Exception) {
                logResponse("❌ ERROR: Exception listing plugins - ${e.message}")
            }
        }
    }
    
    /**
     * Report service status.
     */
    private fun handleServiceStatus(context: Context) {
        logResponse("📊 Service status")
        
        CoroutineScope(Dispatchers.IO).launch {
            try {
                val isRunning = ServiceStateManager.wasServiceRunning(context)
                val registry = PluginRegistry.getInstance()
                val loaded = registry.getLoadedBlePlugins()
                
                logResponse("   Service running: $isRunning")
                logResponse("   Loaded plugins: ${loaded.keys.joinToString(", ")}")
                logResponse("   Plugin count: ${loaded.size}")
                logResponse("✅ SUCCESS: Status query complete")
            } catch (e: Exception) {
                logResponse("❌ ERROR: Exception getting status - ${e.message}")
            }
        }
    }
    
    /**
     * Log a response that can be easily filtered via logcat.
     */
    private fun logResponse(message: String) {
        Log.i(TAG, "$RESPONSE_PREFIX $message")
    }
}
