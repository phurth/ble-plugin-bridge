package com.blemqttbridge.receivers

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.util.Log
import com.blemqttbridge.core.ServiceStateManager

/**
 * BroadcastReceiver for plugin configuration via ADB.
 * 
 * Enable plugin:
 *   adb shell am broadcast -a com.blemqttbridge.ENABLE_PLUGIN --es plugin_id onecontrol
 * 
 * Disable plugin:
 *   adb shell am broadcast -a com.blemqttbridge.DISABLE_PLUGIN --es plugin_id onecontrol
 * 
 * List enabled:
 *   adb shell am broadcast -a com.blemqttbridge.LIST_PLUGINS
 */
class PluginConfigReceiver : BroadcastReceiver() {
    
    companion object {
        private const val TAG = "PluginConfigReceiver"
        const val ACTION_ENABLE_PLUGIN = "com.blemqttbridge.ENABLE_PLUGIN"
        const val ACTION_DISABLE_PLUGIN = "com.blemqttbridge.DISABLE_PLUGIN"
        const val ACTION_LIST_PLUGINS = "com.blemqttbridge.LIST_PLUGINS"
        const val EXTRA_PLUGIN_ID = "plugin_id"
    }
    
    override fun onReceive(context: Context, intent: Intent) {
        when (intent.action) {
            ACTION_ENABLE_PLUGIN -> {
                val pluginId = intent.getStringExtra(EXTRA_PLUGIN_ID)
                if (pluginId.isNullOrEmpty()) {
                    Log.e(TAG, "❌ ENABLE_PLUGIN: Missing plugin_id")
                    return
                }
                
                ServiceStateManager.enableBlePlugin(context, pluginId)
                val enabled = ServiceStateManager.getEnabledBlePlugins(context)
                Log.i(TAG, "✅ Enabled plugin: $pluginId")
                Log.i(TAG, "   All enabled plugins: " + enabled.joinToString(", "))
            }
            
            ACTION_DISABLE_PLUGIN -> {
                val pluginId = intent.getStringExtra(EXTRA_PLUGIN_ID)
                if (pluginId.isNullOrEmpty()) {
                    Log.e(TAG, "❌ DISABLE_PLUGIN: Missing plugin_id")
                    return
                }
                
                ServiceStateManager.disableBlePlugin(context, pluginId)
                val enabled = ServiceStateManager.getEnabledBlePlugins(context)
                Log.i(TAG, "❌ Disabled plugin: $pluginId")
                Log.i(TAG, "   All enabled plugins: " + enabled.joinToString(", "))
            }
            
            ACTION_LIST_PLUGINS -> {
                val enabled = ServiceStateManager.getEnabledBlePlugins(context)
                val outputPlugin = ServiceStateManager.getEnabledOutputPlugin(context)
                
                Log.i(TAG, "📋 Plugin Configuration:")
                val enabledList = if (enabled.isEmpty()) "(none)" else enabled.joinToString(", ")
                Log.i(TAG, "   Enabled BLE plugins: $enabledList")
                Log.i(TAG, "   Enabled output plugin: " + (outputPlugin ?: "(none)"))
                Log.i(TAG, "   Service state: " + ServiceStateManager.getDebugInfo(context))
            }
        }
    }
}
