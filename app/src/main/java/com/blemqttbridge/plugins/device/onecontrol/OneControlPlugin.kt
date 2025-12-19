package com.blemqttbridge.plugins.device.onecontrol

import android.bluetooth.BluetoothDevice
import android.content.Context
import android.util.Log
import com.blemqttbridge.core.interfaces.BlePluginInterface
import com.blemqttbridge.plugins.device.onecontrol.protocol.Constants

/**
 * OneControl BLE Gateway Plugin
 * 
 * Interfaces with Lippert OneControl RV system via BLE gateway.
 * Supports lighting, awnings, leveling, and other CAN-based devices.
 * 
 * Phase 3 Implementation: Plugin wrapper with basic device matching.
 * Full protocol integration (authentication, CAN parsing, commands) will be added incrementally.
 */
class OneControlPlugin : BlePluginInterface {
    
    companion object {
        private const val TAG = "OneControlPlugin"
        private const val PLUGIN_ID = "onecontrol"
        private const val PLUGIN_NAME = "OneControl Gateway"
        private const val PLUGIN_VERSION = "1.0.0"
        
        // Config keys
        private const val CONFIG_GATEWAY_MAC = "gateway_mac"
        private const val CONFIG_GATEWAY_PIN = "gateway_pin"
        private const val CONFIG_GATEWAY_CYPHER = "gateway_cypher"
        
        // Default config (from existing app)
        private const val DEFAULT_GATEWAY_MAC = "24:DC:C3:ED:1E:0A"
        private const val DEFAULT_GATEWAY_PIN = "090336"
        private const val DEFAULT_GATEWAY_CYPHER = 0x8100080DL
    }
    
    private lateinit var context: Context
    
    // Configuration
    private var gatewayMac: String = DEFAULT_GATEWAY_MAC
    private var gatewayPin: String = DEFAULT_GATEWAY_PIN
    private var gatewayCypher: Long = DEFAULT_GATEWAY_CYPHER
    
    // Connection state tracking
    private val connectedDevices = mutableSetOf<String>()  // Device addresses
    
    override fun getPluginId(): String = PLUGIN_ID
    
    override fun getPluginName(): String = PLUGIN_NAME
    
    override fun getPluginVersion(): String = PLUGIN_VERSION
    
    override fun canHandleDevice(device: BluetoothDevice, scanRecord: ByteArray?): Boolean {
        // Match by configured MAC address
        if (device.address.equals(gatewayMac, ignoreCase = true)) {
            Log.i(TAG, "✅ Device matches configured MAC: ${device.address}")
            return true
        }
        
        // Also check for OneControl discovery service UUID in scan record
        scanRecord?.let { record ->
            val serviceUuids = parseScanRecordServiceUuids(record)
            if (serviceUuids.any { it.equals(Constants.DISCOVERY_SERVICE_UUID, ignoreCase = true) }) {
                Log.i(TAG, "✅ Device has OneControl discovery service UUID")
                return true
            }
        }
        
        return false
    }
    
    override fun getDeviceId(device: BluetoothDevice): String {
        // Use MAC address as device ID (stable and unique)
        return device.address.replace(":", "").lowercase()
    }
    
    override suspend fun initialize(
        context: Context,
        config: Map<String, String>
    ): Result<Unit> {
        this.context = context
        
        // Load configuration
        gatewayMac = config[CONFIG_GATEWAY_MAC] ?: DEFAULT_GATEWAY_MAC
        gatewayPin = config[CONFIG_GATEWAY_PIN] ?: DEFAULT_GATEWAY_PIN
        gatewayCypher = config[CONFIG_GATEWAY_CYPHER]?.toLongOrNull() ?: DEFAULT_GATEWAY_CYPHER
        
        Log.i(TAG, "Initialized OneControl plugin")
        Log.i(TAG, "  Gateway MAC: $gatewayMac")
        Log.i(TAG, "  PIN: ${gatewayPin.take(2)}****")
        
        return Result.success(Unit)
    }
    
    override suspend fun onDeviceConnected(device: BluetoothDevice): Result<Unit> {
        Log.i(TAG, "🔗 Device connected: ${device.address}")
        
        connectedDevices.add(device.address)
        
        // Note: Service discovery and characteristic setup is handled by BaseBleService
        // This simplified Phase 3 version focuses on plugin registration
        
        Log.i(TAG, "✅ Device ${device.address} ready")
        
        return Result.success(Unit)
    }
    
    override suspend fun onDeviceDisconnected(device: BluetoothDevice) {
        Log.i(TAG, "🔌 Device disconnected: ${device.address}")
        
        connectedDevices.remove(device.address)
    }
    
