package com.blemqttbridge.plugins.onecontrol

import android.bluetooth.*
import android.bluetooth.le.ScanRecord
import android.content.Context
import android.os.Handler
import android.os.Looper
import android.util.Log
import com.blemqttbridge.core.interfaces.BleDevicePlugin
import com.blemqttbridge.core.interfaces.MqttPublisher
import com.blemqttbridge.core.interfaces.PluginConfig
import com.blemqttbridge.plugins.device.onecontrol.protocol.TeaEncryption
import com.blemqttbridge.plugins.device.onecontrol.protocol.Constants
import com.blemqttbridge.plugins.device.onecontrol.protocol.CobsByteDecoder
import com.blemqttbridge.plugins.device.onecontrol.protocol.CobsDecoder
import org.json.JSONObject
import java.util.*
import java.nio.ByteBuffer
import java.nio.ByteOrder
import java.util.concurrent.ConcurrentLinkedQueue

/**
 * OneControl BLE Device Plugin - NEW ARCHITECTURE
 * 
 * This plugin OWNS the BluetoothGattCallback for OneControl gateway devices.
 * It contains the complete, working code from the legacy android_ble_bridge app.
 * 
 * NO FORWARDING LAYER - this plugin directly handles all BLE callbacks.
 */
class OneControlDevicePlugin : BleDevicePlugin {
    
    companion object {
        private const val TAG = "OneControlDevicePlugin"
        const val PLUGIN_ID = "onecontrol_v2"
        const val PLUGIN_VERSION = "2.0.0"
        
        // OneControl UUIDs (from working legacy app)
        private val DATA_SERVICE_UUID = UUID.fromString("00000030-0200-a58e-e411-afe28044e62c")
        private val DATA_READ_CHARACTERISTIC_UUID = UUID.fromString("00000034-0200-a58e-e411-afe28044e62c")
        private val DATA_WRITE_CHARACTERISTIC_UUID = UUID.fromString("00000033-0200-a58e-e411-afe28044e62c")
        
        // Authentication service (for TEA encryption)
        private val AUTH_SERVICE_UUID = UUID.fromString("00000010-0200-a58e-e411-afe28044e62c")
        private val SEED_CHARACTERISTIC_UUID = UUID.fromString("00000011-0200-a58e-e411-afe28044e62c")
        private val UNLOCK_STATUS_CHARACTERISTIC_UUID = UUID.fromString("00000012-0200-a58e-e411-afe28044e62c")
        private val KEY_CHARACTERISTIC_UUID = UUID.fromString("00000013-0200-a58e-e411-afe28044e62c")
        
        // Device identification
        private const val DEVICE_NAME_PREFIX = "LCI"
    }
    
    override val pluginId: String = PLUGIN_ID
    override val displayName: String = "OneControl Gateway (v2)"
    
    private lateinit var context: Context
    private var config: PluginConfig? = null
    
    // Configuration from settings
    private var gatewayMac: String = "24:DC:C3:ED:1E:0A"
    private var gatewayPin: String = "090336"
    private var gatewayCypher: Long = 0x8100080DL
    
    // Strong reference to callback to prevent GC
    private var gattCallback: BluetoothGattCallback? = null
    
    override fun initialize(context: Context, config: PluginConfig) {
        Log.i(TAG, "Initializing OneControl Device Plugin v$PLUGIN_VERSION")
        this.context = context
        this.config = config
        
        // Load configuration
        gatewayMac = config.getString("gateway_mac", gatewayMac)
        gatewayPin = config.getString("gateway_pin", gatewayPin)
        config.getString("gateway_cypher").toLongOrNull()?.let { gatewayCypher = it }
        
        Log.i(TAG, "Configured for gateway: $gatewayMac")
    }
    
    override fun matchesDevice(
        device: BluetoothDevice,
        scanRecord: ScanRecord?
    ): Boolean {
        val deviceAddress = device.address
        val deviceName = device.name
        // Match by MAC address if configured
        if (deviceAddress.equals(gatewayMac, ignoreCase = true)) {
            Log.d(TAG, "Device matched by MAC: $deviceAddress")
            return true
        }
        
        // Match by name prefix
        if (deviceName?.startsWith(DEVICE_NAME_PREFIX) == true) {
            Log.d(TAG, "Device matched by name: $deviceName")
            return true
        }
        
        // Match by advertised service UUID
        val advertisedServices = scanRecord?.serviceUuids
        if (advertisedServices != null) {
            for (uuid in advertisedServices) {
                if (uuid.uuid == DATA_SERVICE_UUID || uuid.uuid == AUTH_SERVICE_UUID) {
                    Log.d(TAG, "Device matched by service UUID: ${uuid.uuid}")
                    return true
                }
            }
        }
        
        return false
    }
    
    override fun getConfiguredDevices(): List<String> {
        return listOf(gatewayMac)
    }
    
    override fun createGattCallback(
        device: BluetoothDevice,
        context: Context,
        mqttPublisher: MqttPublisher,
        onDisconnect: (BluetoothDevice, Int) -> Unit
    ): BluetoothGattCallback {
        Log.i(TAG, "Creating GATT callback for ${device.address}")
        val callback = OneControlGattCallback(device, context, mqttPublisher, onDisconnect, gatewayPin, gatewayCypher)
        Log.i(TAG, "Created callback with hashCode=${callback.hashCode()}")
        // Keep strong reference to prevent GC
        gattCallback = callback
        return callback
    }
    
    override fun onGattConnected(device: BluetoothDevice, gatt: BluetoothGatt) {
        Log.i(TAG, "GATT connected for ${device.address}")
        // Callback handles everything - nothing needed here
    }
    
    override fun onDeviceDisconnected(device: BluetoothDevice) {
        Log.i(TAG, "Device disconnected: ${device.address}")
        // Cleanup if needed
    }
    
    override fun getMqttBaseTopic(device: BluetoothDevice): String {
        return "onecontrol/${device.address}"
    }
    
    override fun getDiscoveryPayloads(device: BluetoothDevice): List<Pair<String, String>> {
        // Discovery will be done by the callback when devices are enumerated
        return emptyList()
    }
    
    override suspend fun handleCommand(device: BluetoothDevice, commandTopic: String, payload: String): Result<Unit> {
        Log.w(TAG, "Command handling not yet implemented: $commandTopic")
        return Result.failure(Exception("Not implemented"))
    }
    
