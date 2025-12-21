package com.blemqttbridge.receivers

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.util.Log
import com.blemqttbridge.core.AppConfig

/**
 * BroadcastReceiver for configuring MQTT credentials via ADB.
 * 
 * This enables completely remote setup without device interaction.
 * 
 * USAGE:
 * 
 * # Configure MQTT broker
 * adb shell am broadcast --receiver-foreground \
 *   -a com.blemqttbridge.CONFIGURE_MQTT \
 *   --es broker_url "tcp://192.168.1.100:1883" \
 *   --es username "mqtt" \
 *   --es password "mypassword"
 * 
 * # Verify configuration
 * adb logcat | grep "MqttConfig:"
 * 
 * After configuration, the service will use these credentials on next start.
 */
class MqttConfigReceiver : BroadcastReceiver() {
    
    companion object {
        private const val TAG = "MqttConfigReceiver"
        const val ACTION_CONFIGURE_MQTT = "com.blemqttbridge.CONFIGURE_MQTT"
        const val EXTRA_BROKER_URL = "broker_url"
        const val EXTRA_USERNAME = "username"
        const val EXTRA_PASSWORD = "password"
        
        private const val RESPONSE_PREFIX = "MqttConfig:"
    }
    
    override fun onReceive(context: Context, intent: Intent) {
        Log.i(TAG, "$RESPONSE_PREFIX onReceive() called - action: ${intent.action}")
        
        if (intent.action != ACTION_CONFIGURE_MQTT) {
            Log.w(TAG, "$RESPONSE_PREFIX Ignoring action: ${intent.action}")
            return
        }
        
        val brokerUrl = intent.getStringExtra(EXTRA_BROKER_URL)
        val username = intent.getStringExtra(EXTRA_USERNAME) ?: "mqtt"
        val password = intent.getStringExtra(EXTRA_PASSWORD) ?: "mqtt"
        
        if (brokerUrl.isNullOrEmpty()) {
            logResponse("❌ ERROR: Missing 'broker_url' parameter")
            logResponse("   Usage: --es broker_url 'tcp://HOST:1883' --es username 'user' --es password 'pass'")
            return
        }
        
        try {
            // Validate broker URL format
            if (!brokerUrl.startsWith("tcp://") && !brokerUrl.startsWith("ssl://")) {
                logResponse("❌ ERROR: Invalid broker URL format")
                logResponse("   Must start with 'tcp://' or 'ssl://'")
                logResponse("   Example: tcp://192.168.1.100:1883")
                return
            }
            
            // Save configuration
            AppConfig.setMqttBroker(context, brokerUrl, username, password)
            
            logResponse("✅ SUCCESS: MQTT configuration saved")
            logResponse("   Broker: $brokerUrl")
            logResponse("   Username: $username")
            logResponse("   Password: ${if (password.isEmpty()) "(empty)" else "***"}")
            logResponse("")
            logResponse("Configuration will be used on next service start.")
            logResponse("To start service now:")
            logResponse("  adb shell am broadcast --receiver-foreground -a com.blemqttbridge.CONTROL_COMMAND --es command start_service")
            
        } catch (e: Exception) {
            logResponse("❌ ERROR: Failed to save configuration - ${e.message}")
            Log.e(TAG, "Exception saving MQTT config", e)
        }
    }
    
    private fun logResponse(message: String) {
        Log.i(TAG, "$RESPONSE_PREFIX $message")
    }
}
