package com.blemqttbridge.plugins.hughes

import android.bluetooth.*
import android.bluetooth.le.ScanRecord
import android.content.Context
import android.os.Handler
import android.os.Looper
import android.util.Log
import com.blemqttbridge.core.interfaces.BleDevicePlugin
import com.blemqttbridge.core.interfaces.MqttPublisher
import com.blemqttbridge.core.interfaces.PluginConfig
import com.blemqttbridge.plugins.hughes.protocol.HughesConstants
import org.json.JSONArray
import org.json.JSONObject
import java.util.*

/**
 * Hughes Power Watchdog Surge Protector BLE Device Plugin
 * 
 * Supports: PWD/PWS surge protectors (all generations)
 * 
 * Protocol: BLE notifications on 0xFFE2 deliver two 20-byte chunks combined into 40-byte frames
 * - No authentication required
 * - No pairing required (devices may optionally pair for reliability)
 * - Frames contain voltage, amperage, watts, cumulative energy, error code, line indicator
 */
class HughesWatchdogDevicePlugin : BleDevicePlugin {
    
    companion object {
        private const val TAG = "HughesPlugin"
        const val PLUGIN_ID = "hughes_watchdog"
        const val PLUGIN_VERSION = "1.0.0"
    }
    
    override val pluginId: String = PLUGIN_ID
    override var instanceId: String = PLUGIN_ID
    override val supportsMultipleInstances: Boolean = false
    override val displayName: String = "Hughes Power Watchdog"
    
    private var context: Context? = null
    private var config: PluginConfig? = null
    
    // Configuration from settings
    private var watchdogMac: String = ""
    private var expectedName: String? = null
    private var forceVersion: String? = null  // "gen1", "gen2", or null for auto-detect
    
    // Strong reference to callback to prevent GC
    private var gattCallback: BluetoothGattCallback? = null
    private var currentCallback: HughesGattCallback? = null
    
    override fun initialize(context: Context?, config: PluginConfig) {
        Log.i(TAG, "Initializing Hughes Watchdog Plugin v$PLUGIN_VERSION")
        this.context = context
        this.config = config
        
        // Load configuration
        watchdogMac = config.getString("watchdog_mac", watchdogMac)
        expectedName = config.getString("expected_name", "").takeIf { it.isNotEmpty() }
        forceVersion = config.getString("force_version", "").takeIf { it.isNotEmpty() }
        
        Log.i(TAG, "Configured for Watchdog MAC: $watchdogMac (name: $expectedName, gen: $forceVersion)")
    }
    
    override fun matchesDevice(device: BluetoothDevice, scanRecord: ScanRecord?): Boolean {
        // SECURITY: Only match on exact configured MAC address
        if (watchdogMac.isBlank()) {
            return false
        }
        
        val deviceAddress = device.address
        if (!deviceAddress.equals(watchdogMac, ignoreCase = true)) {
            return false
        }
        
        // Optional name assertion
        if (expectedName != null) {
            val deviceName = device.name
            if (deviceName != expectedName) {
                Log.w(TAG, "Device $deviceAddress matched MAC but name mismatch (expected: $expectedName, got: $deviceName)")
                return false
            }
        }
        
        Log.d(TAG, "Device matched by configured MAC: $deviceAddress")
        return true
    }
    
    override fun getConfiguredDevices(): List<String> {
        return if (watchdogMac.isNotBlank()) listOf(watchdogMac) else emptyList()
    }
    
    override fun createGattCallback(
        device: BluetoothDevice,
        context: Context,
        mqttPublisher: MqttPublisher,
        onDisconnect: (BluetoothDevice, Int) -> Unit
    ): BluetoothGattCallback {
        Log.i(TAG, "Creating GATT callback for ${device.address}")
        val callback = HughesGattCallback(device, context, mqttPublisher, instanceId, onDisconnect)
        gattCallback = callback
        currentCallback = callback
        return callback
    }
    
    override fun onGattConnected(device: BluetoothDevice, gatt: BluetoothGatt) {
        Log.i(TAG, "GATT connected for ${device.address}")
        // Callback handles everything
    }
    
