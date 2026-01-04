package com.blemqttbridge.receivers

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.util.Log
import com.blemqttbridge.core.ServiceStateManager
import com.blemqttbridge.data.AppSettings
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch

/**
 * BroadcastReceiver for plugin configuration via ADB.
 * 
 * Enable plugin:
 *   adb shell am broadcast -a com.blemqttbridge.ENABLE_PLUGIN --es plugin_id onecontrol_v2
 * 
 * Disable plugin:
 *   adb shell am broadcast -a com.blemqttbridge.DISABLE_PLUGIN --es plugin_id onecontrol_v2
 * 
 * Configure OneControl:
 *   adb shell am broadcast -a com.blemqttbridge.CONFIGURE_ONECONTROL \
 *     --es gateway_mac "24:DC:C3:ED:1E:0A" \
 *     --es gateway_pin "090336"
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
        const val ACTION_CONFIGURE_ONECONTROL = "com.blemqttbridge.CONFIGURE_ONECONTROL"
        const val EXTRA_PLUGIN_ID = "plugin_id"
        const val EXTRA_GATEWAY_MAC = "gateway_mac"
        const val EXTRA_GATEWAY_PIN = "gateway_pin"
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
            
            ACTION_CONFIGURE_ONECONTROL -> {
                val gatewayMac = intent.getStringExtra(EXTRA_GATEWAY_MAC)
                val gatewayPin = intent.getStringExtra(EXTRA_GATEWAY_PIN)
                
                if (gatewayMac.isNullOrEmpty()) {
                    Log.e(TAG, "❌ CONFIGURE_ONECONTROL: Missing gateway_mac")
                    Log.e(TAG, "   Usage: --es gateway_mac \"24:DC:C3:ED:1E:0A\" --es gateway_pin \"090336\"")
                    return
                }
                
                val settings = AppSettings(context)
                CoroutineScope(Dispatchers.IO).launch {
                    settings.setOneControlGatewayMac(gatewayMac)
                    if (!gatewayPin.isNullOrEmpty()) {
                        settings.setOneControlGatewayPin(gatewayPin)
                    }
                    
                    Log.i(TAG, "✅ OneControl configuration saved")
                    Log.i(TAG, "   Gateway MAC: $gatewayMac")
                    Log.i(TAG, "   Gateway PIN: ${if (gatewayPin.isNullOrEmpty()) "(not changed)" else "***"}")
                    Log.i(TAG, "   Restart service to apply: adb shell am broadcast -a com.blemqttbridge.CONTROL_COMMAND --es command restart_service")
                }
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
