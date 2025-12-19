package com.blemqttbridge.plugins.device

import android.bluetooth.BluetoothDevice
import android.content.Context
import android.util.Log
import com.blemqttbridge.core.interfaces.BlePluginInterface

/**
 * Mock BLE plugin for testing infrastructure.
 * Simulates a simple battery sensor device.
 */
class MockBatteryPlugin : BlePluginInterface {
    
    companion object {
        private const val TAG = "MockBatteryPlugin"
        private const val PLUGIN_ID = "mock_battery"
        private const val VERSION = "1.0.0"
        
        // Mock device name prefix for testing
        private const val DEVICE_NAME_PREFIX = "MockBattery"
    }
    
    override fun getPluginId(): String = PLUGIN_ID
    
    override fun getPluginName(): String = "Mock Battery Sensor"
    
    override fun getPluginVersion(): String = VERSION
    
    override suspend fun initialize(context: Context, config: Map<String, String>): Result<Unit> {
        Log.i(TAG, "Initializing Mock Battery Plugin")
        return Result.success(Unit)
    }
    
    override fun canHandleDevice(device: BluetoothDevice, scanRecord: ByteArray?): Boolean {
        // Match devices with "MockBattery" in the name
        val deviceName = try {
            device.name
        } catch (e: SecurityException) {
            null
        }
        
        val matches = deviceName?.startsWith(DEVICE_NAME_PREFIX) == true
        if (matches) {
            Log.d(TAG, "Device matches: ${device.address} ($deviceName)")
        }
        return matches
    }
    
    override fun getDeviceId(device: BluetoothDevice): String {
        // Use MAC address as device ID (sanitized)
        return device.address.replace(":", "").lowercase()
    }
    
    override suspend fun onDeviceConnected(device: BluetoothDevice): Result<Unit> {
        Log.i(TAG, "Device connected: ${device.address}")
        // In a real plugin, we'd enable notifications on characteristics here
        return Result.success(Unit)
    }
    
    override suspend fun onDeviceDisconnected(device: BluetoothDevice) {
        Log.i(TAG, "Device disconnected: ${device.address}")
    }
    
    override suspend fun onCharacteristicNotification(
        device: BluetoothDevice,
        characteristicUuid: String,
        value: ByteArray
    ): Map<String, String> {
        // Mock: Parse "battery level" from first byte
        val batteryLevel = if (value.isNotEmpty()) value[0].toInt() and 0xFF else 0
        
        Log.d(TAG, "Battery notification: $batteryLevel%")
        
        // Return state update in Home Assistant format
        val stateJson = """{"battery": $batteryLevel, "unit": "%"}"""
        
        return mapOf("state" to stateJson)
    }
    
    override suspend fun handleCommand(
        device: BluetoothDevice,
        commandTopic: String,
        payload: String
    ): Result<Unit> {
        Log.d(TAG, "Command received: $commandTopic = $payload")
        // Mock device has no commands
        return Result.success(Unit)
    }
    
    override suspend fun getDiscoveryPayloads(device: BluetoothDevice): Map<String, String> {
        val deviceId = getDeviceId(device)
        
        // Home Assistant MQTT sensor discovery
        val discoveryPayload = """{
            "name": "Mock Battery Sensor",
            "unique_id": "mock_battery_${deviceId}",
            "state_topic": "test/ble_bridge/device/${deviceId}/state",
            "value_template": "{{ value_json.battery }}",
            "unit_of_measurement": "%",
            "device_class": "battery",
            "availability_topic": "test/ble_bridge/availability",
            "device": {
                "identifiers": ["${deviceId}"],
                "name": "Mock Battery ${deviceId}",
                "model": "Mock Battery Sensor",
                "manufacturer": "BLE Bridge Test"
            }
        }"""
        
        return mapOf(
            "homeassistant/sensor/ble_bridge/${deviceId}/config" to discoveryPayload
        )
    }
    
    override suspend fun performPeriodicPoll(device: BluetoothDevice): Result<Unit> {
        // Mock device uses notifications, no polling needed
        return Result.success(Unit)
    }
    
    override fun getPollingIntervalMs(): Long? {
        // No polling needed for this mock device
        return null
    }
    
    override suspend fun cleanup() {
        Log.i(TAG, "Cleaning up Mock Battery Plugin")
    }
}