    override fun onDeviceDisconnected(device: BluetoothDevice) {
        Log.i(TAG, "Device disconnected: ${device.address}")
        currentCallback = null
    }
    
    override fun getMqttBaseTopic(device: BluetoothDevice): String {
        return "hughes/${device.address}"
    }
    
    override fun getDiscoveryPayloads(device: BluetoothDevice): List<Pair<String, String>> {
        // Discovery is handled by the callback
        return emptyList()
    }
    
    override suspend fun handleCommand(
        device: BluetoothDevice,
        commandTopic: String,
        payload: String
    ): Result<Unit> {
        // Phase 2: Commands not yet implemented
        Log.w(TAG, "Commands not yet supported (Phase 2)")
        return Result.failure(Exception("Commands not yet implemented"))
    }
    
    override fun destroy() {
        Log.i(TAG, "Destroying Hughes Watchdog Plugin")
        currentCallback = null
        gattCallback = null
    }
}

/**
 * GATT Callback for Hughes Power Watchdog.
 * 
 * Handles:
 * - Connection establishment
 * - Service/characteristic discovery
 * - Notification subscription (no authentication needed)
 * - 40-byte frame assembly from two 20-byte chunks
 * - Metric parsing (volts, amps, watts, energy, error, line)
 * - MQTT state publishing
 * - Home Assistant discovery
 */
