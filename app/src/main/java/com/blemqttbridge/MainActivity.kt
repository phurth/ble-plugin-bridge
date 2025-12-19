package com.blemqttbridge

import android.app.Activity
import android.os.Bundle
import android.util.Log
import android.widget.TextView
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import com.blemqttbridge.plugins.output.MqttOutputPlugin

/**
 * Simple test activity for validating MQTT plugin.
 * Configure broker settings below, then deploy to device/emulator.
 */
class MainActivity : Activity() {
    
    companion object {
        private const val TAG = "MqttTest"
        
        // ⚙️ CONFIGURE YOUR MQTT BROKER HERE ⚙️
        private const val BROKER_URL = "tcp://YOUR_BROKER_IP:1883"  // e.g., "tcp://192.168.1.100:1883"
        private const val USERNAME = "YOUR_USERNAME"                 // or "" if no auth
        private const val PASSWORD = "YOUR_PASSWORD"                 // or "" if no auth
        private const val TOPIC_PREFIX = "test/ble_bridge"
    }
    
    private lateinit var statusText: TextView
    private lateinit var mqttPlugin: MqttOutputPlugin
    private val scope = CoroutineScope(Dispatchers.Main)
    
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        
        // Create simple UI
        statusText = TextView(this).apply {
            text = "Initializing MQTT test...\n"
            setPadding(32, 32, 32, 32)
            textSize = 14f
        }
        setContentView(statusText)
        
        mqttPlugin = MqttOutputPlugin(applicationContext)
        
        // Start test
        scope.launch {
            runMqttTest()
        }
    }
    
    private suspend fun runMqttTest() = withContext(Dispatchers.IO) {
        try {
            appendStatus("📡 Testing MQTT Plugin")
            appendStatus("Broker: $BROKER_URL")
            appendStatus("")
            
            // Initialize plugin
            val config = mapOf(
                "broker_url" to BROKER_URL,
                "username" to USERNAME,
                "password" to PASSWORD,
                "topic_prefix" to TOPIC_PREFIX
            )
            
            appendStatus("Connecting to broker...")
            mqttPlugin.initialize(config).getOrThrow()
            appendStatus("✅ Connected!")
            appendStatus("")
            
            // Publish availability
            appendStatus("Publishing availability...")
            mqttPlugin.publishAvailability(online = true)
            appendStatus("✅ Availability published")
            appendStatus("")
            
            // Subscribe to test commands
            appendStatus("Subscribing to commands...")
            mqttPlugin.subscribeToCommands("command/#") { topic, payload ->
                scope.launch {
                    appendStatus("📨 Received: $topic = $payload")
                }
            }
            appendStatus("✅ Subscribed to $TOPIC_PREFIX/command/#")
            appendStatus("")
            
            // Publish test state
            appendStatus("Publishing test state...")
            val testPayload = """{"state":"ON","brightness":100}"""
            mqttPlugin.publishState("device/test_device/state", testPayload, retained = true)
            appendStatus("✅ State published")
            appendStatus("")
            
            // Publish Home Assistant discovery
            appendStatus("Publishing HA discovery...")
            val discoveryPayload = """{
                "name": "BLE Bridge Test Device",
                "state_topic": "$TOPIC_PREFIX/device/test_device/state",
                "command_topic": "$TOPIC_PREFIX/command/test_device",
                "unique_id": "ble_bridge_test_001"
            }"""
            mqttPlugin.publishDiscovery(
                "homeassistant/switch/ble_bridge/test_device/config",
                discoveryPayload
            )
            appendStatus("✅ Discovery published")
            appendStatus("")
            
            appendStatus("🎉 ALL TESTS PASSED!")
            appendStatus("")
            appendStatus("Connection Status: ${mqttPlugin.getConnectionStatus()}")
            appendStatus("")
            appendStatus("Manual test:")
            appendStatus("Use an MQTT client to publish to:")
            appendStatus("  $TOPIC_PREFIX/command/test_device")
            appendStatus("")
            appendStatus("You should see the message appear above.")
            
        } catch (e: Exception) {
            appendStatus("❌ ERROR: ${e.message}")
            Log.e(TAG, "MQTT test failed", e)
        }
    }
    
    private suspend fun appendStatus(message: String) = withContext(Dispatchers.Main) {
        Log.i(TAG, message)
        statusText.append("$message\n")
    }
    
    override fun onDestroy() {
        super.onDestroy()
        if (::mqttPlugin.isInitialized) {
            mqttPlugin.disconnect()
        }
    }
}
