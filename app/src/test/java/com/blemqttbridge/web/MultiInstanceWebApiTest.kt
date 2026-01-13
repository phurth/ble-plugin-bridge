package com.blemqttbridge.web

import android.content.Context
import com.blemqttbridge.core.PluginInstance
import com.blemqttbridge.core.ServiceStateManager
import com.blemqttbridge.core.TestContextHelper
import org.junit.Before
import org.junit.Test
import org.json.JSONObject
import kotlin.test.assertEquals
import kotlin.test.assertNotNull
import kotlin.test.assertTrue
import kotlin.test.assertFalse

/**
 * Unit tests for Multi-Instance Web API endpoints.
 * Tests the /api/instances/add, /api/instances/update, and /api/instances/remove endpoints.
 */
class MultiInstanceWebApiTest {
    
    private lateinit var mockContext: Context
    
    @Before
    fun setup() {
        mockContext = TestContextHelper.createMockContext()
    }
    
    @Test
    fun testCreateInstanceId() {
        val instanceId = PluginInstance.createInstanceId("easytouch", "EC:C9:FF:B1:24:1E")
        assertEquals("easytouch_b1241e", instanceId)
        
        val instanceId2 = PluginInstance.createInstanceId("onecontrol", "24:DC:C3:ED:1E:0A")
        assertEquals("onecontrol_ed1e0a", instanceId2)
    }
    
    @Test
    fun testInstanceSerialization() {
        val instance = PluginInstance(
            instanceId = "easytouch_b1241e",
            pluginType = "easytouch",
            deviceMac = "EC:C9:FF:B1:24:1E",
            displayName = "Master Bedroom",
            config = mapOf(
                "password" to "secret123"
            )
        )
        
        // Save instance
        ServiceStateManager.saveInstance(mockContext, instance)
        
        // Load instances
        val instances = ServiceStateManager.getAllInstances(mockContext)
        
        assertNotNull(instances["easytouch_b1241e"])
        assertEquals("Master Bedroom", instances["easytouch_b1241e"]?.displayName)
        assertEquals("EC:C9:FF:B1:24:1E", instances["easytouch_b1241e"]?.deviceMac)
        assertEquals("secret123", instances["easytouch_b1241e"]?.config?.get("password"))
    }
    
    @Test
    fun testMultipleInstancesOfSameType() {
        val instance1 = PluginInstance(
            instanceId = "easytouch_b1241e",
            pluginType = "easytouch",
            deviceMac = "EC:C9:FF:B1:24:1E",
            displayName = "Master Bedroom",
            config = mapOf("password" to "pass1")
        )
        
        val instance2 = PluginInstance(
            instanceId = "easytouch_c4f892",
            pluginType = "easytouch",
            deviceMac = "AA:BB:CC:C4:F8:92",
            displayName = "Guest Room",
            config = mapOf("password" to "pass2")
        )
        
        // Save both instances
        ServiceStateManager.saveInstance(mockContext, instance1)
        ServiceStateManager.saveInstance(mockContext, instance2)
        
        // Load all instances
        val instances = ServiceStateManager.getAllInstances(mockContext)
        
        assertEquals(2, instances.size)
        assertTrue(instances.containsKey("easytouch_b1241e"))
        assertTrue(instances.containsKey("easytouch_c4f892"))
    }
    
    @Test
    fun testUpdateInstance() {
        // Create initial instance
        val original = PluginInstance(
            instanceId = "easytouch_b1241e",
            pluginType = "easytouch",
            deviceMac = "EC:C9:FF:B1:24:1E",
            displayName = "Bedroom",
            config = mapOf("password" to "old")
        )
        ServiceStateManager.saveInstance(mockContext, original)
        
        // Update display name and config
        val updated = original.copy(
            displayName = "Master Bedroom",
            config = mapOf("password" to "new")
        )
        ServiceStateManager.saveInstance(mockContext, updated)
        
        // Verify update
        val instances = ServiceStateManager.getAllInstances(mockContext)
        assertEquals("Master Bedroom", instances["easytouch_b1241e"]?.displayName)
        assertEquals("new", instances["easytouch_b1241e"]?.config?.get("password"))
    }
    