class HughesGattCallback(
    private val device: BluetoothDevice,
    private val context: Context,
    private val mqttPublisher: MqttPublisher,
    private val instanceId: String,
    private val onDisconnect: (BluetoothDevice, Int) -> Unit
) : BluetoothGattCallback() {
    
    private var gatt: BluetoothGatt? = null
    private var notifyChar: BluetoothGattCharacteristic? = null
    
    private val mainHandler = Handler(Looper.getMainLooper())
    private var discoveryPublished = false
    
    // Frame assembly
    private var chunk1: ByteArray? = null
    private var chunk1Timestamp: Long = 0
    private val chunkTimeout = HughesConstants.FRAME_TIMEOUT_MS
    
    // Current parsed state
    private var currentVolts: Double = 0.0
    private var currentAmps: Double = 0.0
    private var currentWatts: Double = 0.0
    private var currentEnergy: Double = 0.0
    private var currentFrequency: Double = 0.0
    private var currentError: Int = 0
    private var currentLine: Int = 1
    
    private var isConnected = false
    
    companion object {
        private const val TAG = "HughesGattCallback"
    }
    
    private val baseTopic: String
        get() = "hughes_watchdog_${device.address.replace(":", "").lowercase()}"
    
    // ===== LIFECYCLE CALLBACKS =====
    
    override fun onConnectionStateChange(gatt: BluetoothGatt, status: Int, newState: Int) {
        Log.i(TAG, "Connection state: $newState (status: $status)")
        
        when (status) {
            BluetoothGatt.GATT_SUCCESS -> {
                when (newState) {
                    BluetoothProfile.STATE_CONNECTED -> {
                        Log.i(TAG, "Connected to ${device.address}")
                        this.gatt = gatt
                        isConnected = true
                        publishAvailability(true)
                        
                        // Discover services after brief delay
                        mainHandler.postDelayed({
                            gatt.discoverServices()
                        }, HughesConstants.SERVICE_DISCOVERY_DELAY_MS)
                    }
                    BluetoothProfile.STATE_DISCONNECTED -> {
                        Log.w(TAG, "Disconnected from ${device.address}")
                        cleanup()
                        onDisconnect(device, status)
                    }
                }
            }
            else -> {
                Log.e(TAG, "Connection failed with status: $status")
                cleanup()
                onDisconnect(device, status)
            }
        }
    }
    
    override fun onServicesDiscovered(gatt: BluetoothGatt, status: Int) {
        Log.i(TAG, "Services discovered: status=$status, count=${gatt.services.size}")
        if (status != BluetoothGatt.GATT_SUCCESS) {
            Log.e(TAG, "Service discovery failed: $status")
            return
        }
        
        // Find Hughes service
        val service = gatt.getService(HughesConstants.SERVICE_UUID)
        if (service == null) {
            Log.e(TAG, "Hughes service not found!")
            return
        }
        
        // Get notify characteristic
        notifyChar = service.getCharacteristic(HughesConstants.NOTIFY_CHARACTERISTIC_UUID)
        if (notifyChar == null) {
            Log.e(TAG, "Notify characteristic not found!")
            return
        }
        
        Log.i(TAG, "All characteristics found")
        
        // Enable notifications
        mainHandler.postDelayed({
            enableNotifications()
        }, HughesConstants.OPERATION_DELAY_MS)
    }
    
    /**
     * Enable notifications on the notify characteristic (FFE2)
     */
    private fun enableNotifications() {
        val char = notifyChar ?: return
        val g = gatt ?: return
        
        Log.i(TAG, "Enabling notifications on FFE2...")
        
        // Enable local notifications
        if (!g.setCharacteristicNotification(char, true)) {
            Log.e(TAG, "Failed to set characteristic notification")
            return
        }
        
        // Write to CCCD to enable remote notifications
        val descriptor = char.getDescriptor(HughesConstants.CCCD_UUID)
        if (descriptor != null) {
            descriptor.value = BluetoothGattDescriptor.ENABLE_NOTIFICATION_VALUE
            if (!g.writeDescriptor(descriptor)) {
                Log.e(TAG, "Failed to write CCCD descriptor")
            } else {
                Log.i(TAG, "CCCD write initiated")
            }
        } else {
            Log.w(TAG, "CCCD descriptor not found")
        }
    }
    
    override fun onDescriptorWrite(gatt: BluetoothGatt, descriptor: BluetoothGattDescriptor, status: Int) {
        if (descriptor.uuid == HughesConstants.CCCD_UUID) {
            if (status == BluetoothGatt.GATT_SUCCESS) {
                Log.i(TAG, "Notifications enabled, waiting for data...")
                publishDiagnosticsDiscovery()
                publishDiagnosticsState()
            } else {
                Log.e(TAG, "Failed to enable notifications: $status")
            }
        }
    }
    
    // Android 13+ (API 33+) callback with direct value parameter
    override fun onCharacteristicChanged(
        gatt: BluetoothGatt,
        characteristic: BluetoothGattCharacteristic,
        value: ByteArray
    ) {
        if (characteristic.uuid != HughesConstants.NOTIFY_CHARACTERISTIC_UUID) {
            return
        }
        
        handleCharacteristicData(value)
    }
    
    // Older Android versions - deprecated in API 33
    @Deprecated("Deprecated in API 33")
    override fun onCharacteristicChanged(gatt: BluetoothGatt, characteristic: BluetoothGattCharacteristic) {
        if (characteristic.uuid != HughesConstants.NOTIFY_CHARACTERISTIC_UUID) {
            return
        }
        
        val data = characteristic.value
        if (data != null) {
            handleCharacteristicData(data)
        }
    }
    
    /**
     * Handle characteristic data for both API 33+ and older Android versions
     */
    private fun handleCharacteristicData(data: ByteArray) {
        if (data.size != HughesConstants.CHUNK_SIZE) {
            Log.w(TAG, "Unexpected chunk size: ${data.size} (expected ${HughesConstants.CHUNK_SIZE})")
            return
        }
        
        // First chunk or subsequent chunk assembly
        if (chunk1 == null) {
            // This is the first chunk - store it and wait for second
            chunk1 = data
            chunk1Timestamp = System.currentTimeMillis()
            Log.d(TAG, "Received chunk 1 (${data.size} bytes), waiting for chunk 2...")
        } else {
            // This is the second chunk - assemble frame
            val age = System.currentTimeMillis() - chunk1Timestamp
            if (age > chunkTimeout) {
                Log.w(TAG, "Chunk 1 expired (${age}ms > ${chunkTimeout}ms), discarding")
                chunk1 = data
                chunk1Timestamp = System.currentTimeMillis()
                return
            }
            
            Log.d(TAG, "Received chunk 2 (${data.size} bytes), assembling frame...")
            val frame = ByteArray(HughesConstants.FRAME_SIZE)
            System.arraycopy(chunk1!!, 0, frame, 0, HughesConstants.CHUNK_SIZE)
            System.arraycopy(data, 0, frame, HughesConstants.CHUNK_SIZE, HughesConstants.CHUNK_SIZE)
            chunk1 = null
            
            // Parse frame
            parseFrame(frame)
        }
    }
    
    // ===== FRAME PARSING =====
    
    /**
     * Parse a complete 40-byte frame
     */
    private fun parseFrame(frame: ByteArray) {
        if (frame.size != HughesConstants.FRAME_SIZE) {
            Log.e(TAG, "Invalid frame size: ${frame.size}")
            return
        }
        
        try {
            // Extract fields (big-endian)
            currentVolts = readInt32BE(frame, HughesConstants.OFFSET_VOLTS) / 10000.0
            currentAmps = readInt32BE(frame, HughesConstants.OFFSET_AMPS) / 10000.0
            currentWatts = readInt32BE(frame, HughesConstants.OFFSET_WATTS) / 10000.0
            currentEnergy = readInt32BE(frame, HughesConstants.OFFSET_ENERGY) / 10000.0
            val rawFrequency = readInt32BE(frame, HughesConstants.OFFSET_FREQUENCY)
            currentFrequency = rawFrequency / 100.0
            val freqBytes = frame.copyOfRange(HughesConstants.OFFSET_FREQUENCY, HughesConstants.OFFSET_FREQUENCY + 4)
            currentError = frame[HughesConstants.OFFSET_ERROR].toInt() and 0xFF
            
            // Detect line from marker bytes (simplified: assume all non-zero = line 2)
            val marker0 = frame[HughesConstants.OFFSET_LINE_MARKER].toInt() and 0xFF
            val marker1 = frame[HughesConstants.OFFSET_LINE_MARKER + 1].toInt() and 0xFF
            val marker2 = frame[HughesConstants.OFFSET_LINE_MARKER + 2].toInt() and 0xFF
            currentLine = if (marker0 == 0 && marker1 == 0 && marker2 == 0) 1 else 2
            
            Log.d(
                TAG,
                "Parsed frame: V=${String.format("%.2f", currentVolts)}, " +
                    "A=${String.format("%.2f", currentAmps)}, " +
                    "W=${String.format("%.2f", currentWatts)}, " +
                    "E=${String.format("%.2f", currentEnergy)}, " +
                    "F=${String.format("%.2f", currentFrequency)} (raw=$rawFrequency, bytes=${freqBytes.joinToString(" "){ String.format("%02X", it) }}), " +
                    "Line=$currentLine, Error=$currentError"
            )
            
            // Publish to MQTT
            publishMetrics()
            publishDiagnostics()
            publishDiagnosticsState()  // Publish health indicator with every frame
            
            // Publish HA discovery once
            if (!discoveryPublished) {
                publishDiscovery()
                publishDiagnosticsDiscovery()  // Publish diagnostic sensor discovery
                discoveryPublished = true
            }
            
            // Update plugin status
            mqttPublisher.updatePluginStatus(
                pluginId = instanceId,
                connected = true,
                authenticated = false,  // No auth needed
                dataHealthy = true
            )
        } catch (e: Exception) {
            Log.e(TAG, "Error parsing frame", e)
        }
    }
    
    /**
     * Read big-endian 32-bit signed integer from byte array
     */
    private fun readInt32BE(data: ByteArray, offset: Int): Int {
        if (offset + 3 >= data.size) return 0
        return (
            ((data[offset].toInt() and 0xFF) shl 24) or
            ((data[offset + 1].toInt() and 0xFF) shl 16) or
            ((data[offset + 2].toInt() and 0xFF) shl 8) or
            (data[offset + 3].toInt() and 0xFF)
        )
    }
    
    // ===== MQTT PUBLISHING =====
    
    /**
     * Publish metric values
     */
    private fun publishMetrics() {
        val ts = System.currentTimeMillis()
        val lineSuffix = "_l${currentLine}"
        
        // Publish per-line metrics
        mqttPublisher.publishState("$baseTopic/volts$lineSuffix", String.format("%.2f", currentVolts), false)
        mqttPublisher.publishState("$baseTopic/amps$lineSuffix", String.format("%.2f", currentAmps), false)
        mqttPublisher.publishState("$baseTopic/watts$lineSuffix", String.format("%.2f", currentWatts), false)
        mqttPublisher.publishState("$baseTopic/energy$lineSuffix", String.format("%.2f", currentEnergy), false)
        mqttPublisher.publishState("$baseTopic/frequency$lineSuffix", String.format("%.2f", currentFrequency), false)
        
        // Combined state for this line
        val stateJson = JSONObject().apply {
            put("voltage", String.format("%.2f", currentVolts))
            put("current", String.format("%.2f", currentAmps))
            put("power", String.format("%.2f", currentWatts))
            put("energy", String.format("%.2f", currentEnergy))
            put("frequency", String.format("%.2f", currentFrequency))
            put("error_code", currentError)
            put("error", HughesConstants.ERROR_LABELS[currentError] ?: "Unknown")
            put("timestamp", ts)
        }
        mqttPublisher.publishState("$baseTopic/state$lineSuffix", stateJson.toString(), false)
    }
    
    /**
     * Publish diagnostics (error status)
     */
    private fun publishDiagnostics() {
        val errorLabel = HughesConstants.ERROR_LABELS[currentError] ?: "Unknown"
        mqttPublisher.publishState("$baseTopic/error", errorLabel, false)
        mqttPublisher.publishState("$baseTopic/error_code", currentError.toString(), false)
    }
    
    /**
     * Publish availability status
     */
    private fun publishAvailability(online: Boolean) {
        mqttPublisher.publishAvailability("$baseTopic/availability", online)
    }
    
    /**
     * Publish Home Assistant discovery payloads
     */
    private fun publishDiscovery() {
        Log.i(TAG, "Publishing HA discovery for $baseTopic")
        
        val address = device.address
        val macId = device.address.replace(":", "").lowercase()
        val deviceId = "hughes_watchdog_$macId"
        val deviceName = "Hughes Watchdog ${device.address}"
        
        // Get app version
        val appVersion = try {
            context?.packageManager?.getPackageInfo(context?.packageName ?: "", 0)?.versionName ?: "unknown"
        } catch (e: Exception) {
            "unknown"
        }
        
        // Create device info shared by all sensors
        val deviceInfo = JSONObject().apply {
            put("identifiers", JSONArray().put(deviceId))
            put("name", deviceName)
            put("model", "Power Watchdog Surge Protector")
            put("manufacturer", "Hughes")
            put("sw_version", appVersion)
            put("connections", JSONArray().put(JSONArray().put("mac").put(device.address)))
        }
        
        // Publish sensors for Line 1
        publishDiscoverySensor("volts_l1", "L1 Voltage", "V", "voltage", deviceInfo, 1)
        publishDiscoverySensor("amps_l1", "L1 Current", "A", "current", deviceInfo, 1)
        publishDiscoverySensor("watts_l1", "L1 Power", "W", "power", deviceInfo, 1)
        publishDiscoverySensor("energy_l1", "Energy", "kWh", "energy", deviceInfo, 1)
        publishDiscoverySensor("frequency_l1", "L1 Frequency", "Hz", "frequency", deviceInfo, 1)
        
        // Publish sensors for Line 2
        publishDiscoverySensor("volts_l2", "L2 Voltage", "V", "voltage", deviceInfo, 2)
        publishDiscoverySensor("amps_l2", "L2 Current", "A", "current", deviceInfo, 2)
        publishDiscoverySensor("watts_l2", "L2 Power", "W", "power", deviceInfo, 2)
        publishDiscoverySensor("energy_l2", "Cumulative Energy", "kWh", "energy", deviceInfo, 2)
        publishDiscoverySensor("frequency_l2", "L2 Frequency", "Hz", "frequency", deviceInfo, 2)
        
        // Publish single error sensor (not line-specific)
        publishDiscoverySensor("error", "Error Status", null, null, deviceInfo, 0)
        
        // Publish availability state immediately after discovery
        publishAvailability(true)
    }
    
    /**
     * Helper to publish a single sensor discovery
     */
    private fun publishDiscoverySensor(field: String, name: String, unit: String?, deviceClass: String?, deviceInfo: JSONObject, line: Int) {
        val discoveryTopic = "${mqttPublisher.topicPrefix}/sensor/${instanceId}_$field/config"
        
        val payload = JSONObject().apply {
            put("name", name)
            // MqttPublisher adds topic_prefix, so state topics must match what's published
            put("state_topic", "${mqttPublisher.topicPrefix}/$baseTopic/${field}")
            if (unit != null) {
                put("unit_of_measurement", unit)
            }
            if (deviceClass != null) {
                put("device_class", deviceClass)
                put("state_class", if (deviceClass == "energy") "total_increasing" else "measurement")
            }
            put("unique_id", "${instanceId}_$field")
            put("device", deviceInfo)
            put("availability_topic", "${mqttPublisher.topicPrefix}/$baseTopic/availability")
        }
        
        mqttPublisher.publishDiscovery(discoveryTopic, payload.toString())
    }
    
    /**
     * Publish diagnostics discovery (health status binary sensor)
     */
    private fun publishDiagnosticsDiscovery() {
        if (discoveryPublished) return  // Already published
        
        val macId = device.address.replace(":", "").lowercase()
        val deviceId = "hughes_watchdog_$macId"
        val nodeId = "${instanceId}_data_healthy"
        val prefix = mqttPublisher.topicPrefix
        
        // Get app version
        val appVersion = try {
            context?.packageManager?.getPackageInfo(context?.packageName ?: "", 0)?.versionName ?: "unknown"
        } catch (e: Exception) {
            "unknown"
        }
        
        // Device info for HA discovery - must match publishDiscovery() to group entities together
        val deviceInfo = JSONObject().apply {
            put("identifiers", JSONArray().put(deviceId))
            put("name", "Hughes Watchdog ${device.address}")
            put("model", "Power Watchdog Surge Protector")
            put("manufacturer", "Hughes")
            put("sw_version", appVersion)
            put("connections", JSONArray().put(JSONArray().put("mac").put(device.address)))
        }
        
        // Binary diagnostic sensor for health status
        val uniqueId = "hughes_watchdog_${macId}_diag_data_healthy"
        val discoveryTopic = "$prefix/binary_sensor/$nodeId/config"
        
        val payload = JSONObject().apply {
            put("name", "Data Healthy")
            put("unique_id", uniqueId)
            put("state_topic", "$prefix/$baseTopic/diag/data_healthy")
            put("payload_on", "ON")
            put("payload_off", "OFF")
            put("availability_topic", "$prefix/$baseTopic/availability")
            put("payload_available", "online")
            put("payload_not_available", "offline")
            put("entity_category", "diagnostic")
            put("device", deviceInfo)
        }.toString()
        
        mqttPublisher.publishDiscovery(discoveryTopic, payload)
        Log.i(TAG, "Published diagnostic discovery: data_healthy")
    }
    
    /**
     * Publish initial diagnostics state (health indicator)
     */
    private fun publishDiagnosticsState() {
        // Published after every successful frame parse - health is ON when data is healthy
        mqttPublisher.publishState("$baseTopic/diag/data_healthy", "ON", true)
        Log.d(TAG, "Published diagnostic state: data_healthy=ON")
    }
    
    /**
     * Cleanup on disconnect
     */
    private fun cleanup() {
        isConnected = false
        chunk1 = null
        mainHandler.removeCallbacksAndMessages(null)
        // Publish offline status when disconnected
        publishAvailability(false)
        // Also publish health as OFF when disconnected
        mqttPublisher.publishState("$baseTopic/diag/data_healthy", "OFF", true)
        Log.d(TAG, "Published diagnostic state: data_healthy=OFF (disconnected)")
    }
}