    override suspend fun onCharacteristicNotification(
        device: BluetoothDevice,
        characteristicUuid: String,
        value: ByteArray
    ): Map<String, String> {
        Log.d(TAG, "📨 Notification from $characteristicUuid: ${value.toHexString()}")
        
        // Match characteristic UUID and parse data
        when (characteristicUuid.lowercase()) {
            Constants.CAN_READ_CHAR_UUID.lowercase() -> {
                Log.d(TAG, "📨 CAN data received: ${value.toHexString()}")
                // TODO: Decode COBS, parse CAN, extract device states
                return mapOf(
                    "status" to "online",
                    "last_update" to System.currentTimeMillis().toString()
                )
            }
            Constants.SEED_CHAR_UUID.lowercase() -> {
                Log.d(TAG, "📨 Received SEED notification")
                // TODO: Perform TEA encryption authentication
            }
            Constants.UNLOCK_STATUS_CHAR_UUID.lowercase() -> {
                val status = value.decodeToString()
                Log.i(TAG, "🔓 Unlock status: $status")
            }
        }
        
        return emptyMap()
    }
    
    override suspend fun handleCommand(
        device: BluetoothDevice,
        commandTopic: String,
        payload: String
    ): Result<Unit> {
        Log.i(TAG, "📥 Command received - Topic: $commandTopic, Payload: $payload")
        
        // Parse topic to extract device info
        // Format: {device_type}/{device_id}/{command}
        val parts = commandTopic.split("/")
        if (parts.size < 3) {
            return Result.failure(Exception("Invalid topic format: $commandTopic"))
        }
        
        val deviceType = parts[0]  // e.g., "light", "awning"
        val deviceId = parts[1]
        val command = parts[2]  // e.g., "set", "brightness"
        
        Log.i(TAG, "📤 Command parsed: type=$deviceType id=$deviceId cmd=$command payload=$payload")
        
        // TODO: Build CAN command using MyRvLinkCommandBuilder
        // TODO: Encode using MyRvLinkCommandEncoder
        // TODO: Write to CAN_WRITE_CHAR via BaseBleService
        
        return Result.success(Unit)
    }
    
    override suspend fun getDiscoveryPayloads(device: BluetoothDevice): Map<String, String> {
        val payloads = mutableMapOf<String, String>()
        
        // Generate basic status sensor for Phase 3
        // Full implementation will use HomeAssistantMqttDiscovery for all device types
        val deviceId = getDeviceId(device)
        val topic = "homeassistant/binary_sensor/${deviceId}_status/config"
        val payload = """
            {
                "name": "OneControl Gateway Status",
                "unique_id": "${deviceId}_status",
                "state_topic": "onecontrol/${device.address}/status",
                "device_class": "connectivity",
                "payload_on": "online",
                "payload_off": "offline",
                "device": {
                    "identifiers": ["onecontrol_$deviceId"],
                    "name": "OneControl Gateway",
                    "manufacturer": "Lippert Components",
                    "model": "OneControl",
                    "sw_version": "$PLUGIN_VERSION"
                }
            }
        """.trimIndent()
        
        payloads[topic] = payload
        
        Log.i(TAG, "📡 Generated ${payloads.size} discovery payload(s)")
        
        return payloads
    }
    
    override fun getPollingIntervalMs(): Long? {
        // OneControl uses notifications, no polling needed
        return null
    }
    
    override suspend fun cleanup() {
        Log.i(TAG, "🧹 Cleaning up OneControl plugin")
        connectedDevices.clear()
    }
    
    // ============================================================================
    // Private Helper Methods
    // ============================================================================
    
    private fun parseScanRecordServiceUuids(scanRecord: ByteArray): List<String> {
        val uuids = mutableListOf<String>()
        var currentPos = 0
        
        while (currentPos < scanRecord.size) {
            val length = scanRecord[currentPos].toInt() and 0xFF
            if (length == 0 || currentPos + length >= scanRecord.size) break
            
            val type = scanRecord[currentPos + 1].toInt() and 0xFF
            
            // 0x06 = Complete list of 128-bit UUIDs
            // 0x07 = Incomplete list of 128-bit UUIDs
            if (type == 0x06 || type == 0x07) {
                val uuidBytes = scanRecord.copyOfRange(currentPos + 2, currentPos + 1 + length)
                val uuid = parseUuid128(uuidBytes)
                uuid?.let { uuids.add(it) }
            }
            
            currentPos += length + 1
        }
        
        return uuids
    }
    
    private fun parseUuid128(bytes: ByteArray): String? {
        if (bytes.size != 16) return null
        
        // UUID bytes are in reverse order
        val reversed = bytes.reversedArray()
        return "%02x%02x%02x%02x-%02x%02x-%02x%02x-%02x%02x-%02x%02x%02x%02x%02x%02x".format(
            reversed[0], reversed[1], reversed[2], reversed[3],
            reversed[4], reversed[5],
            reversed[6], reversed[7],
            reversed[8], reversed[9],
            reversed[10], reversed[11], reversed[12], reversed[13], reversed[14], reversed[15]
        )
    }
    
    private fun ByteArray.toHexString(): String {
        return joinToString(" ") { "%02x".format(it) }
    }
}
