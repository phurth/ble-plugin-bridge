package com.blemqttbridge.plugins.device.onecontrol

import android.bluetooth.BluetoothDevice
import android.content.Context
import android.util.Log
import com.blemqttbridge.core.interfaces.BlePluginInterface
import com.blemqttbridge.plugins.device.onecontrol.protocol.Constants
import com.blemqttbridge.plugins.device.onecontrol.protocol.TeaEncryption
import kotlinx.coroutines.CompletableDeferred
import kotlinx.coroutines.delay
import kotlinx.coroutines.withTimeout

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
    private val authenticatedDevices = mutableSetOf<String>()  // Authenticated devices
    private val unlockedDevices = mutableSetOf<String>()  // PIN-unlocked devices
    private var gattOperations: BlePluginInterface.GattOperations? = null
    private var pendingSeedResponse: CompletableDeferred<ByteArray>? = null
    
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
    
    override suspend fun onServicesDiscovered(
        device: BluetoothDevice,
        gattOperations: BlePluginInterface.GattOperations
    ): Result<Unit> {
        this.gattOperations = gattOperations
        
        Log.i(TAG, "🔐 Starting authentication for ${device.address}...")
        
        return try {
            // STEP 1: PIN UNLOCK (Only for CAN Service gateways)
            // Data Service gateways (00000030) don't have UNLOCK_CHAR and skip this step
            Log.i(TAG, "Step 1: Checking gateway type and unlock status...")
            val unlockStatusResult = gattOperations.readCharacteristic(Constants.UNLOCK_CHAR_UUID)
            
            val isDataServiceGateway = unlockStatusResult.isFailure
            
            if (isDataServiceGateway) {
                // UNLOCK_CHAR not found - this is a Data Service gateway
                // Data Service gateways use UNLOCK_STATUS challenge-response authentication
                Log.i(TAG, "⏭️ Data Service gateway detected - using UNLOCK_STATUS challenge-response")
                
                // Step 1: Read UNLOCK_STATUS to get challenge
                Log.i(TAG, "🔑 Step 1: Reading UNLOCK_STATUS for challenge...")
                val challengeResult = gattOperations.readCharacteristic(Constants.UNLOCK_STATUS_CHAR_UUID)
                if (challengeResult.isFailure) {
                    return Result.failure(Exception("Failed to read UNLOCK_STATUS challenge: ${challengeResult.exceptionOrNull()?.message}"))
                }
                
                val challengeData = challengeResult.getOrThrow()
                if (challengeData.size == 4) {
                    // Calculate KEY from challenge (big-endian)
                    val challenge = ((challengeData[0].toInt() and 0xFF) shl 24) or
                                  ((challengeData[1].toInt() and 0xFF) shl 16) or
                                  ((challengeData[2].toInt() and 0xFF) shl 8) or
                                  (challengeData[3].toInt() and 0xFF)
                    
                    Log.d(TAG, "Challenge: 0x${challenge.toString(16).padStart(8, '0')}")
                    
                    // Encrypt with TEA (using gateway cypher)
                    val keyValue = TeaEncryption.encrypt(gatewayCypher, challenge.toLong() and 0xFFFFFFFFL)
                    Log.d(TAG, "KEY: 0x${keyValue.toString(16).padStart(8, '0')}")
                    
                    // Convert to big-endian bytes
                    val keyBytes = byteArrayOf(
                        ((keyValue shr 24) and 0xFF).toByte(),
                        ((keyValue shr 16) and 0xFF).toByte(),
                        ((keyValue shr 8) and 0xFF).toByte(),
                        (keyValue and 0xFF).toByte()
                    )
                    
                    // Step 2: Write KEY
                    Log.i(TAG, "🔑 Step 2: Writing KEY response...")
                    val writeResult = gattOperations.writeCharacteristic(Constants.KEY_CHAR_UUID, keyBytes)
                    if (writeResult.isFailure) {
                        return Result.failure(Exception("Failed to write KEY: ${writeResult.exceptionOrNull()?.message}"))
                    }
                    
                    // Small delay for gateway to process
                    delay(200)
                    
                    // Step 3: Verify unlock
                    Log.i(TAG, "🔑 Step 3: Verifying unlock status...")
                    val verifyResult = gattOperations.readCharacteristic(Constants.UNLOCK_STATUS_CHAR_UUID)
                    if (verifyResult.isFailure) {
                        return Result.failure(Exception("Failed to verify unlock: ${verifyResult.exceptionOrNull()?.message}"))
                    }
                    
                    val unlockStatus = String(verifyResult.getOrThrow(), Charsets.UTF_8)
                    if (unlockStatus.contains("Unlocked", ignoreCase = true)) {
                        Log.i(TAG, "✅ Gateway authenticated! Status: $unlockStatus")
                    } else {
                        return Result.failure(Exception("Authentication failed - expected 'Unlocked', got: $unlockStatus"))
                    }
                } else {
                    return Result.failure(Exception("Invalid challenge size: ${challengeData.size}, expected 4"))
                }
                
                // Subscribe to DATA notifications
                Log.d(TAG, "Subscribing to DATA notifications: ${Constants.DATA_READ_CHAR_UUID}")
                val notifyResult = gattOperations.enableNotifications(Constants.DATA_READ_CHAR_UUID)
                if (notifyResult.isFailure) {
                    Log.w(TAG, "⚠️ Failed to subscribe to DATA: ${notifyResult.exceptionOrNull()?.message}")
                } else {
                    Log.i(TAG, "✅ Subscribed to DATA notifications")
                }
                
                authenticatedDevices.add(device.address)
                return Result.success(Unit)
            } else {
                // CAN Service gateway - perform PIN unlock
                val unlockStatus = unlockStatusResult.getOrThrow()
                if (unlockStatus.isEmpty()) {
                    return Result.failure(Exception("Unlock status returned empty data"))
                }
                
                val status = unlockStatus[0].toInt() and 0xFF
                Log.d(TAG, "Unlock status: 0x${status.toString(16)}")
                
                if (status == 0) {
                    // Gateway is locked - write PIN
                    Log.i(TAG, "Gateway is locked, writing PIN...")
                    val pinBytes = gatewayPin.toByteArray(Charsets.UTF_8)
                    val writeResult = gattOperations.writeCharacteristic(Constants.UNLOCK_CHAR_UUID, pinBytes)
                    
                    if (writeResult.isFailure) {
                        return Result.failure(Exception("Failed to write PIN: ${writeResult.exceptionOrNull()?.message}"))
                    }
                    
                    // Wait for unlock to settle
                    Log.d(TAG, "Waiting ${Constants.UNLOCK_VERIFY_DELAY_MS}ms for unlock to complete...")
                    delay(Constants.UNLOCK_VERIFY_DELAY_MS)
                    
                    // Verify unlock
                    val verifyResult = gattOperations.readCharacteristic(Constants.UNLOCK_CHAR_UUID)
                    if (verifyResult.isFailure) {
                        return Result.failure(Exception("Failed to verify unlock: ${verifyResult.exceptionOrNull()?.message}"))
                    }
                    
                    val verifyStatus = verifyResult.getOrThrow()
                    if (verifyStatus.isEmpty() || (verifyStatus[0].toInt() and 0xFF) == 0) {
                        return Result.failure(Exception("Gateway unlock failed (PIN incorrect?)"))
                    }
                    
                    Log.i(TAG, "✅ Gateway unlocked successfully!")
                } else {
                    Log.i(TAG, "✅ Gateway already unlocked")
                }
                
                unlockedDevices.add(device.address)
            }
            
            // STEP 2: TEA AUTHENTICATION (SEED/KEY exchange)
            Log.i(TAG, "Step 2: Starting TEA authentication...")
            
            // Read SEED characteristic
            Log.d(TAG, "Reading SEED from ${Constants.SEED_CHAR_UUID}...")
            val seedResult = gattOperations.readCharacteristic(Constants.SEED_CHAR_UUID)
            
            if (seedResult.isFailure) {
                return Result.failure(Exception("Failed to read SEED: ${seedResult.exceptionOrNull()?.message}"))
            }
            
            val seedBytes = seedResult.getOrThrow()
            
            if (seedBytes.size != 4) {
                return Result.failure(Exception("Invalid SEED size: ${seedBytes.size}, expected 4"))
            }
            
            // Convert bytes to long (little-endian)
            val seed = ((seedBytes[3].toLong() and 0xFF) shl 24) or
                      ((seedBytes[2].toLong() and 0xFF) shl 16) or
                      ((seedBytes[1].toLong() and 0xFF) shl 8) or
                      (seedBytes[0].toLong() and 0xFF)
            
            Log.d(TAG, "Seed: 0x${seed.toString(16).padStart(8, '0')}")
            
            // Step 3: Encrypt seed with TEA using gateway cypher
            val encryptedKey = TeaEncryption.encrypt(gatewayCypher, seed)
            Log.d(TAG, "Key: 0x${encryptedKey.toString(16).padStart(8, '0')}")
            
            // Step 4: Convert encrypted key to bytes (little-endian)
            val keyBytes = byteArrayOf(
                (encryptedKey and 0xFF).toByte(),
                ((encryptedKey shr 8) and 0xFF).toByte(),
                ((encryptedKey shr 16) and 0xFF).toByte(),
                ((encryptedKey shr 24) and 0xFF).toByte()
            )
            
            // Step 5: Write encrypted key back to gateway
            Log.d(TAG, "Writing KEY to ${Constants.KEY_CHAR_UUID}")
            val writeResult = gattOperations.writeCharacteristic(Constants.KEY_CHAR_UUID, keyBytes)
            if (writeResult.isFailure) {
                return Result.failure(Exception("Failed to write KEY: ${writeResult.exceptionOrNull()?.message}"))
            }
            
            Log.i(TAG, "✅ Authentication complete!")
            
            // Step 6: Subscribe to CAN notifications
            Log.d(TAG, "Subscribing to CAN notifications: ${Constants.CAN_READ_CHAR_UUID}")
            val notifyResult = gattOperations.enableNotifications(Constants.CAN_READ_CHAR_UUID)
            if (notifyResult.isFailure) {
                Log.w(TAG, "⚠️ Failed to subscribe to CAN: ${notifyResult.exceptionOrNull()?.message}")
                // Don't fail auth if subscription fails - can retry later
            } else {
                Log.i(TAG, "✅ Subscribed to CAN notifications")
            }
            
            authenticatedDevices.add(device.address)
            Result.success(Unit)
            
        } catch (e: Exception) {
            Log.e(TAG, "❌ Authentication failed: ${e.message}", e)
            pendingSeedResponse = null
            Result.failure(e)
        }
    }
    
    override suspend fun onDeviceDisconnected(device: BluetoothDevice) {
        Log.i(TAG, "🔌 Device disconnected: ${device.address}")
        
        connectedDevices.remove(device.address)
        authenticatedDevices.remove(device.address)
        unlockedDevices.remove(device.address)
        gattOperations = null
        pendingSeedResponse = null
    }
    
    override suspend fun onCharacteristicNotification(
        device: BluetoothDevice,
        characteristicUuid: String,
        value: ByteArray
    ): Map<String, String> {
        Log.d(TAG, "📨 Notification from $characteristicUuid: ${value.toHexString()}")
        
        // Match characteristic UUID and parse data
        when (characteristicUuid.lowercase()) {
            Constants.SEED_CHAR_UUID.lowercase() -> {
                Log.d(TAG, "📨 Received SEED notification: ${value.toHexString()}")
                // Complete the pending SEED request
                pendingSeedResponse?.complete(value)
                pendingSeedResponse = null
            }
            Constants.CAN_READ_CHAR_UUID.lowercase() -> {
                Log.d(TAG, "📨 CAN data received: ${value.toHexString()}")
                // TODO: Decode COBS, parse CAN, extract device states
                return mapOf(
                    "status" to "online",
                    "last_update" to System.currentTimeMillis().toString()
                )
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
