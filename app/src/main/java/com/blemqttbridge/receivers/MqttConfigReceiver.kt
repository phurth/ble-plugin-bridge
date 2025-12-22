package com.blemqttbridge.receivers

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.util.Log
import com.blemqttbridge.data.AppSettings
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch

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
 *   --es broker_host "192.168.1.100" \
 *   --ei broker_port 1883 \
 *   --es username "mqtt" \
 *   --es password "mypassword" \
 *   --es topic_prefix "homeassistant"
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
        const val EXTRA_BROKER_HOST = "broker_host"
        const val EXTRA_BROKER_PORT = "broker_port"
        const val EXTRA_USERNAME = "username"
        const val EXTRA_PASSWORD = "password"
        const val EXTRA_TOPIC_PREFIX = "topic_prefix"
        
        private const val RESPONSE_PREFIX = "MqttConfig:"
    }
    
    override fun onReceive(context: Context, intent: Intent) {
        Log.i(TAG, "$RESPONSE_PREFIX onReceive() called - action: ${intent.action}")
        
        if (intent.action != ACTION_CONFIGURE_MQTT) {
            Log.w(TAG, "$RESPONSE_PREFIX Ignoring action: ${intent.action}")
            return
        }
        
        val brokerHost = intent.getStringExtra(EXTRA_BROKER_HOST)
        val brokerPort = intent.getIntExtra(EXTRA_BROKER_PORT, 1883)
        val username = intent.getStringExtra(EXTRA_USERNAME) ?: "mqtt"
        val password = intent.getStringExtra(EXTRA_PASSWORD) ?: "mqtt"
        val topicPrefix = intent.getStringExtra(EXTRA_TOPIC_PREFIX) ?: "homeassistant"
        
        if (brokerHost.isNullOrEmpty()) {
            logResponse("❌ ERROR: Missing 'broker_host' parameter")
            logResponse("   Usage: --es broker_host 'HOST' --ei broker_port 1883 --es username 'user' --es password 'pass'")
            return
        }
        
        try {
            // Save configuration using DataStore (async)
            val settings = AppSettings(context)
            CoroutineScope(Dispatchers.IO).launch {
                settings.setMqttBrokerHost(brokerHost)
                settings.setMqttBrokerPort(brokerPort)
                settings.setMqttUsername(username)
                settings.setMqttPassword(password)
                settings.setMqttTopicPrefix(topicPrefix)
                
                logResponse("✅ SUCCESS: MQTT configuration saved")
                logResponse("   Broker: $brokerHost:$brokerPort")
                logResponse("   Username: $username")
                logResponse("   Password: ${if (password.isEmpty()) "(empty)" else "***"}")
                logResponse("   Topic Prefix: $topicPrefix")
                logResponse("")
                logResponse("Configuration will be used on next service start.")
                logResponse("To start service now:")
                logResponse("  adb shell am broadcast --receiver-foreground -a com.blemqttbridge.CONTROL_COMMAND --es command start_service")
            }
            
        } catch (e: Exception) {
            logResponse("❌ ERROR: Failed to save configuration - ${e.message}")
            Log.e(TAG, "Exception saving MQTT config", e)
        }
    }
    
    private fun logResponse(message: String) {
        Log.i(TAG, "$RESPONSE_PREFIX $message")
    }
}
