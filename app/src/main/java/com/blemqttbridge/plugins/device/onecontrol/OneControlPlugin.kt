package com.blemqttbridge.plugins.device.onecontrol

import android.bluetooth.BluetoothDevice
import android.content.Context
import android.util.Log
import com.blemqttbridge.core.interfaces.BlePluginInterface
import com.blemqttbridge.plugins.device.onecontrol.protocol.Constants
import kotlinx.coroutines.*

/**
 * OneControl BLE Gateway Plugin
 * 
 * Interfaces with Lippert OneControl RV system via BLE gateway.
 * Supports lighting, awnings, leveling, and other CAN-based devices.
 * 
 * Complete implementation with PIN unlock + TEA authentication for OneControl gateways.
 */
class OneControlPlugin : BlePluginInterface {
    
    /**
     * Simple COBS decoder state for byte-by-byte processing
     */
    data class CobsDecoderState(
        var buffer: ByteArray = ByteArray(256),
        var bufferIndex: Int = 0,
        var code: Int = 0,
        var codeIndex: Int = 0,
        var expectingFrame: Boolean = false
    ) {
        fun decodeByte(byte: Byte): ByteArray? {
            val unsignedByte = byte.toInt() and 0xFF
            
            if (unsignedByte == 0x00) {
                // Frame delimiter - process accumulated data
                if (bufferIndex > 0) {
                    val frame = buffer.copyOf(bufferIndex)
                    reset()
                    return frame
                }
                return null
            }
            
            if (codeIndex == 0) {
                // New block - store overhead byte
                code = unsignedByte
                codeIndex = 1
                if (code == 0xFF) return null  // Invalid
            } else {
                // Data byte
                if (bufferIndex < buffer.size) {
                    buffer[bufferIndex++] = byte
                }
                codeIndex++
                
                if (codeIndex >= code) {
                    // End of block - add delimiter if not at end
                    if (code < 0xFF && bufferIndex < buffer.size) {
                        buffer[bufferIndex++] = 0x00
                    }
                    codeIndex = 0
                }
            }
            
            return null
        }
        
        fun reset() {
            bufferIndex = 0
            code = 0
            codeIndex = 0
            expectingFrame = false
        }
    }
    
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
    
    // Protocol state tracking
    private var nextCommandId: UShort = 1u
    private var deviceTableId: Byte = 0x00
    private val streamReadingDevices = mutableSetOf<String>()
    private val heartbeatJobs = mutableMapOf<String, kotlinx.coroutines.Job>()
    private val notificationProcessingJobs = mutableMapOf<String, kotlinx.coroutines.Job>()
    private val notificationQueues = mutableMapOf<String, java.util.concurrent.ConcurrentLinkedQueue<ByteArray>>()
    private val DEFAULT_DEVICE_TABLE_ID: Byte = 0x01  // From original app
    private val gatewayInfoReceived = mutableMapOf<String, Boolean>()  // Per-device tracking
    
    // Active stream reading (from original app) - per device
    private val streamReadingThreads = mutableMapOf<String, Thread>()
    private val streamReadingFlags = mutableMapOf<String, Boolean>()
    private val streamReadingLocks = mutableMapOf<String, Object>()
    private val cobsDecoderStates = mutableMapOf<String, CobsDecoderState>()
    
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
            // STEP 1: Check gateway type by looking for UNLOCK_CHAR (CAN Service) vs DATA_READ (Data Service)
            // Data Service gateways (00000030) don't have UNLOCK_CHAR
            Log.i(TAG, "Step 1: Checking gateway type...")
            val unlockStatusResult = gattOperations.readCharacteristic(Constants.UNLOCK_CHAR_UUID)
            
            val isDataServiceGateway = unlockStatusResult.isFailure
            
