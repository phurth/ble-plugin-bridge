package com.blemqttbridge.core

import android.bluetooth.BluetoothDevice
import android.content.Context
import com.blemqttbridge.core.interfaces.BlePluginInterface
import com.blemqttbridge.core.interfaces.OutputPluginInterface
import kotlinx.coroutines.runBlocking
import org.junit.Before
import org.junit.Test
import org.mockito.Mockito.mock
import org.mockito.Mockito.`when`
import kotlin.test.assertEquals
import kotlin.test.assertNotNull
import kotlin.test.assertNull
import kotlin.test.assertTrue

/**
 * Unit tests for PluginRegistry.
 */
class PluginRegistryTest {
    
    private lateinit var registry: PluginRegistry
    private lateinit var mockContext: Context
    
    @Before
    fun setup() {
        registry = PluginRegistry.getInstance()
        mockContext = mock(Context::class.java)
    }
    
    @Test
    fun testRegisterBlePlugin() {
        var factoryCalled = false
        
        registry.registerBlePlugin("test_plugin") {
            factoryCalled = true
            createMockBlePlugin("test_plugin")
        }
        
        val registered = registry.getRegisteredBlePlugins()
        assertTrue(registered.contains("test_plugin"))
        assertTrue(!factoryCalled, "Factory should not be called during registration")
    }
    
    @Test
    fun testLoadBlePlugin() = runBlocking {
        val plugin = createMockBlePlugin("test_load")
        
        registry.registerBlePlugin("test_load") { plugin }
        
        val loadedPlugin = registry.getBlePlugin("test_load", mockContext, emptyMap())
        assertNotNull(loadedPlugin)
        assertEquals("test_load", loadedPlugin?.getPluginId())
    }
    
    @Test
    fun testLazyLoading() = runBlocking {
        var plugin1Created = false
        var plugin2Created = false
        
        registry.registerBlePlugin("plugin1") {
            plugin1Created = true
            createMockBlePlugin("plugin1")
        }
        
        registry.registerBlePlugin("plugin2") {
            plugin2Created = true
            createMockBlePlugin("plugin2")
        }
        
        assertTrue(!plugin1Created && !plugin2Created, "Plugins should not be created on registration")
        
        // Load plugin1
        val p1 = registry.getBlePlugin("plugin1", mockContext, emptyMap())
        assertNotNull(p1)
        assertTrue(plugin1Created, "Plugin1 should be created when loaded")
        assertTrue(!plugin2Created, "Plugin2 should not be created when plugin1 is loaded")
    }
    
    @Test
    fun testUnloadOnSwitch() = runBlocking {
        val plugin1 = createMockBlePlugin("switch_plugin1")
        val plugin2 = createMockBlePlugin("switch_plugin2")
        
        registry.registerBlePlugin("switch_plugin1") { plugin1 }
        registry.registerBlePlugin("switch_plugin2") { plugin2 }
        
        // Load plugin1
        val p1 = registry.getBlePlugin("switch_plugin1", mockContext, emptyMap())
        assertEquals("switch_plugin1", p1?.getPluginId())
        
        // Load plugin2 - should unload plugin1
        val p2 = registry.getBlePlugin("switch_plugin2", mockContext, emptyMap())
        assertEquals("switch_plugin2", p2?.getPluginId())
        
        // Current plugin should be plugin2
        val current = registry.getCurrentBlePlugin()
        assertEquals("switch_plugin2", current?.getPluginId())
    }
    
    @Test
    fun testFindPluginForDevice() {
        val mockDevice = mock(BluetoothDevice::class.java)
        `when`(mockDevice.address).thenReturn("AA:BB:CC:DD:EE:FF")
        `when`(mockDevice.name).thenReturn("TestDevice")
        
        registry.registerBlePlugin("finder_test") {
            object : BlePluginInterface {
                override fun getPluginId() = "finder_test"
                override fun getPluginName() = "Finder Test"
                override fun getPluginVersion() = "1.0"
                override suspend fun initialize(context: Context, config: Map<String, String>) = Result.success(Unit)
                
                override fun canHandleDevice(device: BluetoothDevice, scanRecord: ByteArray?): Boolean {
                    return device.name == "TestDevice"
                }
                
                override fun getDeviceId(device: BluetoothDevice) = device.address
                override suspend fun onDeviceConnected(device: BluetoothDevice) = Result.success(Unit)
                override suspend fun onDeviceDisconnected(device: BluetoothDevice) {}
                override suspend fun onCharacteristicNotification(device: BluetoothDevice, characteristicUuid: String, value: ByteArray) = emptyMap<String, String>()
                override suspend fun handleCommand(device: BluetoothDevice, commandTopic: String, payload: String) = Result.success(Unit)
                override suspend fun getDiscoveryPayloads(device: BluetoothDevice) = emptyMap<String, String>()
                override suspend fun cleanup() {}
            }
        }
        
        val foundPluginId = registry.findPluginForDevice(mockDevice, null)
        assertEquals("finder_test", foundPluginId)
    }
    
    @Test
    fun testRegisterOutputPlugin() = runBlocking {
        val mockOutput = createMockOutputPlugin()
        
        registry.registerOutputPlugin("test_output") { mockOutput }
        
        val loaded = registry.getOutputPlugin("test_output", mockContext, emptyMap())
        assertNotNull(loaded)
        assertEquals("test_output", loaded?.getOutputId())
    }
    
    // Helper: Create a mock BLE plugin
    private fun createMockBlePlugin(id: String): BlePluginInterface {
        return object : BlePluginInterface {
            override fun getPluginId() = id
            override fun getPluginName() = "Mock Plugin $id"
            override fun getPluginVersion() = "1.0.0"
            override suspend fun initialize(context: Context, config: Map<String, String>) = Result.success(Unit)
            override fun canHandleDevice(device: BluetoothDevice, scanRecord: ByteArray?) = false
            override fun getDeviceId(device: BluetoothDevice) = device.address
            override suspend fun onDeviceConnected(device: BluetoothDevice) = Result.success(Unit)
            override suspend fun onDeviceDisconnected(device: BluetoothDevice) {}
            override suspend fun onCharacteristicNotification(device: BluetoothDevice, characteristicUuid: String, value: ByteArray) = emptyMap<String, String>()
            override suspend fun handleCommand(device: BluetoothDevice, commandTopic: String, payload: String) = Result.success(Unit)
            override suspend fun getDiscoveryPayloads(device: BluetoothDevice) = emptyMap<String, String>()
            override suspend fun cleanup() {}
        }
    }
    
    // Helper: Create a mock output plugin
    private fun createMockOutputPlugin(): OutputPluginInterface {
        return object : OutputPluginInterface {
            override fun getOutputId() = "test_output"
            override fun getOutputName() = "Test Output"
            override suspend fun initialize(config: Map<String, String>) = Result.success(Unit)
            override suspend fun publishState(topic: String, payload: String, retained: Boolean) {}
            override suspend fun publishDiscovery(topic: String, payload: String) {}
            override suspend fun subscribeToCommands(topicPattern: String, callback: (String, String) -> Unit) {}
            override suspend fun publishAvailability(online: Boolean) {}
            override fun disconnect() {}
            override fun isConnected() = false
            override fun getConnectionStatus() = "Disconnected"
        }
    }
}
