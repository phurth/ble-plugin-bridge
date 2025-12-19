package com.blemqttbridge.core.interfaces

import android.bluetooth.BluetoothDevice
import android.content.Context

/**
 * Plugin interface for BLE device protocols.
 * Each device type (OneControl, EasyTouch, etc.) implements this interface.
 */
interface BlePluginInterface {
    
    /**
     * Unique identifier for this plugin (e.g., "onecontrol", "easytouch")
     */
    fun getPluginId(): String
    
    /**
     * Human-readable name for this plugin
     */
    fun getPluginName(): String
    
    /**
     * Version of this plugin implementation
     */
    fun getPluginVersion(): String
    
    /**
     * Initialize plugin with configuration.
     * Called once when plugin is loaded.
     * 
     * @param context Android context
     * @param config Plugin-specific configuration map
     * @return Result indicating success or failure with error message
     */
    suspend fun initialize(context: Context, config: Map<String, String>): Result<Unit>
    
    /**
     * Check if a discovered BLE device should be handled by this plugin.
     * Called for each discovered device during scanning.
     * 
     * @param device The discovered Bluetooth device
     * @param scanRecord Raw scan record data (may contain manufacturer data, service UUIDs, etc.)
     * @return true if this plugin can handle this device
     */
    fun canHandleDevice(device: BluetoothDevice, scanRecord: ByteArray?): Boolean
    
    /**
     * Get the device ID for MQTT topics and Home Assistant discovery.
     * Should be stable and unique for this specific device.
     * 
     * @param device The Bluetooth device
     * @return Device ID string (e.g., MAC address or UUID-based)
     */
    fun getDeviceId(device: BluetoothDevice): String
    
    /**
     * Called when a matching device is connected.
     * Plugin should perform any connection-specific initialization.
     * 
     * @param device The connected Bluetooth device
     * @return Result indicating success or failure
     */
    suspend fun onDeviceConnected(device: BluetoothDevice): Result<Unit>
    
    /**
     * Called when a device disconnects.
     * Plugin should clean up any device-specific state.
     * 
     * @param device The disconnected Bluetooth device
     */
    suspend fun onDeviceDisconnected(device: BluetoothDevice)
    
    /**
     * Called when a GATT characteristic notification is received.
     * Plugin parses the data and returns state updates for MQTT publishing.
     * 
     * @param device The Bluetooth device that sent the notification
     * @param characteristicUuid UUID of the characteristic that sent the notification
     * @param value Raw notification data
     * @return Map of topic suffix to payload (e.g., "state" -> JSON string)
     *         Returns empty map if notification should be ignored
     */
    suspend fun onCharacteristicNotification(
        device: BluetoothDevice,
        characteristicUuid: String,
        value: ByteArray
    ): Map<String, String>
    
    /**
     * Handle a command received from MQTT.
     * Plugin should convert MQTT payload to BLE write operation.
     * 
     * @param device The target Bluetooth device
     * @param commandTopic The MQTT topic that received the command (relative to device topic)
     * @param payload The command payload from MQTT
     * @return Result indicating success or failure with error message
     */
    suspend fun handleCommand(
        device: BluetoothDevice,
        commandTopic: String,
        payload: String
    ): Result<Unit>
    
    /**
     * Get Home Assistant discovery payloads for this device.
     * Called when device first connects to publish MQTT discovery.
     * 
     * @param device The Bluetooth device
     * @return Map of full MQTT topic to discovery payload JSON
     *         (e.g., "homeassistant/climate/device_id/thermostat/config" -> discovery JSON)
     */
    suspend fun getDiscoveryPayloads(device: BluetoothDevice): Map<String, String>
    
    /**
     * Perform periodic polling if needed.
     * Called at regular intervals (configurable) while device is connected.
     * Some devices require polling for state updates.
     * 
     * @param device The Bluetooth device to poll
     * @return Result indicating success or failure
     */
    suspend fun performPeriodicPoll(device: BluetoothDevice): Result<Unit> {
        // Default: no polling needed
        return Result.success(Unit)
    }
    
    /**
     * Get the polling interval in milliseconds.
     * Return null if device doesn't require polling.
     */
    fun getPollingIntervalMs(): Long? = null
    
    /**
     * Cleanup plugin resources.
     * Called when plugin is unloaded or app is shutting down.
     */
    suspend fun cleanup()
}
