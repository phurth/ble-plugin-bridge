package com.blemqttbridge.core.interfaces

import android.bluetooth.BluetoothDevice
import android.bluetooth.BluetoothGatt
import android.bluetooth.BluetoothGattCallback
import android.bluetooth.le.ScanRecord
import android.content.Context

/**
 * Plugin interface for BLE devices.
 * 
 * Each plugin provides complete BLE protocol handling for a specific device type.
 * The plugin owns the BluetoothGattCallback and handles all BLE events directly.
 * 
 * This architecture allows device-specific quirks and protocols to be fully isolated,
 * with proven code (like the legacy OneControl implementation) being used without
 * modification or forwarding layers.
 */
interface BleDevicePlugin {
    
    // ===== IDENTIFICATION =====
    
    /**
     * Unique plugin identifier (e.g., "onecontrol", "victron").
     * Used for routing and configuration.
     */
    val pluginId: String
    
    /**
     * Human-readable display name for this device type.
     * Example: "OneControl/LCI Gateway"
     */
    val displayName: String
    
    // ===== DEVICE MATCHING =====
    
    /**
     * Check if this plugin can handle the given BLE device.
     * Called during scanning to match discovered devices to plugins.
     * 
     * Implementation can check:
     * - Device name patterns
     * - Service UUIDs in scan record
     * - Manufacturer data
     * - MAC address prefixes
     * 
     * @param device The scanned BLE device
     * @param scanRecord The advertisement scan record (may be null)
     * @return true if this plugin handles this device type
     */
    fun matchesDevice(device: BluetoothDevice, scanRecord: ScanRecord?): Boolean
    
    /**
     * Get list of configured device MAC addresses.
     * Used for direct connection to known/bonded devices without scanning.
     * 
     * @return List of MAC addresses in format "AA:BB:CC:DD:EE:FF", or empty list
     */
    fun getConfiguredDevices(): List<String>
    
    // ===== BLE CONNECTION =====
    
    /**
     * Create the BluetoothGattCallback for this device.
     * 
     * **THIS IS THE KEY METHOD**: The plugin returns a complete callback implementation
     * that handles ALL BLE events (connection, services, characteristics, notifications).
     * 
     * The callback should:
     * - Handle connection state changes
     * - Discover services and characteristics
     * - Perform device-specific authentication
     * - Subscribe to notifications
     * - Process incoming data
     * - Publish state via mqttPublisher
     * - Call onDisconnect when connection is lost
     * 
     * @param device The device being connected to
     * @param context Android context for BLE operations
     * @param mqttPublisher Interface to publish MQTT messages
     * @param onDisconnect Callback to notify core service of disconnection
     * @return Complete BluetoothGattCallback implementation
     */
    fun createGattCallback(
        device: BluetoothDevice,
        context: Context,
        mqttPublisher: MqttPublisher,
        onDisconnect: (BluetoothDevice, Int) -> Unit
    ): BluetoothGattCallback
    
    /**
     * Called after connectGatt() succeeds and returns a GATT object.
     * Plugin can store the reference for later use (writes, reads, etc.).
     * 
     * @param device The connected device
     * @param gatt The BluetoothGatt connection object
     */
    fun onGattConnected(device: BluetoothDevice, gatt: BluetoothGatt)
    
    // ===== MQTT INTEGRATION =====
    
    /**
     * Get the base MQTT topic for this device.
     * 
     * All state and discovery messages will be published under this topic.
     * Example: "onecontrol/24dcc3ed1e0a"
     * 
     * @param device The device
     * @return Base MQTT topic path (no leading/trailing slashes)
     */
    fun getMqttBaseTopic(device: BluetoothDevice): String
    
    /**
     * Get the MQTT command topic pattern for subscribing to commands.
     * 
     * Default pattern is "{baseTopic}/command/#" which works for simple devices.
     * Plugins with hierarchical topics (like zones) should override this.
     * 
     * Example patterns:
     * - "onecontrol/24dcc3ed1e0a/command/#" (simple)
     * - "easytouch/EC:C9:FF:B1:24:1E/+/command/#" (with zone wildcard)
     * 
     * @param device The device
     * @return MQTT topic pattern for command subscription
     */
    fun getCommandTopicPattern(device: BluetoothDevice): String {
        return "${getMqttBaseTopic(device)}/command/#"
    }
    
    /**
     * Get Home Assistant MQTT Discovery payloads for this device.
     * 
     * Called after successful connection to publish device entities.
     * Each payload is a topic/payload pair for MQTT discovery.
     * 
     * Example topics:
     * - "homeassistant/light/onecontrol_24dcc3ed1e0a_light_1/config"
     * - "homeassistant/sensor/onecontrol_24dcc3ed1e0a_battery/config"
     * 
     * @param device The connected device
     * @return List of (topic, jsonPayload) pairs
     */
    fun getDiscoveryPayloads(device: BluetoothDevice): List<Pair<String, String>>
    
    // ===== COMMAND HANDLING =====
    
    /**
     * Handle incoming MQTT command for this device.
     * 
     * Plugin is responsible for:
     * - Parsing the command topic and payload
     * - Translating to appropriate BLE writes
     * - Using stored GATT reference to write characteristics
     * - Returning success/failure result
     * 
     * @param device The target device
     * @param commandTopic The MQTT command topic (relative to base topic)
     * @param payload The command payload (often JSON or simple string)
     * @return Result indicating success or error
     */
    suspend fun handleCommand(
        device: BluetoothDevice,
        commandTopic: String,
        payload: String
    ): Result<Unit>
    
    // ===== LIFECYCLE =====
    
    /**
     * Initialize the plugin.
     * Called once when the plugin is loaded at service startup.
     * 
     * @param context Android application context
     * @param config Plugin-specific configuration
     */
    fun initialize(context: Context, config: PluginConfig)
    
    /**
     * Called when a device disconnects.
     * Plugin should clean up device-specific state.
     * 
     * @param device The disconnected device
     */
    fun onDeviceDisconnected(device: BluetoothDevice)
    
    /**
     * Called when the plugin is being unloaded.
     * Clean up all resources.
     */
    fun destroy()
    
    // ===== BONDING =====
    
    /**
     * Whether this device type requires explicit bonding/pairing.
     * 
     * If true, the service will call createBond() for CONFIGURED devices
     * (from getConfiguredDevices()) that are not already bonded.
     * 
     * This is ONLY called for devices the user has explicitly configured,
     * NOT for auto-discovered devices. This prevents unwanted pairing attempts
     * to neighbors' devices in crowded environments like RV parks.
     * 
     * Default is false (let BLE stack handle bonding implicitly).
     * 
     * @return true if explicit bonding is required
     */
    fun requiresBonding(): Boolean = false
}

/**
 * Configuration for a plugin.
 * Can be loaded from SharedPreferences, JSON file, etc.
 */
data class PluginConfig(
    val parameters: Map<String, String> = emptyMap()
) {
    fun getString(key: String, default: String = ""): String {
        return parameters[key] ?: default
    }
    
    fun getInt(key: String, default: Int = 0): Int {
        return parameters[key]?.toIntOrNull() ?: default
    }
    
    fun getBoolean(key: String, default: Boolean = false): Boolean {
        return parameters[key]?.toBooleanStrictOrNull() ?: default
    }
}
