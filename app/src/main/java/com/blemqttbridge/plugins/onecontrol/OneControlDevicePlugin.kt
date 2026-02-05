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
import com.blemqttbridge.plugins.onecontrol.protocol.TeaEncryption
import com.blemqttbridge.plugins.onecontrol.protocol.Constants
import com.blemqttbridge.plugins.onecontrol.protocol.CobsByteDecoder
import com.blemqttbridge.plugins.onecontrol.protocol.CobsDecoder
import com.blemqttbridge.plugins.onecontrol.protocol.HomeAssistantMqttDiscovery
import com.blemqttbridge.plugins.onecontrol.protocol.MyRvLinkCommandBuilder
import com.blemqttbridge.plugins.onecontrol.protocol.FunctionNameMapper
import com.blemqttbridge.plugins.onecontrol.protocol.AdvertisementParser
import org.json.JSONObject
import org.json.JSONArray
import java.util.*
import java.nio.ByteBuffer
import java.nio.ByteOrder
import java.util.concurrent.ConcurrentLinkedQueue

/**
 * Tank query response collector for multi-frame encrypted responses
 */
data class TankQueryResponse(
    val queryId: String,  // E0, E1, E2, etc.
    val frames: MutableList<ByteArray> = mutableListOf(),
    var isComplete: Boolean = false
)

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
        
        /**
         * Normalize MAC address to consistent format: uppercase with colons.
         * Android's BluetoothDevice.address returns uppercase format like "24:DC:C3:ED:1E:0A".
         * This ensures configs and comparisons use the same format.
         */
        private fun normalizeMac(mac: String?): String? {
            if (mac.isNullOrBlank()) return null
            // Remove all non-hex characters, convert to uppercase, add colons
            val cleaned = mac.replace("[^0-9A-Fa-f]".toRegex(), "")
            if (cleaned.length != 12) return mac // Invalid MAC, return as-is
            return cleaned.chunked(2).joinToString(":").uppercase()
        }
    }
    
    override val pluginId: String = PLUGIN_ID
    override var instanceId: String = PLUGIN_ID  // Same as pluginId by default
    override val supportsMultipleInstances: Boolean = false
    override val displayName: String = "OneControl Gateway (v2)"
    
    /**
     * OneControl gateways require explicit bonding for authentication.
     * This will only bond with the user-configured MAC address, preventing
     * accidental connections to neighbors' gateways in RV parks.
     */
    override fun requiresBonding(): Boolean = true
    
    private var context: Context? = null
    private var config: PluginConfig? = null
    
    // Configuration from settings
    private var gatewayMac: String = ""
    private var gatewayPin: String = "090336"  // PIN for both BLE bonding and protocol authentication
    private var gatewayCypher: Long = 0x8100080DL
    
    // Gateway capabilities (detected from advertisement)
    private var gatewayCapabilities: AdvertisementParser.GatewayCapabilities? = null
    
    // Strong reference to callback to prevent GC
    private var gattCallback: BluetoothGattCallback? = null
    
    // Current callback instance for command handling
    private var currentCallback: OneControlGattCallback? = null
    
    // Get app version dynamically
    private val appVersion: String
        get() = try {
            context?.packageManager?.getPackageInfo(context!!.packageName, 0)?.versionName ?: "unknown"
        } catch (e: Exception) {
            "unknown"
        }
    
    override fun initialize(context: Context?, config: PluginConfig) {
        Log.i(TAG, "Initializing OneControl Device Plugin v$PLUGIN_VERSION")
        this.context = context
        this.config = config
        
        // Load configuration from settings with MAC normalization
        gatewayMac = normalizeMac(config.getString("gateway_mac", gatewayMac)) ?: gatewayMac
        gatewayPin = config.getString("gateway_pin", gatewayPin)
        // gatewayCypher is hardcoded constant - same for all OneControl gateways
        
        Log.i(TAG, "Configured for gateway: $gatewayMac")
        Log.i(TAG, "  PIN: ${gatewayPin.take(2)}****")
    }
    
    override fun matchesDevice(
        device: BluetoothDevice,
        scanRecord: ScanRecord?
    ): Boolean {
        // SECURITY: Only match on exact configured MAC address.
        // This prevents connecting to neighbors' devices in RV parks.
        // No auto-discovery by device name or service UUID.
        
        // Normalize both MACs to ensure case-insensitive comparison
        val normalizedDeviceMac = normalizeMac(device.address)
        val normalizedGatewayMac = normalizeMac(gatewayMac)
        
        if (normalizedDeviceMac == normalizedGatewayMac) {
            // Parse and store capabilities for pairing flow
            gatewayCapabilities = AdvertisementParser.parseCapabilities(scanRecord)
            
            Log.d(TAG, "Device matched by configured MAC: ${device.address}")
            Log.d(TAG, "  Pairing method: ${gatewayCapabilities?.pairingMethod}")
            Log.d(TAG, "  Push-to-pair support: ${gatewayCapabilities?.supportsPushToPair}")
            Log.d(TAG, "  Pairing active: ${gatewayCapabilities?.pairingEnabled}")
            
            return true
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
        val callback = OneControlGattCallback(device, context, mqttPublisher, instanceId, onDisconnect, gatewayPin, gatewayCypher)
        Log.i(TAG, "Created callback with hashCode=${callback.hashCode()}")
        // Keep strong reference to prevent GC
        gattCallback = callback
        currentCallback = callback  // Track for command handling
        return callback
    }
    
    override fun onGattConnected(device: BluetoothDevice, gatt: BluetoothGatt) {
        Log.i(TAG, "GATT connected for ${device.address}")
        // Callback handles everything - nothing needed here
    }
    
    override fun onDeviceDisconnected(device: BluetoothDevice) {
        Log.i(TAG, "Device disconnected: ${device.address}")
        // Cleanup handled by GATT callback cleanup method
    }
    
    /**
     * Get the Bluetooth bonding PIN for legacy gateways.
     * Returns null for modern push-to-pair gateways.
     * 
     * This is used by BaseBleService to provide PIN during BLE bonding.
     * Different from protocol PIN used in authentication.
     */
    fun getBondingPin(): String? {
        return when (gatewayCapabilities?.pairingMethod) {
            AdvertisementParser.PairingMethod.PIN,
            AdvertisementParser.PairingMethod.NONE -> {
                // Legacy gateway - use PIN for both bonding and protocol authentication
                Log.d(TAG, "Providing bonding PIN for legacy gateway: ${gatewayPin.take(2)}****")
                gatewayPin
            }
            AdvertisementParser.PairingMethod.PUSH_BUTTON -> {
                Log.d(TAG, "Modern gateway - no bonding PIN needed (push-to-pair)")
                null
            }
            else -> {
                // Unknown - default to no PIN (safer for modern gateways)
                Log.w(TAG, "Unknown pairing method - assuming push-to-pair")
                null
            }
        }
    }
    
    override fun getMqttBaseTopic(device: BluetoothDevice): String {
        return "onecontrol/${device.address}"
    }
    
    override fun getDiscoveryPayloads(device: BluetoothDevice): List<Pair<String, String>> {
        // Discovery will be done by the callback when devices are enumerated
        return emptyList()
    }
    
    override suspend fun handleCommand(device: BluetoothDevice, commandTopic: String, payload: String): Result<Unit> {
        Log.i(TAG, "📤 Command received: $commandTopic = $payload")
        
        // Get the current callback instance
        val callback = currentCallback
        if (callback == null) {
            Log.w(TAG, "❌ No active callback for command")
            return Result.failure(Exception("No active connection"))
        }
        
        // Delegate to callback which has GATT access
        return callback.handleCommand(commandTopic, payload)
    }
    
    override fun destroy() {
        Log.i(TAG, "Destroying OneControl Device Plugin")
        gattCallback = null
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
    private val instanceId: String,
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
        private const val WATCHDOG_INTERVAL_MS = 60000L  // Check connection health every 60s
        private const val DEFAULT_DEVICE_TABLE_ID: Byte = 0x08
        
        // MTU size from legacy app (Constants.BLE_MTU_SIZE)
        private const val BLE_MTU_SIZE = 185
        
        // GATT 133 retry configuration
        private const val MAX_GATT_133_RETRIES = 3
        private const val GATT_133_RETRY_DELAY_MS = 2000L
        
        // Auth failure / peer-disconnect backoff configuration
        // If the gateway peer-terminates (status 19) repeatedly, it means auth is failing.
        // Use exponential backoff to stop hammering the device and BT stack.
        private const val MAX_CONSECUTIVE_PEER_DISCONNECTS = 3
        private const val PEER_DISCONNECT_BACKOFF_BASE_MS = 5000L  // 5s, 10s, 20s, 40s...
        private const val PEER_DISCONNECT_BACKOFF_MAX_MS = 120000L // Cap at 2 minutes
        
        // Auth challenge: all-zeros means gateway is not ready / auth unavailable
        private val EMPTY_CHALLENGE = byteArrayOf(0, 0, 0, 0)
    }
    
    // Handler for main thread operations
    private val handler = Handler(Looper.getMainLooper())
    
    // Get app version dynamically for HA discovery
    private val appVersion: String
        get() = try {
            context.packageManager.getPackageInfo(context.packageName, 0).versionName ?: "unknown"
        } catch (e: Exception) {
            "unknown"
        }
    
    // Discovery builder for simplified HA discovery generation
    private val discoveryBuilder by lazy {
        HomeAssistantMqttDiscovery.DiscoveryBuilder(device.address, appVersion)
    }
    
    // Connection state
    private var isConnected = false
    private var isAuthenticated = false
    private var seedValue: ByteArray? = null
    private var currentGatt: BluetoothGatt? = null
    
    // GATT 133 retry tracking
    private var gatt133RetryCount = 0
    
    // Peer-disconnect (status 19) backoff tracking
    // Prevents infinite reconnect loop when auth keeps failing
    private var consecutivePeerDisconnects = 0
    private var lastPeerDisconnectTime = 0L
    
    // Diagnostic status tracking (for HA sensors)
    private var lastDataTimestampMs: Long = 0L
    private val DATA_HEALTH_TIMEOUT_MS = 15_000L  // consider healthy if data seen within 15s
    private var diagnosticsDiscoveryPublished = false
    
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
    
    // Home Assistant discovery tracking - prevents duplicate discovery publishes
    private val haDiscoveryPublished = mutableSetOf<String>()

    // Tank query response handling
    private val pendingTankResponses = mutableMapOf<String, TankQueryResponse>()
    
    // Session key for TEA decryption (16-byte auth key from SEED notification)
    private var sessionAuthKey: ByteArray? = null

    // Device metadata from GetDevicesMetadata response
    data class DeviceMetadata(
        val deviceTableId: Int,
        val deviceId: Int,
        val functionName: Int,
        val functionInstance: Int,
        val friendlyName: String
    )
    private val deviceMetadata = mutableMapOf<Int, DeviceMetadata>()
    private var metadataRequested = false
    
    init {
        // Load cached metadata immediately so friendly names are available from first state update
        loadMetadataFromCache()
    }
    
    /**
     * Load device metadata from persistent cache.
     * This allows friendly names to be available immediately without waiting for metadata request.
     */
    private fun loadMetadataFromCache() {
        try {
            val prefs = context.getSharedPreferences("onecontrol_cache", Context.MODE_PRIVATE)
            val cacheKey = "metadata_${device.address.replace(":", "")}"
            val cached = prefs.getString(cacheKey, null)
            
            if (cached != null) {
                val jsonArray = JSONArray(cached)
                var loadedCount = 0
                
                for (i in 0 until jsonArray.length()) {
                    val json = jsonArray.getJSONObject(i)
                    val deviceAddr = json.getInt("deviceAddr")
                    deviceMetadata[deviceAddr] = DeviceMetadata(
                        deviceTableId = json.getInt("deviceTableId"),
                        deviceId = json.getInt("deviceId"),
                        functionName = json.getInt("functionName"),
                        functionInstance = json.getInt("functionInstance"),
                        friendlyName = json.getString("friendlyName")
                    )
                    loadedCount++
                }
                
                Log.i(TAG, "💾 Loaded $loadedCount cached metadata entries for ${device.address}")
            } else {
                Log.d(TAG, "💾 No cached metadata found for ${device.address}")
            }
        } catch (e: Exception) {
            Log.e(TAG, "💾 Error loading cached metadata: ${e.message}", e)
        }
    }
    
    /**
     * Save device metadata to persistent cache.
     * Cache survives app restarts and reconnections.
     */
    private fun saveMetadataToCache() {
        try {
            val jsonArray = JSONArray()
            
            deviceMetadata.forEach { (deviceAddr, metadata) ->
                val json = JSONObject()
                json.put("deviceAddr", deviceAddr)
                json.put("deviceTableId", metadata.deviceTableId)
                json.put("deviceId", metadata.deviceId)
                json.put("functionName", metadata.functionName)
                json.put("functionInstance", metadata.functionInstance)
                json.put("friendlyName", metadata.friendlyName)
                jsonArray.put(json)
            }
            
            val prefs = context.getSharedPreferences("onecontrol_cache", Context.MODE_PRIVATE)
            val cacheKey = "metadata_${device.address.replace(":", "")}"
            prefs.edit().putString(cacheKey, jsonArray.toString()).apply()
            
            Log.i(TAG, "💾 Saved ${deviceMetadata.size} metadata entries to cache for ${device.address}")
        } catch (e: Exception) {
            Log.e(TAG, "💾 Error saving metadata to cache: ${e.message}", e)
        }
    }
    
    /**
     * Get friendly name for a device, or fallback to hex ID
     */
    private fun getDeviceFriendlyName(tableId: Int, deviceId: Int, fallbackType: String): String {
        val deviceAddr = (tableId shl 8) or deviceId
        val metadata = deviceMetadata[deviceAddr]
        val result = if (metadata != null && metadata.friendlyName.isNotEmpty() && !metadata.friendlyName.startsWith("Unknown")) {
            metadata.friendlyName
        } else {
            "$fallbackType %04x".format(deviceAddr)
        }
        Log.d(TAG, "🏷️ getName($tableId:$deviceId): addr=0x%04x -> '$result'".format(deviceAddr))
        return result
    }

    /**
     * Entity types for centralized state publishing
     */
    enum class EntityType(val haComponent: String, val topicPrefix: String) {
        SWITCH("switch", "switch"),
        LIGHT("light", "light"),
        COVER_SENSOR("sensor", "cover_state"),  // Cover as state sensor for safety
        TANK_SENSOR("sensor", "tank"),
        SYSTEM_SENSOR("sensor", "system")
    }

    /**
     * Centralized method to publish entity state and discovery.
     * All entity handlers should call this to ensure consistent behavior.
     * 
     * @param entityType The type of entity (determines discovery topic structure)
     * @param tableId Device table ID
     * @param deviceId Device ID within table
     * @param discoveryKey Unique key for tracking discovery (e.g., "switch_0809")
     * @param state Map of state values to publish (e.g., {"state" to "ON", "brightness" to "128"})
     * @param discoveryProvider Lambda that returns the discovery JSON payload
     */
    private fun publishEntityState(
        entityType: EntityType,
        tableId: Int,
        deviceId: Int,
        discoveryKey: String,
        state: Map<String, String>,
        discoveryProvider: (friendlyName: String, deviceAddr: Int, prefix: String, baseTopic: String) -> JSONObject
    ) {
        val baseTopic = "onecontrol/${device.address}"
        val prefix = mqttPublisher.topicPrefix
        val keyHex = "%02x%02x".format(tableId, deviceId)
        val deviceAddr = (tableId shl 8) or deviceId
        
        // Determine fallback type from entity type
        val fallbackType = when (entityType) {
            EntityType.SWITCH -> "Switch"
            EntityType.LIGHT -> "Light"
            EntityType.COVER_SENSOR -> "Cover"
            EntityType.TANK_SENSOR -> "Tank"
            EntityType.SYSTEM_SENSOR -> "Sensor"
        }
        val friendlyName = getDeviceFriendlyName(tableId, deviceId, fallbackType)
        
        // Publish HA discovery if not already done
        if (haDiscoveryPublished.add(discoveryKey)) {
            Log.i(TAG, "📢 Publishing HA discovery for $entityType $tableId:$deviceId ($friendlyName)")
            try {
                val discovery = discoveryProvider(friendlyName, deviceAddr, prefix, baseTopic)
                val macForTopic = device.address.replace(":", "").lowercase()
                val discoveryTopic = "$prefix/${entityType.haComponent}/onecontrol_ble_$macForTopic/${entityType.topicPrefix}_$keyHex/config"
                Log.d(TAG, "📢 Discovery topic: $discoveryTopic")
                Log.i(TAG, "🔍 OneControl: Calling mqtt.publishDiscovery() for $entityType")
                mqttPublisher.publishDiscovery(discoveryTopic, discovery.toString())
                Log.i(TAG, "🔍 OneControl: publishDiscovery returned for $entityType")
                Log.d(TAG, "📢 Discovery published successfully")
            } catch (e: Exception) {
                Log.e(TAG, "📢 Discovery publish failed: ${e.message}", e)
                // Remove from set so we can retry
                haDiscoveryPublished.remove(discoveryKey)
            }
        }
        
        // Publish state values
        state.forEach { (suffix, value) ->
            val stateTopic = "$baseTopic/device/$tableId/$deviceId/$suffix"
            mqttPublisher.publishState(stateTopic, value, true)
        }
    }

    // Track pending commands by ID to match responses
    private val pendingCommands = mutableMapOf<Int, Int>()

    // Dimmable light control tracking (from legacy app)
    // Key: "tableId:deviceId", Value: last known brightness (1-255)
    private val lastKnownDimmableBrightness = mutableMapOf<String, Int>()
    // Pending dimmable: tracks in-flight brightness commands to suppress conflicting gateway status
    private val pendingDimmable = mutableMapOf<String, Pair<Int, Long>>()  // key -> (brightness, timestamp)
    private val DIMMER_PENDING_WINDOW_MS = 12000L
    // Pending send: debounce rapid slider changes
    private val pendingDimmableSend = mutableMapOf<String, Pair<Int, Long>>()  // key -> (brightness, timestamp)
    private val DIMMER_DEBOUNCE_MS = 200L
    
    // Heartbeat
    private var heartbeatRunnable: Runnable? = null
    private var watchdogRunnable: Runnable? = null
    private var lastSuccessfulOperationTime: Long = System.currentTimeMillis()
    
    override fun onConnectionStateChange(gatt: BluetoothGatt, status: Int, newState: Int) {
        val stateStr = when(newState) {
            BluetoothProfile.STATE_CONNECTED -> "CONNECTED"
            BluetoothProfile.STATE_DISCONNECTED -> "DISCONNECTED"
            else -> "UNKNOWN($newState)"
        }
        mqttPublisher.logBleEvent("STATE_CHANGE: $stateStr (status=$status)")
        Log.i(TAG, "🔌 Connection state changed: status=$status, newState=$newState, callback=${this.hashCode()}")
        
        when (status) {
            BluetoothGatt.GATT_SUCCESS -> {
                when (newState) {
                    BluetoothProfile.STATE_CONNECTED -> {
                        Log.i(TAG, "✅ Connected to ${device.address}, callback=${this.hashCode()}")
                        Log.i(TAG, "Bond state: ${device.bondState}")
                        isConnected = true
                        currentGatt = gatt
                        // Reset retry counters on successful connection
                        gatt133RetryCount = 0
                        consecutivePeerDisconnects = 0
                        mqttPublisher.updatePluginStatus(instanceId, true, isAuthenticated, false)
                        // Publish online availability status
                        publishAvailability(true)
                        
                        // Discover services
                        gatt.discoverServices()
                    }
                    BluetoothProfile.STATE_DISCONNECTED -> {
                        Log.i(TAG, "❌ Disconnected from ${device.address} with status=$status")
                        if (status == 133) {
                            Log.w(TAG, "⚠️ Status 133: Connection timeout or GATT error - check authentication flow")
                        }
                        cleanup(gatt)
                        onDisconnect(device, status)
                    }
                }
            }
            133 -> {
                gatt133RetryCount++
                Log.e(TAG, "⚠️ GATT_ERROR (133) - Connection failed (attempt $gatt133RetryCount/$MAX_GATT_133_RETRIES)")
                cleanup(gatt)
                
                if (gatt133RetryCount < MAX_GATT_133_RETRIES) {
                    Log.i(TAG, "🔄 Retrying connection in ${GATT_133_RETRY_DELAY_MS}ms...")
                    handler.postDelayed({
                        try {
                            Log.i(TAG, "🔄 Attempting reconnection (retry $gatt133RetryCount)...")
                            // Close old GATT first to prevent client_if leak
                            // (cleanup already calls close, but be safe in case currentGatt diverged)
                            try { currentGatt?.close() } catch (_: Exception) {}
                            currentGatt = null
                            // Reconnect using same callback
                            val newGatt = device.connectGatt(
                                context,
                                false,
                                this,
                                BluetoothDevice.TRANSPORT_LE
                            )
                            if (newGatt != null) {
                                currentGatt = newGatt
                                Log.i(TAG, "🔄 Reconnection initiated")
                            } else {
                                Log.e(TAG, "❌ Failed to initiate reconnection")
                                onDisconnect(device, status)
                            }
                        } catch (e: SecurityException) {
                            Log.e(TAG, "❌ Permission denied for reconnection", e)
                            onDisconnect(device, status)
                        }
                    }, GATT_133_RETRY_DELAY_MS)
                } else {
                    Log.e(TAG, "❌ Max retries ($MAX_GATT_133_RETRIES) reached - stopping reconnection attempts")
                    Log.e(TAG, "   Service will stop to prevent hammering the device")
                    onDisconnect(device, status)
                }
            }
            8 -> {
                Log.e(TAG, "⏱️ Connection timeout (status 8)")
                cleanup(gatt)
                onDisconnect(device, status)
            }
            19 -> {
                consecutivePeerDisconnects++
                lastPeerDisconnectTime = System.currentTimeMillis()
                Log.e(TAG, "🚫 Peer terminated connection (status 19) - consecutive: $consecutivePeerDisconnects")
                cleanup(gatt)
                
                if (consecutivePeerDisconnects >= MAX_CONSECUTIVE_PEER_DISCONNECTS) {
                    // Calculate exponential backoff delay
                    val backoffMs = (PEER_DISCONNECT_BACKOFF_BASE_MS * 
                        (1L shl (consecutivePeerDisconnects - MAX_CONSECUTIVE_PEER_DISCONNECTS).coerceAtMost(5)))
                        .coerceAtMost(PEER_DISCONNECT_BACKOFF_MAX_MS)
                    Log.w(TAG, "⏸️ Auth appears to be failing ($consecutivePeerDisconnects consecutive peer disconnects)." +
                        " Backing off ${backoffMs/1000}s before next reconnect attempt.")
                    // Delay before calling onDisconnect so BaseBleService's reconnect also delays
                    handler.postDelayed({
                        onDisconnect(device, status)
                    }, backoffMs)
                } else {
                    onDisconnect(device, status)
                }
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
        
        // Mark MTU as ready and check if we can start authentication
        mtuReady = true
        checkAndStartAuthentication(gatt)
    }
    
    /**
     * Check if both services discovered and MTU ready, then start authentication.
     * This prevents race condition where MTU callback fires before characteristics are cached.
     */
    private fun checkAndStartAuthentication(gatt: BluetoothGatt) {
        if (!servicesDiscovered) {
            Log.d(TAG, "⏳ Waiting for services to be discovered before auth...")
            return
        }
        if (!mtuReady) {
            Log.d(TAG, "⏳ Waiting for MTU exchange before auth...")
            return
        }
        
        // Both ready - start authentication
        Log.i(TAG, "🔑 Starting authentication sequence (services ready, MTU ready)...")
        startAuthentication(gatt)
    }
    
    // Track if notifications have been enabled to avoid duplicates
    private var notificationsEnableStarted = false
    
    // Track readiness for authentication (both must be true)
    private var servicesDiscovered = false
    private var mtuReady = false
    
    override fun onServicesDiscovered(gatt: BluetoothGatt, status: Int) {
        mqttPublisher.logBleEvent("SERVICES_DISCOVERED: status=$status, count=${gatt.services.size}")
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
        
        // Mark services as discovered
        servicesDiscovered = true
        
        // Request MTU - when both MTU ready and services discovered, auth will start
        Log.i(TAG, "📐 Requesting MTU size $BLE_MTU_SIZE...")
        gatt.requestMtu(BLE_MTU_SIZE)
        
        // Also check if MTU already completed (race condition where MTU fires first)
        checkAndStartAuthentication(gatt)
    }
    
    /**
     * Calculate authentication KEY from challenge using BLE unlock algorithm
     * Byte order: BIG-ENDIAN for both challenge and KEY
     */
    private fun calculateAuthKey(seed: Long): ByteArray {
        val cypher = 612643285L  // RvLinkKeySeedCypher (0x248431D5)
        
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
        val hex = data?.joinToString(" ") { "%02X".format(it) } ?: "null"
        mqttPublisher.logBleEvent("READ $uuid: $hex (status=$status)")
        
        Log.i(TAG, "📖 onCharacteristicRead: $uuid, status=$status, ${data?.size ?: 0} bytes")
        
        if (status != BluetoothGatt.GATT_SUCCESS) {
            Log.e(TAG, "❌ Characteristic read failed: status=$status")
            return
        }
        
        lastSuccessfulOperationTime = System.currentTimeMillis()
        
        if (data == null || data.isEmpty()) {
            Log.w(TAG, "⚠️ Empty data from characteristic read")
            return
        }
        
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
            
            // Detect all-zeros challenge = gateway not ready / auth unavailable
            if (data.contentEquals(EMPTY_CHALLENGE)) {
                Log.w(TAG, "⚠️ Challenge is all zeros (00 00 00 00) - gateway auth not ready." +
                    " Skipping auth to avoid infinite loop. Will retry on next connection.")
                return
            }
            
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
        mqttPublisher.updatePluginStatus(instanceId, true, true, false)
        
        // Start stream reading thread
        startActiveStreamReading()
        
        // Send initial GetDevices command after small delay
        handler.postDelayed({
            Log.i(TAG, "📤 Sending initial GetDevices to wake up gateway")
            sendGetDevicesCommand()
            
            // Start heartbeat
            startHeartbeat()
            
            // Start connection watchdog
            startWatchdog()
        }, 500)
        
        // Send GetDevicesMetadata to get friendly names - ALWAYS send, no guards
        Log.i(TAG, "🔍 About to schedule GetDevicesMetadata timer for 1500ms")
        val timerPosted = handler.postDelayed({
            Log.i(TAG, "🔍 Timer fired: metadataRequested=$metadataRequested, isConnected=$isConnected, isAuthenticated=$isAuthenticated")
            if (!metadataRequested) {
                metadataRequested = true
                Log.i(TAG, "🔍 Sending GetDevicesMetadata for friendly names (from timer)")
                sendGetDevicesMetadataCommand()
            } else {
                Log.i(TAG, "🔍 metadataRequested already true - skipping (was sent from elsewhere)")
            }
        }, 1500)
        Log.i(TAG, "🔍 Timer scheduled: result=$timerPosted")
        
        // Publish ready state to MQTT
        mqttPublisher.publishState("onecontrol/${device.address}/status", "ready", true)
        
        // NOTE: Command subscriptions are handled by BaseBleService.subscribeToDeviceCommands()
        // which routes MQTT commands to this plugin's handleCommand() method
        
        // Publish diagnostic sensors
        publishDiagnosticsDiscovery()
        publishDiagnosticsState()
    }

    // Android 13+ (API 33+) uses this signature
    @Suppress("OVERRIDE_DEPRECATION")
    override fun onCharacteristicChanged(
        gatt: BluetoothGatt,
        characteristic: BluetoothGattCharacteristic,
        value: ByteArray
    ) {
        val uuid = characteristic.uuid.toString().lowercase()
        val hex = value.joinToString(" ") { "%02X".format(it) }
        mqttPublisher.logBleEvent("NOTIFY $uuid: $hex")
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
        if (data != null) {
            val uuid = characteristic.uuid.toString().lowercase()
            val hex = data.joinToString(" ") { "%02X".format(it) }
            mqttPublisher.logBleEvent("NOTIFY $uuid: $hex")
        }
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
        // Log to trace file (observation only - doesn't affect comms)
        mqttPublisher.logBleEvent("NOTIFY ${uuid.toString().lowercase()}: $hex")
        Log.i(TAG, "📨 Notification from $uuid: ${data.size} bytes")
        Log.d(TAG, "📨 Data: $hex")
        
        when (uuid) {
            DATA_READ_CHARACTERISTIC_UUID -> {
                // Check if this is a tank query response first
                val tankResult = processTankQueryResponse(data)
                if (tankResult != null) {
                    // Tank query response decoded successfully
                    Log.i(TAG, "🪣 Tank query response decoded: ${tankResult.queryId} -> ${tankResult.level}%")
                    publishTankQueryResult(tankResult)
                } else {
                    // Queue for normal stream reading (like official app)
                    notificationQueue.offer(data)
                    // Update last data timestamp for health tracking
                    lastDataTimestampMs = System.currentTimeMillis()
                    synchronized(streamReadLock) {
                        streamReadLock.notify()
                    }
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
        val hex = characteristic.value?.joinToString(" ") { "%02X".format(it) } ?: "null"
        mqttPublisher.logBleEvent("WRITE $uuid: $hex (status=$status)")
        Log.i(TAG, "📝 onCharacteristicWrite: $uuid, status=$status")
        
        if (status == BluetoothGatt.GATT_SUCCESS) {
            lastSuccessfulOperationTime = System.currentTimeMillis()
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
        // Don't clear sessionAuthKey here - keep it for reconnection
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
        Log.i(TAG, "📦 EVENT TYPE: 0x${eventType.toString(16).padStart(2, '0')} (${decodedFrame.size} bytes)")
        
        when (eventType) {
            0x01 -> {
                // GatewayInformation event
                Log.i(TAG, "📦 GatewayInformation event received!")
                handleGatewayInformationEvent(decodedFrame)
            }
            0x02 -> {
                // DeviceCommand response - GetDevices/GetDevicesMetadata responses
                Log.i(TAG, "📦 DeviceCommand response (0x02)")
                handleCommandResponse(decodedFrame)
            }
            0x03 -> {
                // DeviceOnlineStatus
                Log.i(TAG, "📦 DeviceOnlineStatus event")
                handleDeviceOnlineStatus(decodedFrame)
            }
            0x04 -> {
                // DeviceLockStatus
                Log.i(TAG, "📦 DeviceLockStatus event")
                handleDeviceLockStatus(decodedFrame)
            }
            0x05, 0x06 -> {
                // RelayBasicLatchingStatus (Type1 or Type2)
                Log.i(TAG, "📦 RelayBasicLatchingStatus event")
                handleRelayStatus(decodedFrame)
            }
            0x07 -> {
                // RvStatus - contains system voltage and temperature
                Log.i(TAG, "📦 RvStatus event (voltage/temp)")
                handleRvStatus(decodedFrame)
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
            0x0D, 0x0E -> {
                // RelayHBridgeMomentaryStatus (Type1 or Type2) - covers/slides/awnings
                Log.i(TAG, "📦 RelayHBridgeStatus event (cover)")
                handleHBridgeStatus(decodedFrame)
            }
            0x0F -> {
                // HourMeterStatus
                Log.i(TAG, "📦 HourMeterStatus event")
                handleGenericEvent(decodedFrame, "hour_meter")
            }
            0x10 -> {
                // Leveler4DeviceStatus
                Log.i(TAG, "📦 Leveler4DeviceStatus event")
                handleGenericEvent(decodedFrame, "leveler")
            }
            0x1A -> {
                // DeviceSessionStatus - session heartbeat
                Log.d(TAG, "📦 DeviceSessionStatus (session heartbeat)")
            }
            0x1B -> {
                // TankSensorStatusV2
                Log.i(TAG, "📦 TankSensorStatusV2 event")
                handleTankStatusV2(decodedFrame)
            }
            0x20 -> {
                // RealTimeClock
                Log.i(TAG, "📦 RealTimeClock event")
                handleRealTimeClock(decodedFrame)
            }
            else -> {
                // Check if it's a command response
                if (isCommandResponse(decodedFrame)) {
                    handleCommandResponse(decodedFrame)
                } else {
                    // DESIGN: Publish ALL unknown events so nothing is lost
                    Log.i(TAG, "📦 Unknown event type: 0x${eventType.toString(16)} - publishing to MQTT")
                    handleGenericEvent(decodedFrame, "unknown_0x${eventType.toString(16)}")
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
     * Format: [0x01][byte1][byte2][byte3][deviceTableId][...]
     */
    private fun handleGatewayInformationEvent(data: ByteArray) {
        Log.i(TAG, "📦 GatewayInformation: ${data.size} bytes")
        
        if (data.size >= 5) {
            // deviceTableId is at byte 4, not byte 1
            deviceTableId = data[4]
            gatewayInfoReceived = true
            Log.i(TAG, "📦 Device Table ID: 0x${(deviceTableId.toInt() and 0xFF).toString(16)}")
            
            // If we're receiving GatewayInformation, we ARE authenticated (data is flowing)
            // Set isAuthenticated if not already set (fixes race condition where
            // onAllNotificationsSubscribed never fires due to descriptor write issues)
            if (!isAuthenticated) {
                Log.i(TAG, "✅ Setting isAuthenticated=true (receiving data proves auth)")
                isAuthenticated = true
                mqttPublisher.updatePluginStatus(instanceId, true, true, true)
            }
            
            // Trigger GetDevicesMetadata for friendly names
            if (!metadataRequested) {
                metadataRequested = true
                Log.i(TAG, "🔍 Triggering GetDevicesMetadata from GatewayInformation")
                handler.postDelayed({ sendGetDevicesMetadataCommand() }, 500)
            }
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
     * Raw output state in LOW NIBBLE of status byte
     * Status byte format: upper nibble = flags, lower nibble = state (0x00=OFF, 0x01=ON)
     * Extended format (9 bytes): includes DTC code for fault diagnostics
     */
    private fun handleRelayStatus(data: ByteArray) {
        if (data.size < 5) return
        
        val tableId = data[1].toInt() and 0xFF
        val deviceId = data[2].toInt() and 0xFF
        val statusByte = data[3].toInt() and 0xFF
        val rawOutputState = statusByte and 0x0F  // State is in LOW NIBBLE
        val isOn = rawOutputState == 0x01  // 0x01 = ON, 0x00 = OFF
        
        // Parse extended data if available (DTC code)
        val dtc = if (data.size >= 9) {
            ((data[5].toInt() and 0xFF) shl 8) or (data[6].toInt() and 0xFF)
        } else {
            null
        }
        
        // Create entity instance
        val entity = OneControlEntity.Switch(
            tableId = tableId,
            deviceId = deviceId,
            isOn = isOn
        )
        
        val dtcStr = dtc?.let { " DTC=$it(${DtcCodes.getName(it)})" } ?: ""
        Log.i(TAG, "📦 Relay ${entity.address} statusByte=0x%02X rawOutput=0x%02X state=${entity.state}$dtcStr".format(statusByte, rawOutputState))
        
        // Publish entity state
        publishEntityState(
            entityType = EntityType.SWITCH,
            tableId = entity.tableId,
            deviceId = entity.deviceId,
            discoveryKey = "switch_${entity.key}",
            state = mapOf("state" to entity.state)
        ) { friendlyName, deviceAddr, prefix, baseTopic ->
            val stateTopic = "$baseTopic/device/${entity.tableId}/${entity.deviceId}/state"
            val commandTopic = "$baseTopic/command/switch/${entity.tableId}/${entity.deviceId}"
            val attributesTopic = "$baseTopic/device/${entity.tableId}/${entity.deviceId}/attributes"
            
            // Publish DTC attributes only for gas appliances (water heater, furnace, etc.)
            val shouldPublishDtc = dtc != null && friendlyName.contains("gas", ignoreCase = true)
            if (shouldPublishDtc) {
                val attributesJson = JSONObject().apply {
                    put("dtc_code", dtc)
                    put("dtc_name", DtcCodes.getName(dtc!!))
                    put("fault", DtcCodes.isFault(dtc))
                    put("status_byte", "0x${statusByte.toString(16).uppercase().padStart(2, '0')}")
                }
                Log.d(TAG, "📋 Publishing DTC attributes for $friendlyName to $prefix/$attributesTopic: $attributesJson")
                mqttPublisher.publishState("$prefix/$attributesTopic", attributesJson.toString(), true)
            }
            
            discoveryBuilder.buildSwitch(
                deviceAddr = deviceAddr,
                deviceName = friendlyName,
                stateTopic = "$prefix/$stateTopic",
                commandTopic = "$prefix/$commandTopic",
                attributesTopic = if (shouldPublishDtc) "$prefix/$attributesTopic" else null
            )
        }
    }
    
    /**
     * Handle DimmableLightStatus event
     * Includes pending guard from legacy app to prevent UI bouncing during command execution
     */
    private fun handleDimmableLightStatus(data: ByteArray) {
        if (data.size < 5) return
        
        val tableId = data[1].toInt() and 0xFF
        val deviceId = data[2].toInt() and 0xFF
        val modeByte = data[3].toInt() and 0xFF  // Mode byte: 0=Off, 1=On, 2=Blink, 3=Swell
        val brightness = data[4].toInt() and 0xFF  // Brightness: 0-255
        
        // Create entity instance
        val entity = OneControlEntity.DimmableLight(
            tableId = tableId,
            deviceId = deviceId,
            brightness = brightness,
            mode = modeByte
        )
        
        Log.i(TAG, "📦 Dimmable ${entity.address} brightness=${entity.brightness} mode=${entity.mode}")
        
        // Pending guard: suppress mismatching status updates while a command is pending
        // This prevents the UI from bouncing back to old values during dimmer adjustment
        val pending = pendingDimmable[entity.address]
        val now = System.currentTimeMillis()
        if (pending != null) {
            val (desired, ts) = pending
            val age = now - ts
            if (age <= DIMMER_PENDING_WINDOW_MS) {
                // If reported brightness doesn't match desired, or mode is off when we want on, ignore
                if (entity.brightness != desired || (entity.mode == 0 && desired > 0)) {
                    Log.d(TAG, "🚫 Ignoring dimmer mismatch during pending window: reported=${entity.brightness} desired=$desired age=${age}ms")
                    return  // Don't publish this status update
                }
            }
            // Clear pending once we accept matching status or after the window expires
            pendingDimmable.remove(entity.address)
        }
        
        // Track last known brightness for restore-on-ON feature
        // Only update when we receive non-zero brightness (never clear from status updates)
        if (entity.brightness > 0) {
            lastKnownDimmableBrightness[entity.address] = entity.brightness
        }
        
        // Use centralized publishing for discovery and state
        publishEntityState(
            entityType = EntityType.LIGHT,
            tableId = entity.tableId,
            deviceId = entity.deviceId,
            discoveryKey = "light_${entity.key}",
            state = mapOf(
                "state" to entity.state,
                "brightness" to entity.brightness.toString()
            )
        ) { friendlyName, deviceAddr, prefix, baseTopic ->
            val stateTopic = "$baseTopic/device/${entity.tableId}/${entity.deviceId}/state"
            val brightnessTopic = "$baseTopic/device/${entity.tableId}/${entity.deviceId}/brightness"
            val commandTopic = "$baseTopic/command/dimmable/${entity.tableId}/${entity.deviceId}"
            discoveryBuilder.buildDimmableLight(
                deviceAddr = deviceAddr,
                deviceName = friendlyName,
                stateTopic = "$prefix/$stateTopic",
                commandTopic = "$prefix/$commandTopic",
                brightnessTopic = "$prefix/$brightnessTopic"
            )
        }
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
     * Process tank query responses (E0-E9 encrypted multi-frame responses)
     * Returns decoded tank data if response is complete, null otherwise
     */
    private fun processTankQueryResponse(data: ByteArray): TankData? {
        if (data.size < 4) return null
        
        // Look for tank query response pattern: 00 XX 02 QQ...
        if (data[0] != 0x00.toByte()) return null
        
        val responseType = data[1].toInt() and 0xFF
        if (data[2] != 0x02.toByte()) return null  // Not a query response
        
        val queryId = String.format("%02X", data[3].toInt() and 0xFF)
        
        // Only process tank queries E0-E9
        if (!queryId.matches(Regex("E[0-9]"))) return null
        
        Log.d(TAG, "🪣 Tank query response frame: $queryId, type=0x${responseType.toString(16)}, ${data.size} bytes")
        
        // Get or create response collector
        val response = pendingTankResponses.getOrPut(queryId) { TankQueryResponse(queryId) }
        
        // Add frame to response (skip 00 XX prefix)
        val frameData = data.copyOfRange(2, data.size)
        response.frames.add(frameData)
        
        // Check if this is the final frame (usually starts with 0x0A)
        if (responseType == 0x0A) {
            response.isComplete = true
            pendingTankResponses.remove(queryId)
            
            Log.i(TAG, "🪣 Complete response for $queryId: ${response.frames.size} frames")
            return decodeTankQueryResponse(response)
        }
        
        return null
    }
    
    /**
     * Decode a complete tank query response
     */
    private fun decodeTankQueryResponse(response: TankQueryResponse): TankData? {
        try {
            // Reconstruct multi-frame payload
            val reconstructed = reconstructTankQueryFrames(response.frames)
            Log.d(TAG, "🪣 Reconstructed ${reconstructed.size} bytes for ${response.queryId}")
            
            // COBS decode
            val cobsDecoded = CobsDecoder.decode(reconstructed)
            if (cobsDecoded == null) {
                Log.w(TAG, "❌ COBS decode failed for ${response.queryId}")
                return null
            }
            Log.d(TAG, "🪣 COBS decoded ${cobsDecoded.size} bytes for ${response.queryId}")
            
            // TEA decrypt using session key
            val decrypted = if (cobsDecoded.size >= 8) {
                // Get session key from gateway authentication
                val sessionKey = getSessionKey()
                if (sessionKey != null && sessionKey.size == 8) {
                    val decryptedData = TeaEncryption.decryptByteArray(cobsDecoded, sessionKey)
                    if (decryptedData != null) {
                        Log.i(TAG, "✅ TEA decryption successful for ${response.queryId}")
                        decryptedData
                    } else {
                        Log.w(TAG, "❌ TEA decryption failed for ${response.queryId} - using raw data")
                        cobsDecoded
                    }
                } else {
                    Log.w(TAG, "⚠️ No valid session key available for ${response.queryId} - using raw data")
                    cobsDecoded
                }
            } else {
                Log.w(TAG, "❌ Data too short for ${response.queryId}")
                cobsDecoded
            }
            
            // Parse tank data (level is in first byte, masked to 0x7F)
            val level = if (decrypted.isNotEmpty()) {
                (decrypted[0].toInt() and 0x7F).coerceIn(0, 100)
            } else 0
            
            Log.i(TAG, "🪣 ${response.queryId}: Tank level = $level%")
            
            return TankData(
                queryId = response.queryId,
                tableId = 8, // Assuming table 8 like autonomous systems
                deviceId = response.queryId.substring(1).toIntOrNull() ?: 0, // E0->0, E1->1, etc.
                level = level
            )
            
        } catch (e: Exception) {
            Log.e(TAG, "❌ Error decoding ${response.queryId}: ${e.message}")
            return null
        }
    }
    
    /**
     * Reconstruct frames into complete payload
     */
    private fun reconstructTankQueryFrames(frames: List<ByteArray>): ByteArray {
        val result = mutableListOf<Byte>()
        
        for (frame in frames) {
            // Skip frame headers and add payload data
            if (frame.size > 6) {
                // Skip query response header: 02 QQ 01 XX XX...
                val payload = frame.copyOfRange(6, frame.size)
                result.addAll(payload.toList())
            }
        }
        
        return result.toByteArray()
    }
    
    /**
     * Get the current session key for TEA decryption
     * Uses first 8 bytes of the 16-byte authentication key from SEED notification
     */
    private fun getSessionKey(): ByteArray? {
        val authKey = sessionAuthKey
        return if (authKey != null && authKey.size >= 8) {
            Log.v(TAG, "🔓 Session key available (${authKey.size} bytes)")
            authKey.copyOfRange(0, 8)  // First 8 bytes for TEA decryption
        } else {
            Log.w(TAG, "No session authentication key available for decryption (authKey=${authKey?.size ?: "null"})")
            null
        }
    }
    
    /**
     * Publish decoded tank query result to MQTT
     */
    private fun publishTankQueryResult(tankData: TankData) {
        val keyHex = String.format("%02X%02X", tankData.tableId, tankData.deviceId)
        val uniqueKey = "tank_${tankData.queryId}_${keyHex}" // Use query ID for uniqueness
        
        // Publish entity state
        publishEntityState(
            entityType = EntityType.TANK_SENSOR,
            tableId = tankData.tableId,
            deviceId = tankData.deviceId,
            discoveryKey = uniqueKey,
            state = mapOf(
                "level" to tankData.level.toString(),
                "query_id" to tankData.queryId,
                "communication_type" to "query_response"
            )
        ) { friendlyName, _, prefix, baseTopic ->
            val stateTopic = "$baseTopic/device/${tankData.tableId}/${tankData.deviceId}/level"
            discoveryBuilder.buildSensor(
                sensorName = "Tank ${tankData.queryId} Level",
                stateTopic = "$prefix/$stateTopic",
                unit = "%",
                icon = "mdi:gauge"
            )
        }
    }
    
    /**
     * Tank data structure for both autonomous and query-based tanks
     */
    private data class TankData(
        val queryId: String = "",
        val tableId: Int,
        val deviceId: Int,
        val level: Int
    )
    
    /**
     * Handle TankSensorStatus event (0x0C)
     * Format: [eventType(0x0C)] [deviceTableId] [deviceId1] [percent1] [deviceId2] [percent2] ...
     * Multiple tanks can be reported in a single event as pairs of (DeviceId, Percent)
     */
    private fun handleTankStatus(data: ByteArray) {
        // Minimum: eventType(1) + tableId(1) + deviceId(1) + percent(1) = 4 bytes
        if (data.size < 4) return
        
        val tableId = data[1].toInt() and 0xFF
        val tankCount = (data.size - 2) / 2  // Each tank is 2 bytes (deviceId + percent)
        
        Log.i(TAG, "📦 TankSensorStatus: tableId=$tableId, tankCount=$tankCount, dataSize=${data.size}")
        
        // Iterate through all tank pairs starting at index 2
        var index = 2
        while (index + 1 < data.size) {
            val deviceId = data[index].toInt() and 0xFF
            val level = data[index + 1].toInt() and 0xFF
            
            // Create entity instance
            val entity = OneControlEntity.Tank(
                tableId = tableId,
                deviceId = deviceId,
                level = level
            )
            
            Log.i(TAG, "📦 Tank ${entity.address} level=${entity.level}%")
            
            publishEntityState(
                entityType = EntityType.TANK_SENSOR,
                tableId = entity.tableId,
                deviceId = entity.deviceId,
                discoveryKey = "tank_${entity.key}",
                state = mapOf("level" to entity.level.toString())
            ) { friendlyName, _, prefix, baseTopic ->
                val stateTopic = "$baseTopic/device/${entity.tableId}/${entity.deviceId}/level"
                discoveryBuilder.buildSensor(
                    sensorName = friendlyName,
                    stateTopic = "$prefix/$stateTopic",
                    unit = "%",
                    icon = "mdi:gauge"
                )
            }
            
            index += 2  // Move to next tank pair
        }
    }
    
    /**
     * Handle RvStatus event - contains system voltage and temperature
     * Format: eventType(0x07), voltage(2 bytes 8.8 BE), temp(2 bytes 8.8 BE), flags(1)
     * Invalid/unavailable markers: 0xFFFF for voltage, 0x7FFF or 0xFFFF for temperature
     */
    private fun handleRvStatus(data: ByteArray) {
        if (data.size < 6) return
        
        val voltageRaw = ((data[1].toInt() and 0xFF) shl 8) or (data[2].toInt() and 0xFF)
        val tempRaw = ((data[3].toInt() and 0xFF) shl 8) or (data[4].toInt() and 0xFF)
        
        val voltage = if (voltageRaw == 0xFFFF) null else voltageRaw.toFloat() / 256f
        // 0x7FFF (32767) appears to be "not available" marker for temperature
        val temperature = if (tempRaw == 0xFFFF || tempRaw == 0x7FFF) null else tempRaw.toFloat() / 256f
        
        Log.i(TAG, "📦 RvStatus: voltageRaw=0x%04X (${voltage}V), tempRaw=0x%04X (${temperature}°F)".format(voltageRaw, tempRaw))
        
        val baseTopic = "onecontrol/${device.address}"
        
        // Publish voltage sensor with HA discovery
        voltage?.let {
            val voltageTopic = "$baseTopic/system/voltage"
            
            // Publish HA discovery for voltage sensor if not already done
            val voltageDiscoveryKey = "system_voltage"
            if (haDiscoveryPublished.add(voltageDiscoveryKey)) {
                Log.i(TAG, "📢 Publishing HA discovery for system voltage sensor")
                val prefix = mqttPublisher.topicPrefix
                val discovery = HomeAssistantMqttDiscovery.getSensorDiscovery(
                    gatewayMac = device.address,
                    sensorName = "System Voltage",
                    stateTopic = "$prefix/$voltageTopic",
                    unit = "V",
                    deviceClass = "voltage",
                    icon = "mdi:car-battery",
                    appVersion = appVersion
                )
                val discoveryTopic = "$prefix/sensor/onecontrol_ble_${device.address.replace(":", "").lowercase()}/system_voltage/config"
                mqttPublisher.publishDiscovery(discoveryTopic, discovery.toString())
            }
            mqttPublisher.publishState(voltageTopic, String.format("%.3f", it), true)
        }
        
        // Publish temperature sensor with HA discovery
        temperature?.let {
            val tempTopic = "$baseTopic/system/temperature"
            
            // Publish HA discovery for temperature sensor if not already done
            val tempDiscoveryKey = "system_temperature"
            if (haDiscoveryPublished.add(tempDiscoveryKey)) {
                Log.i(TAG, "📢 Publishing HA discovery for system temperature sensor")
                val prefix = mqttPublisher.topicPrefix
                val discovery = HomeAssistantMqttDiscovery.getSensorDiscovery(
                    gatewayMac = device.address,
                    sensorName = "System Temperature",
                    stateTopic = "$prefix/$tempTopic",
                    unit = "°F",
                    deviceClass = "temperature",
                    icon = "mdi:thermometer",
                    appVersion = appVersion
                )
                val discoveryTopic = "$prefix/sensor/onecontrol_ble_${device.address.replace(":", "").lowercase()}/system_temperature/config"
                mqttPublisher.publishDiscovery(discoveryTopic, discovery.toString())
            }
            mqttPublisher.publishState(tempTopic, String.format("%.1f", it), true)
        }
    }
    
    /**
     * Handle H-Bridge status event (covers/slides/awnings)
     * Format: eventType(0x0D/0x0E), tableId, deviceId, status, position
     * Status: 0xC0=stopped, 0xC2=extending/opening, 0xC3=retracting/closing
     * Position: 0-100 (percentage) or 0xFF if not supported
     */
    private fun handleHBridgeStatus(data: ByteArray) {
        if (data.size < 4) return
        
        val tableId = data[1].toInt() and 0xFF
        val deviceId = data[2].toInt() and 0xFF
        val status = data[3].toInt() and 0xFF
        val position = if (data.size > 4) data[4].toInt() and 0xFF else 0xFF
        
        // Create entity instance
        val entity = OneControlEntity.Cover(
            tableId = tableId,
            deviceId = deviceId,
            status = status,
            position = position
        )
        
        Log.i(TAG, "📦 HBridge (cover) ${entity.address} status=0x${status.toString(16)} position=$position haState=${entity.haState} (${data.size} bytes, raw=${data.joinToString(" ") { "%02X".format(it) }})")
        
        // SAFETY: RV awnings/slides have no limit switches or overcurrent protection.
        // Motors rely on operator judgment - remote control is unsafe. Exposing as state sensor only.
        publishEntityState(
            entityType = EntityType.COVER_SENSOR,
            tableId = entity.tableId,
            deviceId = entity.deviceId,
            discoveryKey = "cover_state_${entity.key}",
            state = mapOf("state" to entity.haState)
        ) { friendlyName, deviceAddr, prefix, baseTopic ->
            val stateTopic = "$baseTopic/device/${entity.tableId}/${entity.deviceId}/state"
            discoveryBuilder.buildCoverStateSensor(
                deviceAddr = deviceAddr,
                deviceName = friendlyName,
                stateTopic = "$prefix/$stateTopic"
            )
        }
    }
    
    /**
     * Handle DeviceLockStatus event
     */
    private fun handleDeviceLockStatus(data: ByteArray) {
        if (data.size < 4) return
        
        val tableId = data[1].toInt() and 0xFF
        val deviceId = data[2].toInt() and 0xFF
        val locked = (data[3].toInt() and 0xFF) != 0
        
        Log.i(TAG, "📦 DeviceLock $tableId:$deviceId locked=$locked")
        
        val baseTopic = "onecontrol/${device.address}"
        val json = JSONObject().apply {
            put("device_table_id", tableId)
            put("device_id", deviceId)
            put("locked", locked)
        }
        mqttPublisher.publishState("$baseTopic/device/$tableId/$deviceId/lock", json.toString(), true)
    }
    
    /**
     * Handle RealTimeClock event
     */
    private fun handleRealTimeClock(data: ByteArray) {
        Log.i(TAG, "📦 RealTimeClock: ${data.size} bytes")
        
        val baseTopic = "onecontrol/${device.address}"
        val json = JSONObject().apply {
            put("event", "rtc")
            put("raw", data.joinToString(" ") { "%02X".format(it) })
        }
        mqttPublisher.publishState("$baseTopic/system/rtc", json.toString(), true)
    }
    
    /**
     * Handle any generic/unknown event - DESIGN: publish everything so nothing is lost
     */
    private fun handleGenericEvent(data: ByteArray, eventName: String) {
        val tableId = if (data.size > 1) data[1].toInt() and 0xFF else 0
        val deviceId = if (data.size > 2) data[2].toInt() and 0xFF else 0
        
        Log.i(TAG, "📦 Generic event '$eventName': tableId=$tableId, deviceId=$deviceId, ${data.size} bytes")
        
        val baseTopic = "onecontrol/${device.address}"
        val json = JSONObject().apply {
            put("event", eventName)
            put("table_id", tableId)
            put("device_id", deviceId)
            put("size", data.size)
            put("raw", data.joinToString(" ") { "%02X".format(it) })
        }
        
        // Publish to device-specific topic if we have table/device IDs, otherwise to events topic
        val topic = if (tableId != 0 || deviceId != 0) {
            "$baseTopic/device/$tableId/$deviceId/$eventName"
        } else {
            "$baseTopic/events/$eventName"
        }
        mqttPublisher.publishState(topic, json.toString(), true)
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
        
        // Topic paths - publishState adds prefix, so use relative paths
        val baseTopic = "onecontrol/${device.address}"
        val stateTopic = "$baseTopic/device/$tableId/$deviceId/level"
        
        // Publish HA discovery if not already done
        val keyHex = "%02x%02x".format(tableId, deviceId)
        val discoveryKey = "tank_$keyHex"
        val friendlyName = getDeviceFriendlyName(tableId, deviceId, "Tank")
        if (haDiscoveryPublished.add(discoveryKey)) {
            Log.i(TAG, "📢 Publishing HA discovery for tank sensor V2 $tableId:$deviceId ($friendlyName)")
            // Discovery payload needs full topic path
            val prefix = mqttPublisher.topicPrefix
            val discovery = HomeAssistantMqttDiscovery.getSensorDiscovery(
                gatewayMac = device.address,
                sensorName = friendlyName,
                stateTopic = "$prefix/$stateTopic",
                unit = "%",
                deviceClass = null,
                icon = "mdi:gauge",
                appVersion = appVersion
            )
            val discoveryTopic = "$prefix/sensor/onecontrol_ble_${device.address.replace(":", "").lowercase()}/tank_$keyHex/config"
            mqttPublisher.publishDiscovery(discoveryTopic, discovery.toString())
        }
        
        // Publish state (relative path, prefix added by publishState)
        mqttPublisher.publishState(stateTopic, level.toString(), true)
    }
    
    /**
     * Handle command response (GetDevices, GetDevicesMetadata)
     */
    private fun handleCommandResponse(data: ByteArray) {
        if (data.size < 4) {
            Log.w(TAG, "📦 Response too short: ${data.size}")
            return
        }
        
        // Command ID is little-endian at bytes 1-2
        val commandId = (data[1].toInt() and 0xFF) or ((data[2].toInt() and 0xFF) shl 8)
        val responseType = data[3].toInt() and 0xFF
        val commandType = pendingCommands[commandId]
        
        val isSuccess = responseType == 0x01 || responseType == 0x81
        val isComplete = responseType == 0x81 || responseType == 0x82
        
        Log.i(TAG, "📦 Response: cmdId=$commandId, respType=$responseType, cmdType=$commandType")
        
        if (commandType == null) {
            Log.w(TAG, "📦 Unknown cmdId $commandId, trying to infer")
            // Try to infer from data structure
            if (data.size >= 8) {
                val protocol = data[7].toInt() and 0xFF
                val payloadSize = if (data.size > 8) data[8].toInt() and 0xFF else 0
                if (protocol == 2 && payloadSize == 17) {
                    Log.i(TAG, "📦 Inferred GetDevicesMetadata")
                    handleGetDevicesMetadataResponse(data)
                    return
                }
            }
            return
        }
        
        if (isComplete) pendingCommands.remove(commandId)
        
        when (commandType) {
            0x01 -> handleGetDevicesResponse(data)
            0x02 -> handleGetDevicesMetadataResponse(data)
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
     * Handle GetDevicesMetadata response - parses function names
     * Format: [0x02][cmdIdLo][cmdIdHi][respType][tableId][startId][count][entries]
     */
    private fun handleGetDevicesMetadataResponse(data: ByteArray) {
        if (data.size < 8) {
            Log.w(TAG, "📋 Metadata response too short: ${data.size}")
            return
        }
        
        val tableId = data[4].toInt() and 0xFF
        val startId = data[5].toInt() and 0xFF
        val count = data[6].toInt() and 0xFF
        
        Log.i(TAG, "📋 Metadata: table=$tableId, start=$startId, count=$count")
        
        var offset = 7
        var index = 0
        
        Log.d(TAG, "📋 Raw data: ${data.joinToString(" ") { "%02X".format(it) }}")
        
        while (index < count && offset + 2 < data.size) {
            val protocol = data[offset].toInt() and 0xFF
            val payloadSize = data[offset + 1].toInt() and 0xFF
            val entrySize = payloadSize + 2
            
            Log.d(TAG, "📋 Entry $index @ offset=$offset: protocol=$protocol, payloadSize=$payloadSize")
            
            if (offset + entrySize > data.size) {
                Log.w(TAG, "📋 Entry overflows data (need ${offset + entrySize}, have ${data.size})")
                break
            }
            
            val deviceId = (startId + index) and 0xFF
            val deviceAddr = (tableId shl 8) or deviceId
            
            if (protocol == 2 && payloadSize == 17) {
                // Function name is BIG-ENDIAN (high byte first)
                val funcNameHi = data[offset + 2].toInt() and 0xFF
                val funcNameLo = data[offset + 3].toInt() and 0xFF
                val funcName = (funcNameHi shl 8) or funcNameLo
                val funcInstance = data[offset + 4].toInt() and 0xFF
                
                val friendlyName = FunctionNameMapper.getFriendlyName(funcName, funcInstance)
                
                deviceMetadata[deviceAddr] = DeviceMetadata(
                    deviceTableId = tableId,
                    deviceId = deviceId,
                    functionName = funcName,
                    functionInstance = funcInstance,
                    friendlyName = friendlyName
                )
                
                Log.i(TAG, "📋 [$tableId:$deviceId] fn=$funcName ($friendlyName)")
                
                // Publish metadata to MQTT
                val json = JSONObject()
                json.put("device_table_id", tableId)
                json.put("device_id", deviceId)
                json.put("function_name", funcName)
                json.put("function_instance", funcInstance)
                json.put("friendly_name", friendlyName)
                mqttPublisher.publishState(
                    "onecontrol/${device.address}/device/$tableId/$deviceId/metadata",
                    json.toString(), true
                )
                
                // Re-publish HA discovery with friendly name
                republishDiscoveryWithFriendlyName(tableId, deviceId, friendlyName)
            }
            
            offset += entrySize
            index++
        }
        
        Log.i(TAG, "📋 Parsed $index entries, ${deviceMetadata.size} total")
        
        // Save metadata to persistent cache
        saveMetadataToCache()
    }
    
    /**
     * Re-publish HA discovery with friendly name when metadata arrives.
     * 
     * CRITICAL: The guards checking haDiscoveryPublished MUST remain in place.
     * They prevent publishing ALL entity types (switch, light, cover_state, tank) for EVERY device.
     * Only entity types that were already published (based on actual device state) get republished
     * with friendly names. Without these guards, a single device creates 4 duplicate entities.
     * 
     * Example: A switch device publishes switch discovery when its state arrives.
     * When metadata arrives later, this only republishes the switch discovery with the friendly name.
     * It does NOT publish light/cover/tank discoveries for that switch.
     */
    private fun republishDiscoveryWithFriendlyName(tableId: Int, deviceId: Int, friendlyName: String) {
        val keyHex = "%02x%02x".format(tableId, deviceId)
        val deviceAddr = (tableId shl 8) or deviceId
        val prefix = mqttPublisher.topicPrefix
        val baseTopic = "onecontrol/${device.address}"
        
        Log.d(TAG, "🔍 Republish check for $tableId:$deviceId ($friendlyName) - published set: ${haDiscoveryPublished.filter { it.contains(keyHex) }}")
        
        // GUARD: Only republish switch if it was already published (prevents duplicate entities)
        if (haDiscoveryPublished.contains("switch_$keyHex")) {
            Log.i(TAG, "📢 Re-pub switch: $friendlyName")
            val stateTopic = "$baseTopic/device/$tableId/$deviceId/state"
            val commandTopic = "$baseTopic/command/switch/$tableId/$deviceId"
            val discovery = HomeAssistantMqttDiscovery.getSwitchDiscovery(
                gatewayMac = device.address,
                deviceAddr = deviceAddr,
                deviceName = friendlyName,
                stateTopic = "$prefix/$stateTopic",
                commandTopic = "$prefix/$commandTopic",
                appVersion = appVersion
            )
            val discoveryTopic = "$prefix/switch/onecontrol_ble_${device.address.replace(":", "").lowercase()}/switch_$keyHex/config"
            mqttPublisher.publishDiscovery(discoveryTopic, discovery.toString())
        }
        
        // GUARD: Only republish light if it was already published (prevents duplicate entities)
        if (haDiscoveryPublished.contains("light_$keyHex")) {
            Log.i(TAG, "📢 Re-pub light: $friendlyName")
            val stateTopic = "$baseTopic/device/$tableId/$deviceId/state"
            val brightnessTopic = "$baseTopic/device/$tableId/$deviceId/brightness"
            val commandTopic = "$baseTopic/command/dimmable/$tableId/$deviceId"
            val discovery = HomeAssistantMqttDiscovery.getDimmableLightDiscovery(
                gatewayMac = device.address,
                deviceAddr = deviceAddr,
                deviceName = friendlyName,
                stateTopic = "$prefix/$stateTopic",
                commandTopic = "$prefix/$commandTopic",
                brightnessTopic = "$prefix/$brightnessTopic",
                appVersion = appVersion
            )
            val discoveryTopic = "$prefix/light/onecontrol_ble_${device.address.replace(":", "").lowercase()}/light_$keyHex/config"
            mqttPublisher.publishDiscovery(discoveryTopic, discovery.toString())
        }
        
        // GUARD: Only republish tank if it was already published (prevents duplicate entities)
        if (haDiscoveryPublished.contains("tank_$keyHex")) {
            Log.i(TAG, "📢 Re-pub tank: $friendlyName")
            val stateTopic = "$baseTopic/device/$tableId/$deviceId/level"
            val discovery = HomeAssistantMqttDiscovery.getSensorDiscovery(
                gatewayMac = device.address,
                sensorName = friendlyName,
                stateTopic = "$prefix/$stateTopic",
                unit = "%",
                icon = "mdi:gauge",
                appVersion = appVersion
            )
            val discoveryTopic = "$prefix/sensor/onecontrol_ble_${device.address.replace(":", "").lowercase()}/tank_$keyHex/config"
            mqttPublisher.publishDiscovery(discoveryTopic, discovery.toString())
        }
        // Note: If tank discovery was deferred (no haDiscoveryPublished entry),
        // it will be published on next tank status event now that metadata is loaded
        
        // GUARD: Only republish cover state sensor if it was already published (prevents duplicate entities)
        if (haDiscoveryPublished.contains("cover_state_$keyHex")) {
            Log.i(TAG, "📢 Re-pub cover state sensor: $friendlyName")
            val stateTopic = "$baseTopic/device/$tableId/$deviceId/state"
            val discovery = HomeAssistantMqttDiscovery.getCoverStateSensorDiscovery(
                gatewayMac = device.address,
                deviceAddr = deviceAddr,
                deviceName = friendlyName,
                stateTopic = "$prefix/$stateTopic",
                appVersion = appVersion
            )
            val discoveryTopic = "$prefix/sensor/onecontrol_ble_${device.address.replace(":", "").lowercase()}/cover_state_$keyHex/config"
            mqttPublisher.publishDiscovery(discoveryTopic, discovery.toString())
        }
        // Note: If cover discovery was deferred (no haDiscoveryPublished entry),
        // it will be published on next cover status event now that metadata is loaded
    }
    
    /**
     * Handle SEED notification - calculate and send auth key
     */
    private fun handleSeedNotification(data: ByteArray) {
        Log.i(TAG, "🌱 Received seed value: ${data.joinToString(" ") { "%02X".format(it) }}")
        seedValue = data
        
        val authKey = calculateAuthKey(data, gatewayPin, gatewayCypher)
        Log.i(TAG, "🔑 Calculated auth key: ${authKey.joinToString(" ") { "%02X".format(it) }}")
        
        // Store auth key for TEA decryption of tank query responses
        try {
            sessionAuthKey = authKey
            Log.d(TAG, "🔐 Stored session key for tank decryption (${authKey.size} bytes)")
        } catch (e: Exception) {
            Log.e(TAG, "❌ Error storing session key: ${e.message}", e)
        }
        
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
            
            // Track pending command
            pendingCommands[commandId.toInt()] = 0x01
            
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
     * Send GetDevicesMetadata command to get function names
     */
    private fun sendGetDevicesMetadataCommand() {
        Log.i(TAG, "🔍 sendGetDevicesMetadataCommand()")
        
        if (!isConnected || currentGatt == null) {
            Log.w(TAG, "🔍 Cannot send - not connected")
            return
        }
        
        val writeChar = dataWriteChar
        if (writeChar == null) {
            Log.w(TAG, "🔍 No write characteristic")
            return
        }
        
        try {
            val commandId = getNextCommandId()
            val tableId = if (deviceTableId != 0x00.toByte()) deviceTableId else DEFAULT_DEVICE_TABLE_ID
            val command = encodeGetDevicesMetadataCommand(commandId, tableId)
            
            // Track pending command
            pendingCommands[commandId.toInt()] = 0x02
            
            Log.i(TAG, "🔍 GetDevicesMetadata: cmdId=$commandId, tableId=${tableId.toInt() and 0xFF}")
            
            val encoded = CobsDecoder.encode(command, prependStartFrame = true, useCrc = true)
            Log.i(TAG, "🔍 Encoded: ${encoded.joinToString(" ") { "%02X".format(it) }}")
            
            writeChar.value = encoded
            writeChar.writeType = BluetoothGattCharacteristic.WRITE_TYPE_NO_RESPONSE
            val result = currentGatt?.writeCharacteristic(writeChar)
            
            Log.i(TAG, "🔍 Sent GetDevicesMetadata: result=$result")
        } catch (e: Exception) {
            Log.e(TAG, "🔍 Failed: ${e.message}", e)
        }
    }
    
    // ========== COMMAND HANDLING ==========
    
    /**
     * Type-safe command handler for OneControl entities.
     * Maps entity types to their respective control methods.
     */
    private fun handleEntityCommand(entity: OneControlEntity, payload: String): Result<Unit> {
        return when (entity) {
            is OneControlEntity.Switch -> 
                controlSwitch(entity.tableId.toByte(), entity.deviceId.toByte(), payload)
            
            is OneControlEntity.DimmableLight -> 
                controlDimmableLight(entity.tableId.toByte(), entity.deviceId.toByte(), payload)
            
            is OneControlEntity.Cover -> {
                Log.w(TAG, "⚠️ Cover control is disabled for safety - use physical controls")
                Result.failure(Exception("Cover control disabled for safety"))
            }
            
            is OneControlEntity.Tank -> {
                Log.w(TAG, "⚠️ Tank sensors are read-only")
                Result.failure(Exception("Tank sensors are read-only"))
            }
            
            is OneControlEntity.SystemVoltageSensor,
            is OneControlEntity.SystemTemperatureSensor -> {
                Log.w(TAG, "⚠️ System sensors are read-only")
                Result.failure(Exception("System sensors are read-only"))
            }
        }
    }
    
    /**
     * Handle MQTT command - parses topic and routes to appropriate control method
     * Command topics: onecontrol/{MAC}/command/{type}/{tableId}/{deviceId}
     */
    fun handleCommand(commandTopic: String, payload: String): Result<Unit> {
        Log.i(TAG, "📤 Handling command: $commandTopic = $payload")
        
        if (!isConnected || !isAuthenticated || currentGatt == null) {
            Log.w(TAG, "❌ Cannot handle command - not ready (connected=$isConnected, auth=$isAuthenticated)")
            return Result.failure(Exception("Not connected or authenticated"))
        }
        
        // Parse topic: command/{type}/{tableId}/{deviceId} or command/{type}/{tableId}/{deviceId}/brightness
        val parts = commandTopic.split("/")
        
        // Find "command" segment and parse from there
        val commandIndex = parts.indexOf("command")
        if (commandIndex == -1 || parts.size < commandIndex + 4) {
            Log.w(TAG, "❌ Invalid command topic format: $commandTopic")
            return Result.failure(Exception("Invalid topic format"))
        }
        
        val kind = parts[commandIndex + 1]
        val tableIdStr = parts[commandIndex + 2]
        val deviceIdStr = parts[commandIndex + 3]
        val subTopic = if (parts.size > commandIndex + 4) parts[commandIndex + 4] else null
        
        val tableId = tableIdStr.toIntOrNull()
        val deviceId = deviceIdStr.toIntOrNull()
        
        if (tableId == null || deviceId == null) {
            Log.w(TAG, "❌ Invalid tableId or deviceId in topic: $commandTopic")
            return Result.failure(Exception("Invalid device address"))
        }
        
        return when (kind) {
            "switch" -> controlSwitch(tableId.toByte(), deviceId.toByte(), payload)
            "dimmable" -> {
                if (subTopic == "brightness") {
                    // Brightness-only command
                    val brightness = payload.toIntOrNull()
                    if (brightness != null) {
                        controlDimmableLight(tableId.toByte(), deviceId.toByte(), brightness.coerceIn(0, 255))
                    } else {
                        Result.failure(Exception("Invalid brightness value"))
                    }
                } else {
                    // On/Off or brightness command
                    controlDimmableLight(tableId.toByte(), deviceId.toByte(), payload)
                }
            }
            // SAFETY: Cover control disabled - RV awnings/slides have no limit switches
            // or overcurrent protection. Motors rely on operator judgment.
            // "cover" -> controlCover(tableId.toByte(), deviceId.toByte(), payload)
            "cover" -> {
                Log.w(TAG, "⚠️ Cover control is disabled for safety - use physical controls")
                Result.failure(Exception("Cover control disabled for safety"))
            }
            else -> {
                Log.w(TAG, "⚠️ Unknown command type: $kind")
                Result.failure(Exception("Unknown command type: $kind"))
            }
        }
    }
    
    /**
     * Control a switch (relay)
     */
    private fun controlSwitch(tableId: Byte, deviceId: Byte, payload: String): Result<Unit> {
        val turnOn = payload.equals("ON", ignoreCase = true) || 
                     payload == "1" || 
                     payload.equals("true", ignoreCase = true)
        
        Log.i(TAG, "📤 Switch control: table=$tableId, device=$deviceId, turnOn=$turnOn")
        
        val writeChar = dataWriteChar ?: return Result.failure(Exception("No write characteristic"))
        
        try {
            val commandId = getNextCommandId()
            val effectiveTableId = if (tableId == 0x00.toByte() && deviceTableId != 0x00.toByte()) {
                deviceTableId
            } else {
                tableId
            }
            
            val command = MyRvLinkCommandBuilder.buildActionSwitch(
                clientCommandId = commandId,
                deviceTableId = effectiveTableId,
                switchState = turnOn,
                deviceIds = listOf(deviceId)
            )
            
            val encoded = CobsDecoder.encode(command, prependStartFrame = true, useCrc = true)
            Log.d(TAG, "📤 Switch command: ${encoded.joinToString(" ") { "%02X".format(it) }}")
            
            writeChar.value = encoded
            writeChar.writeType = BluetoothGattCharacteristic.WRITE_TYPE_NO_RESPONSE
            val result = currentGatt?.writeCharacteristic(writeChar)
            
            Log.i(TAG, "📤 Sent switch command: table=${effectiveTableId.toInt() and 0xFF}, device=${deviceId.toInt() and 0xFF}, turnOn=$turnOn, result=$result")
            
            // Publish optimistic state update
            val baseTopic = "onecontrol/${device.address}"
            mqttPublisher.publishState("$baseTopic/device/${effectiveTableId.toInt() and 0xFF}/${deviceId.toInt() and 0xFF}/state", 
                if (turnOn) "ON" else "OFF", true)
            
            return if (result == true) Result.success(Unit) else Result.failure(Exception("Write failed"))
        } catch (e: Exception) {
            Log.e(TAG, "❌ Failed to send switch command: ${e.message}", e)
            return Result.failure(e)
        }
    }
    
    /**
     * Control a dimmable light - from payload string
     */
    private fun controlDimmableLight(tableId: Byte, deviceId: Byte, payload: String): Result<Unit> {
        // Parse payload: could be "ON", "OFF", or a brightness value, or JSON
        val brightness = when {
            payload.equals("ON", ignoreCase = true) || payload.equals("true", ignoreCase = true) -> 100
            payload.equals("OFF", ignoreCase = true) || payload == "0" || payload.equals("false", ignoreCase = true) -> 0
            payload.toIntOrNull() != null -> payload.toInt().coerceIn(0, 255)
            else -> {
                // Try JSON parse
                try {
                    val json = org.json.JSONObject(payload)
                    val state = json.optString("state", "")
                    val bri = json.optInt("brightness", -1)
                    when {
                        bri >= 0 -> bri.coerceIn(0, 255)
                        state.equals("ON", ignoreCase = true) -> 100
                        state.equals("OFF", ignoreCase = true) -> 0
                        else -> -1
                    }
                } catch (e: Exception) {
                    -1
                }
            }
        }
        
        if (brightness < 0) {
            Log.w(TAG, "❌ Cannot parse dimmable command: $payload")
            return Result.failure(Exception("Invalid payload"))
        }
        
        return controlDimmableLight(tableId, deviceId, brightness)
    }
    
    /**
     * Control a dimmable light - from brightness value
     * Includes debouncing from legacy app to coalesce rapid slider changes
     */
    private fun controlDimmableLight(tableId: Byte, deviceId: Byte, brightness: Int): Result<Unit> {
        Log.i(TAG, "📤 Dimmable control: table=$tableId, device=$deviceId, brightness=$brightness")
        
        val writeChar = dataWriteChar ?: return Result.failure(Exception("No write characteristic"))
        
        val effectiveTableId = if (tableId == 0x00.toByte() && deviceTableId != 0x00.toByte()) {
            deviceTableId
        } else {
            tableId
        }
        
        val tableIdInt = effectiveTableId.toInt() and 0xFF
        val deviceIdInt = deviceId.toInt() and 0xFF
        val key = "$tableIdInt:$deviceIdInt"
        
        // Handle OFF immediately (no debounce needed)
        if (brightness <= 0) {
            pendingDimmable.remove(key)
            pendingDimmableSend.remove(key)
            return sendDimmableCommand(writeChar, effectiveTableId, deviceId, 0)
        }
        
        // Handle brightness: treat all values including 255 as literal brightness
        val targetBrightness = brightness.coerceIn(1, 255)
        
        // Debounce: schedule the command after DIMMER_DEBOUNCE_MS
        // If another command comes in before then, it will replace this one
        val nowTs = System.currentTimeMillis()
        pendingDimmableSend[key] = targetBrightness to nowTs
        
        handler.postDelayed({
            val entry = pendingDimmableSend[key]
            if (entry != null && entry.second == nowTs) {
                // This is still the latest request - send it
                pendingDimmableSend.remove(key)
                sendDimmableCommand(writeChar, effectiveTableId, deviceId, targetBrightness)
                lastKnownDimmableBrightness[key] = targetBrightness
                pendingDimmable[key] = targetBrightness to System.currentTimeMillis()
                
                // Publish optimistic state update
                val baseTopic = "onecontrol/${device.address}"
                mqttPublisher.publishState("$baseTopic/device/$tableIdInt/$deviceIdInt/state", "ON", true)
                mqttPublisher.publishState("$baseTopic/device/$tableIdInt/$deviceIdInt/brightness", targetBrightness.toString(), true)
            }
        }, DIMMER_DEBOUNCE_MS)
        
        return Result.success(Unit)  // Return success immediately, actual send is debounced
    }
    
    /**
     * Actually send the dimmable command to BLE (called after debounce)
     */
    private fun sendDimmableCommand(writeChar: BluetoothGattCharacteristic, effectiveTableId: Byte, deviceId: Byte, brightness: Int): Result<Unit> {
        try {
            val commandId = getNextCommandId()
            val tableIdInt = effectiveTableId.toInt() and 0xFF
            val deviceIdInt = deviceId.toInt() and 0xFF
            
            val command = MyRvLinkCommandBuilder.buildActionDimmable(
                clientCommandId = commandId,
                deviceTableId = effectiveTableId,
                deviceId = deviceId,
                brightness = brightness
            )
            
            val encoded = CobsDecoder.encode(command, prependStartFrame = true, useCrc = true)
            Log.d(TAG, "📤 Dimmable command: ${encoded.joinToString(" ") { "%02X".format(it) }}")
            
            writeChar.value = encoded
            writeChar.writeType = BluetoothGattCharacteristic.WRITE_TYPE_NO_RESPONSE
            val result = currentGatt?.writeCharacteristic(writeChar)
            
            Log.i(TAG, "📤 Sent dimmable command: table=$tableIdInt, device=$deviceIdInt, brightness=$brightness, result=$result")
            
            // For OFF command, publish state immediately
            if (brightness == 0) {
                val baseTopic = "onecontrol/${device.address}"
                mqttPublisher.publishState("$baseTopic/device/$tableIdInt/$deviceIdInt/state", "OFF", true)
                mqttPublisher.publishState("$baseTopic/device/$tableIdInt/$deviceIdInt/brightness", "0", true)
            }
            
            return if (result == true) Result.success(Unit) else Result.failure(Exception("Write failed"))
        } catch (e: Exception) {
            Log.e(TAG, "❌ Failed to send dimmable command: ${e.message}", e)
            return Result.failure(e)
        }
    }
    
    /*
     * DISABLED FOR SAFETY: Cover control removed.
     * RV awnings and slides have NO limit switches and NO overcurrent protection.
     * Testing confirmed motors will continue drawing 19A (awning) / 39A (slide) at
     * mechanical limits with no auto-cutoff - relies entirely on operator judgment.
     * 
     * Control a cover (slide/awning) using H-Bridge command
     * Payload: "OPEN", "CLOSE", "STOP"
     * 
     * Using momentary FORWARD/REVERSE commands.
     * REVERSE = Extend/Open (motor runs in reverse direction)
     * FORWARD = Retract/Close (motor runs in forward direction)
     */
    @Suppress("unused")
    private fun controlCoverDISABLED(tableId: Byte, deviceId: Byte, payload: String): Result<Unit> {
        val command = when (payload.uppercase()) {
            "OPEN" -> MyRvLinkCommandBuilder.HBridgeCommand.REVERSE     // Extend/Open
            "CLOSE" -> MyRvLinkCommandBuilder.HBridgeCommand.FORWARD    // Retract/Close
            "STOP" -> MyRvLinkCommandBuilder.HBridgeCommand.STOP
            else -> {
                Log.w(TAG, "⚠️ Unknown cover command: $payload")
                return Result.failure(Exception("Unknown cover command: $payload"))
            }
        }
        
        Log.i(TAG, "📤 Cover control: table=$tableId, device=$deviceId, action=$payload (cmd=0x${command.toString(16)})")
        
        val writeChar = dataWriteChar ?: return Result.failure(Exception("No write characteristic"))
        
        try {
            val commandId = getNextCommandId()
            val effectiveTableId = if (tableId == 0x00.toByte() && deviceTableId != 0x00.toByte()) {
                deviceTableId
            } else {
                tableId
            }
            
            val bleCommand = MyRvLinkCommandBuilder.buildActionHBridge(
                clientCommandId = commandId,
                deviceTableId = effectiveTableId,
                deviceId = deviceId,
                command = command
            )
            
            val encoded = CobsDecoder.encode(bleCommand, prependStartFrame = true, useCrc = true)
            Log.d(TAG, "📤 Cover command: ${encoded.joinToString(" ") { "%02X".format(it) }}")
            
            writeChar.value = encoded
            writeChar.writeType = BluetoothGattCharacteristic.WRITE_TYPE_NO_RESPONSE
            val result = currentGatt?.writeCharacteristic(writeChar)
            
            Log.i(TAG, "📤 Sent cover command: table=${effectiveTableId.toInt() and 0xFF}, device=${deviceId.toInt() and 0xFF}, action=$payload, result=$result")
            
            // Publish optimistic state update
            val baseTopic = "onecontrol/${device.address}"
            val optimisticState = when (payload.uppercase()) {
                "OPEN" -> "opening"
                "CLOSE" -> "closing"
                "STOP" -> "stopped"
                else -> "unknown"
            }
            mqttPublisher.publishState("$baseTopic/device/${effectiveTableId.toInt() and 0xFF}/${deviceId.toInt() and 0xFF}/state", 
                optimisticState, true)
            
            return if (result == true) Result.success(Unit) else Result.failure(Exception("Write failed"))
        } catch (e: Exception) {
            Log.e(TAG, "❌ Failed to send cover command: ${e.message}", e)
            return Result.failure(e)
        }
    }
    
    // ========== END COMMAND HANDLING ==========
    
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
    
    /**
     * Encode GetDevicesMetadata command
     */
    private fun encodeGetDevicesMetadataCommand(commandId: UShort, deviceTableId: Byte): ByteArray {
        return byteArrayOf(
            (commandId.toInt() and 0xFF).toByte(),
            ((commandId.toInt() shr 8) and 0xFF).toByte(),
            0x02.toByte(),  // CommandType: GetDevicesMetadata
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
                    // Update diagnostic state (data_healthy depends on recent data)
                    publishDiagnosticsState()
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
    
    /**
     * Start connection watchdog - detects zombie states
     */
    private fun startWatchdog() {
        stopWatchdog()
        
        watchdogRunnable = object : Runnable {
            override fun run() {
                val currentGattInstance = currentGatt
                val timeSinceLastOp = System.currentTimeMillis() - lastSuccessfulOperationTime
                
                Log.d(TAG, "🐕 Watchdog check: isConnected=$isConnected, isAuth=$isAuthenticated, gatt=${currentGattInstance != null}, timeSince=${timeSinceLastOp}ms")
                
                // Detect zombie state: should be connected but isn't
                if (currentGattInstance != null && !isConnected) {
                    Log.e(TAG, "🐕 ZOMBIE STATE DETECTED: GATT exists but isConnected=false")
                    Log.e(TAG, "🐕 Forcing cleanup and reconnection...")
                    cleanup(currentGattInstance)
                    onDisconnect(device, -1)
                }
                // Detect stale connection: connected but no operations for 5 minutes
                else if (isConnected && isAuthenticated && timeSinceLastOp > 300000) {
                    Log.e(TAG, "🐕 STALE CONNECTION DETECTED: No operations for ${timeSinceLastOp/1000}s")
                    Log.e(TAG, "🐕 Forcing reconnection...")
                    currentGattInstance?.let { cleanup(it) }
                    onDisconnect(device, -1)
                }
                
                handler.postDelayed(this, WATCHDOG_INTERVAL_MS)
            }
        }
        
        handler.postDelayed(watchdogRunnable!!, WATCHDOG_INTERVAL_MS)
        Log.i(TAG, "🐕 Connection watchdog started (every ${WATCHDOG_INTERVAL_MS/1000}s)")
    }
    
    private fun stopWatchdog() {
        watchdogRunnable?.let {
            handler.removeCallbacks(it)
            watchdogRunnable = null
            Log.d(TAG, "🐕 Watchdog stopped")
        }
    }
    
    private fun cleanup(gatt: BluetoothGatt) {
        stopHeartbeat()
        stopWatchdog()
        stopActiveStreamReading()
        
        try {
            gatt.close()
        } catch (e: Exception) {
            Log.e(TAG, "Error closing GATT", e)
        }
        
        isConnected = false
        isAuthenticated = false
        mqttPublisher.updatePluginStatus(instanceId, false, false, false)
        // Publish offline availability status when disconnected
        publishAvailability(false)
        publishDiagnosticsState()  // Update diagnostic sensors on disconnect
        notificationsEnableStarted = false
        servicesDiscovered = false
        mtuReady = false
        seedValue = null
        currentGatt = null
        gatewayInfoReceived = false
    }
    
    /**
     * Publish availability status for Home Assistant
     * Called when device connects (online) or disconnects (offline)
     */
    private fun publishAvailability(online: Boolean) {
        val baseTopic = "onecontrol/${device.address}"
        val message = if (online) "online" else "offline"
        mqttPublisher.publishState("$baseTopic/availability", message, true)
        Log.d(TAG, "📡 Published availability: $baseTopic/availability = $message")
    }
    
    // ==================== DIAGNOSTIC SENSORS ====================
    
    /**
     * Compute if data stream is healthy (recent frames seen within timeout).
     */
    private fun computeDataHealthy(): Boolean {
        // OneControl is event-driven - it only sends notifications when RV-C state changes.
        // Silence doesn't indicate unhealthy data, just that nothing has changed.
        // Data is healthy as long as we're connected and authenticated.
        return isConnected && isAuthenticated
    }
    
    /**
     * Get current diagnostic status for UI display.
     */
    fun getDiagnosticStatus(): DiagnosticStatus {
        return DiagnosticStatus(
            devicePaired = true,  // If we have a callback, device is paired
            bleConnected = isConnected,
            dataHealthy = computeDataHealthy()
        )
    }
    
    /**
     * Publish Home Assistant discovery payloads for diagnostic binary sensors.
     */
    private fun publishDiagnosticsDiscovery() {
        if (diagnosticsDiscoveryPublished) return
        
        val macId = device.address.replace(":", "").lowercase()
        val nodeId = "onecontrol_$macId"
        val prefix = mqttPublisher.topicPrefix
        val baseTopic = "onecontrol/${device.address}"
        
        // Device info for HA discovery - MUST match HomeAssistantMqttDiscovery.getDeviceInfo()
        // to ensure all entities group under one device
        val deviceInfo = HomeAssistantMqttDiscovery.getDeviceInfo(device.address)
        
        // Diagnostic sensors to publish
        val diagnostics = listOf(
            Triple("authenticated", "Authenticated", "diag/authenticated"),
            Triple("connected", "Connected", "diag/connected"),
            Triple("data_healthy", "Data Healthy", "diag/data_healthy")
        )
        
        diagnostics.forEach { (objectId, name, stateTopic) ->
            val uniqueId = "onecontrol_${macId}_diag_$objectId"
            val discoveryTopic = "$prefix/binary_sensor/$nodeId/$objectId/config"
            
            val payload = JSONObject().apply {
                put("name", name)
                put("unique_id", uniqueId)
                put("state_topic", "$prefix/$baseTopic/$stateTopic")
                put("payload_on", "ON")
                put("payload_off", "OFF")
                put("availability_topic", "$prefix/$baseTopic/availability")
                put("payload_available", "online")
                put("payload_not_available", "offline")
                put("entity_category", "diagnostic")
                put("device", deviceInfo)
            }.toString()
            
            mqttPublisher.publishDiscovery(discoveryTopic, payload)
            Log.i(TAG, "📡 Published diagnostic discovery: $objectId")
        }
        
        diagnosticsDiscoveryPublished = true
        
        // Publish availability state immediately after discovery
        publishAvailability(true)
        
        // Subscribe to command topics: onecontrol/{MAC}/command/#
        // Handles: switch, dimmable, and other command types
        // Process commands directly without posting to main handler to avoid queue delays
        mqttPublisher.subscribeToCommands("$baseTopic/command/#") { topic, payload ->
            val result = handleCommand(topic, payload)
            Log.d(TAG, "📤 Command processed: $topic = $payload, success=${result.isSuccess}")
        }
        Log.i(TAG, "📡 Subscribed to command topics: $baseTopic/command/#")
    }
    
    /**
     * Publish current diagnostic state to MQTT.
     */
    private fun publishDiagnosticsState() {
        val isPaired = isAuthenticated  // Use protocol-level auth, not OS bonding
        val dataHealthy = computeDataHealthy()
        val baseTopic = "onecontrol/${device.address}"
        
        // Publish to MQTT
        mqttPublisher.publishState("$baseTopic/diag/authenticated", if (isPaired) "ON" else "OFF", true)
        mqttPublisher.publishState("$baseTopic/diag/connected", if (isConnected) "ON" else "OFF", true)
        mqttPublisher.publishState("$baseTopic/diag/data_healthy", if (dataHealthy) "ON" else "OFF", true)
        
        // Update UI status for this plugin
        mqttPublisher.updatePluginStatus(
            pluginId = instanceId,
            connected = isConnected,
            authenticated = isPaired,
            dataHealthy = dataHealthy
        )
        
        Log.d(TAG, "📡 Published diagnostic state: authenticated=$isPaired, connected=$isConnected, dataHealthy=$dataHealthy")
    }
    
    /**
     * Diagnostic status data class for UI.
     */
    data class DiagnosticStatus(
        val devicePaired: Boolean,
        val bleConnected: Boolean,
        val dataHealthy: Boolean
    )
}
