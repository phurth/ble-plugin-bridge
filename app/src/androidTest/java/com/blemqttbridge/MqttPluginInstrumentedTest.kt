package com.blemqttbridge

import android.content.Context
import androidx.test.ext.junit.runners.AndroidJUnit4
import androidx.test.platform.app.InstrumentationRegistry
import com.blemqttbridge.plugins.output.MqttOutputPlugin
import kotlinx.coroutines.delay
import kotlinx.coroutines.runBlocking
import org.junit.After
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import kotlin.test.assertEquals
import kotlin.test.assertTrue

/**
 * Instrumented test for MqttOutputPlugin with real MQTT broker.
 * 
 * Configure broker settings below, then run:
 *   ./gradlew connectedDebugAndroidTest
 */
@RunWith(AndroidJUnit4::class)
class MqttPluginInstrumentedTest {
    
    companion object {
        // ⚙️ CONFIGURE YOUR MQTT BROKER HERE ⚙️
        private const val BROKER_URL = "tcp://YOUR_BROKER_IP:1883"  // e.g., "tcp://192.168.1.100:1883"
        private const val USERNAME = "YOUR_USERNAME"                 // or "" if no auth
        private const val PASSWORD = "YOUR_PASSWORD"                 // or "" if no auth
        private const val TOPIC_PREFIX = "test/ble_bridge"
    }
    
    private lateinit var context: Context
    private lateinit var mqttPlugin: MqttOutputPlugin
    
    @Before
    fun setup() {
        context = InstrumentationRegistry.getInstrumentation().targetContext
        mqttPlugin = MqttOutputPlugin(context)
    }
    
    @After
    fun teardown() {
        mqttPlugin.disconnect()
    }
    
    @Test
    fun testConnectToRealBroker() = runBlocking {
        val config = mapOf(
            "broker_url" to BROKER_URL,
            "username" to USERNAME,
            "password" to PASSWORD,
            "topic_prefix" to TOPIC_PREFIX
        )
        
        val result = mqttPlugin.initialize(config)
        assertTrue(result.isSuccess, "Connection should succeed")
        assertTrue(mqttPlugin.isConnected(), "Should be connected")
        
        // Wait a moment for connection to stabilize
        delay(500)
        
        val status = mqttPlugin.getConnectionStatus()
        assertTrue(status.contains("Connected"), "Status should show connected: $status")
    }
    
    @Test
    fun testPublishState() = runBlocking {
        // Connect first
        val config = mapOf(
            "broker_url" to BROKER_URL,
            "username" to USERNAME,
            "password" to PASSWORD,
            "topic_prefix" to TOPIC_PREFIX
        )
        mqttPlugin.initialize(config).getOrThrow()
        delay(500)
        
        // Publish test state
        val testPayload = """{"temperature":22.5,"humidity":65}"""
        mqttPlugin.publishState("sensor/test_sensor/state", testPayload, retained = true)
        
        // No exception = success
        assertTrue(mqttPlugin.isConnected(), "Should remain connected after publish")
    }
    
    @Test
    fun testPublishAndSubscribe() = runBlocking {
        var receivedTopic: String? = null
        var receivedPayload: String? = null
        
        // Connect
        val config = mapOf(
            "broker_url" to BROKER_URL,
            "username" to USERNAME,
            "password" to PASSWORD,
            "topic_prefix" to TOPIC_PREFIX
        )
        mqttPlugin.initialize(config).getOrThrow()
        delay(500)
        
        // Subscribe
        mqttPlugin.subscribeToCommands("command/test/#") { topic, payload ->
            receivedTopic = topic
            receivedPayload = payload
        }
        delay(500)  // Allow subscription to complete
        
        // Publish to subscribed topic
        val testPayload = "TEST_COMMAND_123"
        mqttPlugin.publishState("command/test/device", testPayload, retained = false)
        
        // Wait for message to arrive
        delay(1000)
        
        // Verify received
        assertEquals("$TOPIC_PREFIX/command/test/device", receivedTopic, "Topic should match")
        assertEquals(testPayload, receivedPayload, "Payload should match")
    }
    
    @Test
    fun testPublishAvailability() = runBlocking {
        val config = mapOf(
            "broker_url" to BROKER_URL,
            "username" to USERNAME,
            "password" to PASSWORD,
            "topic_prefix" to TOPIC_PREFIX
        )
        mqttPlugin.initialize(config).getOrThrow()
        delay(500)
        
        // Publish online
        mqttPlugin.publishAvailability(online = true)
        delay(200)
        
        // Publish offline
        mqttPlugin.publishAvailability(online = false)
        delay(200)
        
        // No exception = success
        assertTrue(mqttPlugin.isConnected(), "Should remain connected")
    }
    
    @Test
    fun testDisconnectAndReconnect() = runBlocking {
        val config = mapOf(
            "broker_url" to BROKER_URL,
            "username" to USERNAME,
            "password" to PASSWORD,
            "topic_prefix" to TOPIC_PREFIX
        )
        
        // Connect
        mqttPlugin.initialize(config).getOrThrow()
        delay(500)
        assertTrue(mqttPlugin.isConnected(), "Should be connected initially")
        
        // Disconnect
        mqttPlugin.disconnect()
        delay(200)
        assertTrue(!mqttPlugin.isConnected(), "Should be disconnected")
        
        // Reconnect
        mqttPlugin.initialize(config).getOrThrow()
        delay(500)
        assertTrue(mqttPlugin.isConnected(), "Should be connected again")
    }
}