    override fun destroy() {
        Log.i(TAG, "Destroying OneControl Device Plugin")
    }
}

/**
 * OneControl GATT Callback - contains the COMPLETE working code from legacy app.
 * 
 * This is a DIRECT COPY of the callback logic that works in android_ble_bridge.
 * Includes: notification handling, stream reading, COBS decoding, event processing.
 */
class OneControlGattCallback(
    private val device: BluetoothDevice,
    private val context: Context,
    private val mqttPublisher: MqttPublisher,
    private val onDisconnect: (BluetoothDevice, Int) -> Unit,
    private val gatewayPin: String,
    private val gatewayCypher: Long
) : BluetoothGattCallback() {
    
    companion object {
        private const val TAG = "OneControlGattCallback"
        
        // UUIDs - COPIED DIRECTLY FROM LEGACY APP Constants.kt
        private val DATA_SERVICE_UUID = UUID.fromString("00000030-0200-a58e-e411-afe28044e62c")
        private val DATA_WRITE_CHARACTERISTIC_UUID = UUID.fromString("00000033-0200-a58e-e411-afe28044e62c")
        private val DATA_READ_CHARACTERISTIC_UUID = UUID.fromString("00000034-0200-a58e-e411-afe28044e62c")
        private val AUTH_SERVICE_UUID = UUID.fromString("00000010-0200-a58e-e411-afe28044e62c")
        private val SEED_CHARACTERISTIC_UUID = UUID.fromString("00000011-0200-a58e-e411-afe28044e62c")
        private val UNLOCK_STATUS_CHARACTERISTIC_UUID = UUID.fromString("00000012-0200-a58e-e411-afe28044e62c")
        private val KEY_CHARACTERISTIC_UUID = UUID.fromString("00000013-0200-a58e-e411-afe28044e62c")
        private val AUTH_STATUS_CHARACTERISTIC_UUID = UUID.fromString("00000014-0200-a58e-e411-afe28044e62c")
        
        // Descriptor UUID for enabling notifications
        private val CLIENT_CHARACTERISTIC_CONFIG_UUID = UUID.fromString("00002902-0000-1000-8000-00805f9b34fb")
        
        // Timing constants from legacy app
        private const val HEARTBEAT_INTERVAL_MS = 5000L
        private const val DEFAULT_DEVICE_TABLE_ID: Byte = 0x08
        
        // MTU size from legacy app (Constants.BLE_MTU_SIZE)
        private const val BLE_MTU_SIZE = 185
    }
    
    // Handler for main thread operations
    private val handler = Handler(Looper.getMainLooper())
    
    // Connection state
    private var isConnected = false
    private var isAuthenticated = false
    private var seedValue: ByteArray? = null
    private var currentGatt: BluetoothGatt? = null
    
    // Characteristic references
    private var dataReadChar: BluetoothGattCharacteristic? = null
    private var dataWriteChar: BluetoothGattCharacteristic? = null
    private var seedChar: BluetoothGattCharacteristic? = null
    private var unlockStatusChar: BluetoothGattCharacteristic? = null
    private var keyChar: BluetoothGattCharacteristic? = null
    
    // Notification subscription tracking (from legacy app)
    private var notificationSubscriptionsPending = 0
    private var allNotificationsSubscribed = false
    
    // Stream reading infrastructure (from legacy app)
    private val notificationQueue = ConcurrentLinkedQueue<ByteArray>()
    private var streamReadingThread: Thread? = null
    private var shouldStopStreamReading = false
    private var isStreamReadingActive = false
    private val cobsByteDecoder = CobsByteDecoder(useCrc = true)
    private val streamReadLock = Object()
    
    // MyRvLink command tracking (from legacy app)
    private var nextCommandId: UShort = 1u
    private var deviceTableId: Byte = 0x00
    private var gatewayInfoReceived = false
    
    // Heartbeat
    private var heartbeatRunnable: Runnable? = null
    
    override fun onConnectionStateChange(gatt: BluetoothGatt, status: Int, newState: Int) {
        Log.i(TAG, "🔌 Connection state changed: status=$status, newState=$newState, callback=${this.hashCode()}")
        
        when (status) {
            BluetoothGatt.GATT_SUCCESS -> {
                when (newState) {
                    BluetoothProfile.STATE_CONNECTED -> {
                        Log.i(TAG, "✅ Connected to ${device.address}, callback=${this.hashCode()}")
                        Log.i(TAG, "Bond state: ${device.bondState}")
                        isConnected = true
                        currentGatt = gatt
                        
                        // Discover services
                        gatt.discoverServices()
                    }
                    BluetoothProfile.STATE_DISCONNECTED -> {
                        Log.i(TAG, "❌ Disconnected from ${device.address}")
                        cleanup(gatt)
                        onDisconnect(device, status)
                    }
                }
            }
            133 -> {
                Log.e(TAG, "⚠️ GATT_ERROR (133) - Stale bond / link key mismatch")
                cleanup(gatt)
                onDisconnect(device, status)
            }
            8 -> {
                Log.e(TAG, "⏱️ Connection timeout (status 8)")
                cleanup(gatt)
                onDisconnect(device, status)
            }
            19 -> {
                Log.e(TAG, "🚫 Peer terminated connection (status 19)")
                cleanup(gatt)
                onDisconnect(device, status)
            }
            else -> {
                Log.e(TAG, "❌ Connection failed with status: $status")
                cleanup(gatt)
                onDisconnect(device, status)
            }
        }
    }
    
    override fun onMtuChanged(gatt: BluetoothGatt, mtu: Int, status: Int) {
        if (status == BluetoothGatt.GATT_SUCCESS) {
            Log.i(TAG, "✅ MTU changed to $mtu")
        } else {
            Log.w(TAG, "⚠️ MTU change failed: status=$status")
        }
        
        // After MTU exchange, start challenge-response authentication
        // From AUTHENTICATION_ALGORITHM.md: Read challenge, calculate KEY, write KEY
        Log.i(TAG, "🔑 Starting authentication sequence after MTU exchange...")
        startAuthentication(gatt)
    }
    
    // Track if notifications have been enabled to avoid duplicates
    private var notificationsEnableStarted = false
    
    override fun onServicesDiscovered(gatt: BluetoothGatt, status: Int) {
        Log.i(TAG, "📋 Services discovered: status=$status, gatt=${gatt.hashCode()}, currentGatt=${currentGatt?.hashCode()}")
        
        if (status != BluetoothGatt.GATT_SUCCESS) {
            Log.e(TAG, "Service discovery failed")
            return
        }
        
        // Log all services for debugging
        for (service in gatt.services) {
            Log.i(TAG, "  📦 Service: ${service.uuid}")
        }
        
        // Find Auth service
        val authService = gatt.getService(AUTH_SERVICE_UUID)
        if (authService != null) {
            Log.i(TAG, "✅ Found auth service")
            seedChar = authService.getCharacteristic(SEED_CHARACTERISTIC_UUID)
            unlockStatusChar = authService.getCharacteristic(UNLOCK_STATUS_CHARACTERISTIC_UUID)
            keyChar = authService.getCharacteristic(KEY_CHARACTERISTIC_UUID)
            if (seedChar != null) Log.i(TAG, "✅ Found seed characteristic (00000011)")
            if (unlockStatusChar != null) Log.i(TAG, "✅ Found unlock status characteristic (00000012)")
            if (keyChar != null) Log.i(TAG, "✅ Found key characteristic (00000013)")
        }
        
        // Find Data service
        val dataService = gatt.getService(DATA_SERVICE_UUID)
        if (dataService != null) {
            Log.i(TAG, "✅ Found data service")
            dataWriteChar = dataService.getCharacteristic(DATA_WRITE_CHARACTERISTIC_UUID)
            dataReadChar = dataService.getCharacteristic(DATA_READ_CHARACTERISTIC_UUID)
            if (dataWriteChar != null) Log.i(TAG, "✅ Found data write characteristic")
            if (dataReadChar != null) Log.i(TAG, "✅ Found data read characteristic")
        } else {
            Log.e(TAG, "❌ Data service not found!")
            return
        }
        
        // Request MTU - the onMtuChanged callback will then write KEY and enable notifications
        Log.i(TAG, "📐 Requesting MTU size $BLE_MTU_SIZE...")
        gatt.requestMtu(BLE_MTU_SIZE)
    }
    
    /**
     * Calculate authentication KEY from challenge using BleDeviceUnlockManager.Encrypt() algorithm
     * From decompiled code: MyRvLinkBleGatewayScanResult.RvLinkKeySeedCypher = 612643285
     * COPIED DIRECTLY FROM LEGACY APP
     * Byte order: BIG-ENDIAN for both challenge and KEY
     */
    private fun calculateAuthKey(seed: Long): ByteArray {
        val cypher = 612643285L  // MyRvLink RvLinkKeySeedCypher = 0x2483FFD5
        
        var cypherVar = cypher
        var seedVar = seed
        var num = 2654435769L  // TEA delta = 0x9E3779B9
        
        // BleDeviceUnlockManager.Encrypt() algorithm
        for (i in 0 until 32) {
            seedVar += ((cypherVar shl 4) + 1131376761L) xor (cypherVar + num) xor ((cypherVar shr 5) + 1919510376L)
            seedVar = seedVar and 0xFFFFFFFFL
            cypherVar += ((seedVar shl 4) + 1948272964L) xor (seedVar + num) xor ((seedVar shr 5) + 1400073827L)
            cypherVar = cypherVar and 0xFFFFFFFFL
            num += 2654435769L
            num = num and 0xFFFFFFFFL
        }
        
        // Return as BIG-ENDIAN bytes (as per legacy app)
        val result = seedVar.toInt()
        return byteArrayOf(
            ((result shr 24) and 0xFF).toByte(),
            ((result shr 16) and 0xFF).toByte(),
            ((result shr 8) and 0xFF).toByte(),
            ((result shr 0) and 0xFF).toByte()
        )
    }
    
    /**
     * Start authentication flow:
     * 1. Read UNLOCK_STATUS (00000012) to get challenge value
     * 2. Calculate KEY using calculateAuthKey (BIG-ENDIAN)
     * 3. Write KEY to 00000013 with WRITE_TYPE_NO_RESPONSE
     * 4. Read UNLOCK_STATUS again to verify "Unlocked"
     * 5. Enable notifications
     */
    private fun startAuthentication(gatt: BluetoothGatt) {
        val unlockStatusCharLocal = unlockStatusChar
        if (unlockStatusCharLocal == null) {
            Log.w(TAG, "⚠️ UNLOCK_STATUS characteristic (00000012) not found - trying direct notification enable")
            enableDataNotifications(gatt)
            return
        }
        
        Log.i(TAG, "🔑 Step 1: Reading UNLOCK_STATUS (00000012) to get challenge value...")
        gatt.readCharacteristic(unlockStatusCharLocal)
        // Response handled in onCharacteristicRead
    }
    
    /**
     * Enable notifications on DATA_READ and Auth Service characteristics
     * COPIED FROM LEGACY APP - uses parallel writes with delays (NOT sequential queue)
     */
    private fun enableDataNotifications(gatt: BluetoothGatt) {
        // Prevent duplicate calls from callback + fallback timer
        if (notificationsEnableStarted) {
            Log.d(TAG, "📝 enableDataNotifications already started, skipping")
            return
        }
        notificationsEnableStarted = true
        
        notificationSubscriptionsPending = 0
        allNotificationsSubscribed = false
        
        // Subscribe to Data Read (00000034) - main data channel
        dataReadChar?.let { char ->
            try {
                val props = char.properties
                val hasNotify = (props and BluetoothGattCharacteristic.PROPERTY_NOTIFY) != 0
                val hasIndicate = (props and BluetoothGattCharacteristic.PROPERTY_INDICATE) != 0
                Log.i(TAG, "📝 Enabling notifications for Data read (${char.uuid})")
                Log.i(TAG, "📝 Characteristic properties: 0x${props.toString(16)} (NOTIFY=$hasNotify, INDICATE=$hasIndicate)")
                Log.i(TAG, "📝 Characteristic instanceId: ${char.instanceId}, service: ${char.service.uuid}")
                val notifyResult = gatt.setCharacteristicNotification(char, true)
                Log.i(TAG, "📝 setCharacteristicNotification result: $notifyResult")
                Log.i(TAG, "📝 gatt instance: ${gatt.device?.address}, connected: ${gatt.device?.address != null}")
                
                // Increment pending count BEFORE posting the delayed handler to avoid race condition
                notificationSubscriptionsPending++
                Log.i(TAG, "📝 Queued Data read notification subscription (pending: $notificationSubscriptionsPending)")
                
                // Small delay before writing descriptor (BLE stack needs time to process setCharacteristicNotification)
                handler.postDelayed({
                    val descriptor = char.getDescriptor(CLIENT_CHARACTERISTIC_CONFIG_UUID)
                    if (descriptor != null) {
                        descriptor.value = BluetoothGattDescriptor.ENABLE_NOTIFICATION_VALUE
                        if (gatt.writeDescriptor(descriptor) == true) {
                            Log.i(TAG, "✅ Subscribing to Data read notifications")
                        } else {
                            Log.e(TAG, "❌ Failed to write descriptor for Data read - writeDescriptor returned false, retrying...")
                            // Retry once after another delay
                            handler.postDelayed({
                                if (gatt.writeDescriptor(descriptor) == true) {
                                    Log.i(TAG, "✅ Retry successful: Subscribing to Data read notifications")
                                } else {
                                    Log.e(TAG, "❌ Retry also failed for Data read descriptor write")
                                    // Decrement since we're giving up
                                    notificationSubscriptionsPending--
                                }
                            }, 200)
                        }
                    } else {
                        Log.e(TAG, "❌ Descriptor not found for Data read")
                    }
                }, 100)  // 100ms delay after setCharacteristicNotification
            } catch (e: Exception) {
                Log.e(TAG, "Failed to subscribe to Data read notifications: ${e.message}", e)
            }
        }
        
        // Subscribe to Auth Service characteristics (00000011, 00000014)
        // COPIED FROM LEGACY APP - parallel writes with delays
        val authService = gatt.getService(AUTH_SERVICE_UUID)
        authService?.let { service ->
            // Subscribe to 00000011 (SEED - READ, NOTIFY)
            val char11 = service.getCharacteristic(SEED_CHARACTERISTIC_UUID)
            char11?.let {
                try {
                    gatt.setCharacteristicNotification(it, true)
                    val descriptor = it.getDescriptor(CLIENT_CHARACTERISTIC_CONFIG_UUID)
                    descriptor?.value = BluetoothGattDescriptor.ENABLE_NOTIFICATION_VALUE
                    if (gatt.writeDescriptor(descriptor) == true) {
                        notificationSubscriptionsPending++
                        Log.i(TAG, "📝 Subscribing to Auth Service 00000011/SEED (pending: $notificationSubscriptionsPending)")
                    } else {
                        Log.w(TAG, "Failed to write descriptor for 00000011")
                    }
                } catch (e: Exception) {
                    Log.w(TAG, "Failed to subscribe to 00000011: ${e.message}")
                }
            }
            
            // Subscribe to 00000014 (READ, NOTIFY) - with delay like legacy app
            handler.postDelayed({
                val char14 = service.getCharacteristic(AUTH_STATUS_CHARACTERISTIC_UUID)
                char14?.let {
                    try {
                        gatt.setCharacteristicNotification(it, true)
                        val descriptor = it.getDescriptor(CLIENT_CHARACTERISTIC_CONFIG_UUID)
                        descriptor?.value = BluetoothGattDescriptor.ENABLE_NOTIFICATION_VALUE
                        if (gatt.writeDescriptor(descriptor) == true) {
                            notificationSubscriptionsPending++
                            Log.i(TAG, "📝 Subscribing to Auth Service 00000014 (pending: $notificationSubscriptionsPending)")
                        } else {
                            Log.w(TAG, "Failed to write descriptor for 00000014")
                        }
                    } catch (e: Exception) {
                        Log.w(TAG, "Failed to subscribe to 00000014: ${e.message}")
                    }
                }
            }, 150)  // Small delay between subscription requests - matches legacy app
        }
        
        // If no subscriptions were initiated, mark as complete
        if (notificationSubscriptionsPending == 0) {
            allNotificationsSubscribed = true
            Log.w(TAG, "⚠️ No notification subscriptions initiated")
            onAllNotificationsSubscribed()
        }
    }
    
    /**
     * onDescriptorWrite callback - COPIED FROM LEGACY APP
     * Tracks pending subscriptions and triggers onAllNotificationsSubscribed when done
     */
    override fun onDescriptorWrite(gatt: BluetoothGatt, descriptor: BluetoothGattDescriptor, status: Int) {
        val charUuid = descriptor.characteristic.uuid.toString().lowercase()
        val descriptorUuid = descriptor.uuid.toString().lowercase()
        if (status == BluetoothGatt.GATT_SUCCESS) {
            Log.i(TAG, "✅ Descriptor write successful for $charUuid (descriptor: $descriptorUuid, pending: $notificationSubscriptionsPending)")
            
            notificationSubscriptionsPending--
            if (notificationSubscriptionsPending <= 0 && !allNotificationsSubscribed) {
                allNotificationsSubscribed = true
                Log.i(TAG, "✅ All notification subscriptions complete")
                onAllNotificationsSubscribed()
            } else {
                Log.d(TAG, "  → Still waiting for ${notificationSubscriptionsPending} more descriptor writes...")
            }
        } else {
            val errorMsg = when (status) {
                133 -> "GATT_INTERNAL_ERROR (0x85)"
                5 -> "GATT_INSUFFICIENT_AUTHENTICATION"
                15 -> "GATT_INSUFFICIENT_ENCRYPTION"
                else -> "Error: $status (0x${status.toString(16)})"
            }
            Log.e(TAG, "❌ Descriptor write failed for $charUuid: $errorMsg (descriptor: $descriptorUuid, pending: $notificationSubscriptionsPending)")
            notificationSubscriptionsPending--
            // If all pending writes are done (even if some failed), proceed
            if (notificationSubscriptionsPending <= 0 && !allNotificationsSubscribed) {
                allNotificationsSubscribed = true
                Log.w(TAG, "⚠️ Some descriptor writes failed, but proceeding anyway")
                onAllNotificationsSubscribed()
            }
        }
    }
    
    /**
     * Handle characteristic read response - used for authentication flow
     */
    override fun onCharacteristicRead(gatt: BluetoothGatt, characteristic: BluetoothGattCharacteristic, status: Int) {
        val uuid = characteristic.uuid
        val data = characteristic.value
        
        Log.i(TAG, "📖 onCharacteristicRead: $uuid, status=$status, ${data?.size ?: 0} bytes")
        
        if (status != BluetoothGatt.GATT_SUCCESS) {
            Log.e(TAG, "❌ Characteristic read failed: status=$status")
            return
        }
        
        if (data == null || data.isEmpty()) {
            Log.w(TAG, "⚠️ Empty data from characteristic read")
            return
        }
        
        val hex = data.joinToString(" ") { "%02X".format(it) }
        Log.i(TAG, "📖 Read data: $hex")
        
        when (uuid) {
            UNLOCK_STATUS_CHARACTERISTIC_UUID -> {
                handleUnlockStatusRead(gatt, data)
            }
            SEED_CHARACTERISTIC_UUID -> {
                // Legacy path - not used for Data Service gateway
                Log.d(TAG, "📖 SEED read (not used for Data Service): ${data.joinToString(" ") { "%02X".format(it) }}")
            }
            else -> {
                Log.d(TAG, "📖 Unhandled characteristic read: $uuid")
            }
        }
    }
    
    /**
     * Handle UNLOCK_STATUS read response - either challenge or "Unlocked" status
     * COPIED FROM LEGACY APP - Data Service authentication flow
     */
    private fun handleUnlockStatusRead(gatt: BluetoothGatt, data: ByteArray) {
        // Check if this is the "Unlocked" response (text)
        val unlockStatus = try {
            String(data, Charsets.UTF_8)
        } catch (e: Exception) {
            data.joinToString(" ") { "%02X".format(it) }
        }
        Log.i(TAG, "📖 Unlock status (00000012): $unlockStatus (${data.size} bytes)")
        
        if (unlockStatus.contains("Unlocked", ignoreCase = true)) {
            // Auth successful!
            Log.i(TAG, "✅ Gateway confirms UNLOCKED - authentication complete!")
            isAuthenticated = true
            
            // Now enable notifications and start communication
            handler.postDelayed({
                currentGatt?.let { enableDataNotifications(it) }
            }, 200)
        } else if (data.size == 4) {
            // This is the challenge! Calculate and write KEY response
            val challenge = data.joinToString(" ") { "%02X".format(it) }
            Log.i(TAG, "🔑 Step 2: Received challenge: $challenge")
            
            // Calculate KEY using BleDeviceUnlockManager.Encrypt() algorithm
            // Byte order: BIG-ENDIAN for challenge parsing
            val seedBigEndian = ((data[0].toInt() and 0xFF) shl 24) or
                               ((data[1].toInt() and 0xFF) shl 16) or
                               ((data[2].toInt() and 0xFF) shl 8) or
                               ((data[3].toInt() and 0xFF) shl 0)
            val keyValue = calculateAuthKey(seedBigEndian.toLong() and 0xFFFFFFFFL)
            
            val keyCharLocal = keyChar
            if (keyCharLocal != null) {
                keyCharLocal.value = keyValue
                // CRITICAL: Must use WRITE_TYPE_NO_RESPONSE (as per legacy app)
                keyCharLocal.writeType = BluetoothGattCharacteristic.WRITE_TYPE_NO_RESPONSE
                val writeResult = gatt.writeCharacteristic(keyCharLocal)
                val keyHex = keyValue.joinToString(" ") { "%02X".format(it) }
                Log.i(TAG, "🔑 Step 3: KEY write: $writeResult, value: $keyHex")
                
                // Step 4: Read unlock status again to verify
                handler.postDelayed({
                    unlockStatusChar?.let { unlockChar ->
                        Log.i(TAG, "🔑 Step 4: Reading unlock status to verify...")
                        gatt.readCharacteristic(unlockChar)
                    }
                }, 500)
            } else {
                Log.e(TAG, "❌ KEY characteristic not found!")
                enableDataNotifications(gatt)
            }
        } else {
            Log.w(TAG, "⚠️ Gateway not unlocked, unexpected response size: ${data.size} bytes")
            // Try to proceed anyway
            handler.postDelayed({
                currentGatt?.let { enableDataNotifications(it) }
            }, 200)
        }
    }
    
    /**
     * Called when all notifications are subscribed
     * COPIED FROM LEGACY APP - starts stream reading and sends initial command
     */
    private fun onAllNotificationsSubscribed() {
        Log.i(TAG, "✅ All notifications enabled - starting stream reader and initial command")
        
        // Mark as authenticated for Data Service gateway (no TEA auth needed)
        isAuthenticated = true
        
        // Start stream reading thread
        startActiveStreamReading()
        
        // Send initial GetDevices command after small delay
        handler.postDelayed({
            Log.i(TAG, "📤 Sending initial GetDevices to wake up gateway")
            sendGetDevicesCommand()
            
            // Start heartbeat
            startHeartbeat()
        }, 500)
        
        // Publish ready state to MQTT
        mqttPublisher.publishState("onecontrol/${device.address}/status", "ready", true)
    }
    
    // Android 13+ (API 33+) uses this signature
    @Suppress("OVERRIDE_DEPRECATION")
    override fun onCharacteristicChanged(
        gatt: BluetoothGatt,
        characteristic: BluetoothGattCharacteristic,
        value: ByteArray
    ) {
        Log.i(TAG, "📨📨📨 onCharacteristicChanged (API33+): ${characteristic.uuid}, ${value.size} bytes, callback=${this.hashCode()}")
        handleCharacteristicNotification(characteristic.uuid, value)
    }
    
    // Older Android versions use this signature
    @Deprecated("Deprecated in API 33")
    override fun onCharacteristicChanged(
        gatt: BluetoothGatt,
        characteristic: BluetoothGattCharacteristic
    ) {
        val data = characteristic.value
        Log.i(TAG, "📨📨📨 onCharacteristicChanged (legacy): ${characteristic.uuid}, ${data?.size ?: 0} bytes, callback=${this.hashCode()}")
        if (data != null) {
            handleCharacteristicNotification(characteristic.uuid, data)
        }
    }
    
    /**
     * Handle characteristic notification
     * COPIED FROM LEGACY APP - queues data for stream reading
     */
    private fun handleCharacteristicNotification(uuid: UUID, data: ByteArray) {
        if (data.isEmpty()) {
            Log.w(TAG, "📨 Empty notification from $uuid")
            return
        }
        
        val hex = data.joinToString(" ") { "%02X".format(it) }
        Log.i(TAG, "📨 Notification from $uuid: ${data.size} bytes")
        Log.d(TAG, "📨 Data: $hex")
        
        when (uuid) {
            DATA_READ_CHARACTERISTIC_UUID -> {
                // Queue for stream reading (like official app)
                notificationQueue.offer(data)
                synchronized(streamReadLock) {
                    streamReadLock.notify()
                }
            }
            SEED_CHARACTERISTIC_UUID -> {
                Log.i(TAG, "🌱 SEED notification received")
                handleSeedNotification(data)
            }
            KEY_CHARACTERISTIC_UUID -> {
                Log.i(TAG, "🔐 KEY (00000013) notification received: $hex")
                // Check if this is "Unlocked" response
                val text = String(data, Charsets.US_ASCII)
                if (text.contains("Unlocked", ignoreCase = true)) {
                    Log.i(TAG, "✅ Gateway confirms UNLOCKED - authentication complete!")
                    isAuthenticated = true
                }
            }
            AUTH_STATUS_CHARACTERISTIC_UUID -> {
                Log.i(TAG, "🔐 Auth Status (14) notification: $hex")
            }
            else -> {
                Log.d(TAG, "📨 Unknown characteristic: $uuid")
            }
        }
    }
    
    override fun onCharacteristicWrite(gatt: BluetoothGatt, characteristic: BluetoothGattCharacteristic, status: Int) {
        val uuid = characteristic.uuid.toString().lowercase()
        Log.i(TAG, "📝 onCharacteristicWrite: $uuid, status=$status")
        
        if (status == BluetoothGatt.GATT_SUCCESS) {
            Log.i(TAG, "✅ Write successful to $uuid")
            
            // After KEY write, handleUnlockStatusRead will re-read UNLOCK_STATUS to verify
            // Don't call enableDataNotifications here - let the verify step do it
            if (characteristic.uuid == KEY_CHARACTERISTIC_UUID) {
                Log.i(TAG, "✅ KEY write complete - waiting for UNLOCK_STATUS verify read...")
                // Note: The re-read is already scheduled in handleUnlockStatusRead
            }
        } else {
            Log.e(TAG, "❌ Write failed to $uuid: status=$status")
            // If KEY write fails, skip verification and try to enable notifications anyway
            if (characteristic.uuid == KEY_CHARACTERISTIC_UUID) {
                Log.w(TAG, "⚠️ KEY write failed, skipping verification, attempting notifications...")
                handler.postDelayed({
                    enableDataNotifications(gatt)
                }, 100)
            }
        }
    }
    
    /**
     * Start active stream reading loop
     * COPIED FROM LEGACY APP - processes notification queue and decodes COBS frames
     */
    private fun startActiveStreamReading() {
        if (isStreamReadingActive) {
            Log.d(TAG, "🔄 Stream reading already active, skipping")
            return
        }
        
        stopActiveStreamReading()
        
        isStreamReadingActive = true
        shouldStopStreamReading = false
        
        streamReadingThread = Thread {
            Log.i(TAG, "🔄 Active stream reading started")
            
            while (!shouldStopStreamReading && isConnected) {
                try {
                    // Wait for data with 8-second timeout (like official app)
                    var hasData = false
                    synchronized(streamReadLock) {
                        if (notificationQueue.isEmpty()) {
                            streamReadLock.wait(8000)
                        }
                        hasData = notificationQueue.isNotEmpty()
                    }
                    
                    if (!hasData) {
                        if (!isConnected || shouldStopStreamReading) {
                            continue
                        }
                        Thread.sleep(250)
                        continue
                    }
                    
                    // Process all queued notification packets
                    while (notificationQueue.isNotEmpty() && !shouldStopStreamReading) {
                        val notificationData = notificationQueue.poll() ?: continue
                        
                        Log.i(TAG, "📥 Processing queued notification: ${notificationData.size} bytes")
                        
                        // Feed bytes one at a time to COBS decoder
                        for (byte in notificationData) {
                            val decodedFrame = cobsByteDecoder.decodeByte(byte)
                            if (decodedFrame != null) {
                                Log.i(TAG, "✅ Decoded COBS frame: ${decodedFrame.size} bytes")
                                processDecodedFrame(decodedFrame)
                            }
                        }
                    }
                } catch (e: InterruptedException) {
                    Log.d(TAG, "Stream reading thread interrupted")
                    break
                } catch (e: Exception) {
                    Log.e(TAG, "Error in stream reading loop: ${e.message}", e)
                }
            }
            
            isStreamReadingActive = false
            Log.i(TAG, "🔄 Active stream reading stopped")
        }.apply {
            name = "OneControlStreamReader"
            isDaemon = true
            start()
        }
    }
    
    private fun stopActiveStreamReading() {
        isStreamReadingActive = false
        shouldStopStreamReading = true
        synchronized(streamReadLock) {
            streamReadLock.notify()
        }
        streamReadingThread?.interrupt()
        streamReadingThread?.join(1000)
        streamReadingThread = null
        notificationQueue.clear()
        cobsByteDecoder.reset()
    }
    
    /**
     * Process a decoded COBS frame
     * COPIED FROM LEGACY APP - handles MyRvLink events and command responses
     */
    private fun processDecodedFrame(decodedFrame: ByteArray) {
        if (decodedFrame.isEmpty()) return
        
        val hex = decodedFrame.joinToString(" ") { "%02X".format(it) }
        Log.d(TAG, "📦 Processing decoded frame: ${decodedFrame.size} bytes - $hex")
        
        // Try to decode as MyRvLink event first
        val eventType = decodedFrame[0].toInt() and 0xFF
        
        when (eventType) {
            0x01 -> {
                // GatewayInformation event
                Log.i(TAG, "📦 GatewayInformation event received!")
                handleGatewayInformationEvent(decodedFrame)
            }
            0x03 -> {
                // DeviceOnlineStatus
                Log.i(TAG, "📦 DeviceOnlineStatus event")
                handleDeviceOnlineStatus(decodedFrame)
            }
            0x05, 0x06 -> {
                // RelayBasicLatchingStatus (Type1 or Type2)
                Log.i(TAG, "📦 RelayBasicLatchingStatus event")
                handleRelayStatus(decodedFrame)
            }
            0x08 -> {
                // DimmableLightStatus
                Log.i(TAG, "📦 DimmableLightStatus event")
                handleDimmableLightStatus(decodedFrame)
            }
            0x0B -> {
                // HvacStatus
                Log.i(TAG, "📦 HvacStatus event")
                handleHvacStatus(decodedFrame)
            }
            0x0C -> {
                // TankSensorStatus
                Log.i(TAG, "📦 TankSensorStatus event")
                handleTankStatus(decodedFrame)
            }
            0x1B -> {
                // TankSensorStatusV2
                Log.i(TAG, "📦 TankSensorStatusV2 event")
                handleTankStatusV2(decodedFrame)
            }
            else -> {
                // Check if it's a command response
                if (isCommandResponse(decodedFrame)) {
                    handleCommandResponse(decodedFrame)
                } else {
                    Log.d(TAG, "📦 Unknown event type: 0x${eventType.toString(16)}")
                }
            }
        }
    }
    
    /**
     * Check if data looks like a command response
     */
    private fun isCommandResponse(data: ByteArray): Boolean {
        if (data.size < 3) return false
        val commandId = ((data[1].toInt() and 0xFF) shl 8) or (data[0].toInt() and 0xFF)
        if (commandId !in 1..0xFFFE) return false
        val commandType = data[2].toInt() and 0xFF
        return commandType == 0x01 || commandType == 0x02
    }
    
    /**
     * Handle GatewayInformation event
     */
    private fun handleGatewayInformationEvent(data: ByteArray) {
        Log.i(TAG, "📦 GatewayInformation: ${data.size} bytes")
        
        if (data.size >= 2) {
            deviceTableId = data[1]
            gatewayInfoReceived = true
            Log.i(TAG, "📦 Device Table ID: 0x${deviceTableId.toString(16)}")
        }
        
        // Publish to MQTT
        val json = JSONObject().apply {
            put("event", "gateway_information")
            put("device_table_id", deviceTableId.toInt() and 0xFF)
        }
        mqttPublisher.publishState("onecontrol/${device.address}/gateway", json.toString(), true)
    }
    
    /**
     * Handle DeviceOnlineStatus event
     */
    private fun handleDeviceOnlineStatus(data: ByteArray) {
        if (data.size < 4) return
        
        val tableId = data[1].toInt() and 0xFF
        val deviceId = data[2].toInt() and 0xFF
        val isOnline = (data[3].toInt() and 0xFF) != 0
        
        Log.i(TAG, "📦 Device $tableId:$deviceId online=$isOnline")
        
        val json = JSONObject().apply {
            put("device_table_id", tableId)
            put("device_id", deviceId)
            put("online", isOnline)
        }
        mqttPublisher.publishState("onecontrol/${device.address}/device/$tableId/$deviceId/online", json.toString(), true)
    }
    
    /**
     * Handle RelayBasicLatchingStatus event (lights, switches)
     */
    private fun handleRelayStatus(data: ByteArray) {
        if (data.size < 5) return
        
        val tableId = data[1].toInt() and 0xFF
        val deviceId = data[2].toInt() and 0xFF
        val isOn = (data[3].toInt() and 0xFF) != 0
        
        Log.i(TAG, "📦 Relay $tableId:$deviceId state=$isOn")
        
        mqttPublisher.publishState("onecontrol/${device.address}/device/$tableId/$deviceId/state", if (isOn) "ON" else "OFF", true)
    }
    
    /**
     * Handle DimmableLightStatus event
     */
    private fun handleDimmableLightStatus(data: ByteArray) {
        if (data.size < 5) return
        
        val tableId = data[1].toInt() and 0xFF
        val deviceId = data[2].toInt() and 0xFF
        val brightness = data[3].toInt() and 0xFF
        val isOn = brightness > 0
        
        Log.i(TAG, "📦 Dimmable $tableId:$deviceId brightness=$brightness")
        
        mqttPublisher.publishState("onecontrol/${device.address}/device/$tableId/$deviceId/state", if (isOn) "ON" else "OFF", true)
        mqttPublisher.publishState("onecontrol/${device.address}/device/$tableId/$deviceId/brightness", brightness.toString(), true)
    }
    
    /**
     * Handle HvacStatus event
     */
    private fun handleHvacStatus(data: ByteArray) {
        if (data.size < 10) return
        
        val tableId = data[1].toInt() and 0xFF
        val deviceId = data[2].toInt() and 0xFF
        
        Log.i(TAG, "📦 HVAC $tableId:$deviceId")
        
        val json = JSONObject().apply {
            put("device_table_id", tableId)
            put("device_id", deviceId)
            put("raw", data.joinToString(" ") { "%02X".format(it) })
        }
        mqttPublisher.publishState("onecontrol/${device.address}/device/$tableId/$deviceId/hvac", json.toString(), true)
    }
    
    /**
     * Handle TankSensorStatus event
     */
    private fun handleTankStatus(data: ByteArray) {
        if (data.size < 5) return
        
        val tableId = data[1].toInt() and 0xFF
        val deviceId = data[2].toInt() and 0xFF
        val level = data[3].toInt() and 0xFF
        
        Log.i(TAG, "📦 Tank $tableId:$deviceId level=$level%")
        
        mqttPublisher.publishState("onecontrol/${device.address}/device/$tableId/$deviceId/level", level.toString(), true)
    }
    
    /**
     * Handle TankSensorStatusV2 event
     */
    private fun handleTankStatusV2(data: ByteArray) {
        if (data.size < 6) return
        
        val tableId = data[1].toInt() and 0xFF
        val deviceId = data[2].toInt() and 0xFF
        val level = data[3].toInt() and 0xFF
        
        Log.i(TAG, "📦 TankV2 $tableId:$deviceId level=$level%")
        
        mqttPublisher.publishState("onecontrol/${device.address}/device/$tableId/$deviceId/level", level.toString(), true)
    }
    
    /**
     * Handle command response (GetDevices, etc.)
     */
    private fun handleCommandResponse(data: ByteArray) {
        val commandId = ((data[1].toInt() and 0xFF) shl 8) or (data[0].toInt() and 0xFF)
        val commandType = data[2].toInt() and 0xFF
        
        Log.i(TAG, "📦 Command Response: ID=0x${commandId.toString(16)}, Type=0x${commandType.toString(16)}")
        
        when (commandType) {
            0x01 -> handleGetDevicesResponse(data)
            0x02 -> Log.i(TAG, "📦 GetDevicesMetadata response")
        }
    }
    
    /**
     * Handle GetDevices response
     */
    private fun handleGetDevicesResponse(data: ByteArray) {
        Log.i(TAG, "📦 GetDevices response: ${data.size} bytes")
        
        // Parse device list and publish to MQTT
        val json = JSONObject().apply {
            put("command", "get_devices_response")
            put("size", data.size)
            put("raw", data.joinToString(" ") { "%02X".format(it) })
        }
        mqttPublisher.publishState("onecontrol/${device.address}/devices", json.toString(), true)
    }
    
    /**
     * Handle SEED notification - calculate and send auth key
     */
    private fun handleSeedNotification(data: ByteArray) {
        Log.i(TAG, "🌱 Received seed value: ${data.joinToString(" ") { "%02X".format(it) }}")
        seedValue = data
        
        val authKey = calculateAuthKey(data, gatewayPin, gatewayCypher)
        Log.i(TAG, "🔑 Calculated auth key: ${authKey.joinToString(" ") { "%02X".format(it) }}")
        
        // Write auth key to KEY characteristic (00000013)
        keyChar?.let { char ->
            Log.i(TAG, "📝 Writing auth key to KEY characteristic (00000013)")
            char.value = authKey
            val success = currentGatt?.writeCharacteristic(char) ?: false
            Log.i(TAG, "📝 Write initiated: success=$success")
        } ?: Log.e(TAG, "❌ Auth14 characteristic not found!")
    }
    
    /**
     * Calculate authentication key from seed
     * COPIED FROM LEGACY APP - TEA encryption
     */
    private fun calculateAuthKey(seed: ByteArray, pin: String, cypher: Long): ByteArray {
        val seedValue = ByteBuffer.wrap(seed).order(ByteOrder.LITTLE_ENDIAN).int.toLong() and 0xFFFFFFFFL
        
        Log.i(TAG, "🔢 Seed value: 0x${seedValue.toString(16).uppercase()}")
        
        val encryptedSeed = TeaEncryption.encrypt(cypher, seedValue)
        
        Log.i(TAG, "🔐 Encrypted seed: 0x${encryptedSeed.toString(16).uppercase()}")
        
        val keyBytes = ByteBuffer.allocate(4).order(ByteOrder.LITTLE_ENDIAN).putInt(encryptedSeed.toInt()).array()
        
        val authKey = ByteArray(16)
        System.arraycopy(keyBytes, 0, authKey, 0, 4)
        
        val pinBytes = pin.toByteArray(Charsets.US_ASCII)
        System.arraycopy(pinBytes, 0, authKey, 4, minOf(pinBytes.size, 6))
        
        return authKey
    }
    
    /**
     * Send GetDevices command
     * COPIED FROM LEGACY APP
     */
    private fun sendGetDevicesCommand() {
        if (!isConnected || currentGatt == null) {
            Log.w(TAG, "Cannot send command - not connected")
            return
        }
        
        val writeChar = dataWriteChar
        if (writeChar == null) {
            Log.w(TAG, "No write characteristic available")
            return
        }
        
        try {
            val commandId = getNextCommandId()
            val effectiveTableId = if (deviceTableId == 0x00.toByte()) DEFAULT_DEVICE_TABLE_ID else deviceTableId
            val command = encodeGetDevicesCommand(commandId, effectiveTableId)
            
            Log.d(TAG, "📤 GetDevices: CommandId=0x${commandId.toString(16)}, TableId=0x${effectiveTableId.toString(16)}")
            
            val encoded = CobsDecoder.encode(command, prependStartFrame = true, useCrc = true)
            Log.d(TAG, "📤 Encoded: ${encoded.joinToString(" ") { "%02X".format(it) }}")
            
            writeChar.value = encoded
            writeChar.writeType = BluetoothGattCharacteristic.WRITE_TYPE_NO_RESPONSE
            val result = currentGatt?.writeCharacteristic(writeChar)
            
            Log.i(TAG, "📤 Sent GetDevices command: result=$result")
        } catch (e: Exception) {
            Log.e(TAG, "Failed to send command: ${e.message}", e)
        }
    }
    
    /**
     * Encode GetDevices command
     */
    private fun encodeGetDevicesCommand(commandId: UShort, deviceTableId: Byte): ByteArray {
        return byteArrayOf(
            (commandId.toInt() and 0xFF).toByte(),
            ((commandId.toInt() shr 8) and 0xFF).toByte(),
            0x01.toByte(),  // CommandType: GetDevices
            deviceTableId,
            0x00.toByte(),  // StartDeviceId
            0xFF.toByte()   // MaxDeviceRequestCount
        )
    }
    
    private fun getNextCommandId(): UShort {
        val id = nextCommandId
        nextCommandId = if (nextCommandId >= 0xFFFEu) 1u else (nextCommandId + 1u).toUShort()
        return id
    }
    
    /**
     * Start heartbeat
     * COPIED FROM LEGACY APP - sends periodic GetDevices to keep connection alive
     */
    private fun startHeartbeat() {
        stopHeartbeat()
        
        heartbeatRunnable = object : Runnable {
            override fun run() {
                if (isConnected && isAuthenticated && currentGatt != null) {
                    Log.i(TAG, "💓 Heartbeat: sending GetDevices")
                    sendGetDevicesCommand()
                    handler.postDelayed(this, HEARTBEAT_INTERVAL_MS)
                } else {
                    Log.w(TAG, "💓 Heartbeat skipped - not ready")
                }
            }
        }
        
        handler.postDelayed(heartbeatRunnable!!, HEARTBEAT_INTERVAL_MS)
        Log.i(TAG, "💓 Heartbeat started (every ${HEARTBEAT_INTERVAL_MS}ms)")
    }
    
    private fun stopHeartbeat() {
        heartbeatRunnable?.let {
            handler.removeCallbacks(it)
            heartbeatRunnable = null
            Log.d(TAG, "💓 Heartbeat stopped")
        }
    }
    
    private fun cleanup(gatt: BluetoothGatt) {
        stopHeartbeat()
        stopActiveStreamReading()
        
        try {
            gatt.close()
        } catch (e: Exception) {
            Log.e(TAG, "Error closing GATT", e)
        }
        
        isConnected = false
        isAuthenticated = false
        notificationsEnableStarted = false
        seedValue = null
        currentGatt = null
        gatewayInfoReceived = false
    }
}