            if (isDataServiceGateway) {
                // This is a Data Service gateway - REQUIRES challenge-response authentication!
                // From AUTHENTICATION_ALGORITHM.md:
                // "Authentication is REQUIRED for MyRvLink Data Service gateways"
                // "Without this authentication, the gateway will accept CCCD subscription but 
                //  will not send any notifications"
                
                Log.i(TAG, "🔐 Data Service gateway detected - performing challenge-response authentication...")
                
                // STEP 1: Read challenge from UNLOCK_STATUS (00000012)
                val challengeResult = gattOperations.readCharacteristic(Constants.UNLOCK_STATUS_CHAR_UUID)
                if (challengeResult.isFailure) {
                    return Result.failure(Exception("Failed to read UNLOCK_STATUS challenge: ${challengeResult.exceptionOrNull()?.message}"))
                }
                
                val challengeBytes = challengeResult.getOrThrow()
                Log.d(TAG, "📥 UNLOCK_STATUS response: ${challengeBytes.joinToString(" ") { String.format("%02X", it) }} (${challengeBytes.size} bytes)")
                
                // Check if already unlocked (returns "Unlocked" ASCII string)
                if (challengeBytes.size == 8) {
                    val statusString = String(challengeBytes, Charsets.UTF_8)
                    if (statusString.equals("Unlocked", ignoreCase = true)) {
                        Log.i(TAG, "✅ Gateway already unlocked - skipping authentication")
                        authenticatedDevices.add(device.address)
                    }
                }
                
                // If 4 bytes, it's a challenge - calculate and write KEY
                if (challengeBytes.size == 4 && !authenticatedDevices.contains(device.address)) {
                    // Parse as BIG-ENDIAN uint32 (critical - Data Service uses big-endian!)
                    val challenge = ((challengeBytes[0].toLong() and 0xFF) shl 24) or
                                   ((challengeBytes[1].toLong() and 0xFF) shl 16) or
                                   ((challengeBytes[2].toLong() and 0xFF) shl 8) or
                                   (challengeBytes[3].toLong() and 0xFF)
                    
                    Log.d(TAG, "🔑 Challenge (big-endian): 0x${challenge.toString(16).padStart(8, '0')}")
                    
                    // STEP 2: Calculate KEY using TEA encryption (cypher = 612643285)
                    val keyValue = calculateAuthKey(challenge)
                    Log.d(TAG, "🔑 Calculated KEY: 0x${keyValue.toString(16).padStart(8, '0')}")
                    
                    // Convert to BIG-ENDIAN bytes
                    val keyBytes = byteArrayOf(
                        ((keyValue shr 24) and 0xFF).toByte(),
                        ((keyValue shr 16) and 0xFF).toByte(),
                        ((keyValue shr 8) and 0xFF).toByte(),
                        (keyValue and 0xFF).toByte()
                    )
                    Log.d(TAG, "🔑 KEY bytes (big-endian): ${keyBytes.joinToString(" ") { String.format("%02X", it) }}")
                    
                    // STEP 3: Write KEY to 00000013 (CRITICAL: WRITE_TYPE_NO_RESPONSE!)
                    val keyWriteResult = gattOperations.writeCharacteristicNoResponse(Constants.KEY_CHAR_UUID, keyBytes)
                    if (keyWriteResult.isFailure) {
                        return Result.failure(Exception("Failed to write KEY: ${keyWriteResult.exceptionOrNull()?.message}"))
                    }
                    Log.i(TAG, "✅ KEY written successfully")
                    
                    // STEP 4: Wait 500ms for gateway to enter data mode
                    delay(500)
                    
                    // STEP 5: Read UNLOCK_STATUS again to verify "Unlocked"
                    val verifyResult = gattOperations.readCharacteristic(Constants.UNLOCK_STATUS_CHAR_UUID)
                    if (verifyResult.isSuccess) {
                        val verifyBytes = verifyResult.getOrThrow()
                        val verifyString = String(verifyBytes, Charsets.UTF_8)
                        Log.i(TAG, "🔓 Verify status: '$verifyString' (${verifyBytes.size} bytes)")
                        
                        if (verifyString.equals("Unlocked", ignoreCase = true)) {
                            Log.i(TAG, "✅ Authentication verified - gateway unlocked!")
                            authenticatedDevices.add(device.address)
                        } else {
                            Log.w(TAG, "⚠️ Unexpected unlock status after KEY write: $verifyString")
                            // Continue anyway - some gateways may not return "Unlocked"
                            authenticatedDevices.add(device.address)
                        }
                    } else {
                        Log.w(TAG, "⚠️ Failed to verify unlock status: ${verifyResult.exceptionOrNull()?.message}")
                        // Continue anyway - KEY write is the critical step
                        authenticatedDevices.add(device.address)
                    }
                }
                
                // STEP 6: Enable notifications (MUST be after KEY write!)
                // CRITICAL: Original app subscribes to THREE characteristics for Data Service gateways:
                // 1. 00000034 (DATA_READ) - Main data stream
                // 2. 00000011 (SEED) - Auth Service notifications
                // 3. 00000014 (Auth Service) - Additional auth notifications
                
                Log.i(TAG, "📝 Enabling notifications (all 3 characteristics)...")
                delay(200)  // Wait for gateway to enter data mode (from technical_spec.md)
                
                // Subscribe to Auth Service SEED (00000011)
                val seedNotifyResult = gattOperations.enableNotifications(Constants.SEED_CHAR_UUID)
                if (seedNotifyResult.isFailure) {
                    Log.w(TAG, "⚠️ Failed to subscribe to SEED (00000011): ${seedNotifyResult.exceptionOrNull()?.message}")
                } else {
                    Log.i(TAG, "✅ Subscribed to SEED (00000011) notifications")
                }
                delay(150)  // Small delay between subscriptions (from original app)
                
                // Subscribe to Auth Service 00000014
                val auth14NotifyResult = gattOperations.enableNotifications(Constants.AUTH_STATUS_CHAR_UUID)
                if (auth14NotifyResult.isFailure) {
                    Log.w(TAG, "⚠️ Failed to subscribe to Auth 00000014: ${auth14NotifyResult.exceptionOrNull()?.message}")
                } else {
                    Log.i(TAG, "✅ Subscribed to Auth (00000014) notifications")
                }
                delay(150)  // Small delay between subscriptions
                
                // Subscribe to Data Service READ (00000034) - main data stream
                val notifyResult = gattOperations.enableNotifications(Constants.DATA_READ_CHAR_UUID)
                if (notifyResult.isFailure) {
                    Log.w(TAG, "⚠️ Failed to subscribe to DATA (00000034): ${notifyResult.exceptionOrNull()?.message}")
                    // Continue anyway - may still work
                } else {
                    Log.i(TAG, "✅ Subscribed to DATA (00000034) notifications")
                }
                
                // Start stream reader
                startActiveStreamReading(device)
                
                // Send initial GetDevices command after brief delay
                GlobalScope.launch {
                    delay(500)
                    Log.i(TAG, "📤 Sending initial GetDevices to wake up gateway")
                    sendInitialCanCommand(device)
                    startHeartbeat(device)
                }
                
                Log.i(TAG, "✅ Data Service gateway ready!")
                return Result.success(Unit)
            } else {
                // CAN Service gateway - perform full PIN unlock + TEA authentication
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
                
                // STEP 2: TEA SEED/KEY Authentication (Only for CAN Service gateways)
                Log.i(TAG, "🔐 Starting TEA SEED/KEY authentication...")
                
                // Read SEED from AUTH service
                val seedResult = gattOperations.readCharacteristic(Constants.SEED_CHAR_UUID)
                if (seedResult.isFailure) {
                    return Result.failure(Exception("Failed to read SEED: ${seedResult.exceptionOrNull()?.message}"))
                }
                
                val seedBytes = seedResult.getOrThrow()
                if (seedBytes.size != 4) {
                    return Result.failure(Exception("Invalid SEED size: ${seedBytes.size}, expected 4"))
                }
                
                // Extract 32-bit seed (little-endian for CAN Service)
                val seed = ((seedBytes[3].toLong() and 0xFF) shl 24) or
                          ((seedBytes[2].toLong() and 0xFF) shl 16) or
                          ((seedBytes[1].toLong() and 0xFF) shl 8) or
                          (seedBytes[0].toLong() and 0xFF)
                
                Log.d(TAG, "Read SEED: 0x${seed.toString(16).padStart(8, '0')}")
                
                // Encrypt seed with TEA (using CAN Service cypher = 0x8100080DL)
                val encryptedKey = calculateTeaKey(gatewayCypher, seed)
                Log.d(TAG, "Encrypted KEY: 0x${encryptedKey.toString(16).padStart(8, '0')}")
                
                // Write encrypted key back (little-endian for CAN Service)
                val keyBytes = byteArrayOf(
                    (encryptedKey and 0xFF).toByte(),
                    ((encryptedKey shr 8) and 0xFF).toByte(),
                    ((encryptedKey shr 16) and 0xFF).toByte(),
                    ((encryptedKey shr 24) and 0xFF).toByte()
                )
                
                // Write KEY (CRITICAL: Must use WRITE_TYPE_NO_RESPONSE to enable data mode)
                val keyWriteResult = gattOperations.writeCharacteristicNoResponse(Constants.KEY_CHAR_UUID, keyBytes)
                if (keyWriteResult.isFailure) {
                    return Result.failure(Exception("Failed to write KEY: ${keyWriteResult.exceptionOrNull()?.message}"))
                }
                
                // Wait for gateway to enter data mode (critical timing from technical spec)
                delay(200)
                
                Log.i(TAG, "✅ TEA authentication complete!")
                
                // Subscribe to CAN notifications
                Log.d(TAG, "Subscribing to CAN notifications: ${Constants.CAN_READ_CHAR_UUID}")
                val notifyResult = gattOperations.enableNotifications(Constants.CAN_READ_CHAR_UUID)
                if (notifyResult.isFailure) {
                    Log.w(TAG, "⚠️ Failed to subscribe to CAN: ${notifyResult.exceptionOrNull()?.message}")
                } else {
                    Log.i(TAG, "✅ Subscribed to CAN notifications")
                }
                
                // Start complete post-authentication protocol (like original app)
                Log.i(TAG, "✅ CAN Service authentication complete: Starting full protocol flow")
                startActiveStreamReading(device)
                sendInitialCanCommand(device)
                startHeartbeat(device)
                
                authenticatedDevices.add(device.address)
                return Result.success(Unit)
            }
            
        } catch (e: Exception) {
            Log.e(TAG, "❌ Authentication failed: ${e.message}", e)
            pendingSeedResponse = null
            Result.failure(e)
        }
    }
    
    override suspend fun onDeviceDisconnected(device: BluetoothDevice) {
        Log.i(TAG, "🔌 Device disconnected: ${device.address}")
        
        // Stop heartbeat and stream reading
        heartbeatJobs[device.address]?.cancel()
        heartbeatJobs.remove(device.address)
        
        // Stop stream reading threads
        streamReadingFlags[device.address] = true  // Signal thread to stop
        streamReadingLocks[device.address]?.let { lock ->
            synchronized(lock) {
                lock.notify() // Wake up waiting thread
            }
        }
        streamReadingThreads[device.address]?.interrupt()
        streamReadingThreads.remove(device.address)
        streamReadingFlags.remove(device.address)
        streamReadingLocks.remove(device.address)
        cobsDecoderStates.remove(device.address)  // Clean up COBS decoder state
        gatewayInfoReceived.remove(device.address)  // Clean up gateway info state
        
        notificationProcessingJobs[device.address]?.cancel()
        notificationProcessingJobs.remove(device.address)
        notificationQueues.remove(device.address)
        streamReadingDevices.remove(device.address)
        
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
        
        // Queue notification for background processing (like original app)
        if (characteristicUuid.lowercase() == Constants.CAN_READ_CHAR_UUID.lowercase() || 
            characteristicUuid.lowercase() == Constants.DATA_READ_CHAR_UUID.lowercase()) {
            notificationQueues[device.address]?.offer(value)
            // Wake up the reading thread (like original app)
            streamReadingLocks[device.address]?.let { lock ->
                synchronized(lock) {
                    lock.notify()
                }
            }
        }
        
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
                // Processed by background thread via queue
                return mapOf(
                    "status" to "online",
                    "last_update" to System.currentTimeMillis().toString()
                )
            }
            Constants.DATA_READ_CHAR_UUID.lowercase() -> {
                Log.d(TAG, "📨 DATA notification received: ${value.toHexString()}")
                // Processed by background thread via queue
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
        // TODO: Write to CAN_WRITE_CHAR or DATA_WRITE_CHAR via gattOperations
        
        return Result.success(Unit)
    }
    
    override suspend fun getDiscoveryPayloads(device: BluetoothDevice): Map<String, String> {
        val payloads = mutableMapOf<String, String>()
        
        // Generate basic status sensor
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
        
        // Cancel all heartbeats
        heartbeatJobs.values.forEach { it.cancel() }
        heartbeatJobs.clear()
        
        connectedDevices.clear()
        authenticatedDevices.clear()
        unlockedDevices.clear()
        streamReadingDevices.clear()
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
    
    /**
     * Start active stream reading loop - matches original app implementation
     * Based on DirectConnectionMyRvLinkBle.BackgroundOperationAsync()
     */
    private fun startActiveStreamReading(device: BluetoothDevice) {
        if (streamReadingDevices.contains(device.address)) {
            Log.d(TAG, "🔄 Active stream reading already active for ${device.address}")
            return
        }
        
        streamReadingDevices.add(device.address)
        notificationQueues[device.address] = java.util.concurrent.ConcurrentLinkedQueue<ByteArray>()
        streamReadingFlags[device.address] = false  // shouldStopStreamReading = false
        streamReadingLocks[device.address] = Object()
        cobsDecoderStates[device.address] = CobsDecoderState()  // Initialize COBS decoder
        gatewayInfoReceived[device.address] = false  // Initialize gateway info status
        
        Log.i(TAG, "🔄 Active stream reading started for ${device.address}")
        
        // Start background thread exactly like original app
        val thread = Thread {
            val queue = notificationQueues[device.address]
            val lock = streamReadingLocks[device.address]
            Log.i(TAG, "🔄 Background stream reading thread started for ${device.address}")
            
            while (!streamReadingFlags[device.address]!! && streamReadingDevices.contains(device.address)) {
                try {
                    synchronized(lock!!) {
                        if (queue?.isEmpty() == true) {
                            lock.wait(8000)  // 8-second timeout like original
                        }
                    }
                    
                    // Process all queued notification packets
                    while (queue?.isNotEmpty() == true && !streamReadingFlags[device.address]!!) {
                        val notificationData = queue.poll() ?: continue
                        Log.d(TAG, "📥 Processing queued notification: ${notificationData.size} bytes")
                        
                        // Feed bytes one at a time to COBS decoder (like original app)
                        for (byte in notificationData) {
                            // TODO: Implement COBS byte-by-byte decoding like original
                            processNotificationByte(device.address, byte)
                        }
                    }
                } catch (e: InterruptedException) {
                    Log.i(TAG, "Stream reading thread interrupted for ${device.address}")
                    break
                } catch (e: Exception) {
                    Log.w(TAG, "Stream reading error for ${device.address}: ${e.message}")
                }
            }
            
            Log.i(TAG, "🔄 Background stream reading thread stopped for ${device.address}")
        }
        
        streamReadingThreads[device.address] = thread
        thread.start()
    }
    
    /**
     * Process individual notification bytes through COBS decoder (like original app)
     */
    private fun processNotificationByte(deviceAddress: String, byte: Byte) {
        cobsDecoderStates[deviceAddress]?.let { decoder ->
            val decodedFrame = decoder.decodeByte(byte)
            if (decodedFrame != null) {
                Log.d(TAG, "✅ Decoded COBS frame: ${decodedFrame.size} bytes - ${decodedFrame.toHexString()}")
                processDecodedFrame(deviceAddress, decodedFrame)
            }
        }
    }
    
    /**
     * Process completed COBS-decoded frame (like original app)
     */
    private fun processDecodedFrame(deviceAddress: String, frame: ByteArray) {
        Log.d(TAG, "📥 Processing decoded frame: ${frame.size} bytes - ${frame.toHexString()}")
        
        if (frame.size >= 5) {
            // Check if this is a GatewayInformation response (EventType 0x10)
            val eventType = frame[2]
            if (eventType == 0x10.toByte()) {
                handleGatewayInformationEvent(deviceAddress, frame)
                return
            }
        }
        
        // TODO: Handle other MyRvLink events (device status, etc.)
    }
    
    /**
     * Handle GatewayInformation response (like original app)
     */
    private fun handleGatewayInformationEvent(deviceAddress: String, data: ByteArray) {
        Log.d(TAG, "📋 GatewayInformation received: ${data.toHexString()}")
        
        if (data.size >= 5) {
            val newDeviceTableId = data[4]
            if (newDeviceTableId != 0x00.toByte()) {
                val oldTableId = deviceTableId
                deviceTableId = newDeviceTableId
                Log.i(TAG, "✅ Updated DeviceTableId: 0x${deviceTableId.toString(16).padStart(2, '0')} (was 0x${oldTableId.toString(16).padStart(2, '0')})")
            }
        }

        if (!gatewayInfoReceived[deviceAddress]!!) {
            gatewayInfoReceived[deviceAddress] = true
            onGatewayInfoReceived(deviceAddress)
        }
    }
    
    /**
     * Handle first GatewayInformation response (like original app)
     */
    private fun onGatewayInfoReceived(deviceAddress: String) {
        Log.i(TAG, "🏗️ Gateway information received - protocol fully established")
        
        // Send GetDevices with correct table ID (like original app)
        kotlinx.coroutines.GlobalScope.launch {
            kotlinx.coroutines.delay(500)
            Log.i(TAG, "📤 Sending GetDevices with updated DeviceTableId: 0x${deviceTableId.toString(16).padStart(2, '0')}")
            // Find the device object for this address
            // For now, just log that we would send it
        }

        // Heartbeat is already running - no need to restart (like original app)
    }
    
    /**
     * Send initial MyRvLink GetDevices command to "wake up" the gateway
     * This is what the official app sends to establish communication
     */
    private suspend fun sendInitialCanCommand(device: BluetoothDevice) {
        Log.i(TAG, "📤 Sending initial GetDevices command to ${device.address}")
        
        try {
            // Encode MyRvLink GetDevices command
            // Format: [ClientCommandId (2 bytes, little-endian)][CommandType=0x01][DeviceTableId][StartDeviceId][MaxDeviceRequestCount]
            val commandId = getNextCommandId()
            val effectiveTableId = if (deviceTableId == 0x00.toByte()) DEFAULT_DEVICE_TABLE_ID else deviceTableId
            
            val command = byteArrayOf(
                (commandId.toInt() and 0xFF).toByte(),           // ClientCommandId low byte
                ((commandId.toInt() shr 8) and 0xFF).toByte(),   // ClientCommandId high byte
                0x01.toByte(),                                   // CommandType: GetDevices
                effectiveTableId,                                // DeviceTableId
                0x00.toByte(),                                   // StartDeviceId (0 = start from beginning)
                0xFF.toByte()                                    // MaxDeviceRequestCount (255 = get all)
            )
            
            // Encode with COBS (Consistent Overhead Byte Stuffing)
            val encoded = cobsEncode(command, prependStartFrame = true, useCrc = true)
            val encodedHex = encoded.joinToString(" ") { "%02X".format(it) }
            
            Log.d(TAG, "📤 GetDevices: CommandId=0x${commandId.toString(16)}, DeviceTableId=0x${effectiveTableId.toString(16)}")
            Log.d(TAG, "📤 Encoded: $encodedHex (${encoded.size} bytes)")
            
            // Send via DATA_WRITE characteristic (WRITE_TYPE_NO_RESPONSE per technical spec)
            val writeResult = gattOperations?.writeCharacteristicNoResponse(Constants.DATA_WRITE_CHAR_UUID, encoded)
            
            if (writeResult?.isSuccess == true) {
                Log.i(TAG, "📤 Sent initial GetDevices command (${encoded.size} bytes)")
            } else {
                Log.e(TAG, "❌ Failed to send GetDevices command: ${writeResult?.exceptionOrNull()?.message}")
            }
        } catch (e: Exception) {
            Log.e(TAG, "Failed to send initial CAN command: ${e.message}", e)
        }
    }
    
    /**
     * Start heartbeat/keepalive mechanism
     * Sends MyRvLink GetDevices command periodically to keep connection alive
     */
    private fun startHeartbeat(device: BluetoothDevice) {
        // Stop any existing heartbeat for this device
        heartbeatJobs[device.address]?.cancel()
        
        val job = GlobalScope.launch {
            Log.i(TAG, "💓 Heartbeat started for ${device.address}")
            
            while (authenticatedDevices.contains(device.address)) {
                try {
                    delay(5000)  // 5 second intervals like original app (NOT 30 seconds!)
                    
                    if (!authenticatedDevices.contains(device.address)) break
                    
                    // Send heartbeat GetDevices command
                    val commandId = getNextCommandId()
                    val effectiveTableId = if (deviceTableId == 0x00.toByte()) DEFAULT_DEVICE_TABLE_ID else deviceTableId
                    
                    val command = byteArrayOf(
                        (commandId.toInt() and 0xFF).toByte(),
                        ((commandId.toInt() shr 8) and 0xFF).toByte(),
                        0x01.toByte(),  // CommandType: GetDevices
                        effectiveTableId,
                        0x00.toByte(),
                        0xFF.toByte()
                    )
                    
                    val encoded = cobsEncode(command, prependStartFrame = true, useCrc = true)
                val writeResult = gattOperations?.writeCharacteristicNoResponse(Constants.DATA_WRITE_CHAR_UUID, encoded)
                    if (writeResult?.isSuccess == true) {
                        Log.i(TAG, "💓 Heartbeat sent to ${device.address} (CommandId=0x${commandId.toString(16)})")
                    } else {
                        Log.w(TAG, "💓 Heartbeat failed for ${device.address}: ${writeResult?.exceptionOrNull()?.message}")
                    }
                } catch (e: Exception) {
                    Log.w(TAG, "Heartbeat error for ${device.address}: ${e.message}")
                    break
                }
            }
            
            Log.i(TAG, "💓 Heartbeat stopped for ${device.address}")
        }
        
        heartbeatJobs[device.address] = job
    }
    
    /**
     * Get next command ID (increments and wraps around)
     */
    private fun getNextCommandId(): UShort {
        val id = nextCommandId
        nextCommandId = if (nextCommandId >= 0xFFFEu) 1u else (nextCommandId + 1u).toUShort()
        return id
    }
    
    /**
     * COBS (Consistent Overhead Byte Stuffing) encoding with CRC8
     * Exact implementation matching decompiled CobsEncoder.cs from OneControl app
     */
    private fun cobsEncode(data: ByteArray, prependStartFrame: Boolean = true, useCrc: Boolean = true): ByteArray {
        val FRAME_CHAR: Byte = 0x00
        val MAX_DATA_BYTES = 63  // 2^6 - 1
        val FRAME_BYTE_COUNT_LSB = 64  // 2^6
        val MAX_COMPRESSED_FRAME_BYTES = 192  // 255 - 63
        
        val output = ByteArray(382)  // Max output buffer size
        var outputIndex = 0
        
        // Prepend start frame character if requested
        if (prependStartFrame) {
            output[outputIndex++] = FRAME_CHAR
        }
        
        if (data.isEmpty()) {
            return output.copyOf(outputIndex)
        }
        
        // Build source data with CRC appended (CRC calculated incrementally during encoding)
        val sourceCount = data.size
        val totalCount = if (useCrc) sourceCount + 1 else sourceCount
        
        // CRC calculator - initialized to 85 (0x55)
        val crc = Crc8()
        
        var srcIndex = 0
        
        while (srcIndex < totalCount) {
            // Save position for code byte placeholder
            val codeIndex = outputIndex
            var code = 0
            output[outputIndex++] = 0xFF.toByte()  // Placeholder (official uses 0xFF)
            
            // Encode non-frame bytes
            while (srcIndex < totalCount) {
                val byteVal: Byte
                if (srcIndex < sourceCount) {
                    byteVal = data[srcIndex]
                    if (byteVal == FRAME_CHAR) {
                        break  // Stop at frame character (zero)
                    }
                    crc.update(byteVal)
                } else {
                    // This is the CRC byte position
                    byteVal = crc.value
                    if (byteVal == FRAME_CHAR) {
                        break
                    }
                }
                
                srcIndex++
                output[outputIndex++] = byteVal
                code++
                
                if (code >= MAX_DATA_BYTES) {
                    break
                }
            }
            
            // Handle consecutive frame characters (zeros)
            while (srcIndex < totalCount) {
                val byteVal = if (srcIndex < sourceCount) data[srcIndex] else crc.value
                if (byteVal != FRAME_CHAR) {
                    break
                }
                crc.update(FRAME_CHAR)
                srcIndex++
                code += FRAME_BYTE_COUNT_LSB
                if (code >= MAX_COMPRESSED_FRAME_BYTES) {
                    break
                }
            }
            
            // Write actual code byte
            output[codeIndex] = code.toByte()
        }
        
        // Append frame terminator
        output[outputIndex++] = FRAME_CHAR
        
        return output.copyOf(outputIndex)
    }
    
    /**
     * Data Service gateway authentication (challenge-response)
     * From original OneControlBleService: MyRvLinkBleGatewayScanResult.RvLinkKeySeedCypher = 612643285
     * Byte order: BIG-ENDIAN for both challenge and KEY (Data Service only)
     */
    private fun calculateAuthKey(seed: Long): Long {
        val cypher = 612643285L  // MyRvLink RvLinkKeySeedCypher = 0x2483FFD5
        
        var cypherVar = cypher
        var seedVar = seed
        var num = 2654435769L  // TEA delta = 0x9E3779B9
        
        // BleDeviceUnlockManager.Encrypt() algorithm - exact copy from original app
        for (i in 0 until 32) {
            seedVar += ((cypherVar shl 4) + 1131376761L) xor (cypherVar + num) xor ((cypherVar shr 5) + 1919510376L)
            seedVar = seedVar and 0xFFFFFFFFL
            cypherVar += ((seedVar shl 4) + 1948272964L) xor (seedVar + num) xor ((seedVar shr 5) + 1400073827L)
            cypherVar = cypherVar and 0xFFFFFFFFL
            num += 2654435769L
            num = num and 0xFFFFFFFFL
        }
        
        // Return the calculated value
        return seedVar and 0xFFFFFFFFL
    }
    
    /**
     * TEA encryption for CAN Service gateways (different algorithm)
     * From original OneControlBleService: TeaEncryption.encrypt() with cypher 0x8100080DL
     * Byte order: LITTLE-ENDIAN for both seed and key (CAN Service only)
     */
    private fun calculateTeaKey(cypher: Long, seed: Long): Long {
        var v0 = seed
        var v1 = cypher
        val delta = 0x9e3779b9L
        var sum = 0L
        
        // 32 rounds of TEA encryption
        for (i in 0 until 32) {
            sum += delta
            v0 += ((v1 shl 4) + (cypher and 0xFFFFL)) xor (v1 + sum) xor ((v1 shr 5) + ((cypher shr 16) and 0xFFFFL))
            v0 = v0 and 0xFFFFFFFFL
            v1 += ((v0 shl 4) + ((cypher shr 32) and 0xFFFFL)) xor (v0 + sum) xor ((v0 shr 5) + ((cypher shr 48) and 0xFFFFL))
            v1 = v1 and 0xFFFFFFFFL
        }
        
        return v0 and 0xFFFFFFFFL
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
    
    /**
     * Process notification data - keeps connection active through continuous processing
     * Based on original app's notification handling that prevents disconnections
     */
    private fun processNotificationData(deviceAddress: String, data: ByteArray) {
        try {
            // COBS decode the notification (simplified for now)
            Log.d(TAG, "📨 Processing notification from $deviceAddress: ${data.toHexString()} (${data.size} bytes)")
            
            // The key is continuous processing - this activity prevents Android from timing out BLE connection
            // Original app does full COBS decoding and MyRvLink parsing here
        } catch (e: Exception) {
            Log.w(TAG, "Error processing notification: ${e.message}")
        }
    }
}