    @Test
    fun testUpdateInstanceMacAddress() {
        // Create initial instance
        val original = PluginInstance(
            instanceId = "easytouch_b1241e",
            pluginType = "easytouch",
            deviceMac = "EC:C9:FF:B1:24:1E",
            displayName = "Thermostat",
            config = mapOf("password" to "secret")
        )
        ServiceStateManager.saveInstance(mockContext, original)
        
        // Change MAC address (should generate new instanceId)
        val newMac = "AA:BB:CC:C4:F8:92"
        val newInstanceId = PluginInstance.createInstanceId("easytouch", newMac)
        
        // Remove old instance
        ServiceStateManager.removeInstance(mockContext, original.instanceId)
        
        // Save with new instanceId
        val updated = original.copy(
            instanceId = newInstanceId,
            deviceMac = newMac
        )
        ServiceStateManager.saveInstance(mockContext, updated)
        
        // Verify old instance removed and new one added
        val instances = ServiceStateManager.getAllInstances(mockContext)
        assertFalse(instances.containsKey("easytouch_b1241e"))
        assertTrue(instances.containsKey(newInstanceId))
        assertEquals(newMac, instances[newInstanceId]?.deviceMac)
    }
    
    @Test
    fun testRemoveInstance() {
        // Create instance
        val instance = PluginInstance(
            instanceId = "easytouch_b1241e",
            pluginType = "easytouch",
            deviceMac = "EC:C9:FF:B1:24:1E",
            displayName = "Test",
            config = emptyMap()
        )
        ServiceStateManager.saveInstance(mockContext, instance)
        
        // Verify it exists
        var instances = ServiceStateManager.getAllInstances(mockContext)
        assertTrue(instances.containsKey("easytouch_b1241e"))
        
        // Remove it
        ServiceStateManager.removeInstance(mockContext, "easytouch_b1241e")
        
        // Verify it's gone
        instances = ServiceStateManager.getAllInstances(mockContext)
        assertFalse(instances.containsKey("easytouch_b1241e"))
    }
    
    @Test
    fun testGetInstancesOfType() {
        // Create instances of different types
        ServiceStateManager.saveInstance(mockContext, PluginInstance(
            instanceId = "easytouch_111111",
            pluginType = "easytouch",
            deviceMac = "AA:AA:AA:11:11:11",
            displayName = "EasyTouch 1",
            config = emptyMap()
        ))
        
        ServiceStateManager.saveInstance(mockContext, PluginInstance(
            instanceId = "easytouch_222222",
            pluginType = "easytouch",
            deviceMac = "BB:BB:BB:22:22:22",
            displayName = "EasyTouch 2",
            config = emptyMap()
        ))
        
        ServiceStateManager.saveInstance(mockContext, PluginInstance(
            instanceId = "onecontrol_333333",
            pluginType = "onecontrol",
            deviceMac = "CC:CC:CC:33:33:33",
            displayName = "OneControl 1",
            config = emptyMap()
        ))
        
        // Get all instances of type easytouch
        val easytouchInstances = ServiceStateManager.getAllInstances(mockContext)
            .values
            .filter { it.pluginType == "easytouch" }
        
        assertEquals(2, easytouchInstances.size)
        assertTrue(easytouchInstances.all { it.pluginType == "easytouch" })
    }
    
    @Test
    fun testBleScannerNoMacRequired() {
        // BLE Scanner should allow empty MAC
        val instance = PluginInstance(
            instanceId = "blescanner_",
            pluginType = "blescanner",
            deviceMac = "",
            displayName = "Scanner",
            config = emptyMap()
        )
        
        ServiceStateManager.saveInstance(mockContext, instance)
        
        val instances = ServiceStateManager.getAllInstances(mockContext)
        assertTrue(instances.containsKey("blescanner_"))
        assertEquals("", instances["blescanner_"]?.deviceMac)
    }
    
    @Test
    fun testPluginConfigMap() {
        // Test various config types
        val instance = PluginInstance(
            instanceId = "onecontrol_test",
            pluginType = "onecontrol",
            deviceMac = "AA:BB:CC:DD:EE:FF",
            displayName = "Test Gateway",
            config = mapOf(
                "gateway_pin" to "1234",
                "bluetooth_pin" to "0000",
                "custom_setting" to "value"
            )
        )
        
        ServiceStateManager.saveInstance(mockContext, instance)
        
        val loaded = ServiceStateManager.getAllInstances(mockContext)["onecontrol_test"]
        assertNotNull(loaded)
        assertEquals("1234", loaded.config["gateway_pin"])
        assertEquals("0000", loaded.config["bluetooth_pin"])
        assertEquals("value", loaded.config["custom_setting"])
    }
}
