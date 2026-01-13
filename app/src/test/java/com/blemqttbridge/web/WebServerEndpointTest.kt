package com.blemqttbridge.web

import android.content.Context
import com.blemqttbridge.core.PluginInstance
import com.blemqttbridge.core.ServiceStateManager
import org.json.JSONObject
import org.junit.Before
import org.junit.Test
import org.mockito.Mockito.mock
import org.mockito.Mockito.`when`
import java.io.File
import kotlin.test.assertEquals
import kotlin.test.assertNotNull
import kotlin.test.assertTrue

/**
 * Unit tests for web server endpoints
 * Note: These are unit tests for request/response handling, not full integration tests
 */
class WebServerEndpointTest {
    
    private lateinit var mockContext: Context
    private lateinit var tempDir: File
    
    @Before
    fun setup() {
        mockContext = mock(Context::class.java)
        tempDir = File.createTempFile("test", "dir")
        tempDir.delete()
        tempDir.mkdirs()
        
        `when`(mockContext.filesDir).thenReturn(tempDir)
    }
    
    @Test
    fun testAddInstancePayloadParsing() {
        // Simulate request payload for adding an instance
        val payload = """
        {
            "instanceId": "test-instance",
            "pluginType": "onecontrol",
            "deviceMac": "AA:BB:CC:DD:EE:FF",
            "displayName": "Test Gateway",
            "config": {
                "pin": "1234"
            }
        }
        """.trimIndent()
        
        val json = JSONObject(payload)
        
        assertEquals("test-instance", json.getString("instanceId"))
        assertEquals("onecontrol", json.getString("pluginType"))
        assertEquals("AA:BB:CC:DD:EE:FF", json.getString("deviceMac"))
    }
    
    @Test
    fun testInstanceListingSerialization() {
        // Create test instances
        val instance1 = PluginInstance(
            instanceId = "instance-1",
            pluginType = "onecontrol",
            deviceMac = "AA:BB:CC:DD:EE:FF",
            displayName = "Gateway 1",
            config = mapOf("pin" to "1234")
        )
        
        val instance2 = PluginInstance(
            instanceId = "instance-2",
            pluginType = "mopeka",
            deviceMac = "11:22:33:44:55:66",
            displayName = "Tank Sensor",
            config = mapOf("threshold" to "20")
        )
        
        ServiceStateManager.saveInstance(mockContext, instance1)
        ServiceStateManager.saveInstance(mockContext, instance2)
        
        val instances = ServiceStateManager.getAllInstances(mockContext)
        
        // Verify instances can be serialized to JSON
        val jsonArray = org.json.JSONArray()
        for ((_, instance) in instances) {
            jsonArray.put(JSONObject(PluginInstance.toJson(instance)))
        }
        
        assertEquals(2, jsonArray.length())
    }
    
    @Test
    fun testMqttConfigPayloadStructure() {
        // Test that MQTT config payload has expected structure
        val payload = """
        {
            "broker": "192.168.1.100",
            "port": 1883,
            "username": "user",
            "password": "pass",
            "topicPrefix": "home/ble"
        }
        """.trimIndent()
        
        val json = JSONObject(payload)
        
        assertEquals("192.168.1.100", json.getString("broker"))
        assertEquals(1883, json.getInt("port"))
        assertEquals("user", json.getString("username"))
        assertEquals("pass", json.getString("password"))
        assertEquals("home/ble", json.getString("topicPrefix"))
    }
    
    @Test
    fun testPortConfigurationPayload() {
        val payload = """
        {
            "port": 8888
        }
        """.trimIndent()
        
        val json = JSONObject(payload)
        val port = json.getInt("port")
        
        assertEquals(8888, port)
    }
    
    @Test
    fun testInstanceDeletionPayload() {
        val payload = """
        {
            "instanceId": "instance-to-delete"
        }
        """.trimIndent()
        
        val json = JSONObject(payload)
        val instanceId = json.getString("instanceId")
        
        assertEquals("instance-to-delete", instanceId)
    }
    
    @Test
    fun testWebAuthPayloadValidation() {
        val payload = """
        {
            "authEnabled": true,
            "username": "admin",
            "password": "secret123"
        }
        """.trimIndent()
        
        val json = JSONObject(payload)
        
        assertTrue(json.getBoolean("authEnabled"))
        assertEquals("admin", json.getString("username"))
        assertEquals("secret123", json.getString("password"))
    }
}
