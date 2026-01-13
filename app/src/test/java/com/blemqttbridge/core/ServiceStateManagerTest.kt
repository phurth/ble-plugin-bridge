package com.blemqttbridge.core

import android.content.Context
import org.junit.Before
import org.junit.Test
import kotlin.test.assertEquals
import kotlin.test.assertNotNull
import kotlin.test.assertNull
import kotlin.test.assertTrue

/**
 * Unit tests for ServiceStateManager
 */
class ServiceStateManagerTest {
    
    private lateinit var mockContext: Context
    
    @Before
    fun setup() {
        mockContext = TestContextHelper.createMockContext()
    }
    
    @Test
    fun testSaveAndLoadInstance() {
        val instance = PluginInstance(
            instanceId = "test-instance-1",
            pluginType = "onecontrol",
            deviceMac = "AA:BB:CC:DD:EE:FF",
            displayName = "Test Gateway",
            config = mapOf(
                "pin" to "1234",
                "name" to "My Gateway"
            )
        )
        
        ServiceStateManager.saveInstance(mockContext, instance)
        
        val all = ServiceStateManager.getAllInstances(mockContext)
        val loaded = all["test-instance-1"]
        
        assertNotNull(loaded)
        assertEquals("test-instance-1", loaded?.instanceId)
        assertEquals("onecontrol", loaded?.pluginType)
        assertEquals("AA:BB:CC:DD:EE:FF", loaded?.deviceMac)
        assertEquals("Test Gateway", loaded?.displayName)
        assertEquals("1234", loaded?.config?.get("pin"))
    }
    
    @Test
    fun testSaveMultipleInstances() {
        val instance1 = PluginInstance(
            instanceId = "instance-1",
            pluginType = "onecontrol",
            deviceMac = "AA:BB:CC:DD:EE:FF",
            displayName = "Gateway 1",
            config = emptyMap()
        )
        
        val instance2 = PluginInstance(
            instanceId = "instance-2",
            pluginType = "easytouch",
            deviceMac = "11:22:33:44:55:66",
            displayName = "Thermostat",
            config = emptyMap()
        )
        
        ServiceStateManager.saveInstance(mockContext, instance1)
        ServiceStateManager.saveInstance(mockContext, instance2)
        
        val all = ServiceStateManager.getAllInstances(mockContext)
        assertEquals(2, all.size)
        assertNotNull(all["instance-1"])
        assertNotNull(all["instance-2"])
    }
    
    @Test
    fun testUpdateInstance() {
        val original = PluginInstance(
            instanceId = "update-test",
            pluginType = "onecontrol",
            deviceMac = "AA:BB:CC:DD:EE:FF",
            displayName = "Original Name",
            config = mapOf("pin" to "1234")
        )
        
        ServiceStateManager.saveInstance(mockContext, original)
        
        val updated = PluginInstance(
            instanceId = "update-test",
            pluginType = "onecontrol",
            deviceMac = "AA:BB:CC:DD:EE:FF",
            displayName = "Updated Name",
            config = mapOf("pin" to "5678")
        )
        
        ServiceStateManager.saveInstance(mockContext, updated)
        
        val all = ServiceStateManager.getAllInstances(mockContext)
        val loaded = all["update-test"]
        assertEquals("Updated Name", loaded?.displayName)
        assertEquals("5678", loaded?.config?.get("pin"))
    }
    
    @Test
    fun testRemoveInstance() {
        val instance = PluginInstance(
            instanceId = "delete-test",
            pluginType = "onecontrol",
            deviceMac = "AA:BB:CC:DD:EE:FF",
            displayName = "To Delete",
            config = emptyMap()
        )
        
        ServiceStateManager.saveInstance(mockContext, instance)
        assertNotNull(ServiceStateManager.getAllInstances(mockContext)["delete-test"])
        
        ServiceStateManager.removeInstance(mockContext, "delete-test")
        
        assertNull(ServiceStateManager.getAllInstances(mockContext)["delete-test"])
    }
    
    @Test
    fun testGetAllInstancesEmpty() {
        val all = ServiceStateManager.getAllInstances(mockContext)
        assertTrue(all.isEmpty())
    }
    
    @Test
    fun testInstanceSerialization() {
        val instance = PluginInstance(
            instanceId = "serial-test",
            pluginType = "mopeka",
            deviceMac = "AA:BB:CC:DD:EE:FF",
            displayName = "Tank Sensor",
            config = mapOf(
                "threshold_low" to "20",
                "threshold_critical" to "5",
                "tank_type" to "propane"
            )
        )
        
        // Serialize to JSON and back using PluginInstance companion object methods
        val json = PluginInstance.toJson(instance)
        val deserialized = PluginInstance.fromJson(json)
        
        assertNotNull(deserialized)
        assertEquals(instance.instanceId, deserialized?.instanceId)
        assertEquals(instance.pluginType, deserialized?.pluginType)
        assertEquals(instance.deviceMac, deserialized?.deviceMac)
        assertEquals(instance.displayName, deserialized?.displayName)
        assertEquals(instance.config, deserialized?.config)
    }
}
