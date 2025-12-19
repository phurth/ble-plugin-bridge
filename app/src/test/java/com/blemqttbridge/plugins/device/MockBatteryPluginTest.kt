package com.blemqttbridge.plugins.device

import android.bluetooth.BluetoothDevice
import android.content.Context
import kotlinx.coroutines.runBlocking
import org.junit.Before
import org.junit.Test
import org.mockito.Mockito.mock
import org.mockito.Mockito.`when`
import kotlin.test.assertEquals
import kotlin.test.assertFalse
import kotlin.test.assertTrue

/**
 * Unit tests for MockBatteryPlugin.
 */
class MockBatteryPluginTest {
    
    private lateinit var plugin: MockBatteryPlugin
    private lateinit var mockContext: Context
    private lateinit var mockDevice: BluetoothDevice
    
    @Before
    fun setup() {
        plugin = MockBatteryPlugin()
        mockContext = mock(Context::class.java)
        mockDevice = mock(BluetoothDevice::class.java)
    }
    
    @Test
    fun testPluginMetadata() {
        assertEquals("mock_battery", plugin.getPluginId())
        assertEquals("Mock Battery Sensor", plugin.getPluginName())
        assertEquals("1.0.0", plugin.getPluginVersion())
    }
    
    @Test
    fun testInitialize() = runBlocking {
        val result = plugin.initialize(mockContext, emptyMap())
        assertTrue(result.isSuccess)
    }
    
    @Test
    fun testCanHandleDevice() {
        `when`(mockDevice.name).thenReturn("MockBattery_001")
        `when`(mockDevice.address).thenReturn("AA:BB:CC:DD:EE:FF")
        
        assertTrue(plugin.canHandleDevice(mockDevice, null))
    }
    
    @Test
    fun testCannotHandleOtherDevice() {
        `when`(mockDevice.name).thenReturn("OtherDevice")
        
        assertFalse(plugin.canHandleDevice(mockDevice, null))
    }
    
    @Test
    fun testGetDeviceId() {
        `when`(mockDevice.address).thenReturn("AA:BB:CC:DD:EE:FF")
        
        val deviceId = plugin.getDeviceId(mockDevice)
        assertEquals("aabbccddeeff", deviceId)
    }
    
    @Test
    fun testOnCharacteristicNotification() = runBlocking {
        // Simulate battery level notification
        val batteryData = byteArrayOf(75) // 75%
        
        val stateUpdates = plugin.onCharacteristicNotification(
            mockDevice,
            "00002a19-0000-1000-8000-00805f9b34fb", // Battery Level UUID
            batteryData
        )
        
        assertEquals(1, stateUpdates.size)
        assertTrue(stateUpdates.containsKey("state"))
        
        val payload = stateUpdates["state"]!!
        assertTrue(payload.contains("\"battery\": 75"))
    }
    
    @Test
    fun testGetDiscoveryPayloads() = runBlocking {
        `when`(mockDevice.address).thenReturn("AA:BB:CC:DD:EE:FF")
        
        val payloads = plugin.getDiscoveryPayloads(mockDevice)
        
        assertEquals(1, payloads.size)
        
        val topic = payloads.keys.first()
        assertTrue(topic.startsWith("homeassistant/sensor/"))
        assertTrue(topic.contains("aabbccddeeff"))
        
        val payload = payloads.values.first()
        assertTrue(payload.contains("\"device_class\": \"battery\""))
        assertTrue(payload.contains("\"unit_of_measurement\": \"%\""))
    }
    
    @Test
    fun testNoPollingRequired() {
        val pollingInterval = plugin.getPollingIntervalMs()
        assertEquals(null, pollingInterval)
    }
}
