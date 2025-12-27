package com.blemqttbridge.plugins.easytouch

import android.bluetooth.*
import android.bluetooth.le.ScanRecord
import android.content.Context
import android.os.Handler
import android.os.Looper
import android.util.Log
import com.blemqttbridge.core.interfaces.BleDevicePlugin
import com.blemqttbridge.util.DebugLog
import com.blemqttbridge.core.interfaces.MqttPublisher
import com.blemqttbridge.core.interfaces.PluginConfig
import com.blemqttbridge.plugins.easytouch.protocol.EasyTouchConstants
import org.json.JSONArray
import org.json.JSONObject
import java.nio.charset.StandardCharsets
import java.util.*

/**
 * EasyTouch Thermostat BLE Device Plugin
 * 
 * This plugin handles Micro-Air EasyTouch RV thermostats via BLE.
 * It implements the BleDevicePlugin interface and owns the complete
 * BluetoothGattCallback for device communication.
 * 
 * Protocol based on: https://github.com/k3vmcd/ha-micro-air-easytouch
 */
class EasyTouchDevicePlugin : BleDevicePlugin {
    
    companion object {
        private const val TAG = "EasyTouchPlugin"
        const val PLUGIN_ID = "easytouch"
        const val PLUGIN_VERSION = "1.0.0"
    }
    
    override val pluginId: String = PLUGIN_ID
    override val displayName: String = "EasyTouch Thermostat"
    
    private lateinit var context: Context
    private var config: PluginConfig? = null
    
    // Configuration from settings
    private var thermostatMac: String = ""
    private var thermostatPassword: String = ""
    
    // Strong reference to callback to prevent GC
    private var gattCallback: BluetoothGattCallback? = null
    
    // Current callback instance for command handling
    private var currentCallback: EasyTouchGattCallback? = null
    
    override fun initialize(context: Context, config: PluginConfig) {
        Log.i(TAG, "Initializing EasyTouch Device Plugin v$PLUGIN_VERSION")
        this.context = context
        this.config = config
        
        // Load configuration
        thermostatMac = config.getString("thermostat_mac", "")
        thermostatPassword = config.getString("thermostat_password", "")
        
        if (thermostatMac.isNotEmpty()) {
            Log.i(TAG, "Configured for thermostat: $thermostatMac")
        } else {
            Log.w(TAG, "No thermostat MAC configured!")
        }
    }
    
    override fun matchesDevice(device: BluetoothDevice, scanRecord: ScanRecord?): Boolean {
        val deviceAddress = device.address
        val deviceName = device.name
        
        // Match by configured MAC address
        if (thermostatMac.isNotEmpty() && deviceAddress.equals(thermostatMac, ignoreCase = true)) {
            DebugLog.d(TAG, "Device matched by MAC: $deviceAddress")
            return true
        }
        
        // Match by device name prefix
        if (deviceName?.startsWith(EasyTouchConstants.DEVICE_NAME_PREFIX, ignoreCase = true) == true) {
            DebugLog.d(TAG, "Device matched by name: $deviceName")
            return true
        }
        
        // Match by advertised service UUID
        val advertisedServices = scanRecord?.serviceUuids
        if (advertisedServices != null) {
            for (uuid in advertisedServices) {
                if (uuid.uuid == EasyTouchConstants.SERVICE_UUID) {
                    DebugLog.d(TAG, "Device matched by service UUID: ${uuid.uuid}")
                    return true
                }
            }
        }
        
        return false
    }
    
    override fun getConfiguredDevices(): List<String> {
        return if (thermostatMac.isNotEmpty()) {
            listOf(thermostatMac)
        } else {
            emptyList()
        }
    }
    
    override fun createGattCallback(
        device: BluetoothDevice,
        context: Context,
        mqttPublisher: MqttPublisher,
        onDisconnect: (BluetoothDevice, Int) -> Unit
    ): BluetoothGattCallback {
        Log.i(TAG, "Creating GATT callback for ${device.address}")
        val callback = EasyTouchGattCallback(
            device = device,
            context = context,
            mqttPublisher = mqttPublisher,
            onDisconnect = onDisconnect,
            password = thermostatPassword
        )
        Log.i(TAG, "Created callback with hashCode=${callback.hashCode()}")
        // Keep strong reference to prevent GC
        gattCallback = callback
        currentCallback = callback
        return callback
    }
    
    override fun onGattConnected(device: BluetoothDevice, gatt: BluetoothGatt) {
        Log.i(TAG, "GATT connected for ${device.address}")
    }
    
    override fun onDeviceDisconnected(device: BluetoothDevice) {
        Log.i(TAG, "Device disconnected: ${device.address}")
    }
    
    override fun getMqttBaseTopic(device: BluetoothDevice): String {
        return "easytouch/${device.address}"
    }
    
    override fun getCommandTopicPattern(device: BluetoothDevice): String {
        // EasyTouch uses zone-based topics: easytouch/{MAC}/zone_N/command/#
        // Also supports device-level commands: easytouch/{MAC}/device/command/reboot
        // The + wildcard matches zone_0, zone_1, device, etc.
        return "${getMqttBaseTopic(device)}/+/command/#"
    }
    
    override fun getDiscoveryPayloads(device: BluetoothDevice): List<Pair<String, String>> {
        // Discovery is published by callback after zone detection
        return emptyList()
    }
    
    override suspend fun handleCommand(
        device: BluetoothDevice,
        commandTopic: String,
        payload: String
    ): Result<Unit> {
        Log.i(TAG, "Command received: $commandTopic = $payload")
        val callback = currentCallback ?: return Result.failure(Exception("No active connection"))
        return callback.handleCommand(commandTopic, payload)
    }
    
    override fun destroy() {
        Log.i(TAG, "Destroying EasyTouch Plugin")
        currentCallback = null
        gattCallback = null
    }
}

/**
 * GATT Callback for EasyTouch thermostat.
 * 
 * Handles:
 * - Connection establishment
 * - Service/characteristic discovery
 * - Password authentication
 * - Notification subscription
 * - JSON command/response handling
 * - State parsing and MQTT publishing
 * - Multi-zone support (publishes separate entity per zone)
 */
class EasyTouchGattCallback(
    private val device: BluetoothDevice,
    private val context: Context,
    private val mqttPublisher: MqttPublisher,
    private val onDisconnect: (BluetoothDevice, Int) -> Unit,
    private val password: String
) : BluetoothGattCallback() {
    
    private var gatt: BluetoothGatt? = null
    private var passwordCmdChar: BluetoothGattCharacteristic? = null
    private var jsonCmdChar: BluetoothGattCharacteristic? = null
    private var jsonReturnChar: BluetoothGattCharacteristic? = null
    
    private val mainHandler = Handler(Looper.getMainLooper())
    private var isAuthenticated = false
    private var discoveryPublished = false
    
    // Device Information Service data (read from BLE 0x180A service or parsed from status)
    private var bleManufacturerName: String? = null
    private var bleModelNumber: String? = null    // Device type (TT field, e.g., "EasyTouch")
    private var bleSerialNumber: String? = null   // Serial number (SN field)
    private var bleHardwareRevision: String? = null  // Config index (CI field)
    private var bleFirmwareRevision: String? = null  // Firmware version (REV field)
    private var bleSoftwareRevision: String? = null
    private var deviceModel: String? = null       // Derived model number from serial prefix (e.g., "352")
    private var deviceInfoReadComplete = false
    private var deviceInfoCharsToRead = mutableListOf<BluetoothGattCharacteristic>()
    
    // Current state (multi-zone aware)
    private var currentState: ThermostatState? = null
    
    // Zone configurations (includes available modes bitmask from device)
    // Key = zone number, Value = ZoneConfiguration
    private val zoneConfigs = mutableMapOf<Int, ZoneConfiguration>()
    private var configRequestedZone = 0  // Track which zone we're requesting config for
    
    // Response buffer for multi-packet JSON responses
    private val responseBuffer = StringBuilder()
    
    // Status polling (matches official app's 4-second interval)
    private var isPollingActive = false
    
    // Status update suppression (prevents UI bounce-back after sending commands)
    @Volatile private var isStatusSuppressed = false
    
    private val statusPollRunnable = object : Runnable {
        override fun run() {
            if (!isPollingActive || !isAuthenticated) {
                DebugLog.d(TAG, "Polling stopped: active=$isPollingActive, auth=$isAuthenticated")
                return
            }
            
            DebugLog.d(TAG, "Polling status...")
            requestStatus(zone = 0)
            
            // Schedule next poll in 4 seconds (same as official app)
            mainHandler.postDelayed(this, STATUS_POLL_INTERVAL_MS)
        }
    }
    
    companion object {
        private const val TAG = "EasyTouchGattCallback"
        private const val STATUS_POLL_INTERVAL_MS = 4000L // 4 seconds, same as official app
    }
    
    // Base topic
    private val baseTopic: String
        get() = "easytouch/${device.address}"
    
    // ===== LIFECYCLE CALLBACKS =====
    
    override fun onConnectionStateChange(gatt: BluetoothGatt, status: Int, newState: Int) {
        Log.i(TAG, "Connection state: $newState (status: $status)")
        
        when (newState) {
            BluetoothProfile.STATE_CONNECTED -> {
                Log.i(TAG, "Connected to ${device.address}")
                this.gatt = gatt
                publishAvailability(true)
                publishDiagnosticsDiscovery()
                publishDiagnosticsState(isConnected = true)
                
                // Discover services after brief delay
                mainHandler.postDelayed({
                    gatt.discoverServices()
                }, EasyTouchConstants.SERVICE_DISCOVERY_DELAY_MS)
            }
            BluetoothProfile.STATE_DISCONNECTED -> {
                Log.w(TAG, "Disconnected from ${device.address}")
                stopStatusPolling()  // Stop polling on disconnect
                publishAvailability(false)
                publishDiagnosticsState(isConnected = false)
                isAuthenticated = false
                discoveryPublished = false
                onDisconnect(device, status)
            }
        }
    }
    
    override fun onServicesDiscovered(gatt: BluetoothGatt, status: Int) {
        if (status != BluetoothGatt.GATT_SUCCESS) {
            Log.e(TAG, "Service discovery failed: $status")
            return
        }
        
        Log.i(TAG, "Services discovered")
        
        // Try to read Device Information Service (0x180A) for model/firmware info
        readDeviceInformationService(gatt)
        
        // Find EasyTouch service
        val service = gatt.getService(EasyTouchConstants.SERVICE_UUID)
        if (service == null) {
            Log.e(TAG, "EasyTouch service not found!")
            return
        }
        
        // Get characteristics
        passwordCmdChar = service.getCharacteristic(EasyTouchConstants.PASSWORD_CMD_UUID)
        jsonCmdChar = service.getCharacteristic(EasyTouchConstants.JSON_CMD_UUID)
        jsonReturnChar = service.getCharacteristic(EasyTouchConstants.JSON_RETURN_UUID)
        
        if (passwordCmdChar == null || jsonCmdChar == null || jsonReturnChar == null) {
            Log.e(TAG, "Missing characteristics! pwd=${passwordCmdChar != null}, cmd=${jsonCmdChar != null}, ret=${jsonReturnChar != null}")
            return
        }
        
        Log.i(TAG, "All characteristics found")
        
        // Start authentication (or device info reading if available)
        mainHandler.postDelayed({
            if (deviceInfoCharsToRead.isNotEmpty()) {
                readNextDeviceInfoChar(gatt)
            } else {
                authenticate()
            }
        }, EasyTouchConstants.AUTH_STEP_DELAY_MS)
    }
    
    /**
     * Read standard BLE Device Information Service (0x180A) if available.
     * Contains manufacturer name, model number, serial number, firmware revision, etc.
     */
    private fun readDeviceInformationService(gatt: BluetoothGatt) {
        val deviceInfoService = gatt.getService(EasyTouchConstants.DEVICE_INFO_SERVICE_UUID)
        if (deviceInfoService == null) {
            Log.i(TAG, "Device Information Service (0x180A) not available")
            deviceInfoReadComplete = true
            return
        }
        
        Log.i(TAG, "Found Device Information Service, queuing characteristic reads...")
        
        // Queue all available characteristics for reading
        deviceInfoCharsToRead.clear()
        
        deviceInfoService.getCharacteristic(EasyTouchConstants.MANUFACTURER_NAME_UUID)?.let {
            deviceInfoCharsToRead.add(it)
        }
        deviceInfoService.getCharacteristic(EasyTouchConstants.MODEL_NUMBER_UUID)?.let {
            deviceInfoCharsToRead.add(it)
        }
        deviceInfoService.getCharacteristic(EasyTouchConstants.SERIAL_NUMBER_UUID)?.let {
            deviceInfoCharsToRead.add(it)
        }
        deviceInfoService.getCharacteristic(EasyTouchConstants.HARDWARE_REVISION_UUID)?.let {
            deviceInfoCharsToRead.add(it)
        }
        deviceInfoService.getCharacteristic(EasyTouchConstants.FIRMWARE_REVISION_UUID)?.let {
            deviceInfoCharsToRead.add(it)
        }
        deviceInfoService.getCharacteristic(EasyTouchConstants.SOFTWARE_REVISION_UUID)?.let {
            deviceInfoCharsToRead.add(it)
        }
        
        Log.i(TAG, "Found ${deviceInfoCharsToRead.size} device info characteristics to read")
        
        if (deviceInfoCharsToRead.isEmpty()) {
            deviceInfoReadComplete = true
        }
    }
    
    /**
     * Read the next device info characteristic in the queue.
     */
    private fun readNextDeviceInfoChar(gatt: BluetoothGatt) {
        if (deviceInfoCharsToRead.isEmpty()) {
            Log.i(TAG, "Device info reading complete: manufacturer=$bleManufacturerName, model=$bleModelNumber, serial=$bleSerialNumber, hw=$bleHardwareRevision, fw=$bleFirmwareRevision, sw=$bleSoftwareRevision")
            deviceInfoReadComplete = true
            authenticate()
            return
        }
        
        val char = deviceInfoCharsToRead.removeAt(0)
        if (!gatt.readCharacteristic(char)) {
            Log.w(TAG, "Failed to read characteristic ${char.uuid}, skipping...")
            readNextDeviceInfoChar(gatt)
        }
    }
    
    // ===== STATUS POLLING =====
    
    /**
     * Start continuous status polling.
     * Matches the official EasyTouch app behavior: polls every 4 seconds.
     */
    private fun startStatusPolling() {
        if (isPollingActive) {
            DebugLog.d(TAG, "Polling already active")
            return
        }
        
        Log.i(TAG, "Starting status polling loop (${STATUS_POLL_INTERVAL_MS}ms interval)")
        isPollingActive = true
        publishDiagnosticsState(isConnected = true)
        
        // Request first status immediately, then continue on interval
        mainHandler.postDelayed({
            statusPollRunnable.run()
        }, EasyTouchConstants.AUTH_STEP_DELAY_MS)
    }
    
    /**
     * Stop the status polling loop.
     * Called on disconnect or when connection is lost.
     */
    private fun stopStatusPolling() {
        Log.i(TAG, "Stopping status polling loop")
        isPollingActive = false
        mainHandler.removeCallbacks(statusPollRunnable)
    }
    
    /**
     * Temporarily suppress status updates to prevent UI bounce-back.
     * Called after sending commands to give the thermostat time to process.
     * Matches official app's "StartIgnoreStatus" behavior.
     */
    private fun suppressStatusUpdates(durationMs: Long) {
        DebugLog.d(TAG, "Suppressing status updates for ${durationMs}ms")
        isStatusSuppressed = true
        mainHandler.postDelayed({
            isStatusSuppressed = false
            DebugLog.d(TAG, "Status updates resumed")
        }, durationMs)
    }
    
    // ===== AUTHENTICATION =====
    
    private fun authenticate() {
        val char = passwordCmdChar ?: return
        val g = gatt ?: return
        
        Log.i(TAG, "Authenticating with password...")
        
        // Write password to passwordCmd characteristic
        val passwordBytes = password.toByteArray(StandardCharsets.UTF_8)
        char.value = passwordBytes
        
        if (g.writeCharacteristic(char)) {
            Log.i(TAG, "Password write initiated")
        } else {
            Log.e(TAG, "Failed to initiate password write")
        }
    }
    
    override fun onCharacteristicWrite(
        gatt: BluetoothGatt,
        characteristic: BluetoothGattCharacteristic,
        status: Int
    ) {
        val uuid = characteristic.uuid
        DebugLog.d(TAG, "Characteristic write complete: $uuid, status=$status")
        
        when (uuid) {
            EasyTouchConstants.PASSWORD_CMD_UUID -> {
                if (status == BluetoothGatt.GATT_SUCCESS) {
                    Log.i(TAG, "Password written successfully")
                    isAuthenticated = true
                    Log.i(TAG, "Fully authenticated! Requesting device config...")
                    // Request config first to get available modes, then start polling
                    requestConfig(zone = 0)
                } else {
                    Log.e(TAG, "Password write failed: $status")
                }
            }
            EasyTouchConstants.JSON_CMD_UUID -> {
                if (status == BluetoothGatt.GATT_SUCCESS) {
                    DebugLog.d(TAG, "JSON command written successfully")
                    // Read response after a short delay (no notifications available)
                    mainHandler.postDelayed({
                        readJsonResponse()
                    }, EasyTouchConstants.READ_DELAY_MS)
                } else {
                    Log.e(TAG, "JSON command write failed: $status")
                }
            }
        }
    }
    
    /**
     * Read the jsonReturn characteristic to get response data.
     * Official EasyTouch app pattern: read immediately after writing command.
     */
    private fun readJsonResponse() {
        val char = jsonReturnChar ?: return
        val g = gatt ?: return
        
        DebugLog.d(TAG, "Reading jsonReturn characteristic...")
        if (!g.readCharacteristic(char)) {
            Log.e(TAG, "Failed to initiate read on jsonReturn")
        }
    }
    
    /**
     * Handle characteristic read completion
     */
    override fun onCharacteristicRead(
        gatt: BluetoothGatt,
        characteristic: BluetoothGattCharacteristic,
        status: Int
    ) {
        if (status != BluetoothGatt.GATT_SUCCESS) {
            Log.w(TAG, "Characteristic read failed: ${characteristic.uuid}, status=$status")
            // If reading device info, continue to next characteristic
            if (!deviceInfoReadComplete) {
                readNextDeviceInfoChar(gatt)
            }
            return
        }
        
        val uuid = characteristic.uuid
        
        // Handle Device Information Service characteristics
        when (uuid) {
            EasyTouchConstants.MANUFACTURER_NAME_UUID -> {
                bleManufacturerName = characteristic.getStringValue(0)
                Log.i(TAG, "BLE Manufacturer: $bleManufacturerName")
                readNextDeviceInfoChar(gatt)
                return
            }
            EasyTouchConstants.MODEL_NUMBER_UUID -> {
                bleModelNumber = characteristic.getStringValue(0)
                Log.i(TAG, "BLE Model: $bleModelNumber")
                readNextDeviceInfoChar(gatt)
                return
            }
            EasyTouchConstants.SERIAL_NUMBER_UUID -> {
                bleSerialNumber = characteristic.getStringValue(0)
                Log.i(TAG, "BLE Serial: $bleSerialNumber")
                readNextDeviceInfoChar(gatt)
                return
            }
            EasyTouchConstants.HARDWARE_REVISION_UUID -> {
                bleHardwareRevision = characteristic.getStringValue(0)
                Log.i(TAG, "BLE Hardware Rev: $bleHardwareRevision")
                readNextDeviceInfoChar(gatt)
                return
            }
            EasyTouchConstants.FIRMWARE_REVISION_UUID -> {
                bleFirmwareRevision = characteristic.getStringValue(0)
                Log.i(TAG, "BLE Firmware Rev: $bleFirmwareRevision")
                readNextDeviceInfoChar(gatt)
                return
            }
            EasyTouchConstants.SOFTWARE_REVISION_UUID -> {
                bleSoftwareRevision = characteristic.getStringValue(0)
                Log.i(TAG, "BLE Software Rev: $bleSoftwareRevision")
                readNextDeviceInfoChar(gatt)
                return
            }
        }
        
        // Handle EasyTouch JSON response
        if (uuid == EasyTouchConstants.JSON_RETURN_UUID) {
            processReceivedData(characteristic.value)
        }
    }
    
    /**
     * Process data received from either read or notification
     */
    private fun processReceivedData(data: ByteArray?) {
        if (data == null || data.isEmpty()) {
            DebugLog.d(TAG, "Empty response from jsonReturn")
            return
        }
        
        val chunk = String(data, StandardCharsets.UTF_8)
        DebugLog.d(TAG, "Received data: $chunk")
        
        responseBuffer.append(chunk)
        
        // Try to parse complete JSON
        val bufferStr = responseBuffer.toString()
        try {
            val json = JSONObject(bufferStr)
            // Valid JSON - process it
            responseBuffer.clear()
            handleJsonResponse(json)
        } catch (e: Exception) {
            // Incomplete JSON - might need another read
            DebugLog.d(TAG, "Waiting for more data (buffer: ${responseBuffer.length} chars)")
            // Schedule another read to get more data
            mainHandler.postDelayed({
                readJsonResponse()
            }, 100)
        }
    }
    
    // ===== JSON COMMUNICATION =====
    
    /**
     * Request device configuration for a zone.
     * This retrieves the MAV (Mode AVailable) bitmask which tells us what modes the device supports.
     */
    private fun requestConfig(zone: Int) {
        configRequestedZone = zone
        val command = JSONObject().apply {
            put("Type", "Get Config")
            put("Zone", zone)
        }
        Log.i(TAG, "Requesting config for zone $zone...")
        writeJsonCommand(command)
    }
    
    private fun requestStatus(zone: Int) {
        val command = JSONObject().apply {
            put("Type", "Get Status")
            put("Zone", zone)
            put("EM", "x")  // Email placeholder (required by protocol)
            put("TM", 0)    // Time placeholder
        }
        writeJsonCommand(command)
    }
    
    private fun writeJsonCommand(json: JSONObject, retryCount: Int = 0) {
        val char = jsonCmdChar ?: run {
            Log.e(TAG, "Cannot write: jsonCmdChar is null")
            return
        }
        val g = gatt ?: run {
            Log.e(TAG, "Cannot write: gatt is null")
            return
        }
        
        val jsonString = json.toString()
        
        // Add small delay before write to improve reliability (like HACS integration does)
        val delayMs = if (retryCount == 0) 100L else (200L * (retryCount + 1))
        
        mainHandler.postDelayed({
            char.value = jsonString.toByteArray(StandardCharsets.UTF_8)
            val success = g.writeCharacteristic(char)
            
            if (success) {
                DebugLog.d(TAG, "Writing JSON command (attempt ${retryCount + 1}): $jsonString")
            } else {
                Log.e(TAG, "Failed to write JSON command (attempt ${retryCount + 1})")
                // Retry up to 2 more times with increasing delays
                if (retryCount < 2) {
                    Log.i(TAG, "Retrying write in ${delayMs * 2}ms...")
                    writeJsonCommand(json, retryCount + 1)
                } else {
                    Log.e(TAG, "Write failed after 3 attempts: $jsonString")
                }
            }
        }, delayMs)
    }
    
    override fun onCharacteristicChanged(
        gatt: BluetoothGatt,
        characteristic: BluetoothGattCharacteristic
    ) {
        if (characteristic.uuid != EasyTouchConstants.JSON_RETURN_UUID) return
        
        val data = characteristic.value
        if (data == null || data.isEmpty()) return
        
        val chunk = String(data, StandardCharsets.UTF_8)
        DebugLog.d(TAG, "Received chunk: $chunk")
        
        responseBuffer.append(chunk)
        
        // Try to parse complete JSON
        val bufferStr = responseBuffer.toString()
        try {
            val json = JSONObject(bufferStr)
            // Valid JSON - process it
            responseBuffer.clear()
            handleJsonResponse(json)
        } catch (e: Exception) {
            // Incomplete JSON, wait for more data
            DebugLog.d(TAG, "Waiting for more data (buffer: ${responseBuffer.length} chars)")
        }
    }
    
    private fun handleJsonResponse(json: JSONObject) {
        DebugLog.d(TAG, "Received JSON: ${json.toString().take(200)}...")
        
        val type = json.optString("Type", "")
        val responseType = json.optString("RT", "")  // Response type field
        
        when {
            // Config response: Type="Response", RT="Config"
            type == "Response" && responseType == "Config" -> {
                parseConfigResponse(json)
            }
            // Status response: Type="Response", RT="Status"
            type == "Response" && responseType == "Status" -> {
                val state = parseMultiZoneStatus(json)
                currentState = state
                
                // Skip publishing if status updates are suppressed (command in progress)
                if (isStatusSuppressed) {
                    DebugLog.d(TAG, "Status update suppressed (command in progress)")
                } else {
                    publishState(state)
                    Log.i(TAG, "Status update received and published")
                }
            }
            // Legacy format: Type="Status"
            type == "Status" -> {
                val state = parseMultiZoneStatus(json)
                currentState = state
                
                // Skip publishing if status updates are suppressed (command in progress)
                if (!isStatusSuppressed) {
                    publishState(state)
                }
            }
            type == "Change Result" || (type == "Response" && responseType == "Change") -> {
                val success = json.optBoolean("Success", false)
                Log.i(TAG, "Change result: success=$success")
                if (success) {
                    // Request fresh status after change
                    mainHandler.postDelayed({
                        requestStatus(zone = 0)
                    }, 500)
                }
            }
            else -> {
                Log.w(TAG, "Unknown response: Type=$type, RT=$responseType")
            }
        }
    }
    
    /**
     * Parse device configuration response.
     * Contains MAV (Mode AVailable) bitmask and setpoint limits.
     * Response format: {"Type":"Response","RT":"Config",...,"CFG":{"Zone":N,"MAV":...,"SPL":[...]}}
     */
    private fun parseConfigResponse(json: JSONObject) {
        // Config data is nested inside CFG object
        val cfg = json.optJSONObject("CFG")
        if (cfg == null) {
            Log.w(TAG, "⚠️ No CFG object in config response")
            // Continue to next zone anyway
            val nextZone = configRequestedZone + 1
            if (nextZone <= 3) {
                mainHandler.postDelayed({ requestConfig(nextZone) }, 300)
            } else {
                startStatusPolling()
            }
            return
        }
        
        val zone = cfg.optInt("Zone", configRequestedZone)
        val mav = cfg.optInt("MAV", 0)  // Mode Available bitmask
        
        // Parse setpoint limits (SPL array: [minCool, maxCool, minHeat, maxHeat])
        val splArray = cfg.optJSONArray("SPL")
        val minCool = splArray?.optInt(0) ?: EasyTouchConstants.MIN_TEMP_F
        val maxCool = splArray?.optInt(1) ?: EasyTouchConstants.MAX_TEMP_F
        val minHeat = splArray?.optInt(2) ?: EasyTouchConstants.MIN_TEMP_F
        val maxHeat = splArray?.optInt(3) ?: EasyTouchConstants.MAX_TEMP_F
        
        val config = ZoneConfiguration(
            zone = zone,
            availableModesBitmask = mav,
            minCoolSetpoint = minCool,
            maxCoolSetpoint = maxCool,
            minHeatSetpoint = minHeat,
            maxHeatSetpoint = maxHeat
        )
        
        zoneConfigs[zone] = config
        
        // Log discovered capabilities
        val supportedModes = getModesFromBitmask(mav)
        Log.i(TAG, "Zone $zone config: MAV=0x${mav.toString(16)} (${mav.toString(2).padStart(16, '0')})")
        Log.i(TAG, "   Supported modes: $supportedModes")
        Log.i(TAG, "   Setpoint limits: cool=$minCool-$maxCool, heat=$minHeat-$maxHeat")
        
        // Request config for next zone (up to zone 3) or start polling
        if (zone < 3) {
            mainHandler.postDelayed({
                requestConfig(zone + 1)
            }, 300)
        } else {
            // All zones configured, now start polling
            Log.i(TAG, "All zone configs received. Starting status polling...")
            startStatusPolling()
        }
    }
    
    /**
     * Convert MAV bitmask to list of HA mode strings.
     * MAV bit positions correspond to device mode numbers:
     * bit 0 = mode 0 (off), bit 1 = mode 1 (fan_only), bit 2 = mode 2 (cool), etc.
     */
    private fun getModesFromBitmask(mav: Int): List<String> {
        if (mav == 0) {
            // If no MAV provided, return default modes
            return EasyTouchConstants.SUPPORTED_HVAC_MODES
        }
        
        val modes = mutableListOf<String>()
        // Always include "off" - it's a universal mode
        modes.add("off")
        
        // Check each bit in the MAV
        for (bit in 0..15) {
            if ((mav and (1 shl bit)) != 0) {
                val haMode = EasyTouchConstants.DEVICE_TO_HA_MODE[bit]
                if (haMode != null && haMode != "off" && !modes.contains(haMode)) {
                    modes.add(haMode)
                }
            }
        }
        
        return modes
    }
    
    // ===== STATE PARSING (Multi-Zone) =====
    
    private fun parseMultiZoneStatus(json: JSONObject): ThermostatState {
        val zSts = json.optJSONObject("Z_sts")
        
        if (zSts == null) {
            Log.e(TAG, "No Z_sts in status response")
            return ThermostatState(emptyList(), emptyMap())
        }
        
        // Extract device info from status response (SN, REV, TT, CI fields)
        val serialNumber = json.optString("SN", null)
        val firmwareRevision = json.optString("REV", null)
        val deviceType = json.optString("TT", null)
        val configIndex = json.optInt("CI", -1).takeIf { it >= 0 }
        
        // Store device info for diagnostic sensors
        var deviceInfoUpdated = false
        if (serialNumber != null && bleSerialNumber == null) {
            bleSerialNumber = serialNumber
            Log.i(TAG, "Parsed Serial Number from status: $serialNumber")
            
            // Derive model number from first 3 chars of serial number
            // Based on official EasyTouch app: Serial_number.substring(0, 3) determines model
            // e.g., "352016935" -> model "352"
            // 350 = Dometic CCC, 351/352 = EasyTouch RV, 353 = Furrion, 354-357/359 = Other variants
            if (serialNumber.length >= 3 && deviceModel == null) {
                deviceModel = serialNumber.take(3)
                Log.i(TAG, "Derived model from serial prefix: $deviceModel")
            }
            deviceInfoUpdated = true
        }
        if (firmwareRevision != null && bleFirmwareRevision == null) {
            bleFirmwareRevision = firmwareRevision
            Log.i(TAG, "Parsed Firmware Revision from status: $firmwareRevision")
            deviceInfoUpdated = true
        }
        if (deviceType != null && bleModelNumber == null) {
            bleModelNumber = deviceType
            Log.i(TAG, "Parsed Device Type from status: $deviceType")
            deviceInfoUpdated = true
        }
        if (configIndex != null && bleHardwareRevision == null) {
            bleHardwareRevision = configIndex.toString()
            Log.i(TAG, "Parsed Config Index from status: $configIndex")
            deviceInfoUpdated = true
        }
        
        // If device info was updated, re-publish diagnostic states
        if (deviceInfoUpdated) {
            publishDiagnosticsState(isConnected = true)
        }
        
        // Parse PRM array for system-level flags
        // PRM[1] contains: bit0=wifiConnected, bit1=awsConnected, bit2=pushNotify, bit3=systemPower
        val prmArray = json.optJSONArray("PRM")
        val systemPower = if (prmArray != null && prmArray.length() > 1) {
            val flags = prmArray.optInt(1, 0)
            (flags and 8) != 0  // bit 3 = systemPower
        } else {
            true  // Default to on if PRM not present
        }
        DebugLog.d(TAG, "System power: $systemPower (PRM[1]=${prmArray?.optInt(1, 0) ?: "N/A"})")
        
        val availableZones = mutableListOf<Int>()
        val zones = mutableMapOf<Int, ZoneState>()
        
        // Iterate all zone keys
        val keys = zSts.keys()
        while (keys.hasNext()) {
            val key = keys.next()
            val zoneNum = key.toIntOrNull() ?: continue
            availableZones.add(zoneNum)
            
            val arr = zSts.optJSONArray(key)
            if (arr == null || arr.length() < 12) {
                Log.w(TAG, "Invalid zone data for zone $zoneNum")
                continue
            }
            
            // Parse status flags (index 15)
            val statusFlags = if (arr.length() > 15) arr.optInt(EasyTouchConstants.StatusIndex.STATUS_FLAGS) else 0
            val cycleActive = (statusFlags and 1) != 0
            val isCooling = (statusFlags and 2) != 0
            val isHeating = (statusFlags and 4) != 0
            
            zones[zoneNum] = ZoneState(
                zoneNumber = zoneNum,
                ambientTemperature = arr.optInt(EasyTouchConstants.StatusIndex.AMBIENT_TEMP),
                coolSetpoint = arr.optInt(EasyTouchConstants.StatusIndex.COOL_SETPOINT),
                heatSetpoint = arr.optInt(EasyTouchConstants.StatusIndex.HEAT_SETPOINT),
                autoCoolSetpoint = arr.optInt(EasyTouchConstants.StatusIndex.AUTO_COOL_SETPOINT),
                autoHeatSetpoint = arr.optInt(EasyTouchConstants.StatusIndex.AUTO_HEAT_SETPOINT),
                drySetpoint = arr.optInt(EasyTouchConstants.StatusIndex.DRY_SETPOINT),
                modeNum = arr.optInt(EasyTouchConstants.StatusIndex.MODE),
                fanOnlySpeed = arr.optInt(EasyTouchConstants.StatusIndex.FAN_ONLY_SPEED),
                coolFanSpeed = arr.optInt(EasyTouchConstants.StatusIndex.COOL_FAN_SPEED),
                electricFanSpeed = arr.optInt(EasyTouchConstants.StatusIndex.ELECTRIC_FAN_SPEED),
                autoFanSpeed = arr.optInt(EasyTouchConstants.StatusIndex.AUTO_FAN_SPEED),
                gasFanSpeed = arr.optInt(EasyTouchConstants.StatusIndex.GAS_FAN_SPEED),
                cycleActive = cycleActive,
                isCooling = isCooling,
                isHeating = isHeating
            )
        }
        
        Log.i(TAG, "Parsed ${availableZones.size} zone(s): ${availableZones.sorted()}, systemPower=$systemPower")
        
        return ThermostatState(
            availableZones = availableZones.sorted(),
            zones = zones,
            systemPower = systemPower
        )
    }
    
    // ===== STATE PUBLISHING (Multi-Zone Aware) =====
    
    private fun publishState(state: ThermostatState) {
        // Publish discovery for each zone (once)
        if (!discoveryPublished && state.availableZones.isNotEmpty()) {
            publishDiscovery(state.availableZones)
            // Note: Command subscription is handled by BaseBleService using getCommandTopicPattern()
            discoveryPublished = true
        }
        
        // Publish state for each zone
        for (zoneNum in state.availableZones) {
            val zoneState = state.zones[zoneNum] ?: continue
            publishZoneState(zoneNum, zoneState, state.systemPower)
        }
    }
    
    private fun publishZoneState(zoneNum: Int, state: ZoneState, systemPower: Boolean) {
        val zoneTopic = "$baseTopic/zone_$zoneNum"
        
        // When systemPower is off, report mode as "off" regardless of actual mode setting
        // This matches official EasyTouch app behavior
        val mode = if (!systemPower) {
            "off"
        } else {
            EasyTouchConstants.DEVICE_TO_HA_MODE[state.modeNum] ?: "off"
        }
        
        // Current temperature (ambient)
        mqttPublisher.publishState("$zoneTopic/state/current_temperature", state.ambientTemperature.toString(), true)
        
        // Mode
        mqttPublisher.publishState("$zoneTopic/state/mode", mode, true)
        
        // Target temperature based on mode
        // For HA MQTT climate, we need to always publish high/low temps so the UI knows about them
        // In non-auto modes, we publish "None" to indicate they're not active
        when (mode) {
            "cool" -> {
                mqttPublisher.publishState("$zoneTopic/state/target_temperature", state.coolSetpoint.toString(), true)
                mqttPublisher.publishState("$zoneTopic/state/target_temperature_high", "None", true)
                mqttPublisher.publishState("$zoneTopic/state/target_temperature_low", "None", true)
            }
            "heat" -> {
                mqttPublisher.publishState("$zoneTopic/state/target_temperature", state.heatSetpoint.toString(), true)
                mqttPublisher.publishState("$zoneTopic/state/target_temperature_high", "None", true)
                mqttPublisher.publishState("$zoneTopic/state/target_temperature_low", "None", true)
            }
            "auto" -> {
                // In auto mode, publish high/low temps and clear single target
                mqttPublisher.publishState("$zoneTopic/state/target_temperature", "None", true)
                mqttPublisher.publishState("$zoneTopic/state/target_temperature_high", state.autoCoolSetpoint.toString(), true)
                mqttPublisher.publishState("$zoneTopic/state/target_temperature_low", state.autoHeatSetpoint.toString(), true)
            }
            "dry" -> {
                mqttPublisher.publishState("$zoneTopic/state/target_temperature", state.drySetpoint.toString(), true)
                mqttPublisher.publishState("$zoneTopic/state/target_temperature_high", "None", true)
                mqttPublisher.publishState("$zoneTopic/state/target_temperature_low", "None", true)
            }
            else -> {
                // Off or fan_only modes - clear all temperature targets
                mqttPublisher.publishState("$zoneTopic/state/target_temperature", "None", true)
                mqttPublisher.publishState("$zoneTopic/state/target_temperature_high", "None", true)
                mqttPublisher.publishState("$zoneTopic/state/target_temperature_low", "None", true)
            }
        }
        
        // Fan mode - use appropriate fan speed based on current mode
        val fanMode = when (mode) {
            "fan_only" -> fanValueToString(state.fanOnlySpeed, isFanOnly = true)
            "cool" -> fanValueToString(state.coolFanSpeed, isFanOnly = false)
            "heat" -> fanValueToString(state.electricFanSpeed, isFanOnly = false)  // or gasFanSpeed depending on unit
            "auto" -> fanValueToString(state.autoFanSpeed, isFanOnly = false)
            else -> "off"
        }
        mqttPublisher.publishState("$zoneTopic/state/fan_mode", fanMode, true)
        
        // Action (what's currently happening)
        val action = getAction(state)
        mqttPublisher.publishState("$zoneTopic/state/action", action, true)
        
        DebugLog.d(TAG, "Published zone $zoneNum: temp=${state.ambientTemperature}°F, mode=$mode, fan=$fanMode, action=$action")
    }
    
    private fun fanValueToString(value: Int, isFanOnly: Boolean): String {
        return if (isFanOnly) {
            when (value) {
                0 -> "off"
                1 -> "low"
                2 -> "high"
                else -> "off"
            }
        } else {
            when (value) {
                0 -> "off"
                1, 65 -> "low"
                2, 66 -> "high"
                128 -> "auto"
                else -> "auto"
            }
        }
    }
    
    private fun getAction(state: ZoneState): String {
        // Use status flags to determine actual action
        return when {
            state.isCooling -> "cooling"
            state.isHeating -> "heating"
            state.cycleActive && state.modeNum == EasyTouchConstants.DeviceMode.FAN_ONLY -> "fan"
            state.modeNum == EasyTouchConstants.DeviceMode.OFF -> "off"
            else -> "idle"
        }
    }
    
    private fun publishAvailability(online: Boolean) {
        // Use publishState for per-device availability (publishAvailability is for global status)
        val payload = if (online) "online" else "offline"
        mqttPublisher.publishState("$baseTopic/availability", payload, true)
        DebugLog.d(TAG, "Published availability: $payload to $baseTopic/availability")
    }
    
    // ===== DISCOVERY PUBLISHING =====
    
    private fun publishDiscovery(zones: List<Int>) {
        val mac = device.address.replace(":", "").lowercase()
        val deviceName = device.name ?: "EasyTouch"
        val prefix = mqttPublisher.topicPrefix  // e.g., "homeassistant"
        
        for (zone in zones) {
            // For single zone, just use "Climate" as the entity name
            // For multi-zone, use "Zone 1", "Zone 2", etc.
            // This prevents duplication like "easytouch_352016935_easytouch_352016935"
            val entityName = if (zones.size == 1) {
                "Climate"  // Simple name - device name already identifies it
            } else {
                "Zone ${zone + 1}"  // Multi-zone: just zone number
            }
            
            val uniqueId = "easytouch_${mac}_zone_$zone"
            val zoneTopic = "$baseTopic/zone_$zone"
            // Full topic path including prefix (for discovery config)
            val fullZoneTopic = "$prefix/$zoneTopic"
            val fullBaseTopic = "$prefix/$baseTopic"
            
            // Get available modes from device config (MAV bitmask)
            val zoneConfig = zoneConfigs[zone]
            val supportedModes = if (zoneConfig != null && zoneConfig.availableModesBitmask != 0) {
                getModesFromBitmask(zoneConfig.availableModesBitmask)
            } else {
                EasyTouchConstants.SUPPORTED_HVAC_MODES  // Fallback to defaults
            }
            
            // Get setpoint limits from config
            val minTemp = zoneConfig?.minHeatSetpoint ?: EasyTouchConstants.MIN_TEMP_F
            val maxTemp = zoneConfig?.maxCoolSetpoint ?: EasyTouchConstants.MAX_TEMP_F
            
            Log.i(TAG, "📝 Discovery for zone $zone: modes=$supportedModes, temp range=$minTemp-$maxTemp")
            
            // Get app version
            val appVersion = try {
                context.packageManager.getPackageInfo(context.packageName, 0).versionName ?: "unknown"
            } catch (e: Exception) {
                "unknown"
            }
            
            val payload = JSONObject().apply {
                put("name", entityName)
                put("unique_id", uniqueId)
                put("object_id", "easytouch_${mac}_zone_$zone")  // Explicit entity ID base
                put("device", JSONObject().apply {
                    put("identifiers", JSONArray().put("easytouch_$mac"))
                    put("name", deviceName)
                    put("manufacturer", "phurth")
                    put("model", "Micro-Air EasyTouch RV thermostat plugin for the Android BLE to MQTT Bridge")
                    put("sw_version", appVersion)
                    put("connections", JSONArray().put(JSONArray().put("mac").put(device.address)))
                })
                put("modes", JSONArray(supportedModes))  // Use actual device capabilities
                put("fan_modes", JSONArray(EasyTouchConstants.SUPPORTED_FAN_MODES))
                put("temperature_unit", "F")
                put("temp_step", EasyTouchConstants.TEMP_STEP)
                put("min_temp", minTemp)
                put("max_temp", maxTemp)
                
                // State topics (full path including prefix)
                put("mode_state_topic", "$fullZoneTopic/state/mode")
                put("current_temperature_topic", "$fullZoneTopic/state/current_temperature")
                put("temperature_state_topic", "$fullZoneTopic/state/target_temperature")
                put("temperature_high_state_topic", "$fullZoneTopic/state/target_temperature_high")
                put("temperature_low_state_topic", "$fullZoneTopic/state/target_temperature_low")
                put("fan_mode_state_topic", "$fullZoneTopic/state/fan_mode")
                put("action_topic", "$fullZoneTopic/state/action")
                
                // Command topics (full path including prefix)
                put("mode_command_topic", "$fullZoneTopic/command/mode")
                put("temperature_command_topic", "$fullZoneTopic/command/temperature")
                put("temperature_high_command_topic", "$fullZoneTopic/command/temperature_high")
                put("temperature_low_command_topic", "$fullZoneTopic/command/temperature_low")
                put("fan_mode_command_topic", "$fullZoneTopic/command/fan_mode")
                
                // Optimistic mode - assume command succeeds immediately to prevent UI bounce
                put("optimistic", true)
                
                // Availability (full path including prefix)
                put("availability_topic", "$fullBaseTopic/availability")
            }
            
            val discoveryTopic = "homeassistant/climate/$uniqueId/config"
            mqttPublisher.publishDiscovery(discoveryTopic, payload.toString())
            Log.i(TAG, "Published discovery for zone $zone: $discoveryTopic")
        }
    }
    
    // ===== COMMAND HANDLING (Multi-Zone Aware) =====
    
    /**
     * Extract zone number from topic.
     * Topic format: easytouch/{MAC}/zone_{N}/command/{cmd}
     * Returns zone number or 0 if not found.
     */
    private fun extractZoneFromTopic(topic: String): Int {
        val regex = Regex("""/zone_(\d+)/""")
        val match = regex.find(topic)
        return match?.groupValues?.get(1)?.toIntOrNull() ?: 0
    }
    
    fun handleCommand(commandTopic: String, payload: String): Result<Unit> {
        if (!isAuthenticated) {
            return Result.failure(Exception("Not authenticated"))
        }
        
        // Extract zone from topic
        val zone = extractZoneFromTopic(commandTopic)
        DebugLog.d(TAG, "Command for zone $zone: $commandTopic = $payload")
        
        return try {
            when {
                commandTopic.endsWith("/command/reboot") -> handleRebootCommand(payload)
                commandTopic.endsWith("/command/mode") -> handleModeCommand(zone, payload)
                commandTopic.endsWith("/command/temperature") -> handleTemperatureCommand(zone, payload)
                commandTopic.endsWith("/command/temperature_high") -> handleTemperatureHighCommand(zone, payload)
                commandTopic.endsWith("/command/temperature_low") -> handleTemperatureLowCommand(zone, payload)
                commandTopic.endsWith("/command/fan_mode") -> handleFanModeCommand(zone, payload)
                else -> Result.failure(Exception("Unknown command: $commandTopic"))
            }
        } catch (e: Exception) {
            Log.e(TAG, "Command error: ${e.message}")
            Result.failure(e)
        }
    }
    
    private fun handleModeCommand(zone: Int, payload: String): Result<Unit> {
        val modeValue = EasyTouchConstants.HA_MODE_TO_DEVICE[payload]
            ?: return Result.failure(Exception("Unknown mode: $payload"))
        
        // HACS integration sends power: 0 for OFF, power: 1 for other modes
        // This is required for the thermostat to accept mode changes
        val powerValue = if (payload == "off") 0 else 1
        
        val command = JSONObject().apply {
            put("Type", "Change")
            put("Changes", JSONObject().apply {
                put("zone", zone)
                put("power", powerValue)
                put("mode", modeValue)
            })
        }
        
        Log.i(TAG, "📤 Sending mode command: zone=$zone, power=$powerValue, mode=$modeValue (HA mode: $payload)")
        writeJsonCommand(command)
        
        // Suppress status updates for 2 seconds to let thermostat process command
        // (prevents UI bounce-back from stale status)
        suppressStatusUpdates(2000)
        
        return Result.success(Unit)
    }
    
    private fun handleTemperatureCommand(zone: Int, payload: String): Result<Unit> {
        val temp = payload.toIntOrNull()
            ?: return Result.failure(Exception("Invalid temperature: $payload"))
        
        val state = currentState?.zones?.get(zone)
            ?: return Result.failure(Exception("No current state for zone $zone"))
        val mode = EasyTouchConstants.DEVICE_TO_HA_MODE[state.modeNum] ?: "off"
        
        val changes = JSONObject().apply {
            put("zone", zone)
            when (mode) {
                "cool" -> put("cool_sp", temp)
                "heat" -> put("heat_sp", temp)
                "dry" -> put("dry_sp", temp)
            }
        }
        
        if (changes.length() > 1) {  // Has more than just zone
            val command = JSONObject().apply {
                put("Type", "Change")
                put("Changes", changes)
            }
            Log.i(TAG, "📤 Sending temperature command: zone=$zone, temp=$temp for mode=$mode")
            writeJsonCommand(command)
            suppressStatusUpdates(2000)
        }
        
        return Result.success(Unit)
    }
    
    private fun handleTemperatureHighCommand(zone: Int, payload: String): Result<Unit> {
        val temp = payload.toIntOrNull()
            ?: return Result.failure(Exception("Invalid temperature: $payload"))
        
        val command = JSONObject().apply {
            put("Type", "Change")
            put("Changes", JSONObject().apply {
                put("zone", zone)
                put("autoCool_sp", temp)
            })
        }
        
        Log.i(TAG, "📤 Sending temp high command: zone=$zone, autoCool_sp=$temp")
        writeJsonCommand(command)
        suppressStatusUpdates(2000)
        return Result.success(Unit)
    }
    
    private fun handleTemperatureLowCommand(zone: Int, payload: String): Result<Unit> {
        val temp = payload.toIntOrNull()
            ?: return Result.failure(Exception("Invalid temperature: $payload"))
        
        val command = JSONObject().apply {
            put("Type", "Change")
            put("Changes", JSONObject().apply {
                put("zone", zone)
                put("autoHeat_sp", temp)
            })
        }
        
        Log.i(TAG, "📤 Sending temp low command: zone=$zone, autoHeat_sp=$temp")
        writeJsonCommand(command)
        suppressStatusUpdates(2000)
        return Result.success(Unit)
    }
    
    private fun handleFanModeCommand(zone: Int, payload: String): Result<Unit> {
        val state = currentState?.zones?.get(zone)
            ?: return Result.failure(Exception("No current state for zone $zone"))
        val mode = EasyTouchConstants.DEVICE_TO_HA_MODE[state.modeNum] ?: "off"
        
        val fanValue = EasyTouchConstants.HA_FAN_TO_VALUE[payload]
            ?: return Result.failure(Exception("Unknown fan mode: $payload"))
        
        val changes = JSONObject().apply {
            put("zone", zone)
        }
        
        when (mode) {
            "fan_only" -> changes.put("fanOnly", fanValue)
            "cool" -> changes.put("coolFan", fanValue)
            "heat" -> changes.put("heatFan", fanValue)
            "auto" -> changes.put("autoFan", fanValue)
        }
        
        val command = JSONObject().apply {
            put("Type", "Change")
            put("Changes", changes)
        }
        
        Log.i(TAG, "📤 Sending fan mode command: zone=$zone, fan=$fanValue for mode=$mode")
        writeJsonCommand(command)
        suppressStatusUpdates(2000)
        return Result.success(Unit)
    }
    
    // ===== DEVICE COMMANDS =====
    
    /**
     * Handle reboot command.
     * Sends reset command to thermostat which triggers a device reboot.
     * Based on HACS integration: {"Type": "Change", "Changes": {"zone": 0, "reset": " OK"}}
     */
    private fun handleRebootCommand(payload: String): Result<Unit> {
        // Payload is typically "PRESS" from Home Assistant button
        Log.i(TAG, "📤 Sending REBOOT command to thermostat")
        
        val command = JSONObject().apply {
            put("Type", "Change")
            put("Changes", JSONObject().apply {
                put("zone", 0)
                put("reset", " OK")  // Note: space before OK is required per HACS integration
            })
        }
        
        writeJsonCommand(command)
        
        // The device will disconnect as it reboots
        // Suppress status updates to avoid confusing the UI during reboot
        suppressStatusUpdates(10000)  // 10 seconds for reboot
        
        return Result.success(Unit)
    }
    
    // ===== DIAGNOSTIC HEALTH STATUS =====
    
    private var diagnosticsDiscoveryPublished = false
    
    /**
     * Publish diagnostic sensor discovery to Home Assistant.
     */
    private fun publishDiagnosticsDiscovery() {
        if (diagnosticsDiscoveryPublished) return
        
        val macId = device.address.replace(":", "").lowercase()
        val nodeId = "easytouch_$macId"
        val prefix = mqttPublisher.topicPrefix
        
        // Get app version
        val appVersion = try {
            context.packageManager.getPackageInfo(context.packageName, 0).versionName ?: "unknown"
        } catch (e: Exception) {
            "unknown"
        }
        
        // Device info for HA discovery (no hw_version - published as diagnostic sensors instead)
        val deviceInfo = JSONObject().apply {
            put("identifiers", JSONArray().put("easytouch_$macId"))
            put("name", "EasyTouch ${device.address}")
            put("manufacturer", "phurth")
            put("model", "Micro-Air EasyTouch RV thermostat plugin for the Android BLE to MQTT Bridge")
            put("sw_version", appVersion)
            put("connections", JSONArray().put(JSONArray().put("mac").put(device.address)))
        }
        
        // Binary diagnostic sensors
        val binaryDiagnostics = listOf(
            Triple("authenticated", "Authenticated", "diag/authenticated"),
            Triple("connected", "Connected", "diag/connected"),
            Triple("data_healthy", "Data Healthy", "diag/data_healthy")
        )
        
        binaryDiagnostics.forEach { (objectId, name, stateTopic) ->
            val uniqueId = "easytouch_${macId}_diag_$objectId"
            val discoveryTopic = "$prefix/binary_sensor/$nodeId/$objectId/config"
            
            val payload = JSONObject().apply {
                put("name", name)
                put("unique_id", uniqueId)
                put("state_topic", "$prefix/$baseTopic/$stateTopic")
                put("payload_on", "ON")
                put("payload_off", "OFF")
                put("entity_category", "diagnostic")
                put("device", deviceInfo)
            }.toString()
            
            mqttPublisher.publishDiscovery(discoveryTopic, payload)
            Log.i(TAG, "Published diagnostic discovery: $objectId")
        }
        
        // Text diagnostic sensors for device info (Model, Serial, Firmware, Device Type, Config Index)
        data class TextSensor(val objectId: String, val name: String, val stateTopic: String, val icon: String)
        val textDiagnostics = listOf(
            TextSensor("model_number", "Model Number", "diag/model_number", "mdi:barcode"),
            TextSensor("serial_number", "Serial Number", "diag/serial_number", "mdi:identifier"),
            TextSensor("firmware_version", "Firmware Version", "diag/firmware_version", "mdi:chip"),
            TextSensor("device_type", "Device Type", "diag/device_type", "mdi:thermostat"),
            TextSensor("config_index", "Config Index", "diag/config_index", "mdi:cog")
        )
        
        textDiagnostics.forEach { sensor ->
            val uniqueId = "easytouch_${macId}_diag_${sensor.objectId}"
            val discoveryTopic = "$prefix/sensor/$nodeId/${sensor.objectId}/config"
            
            val payload = JSONObject().apply {
                put("name", sensor.name)
                put("unique_id", uniqueId)
                put("state_topic", "$prefix/$baseTopic/${sensor.stateTopic}")
                put("icon", sensor.icon)
                put("entity_category", "diagnostic")
                put("device", deviceInfo)
            }.toString()
            
            mqttPublisher.publishDiscovery(discoveryTopic, payload)
            Log.i(TAG, "Published diagnostic sensor discovery: ${sensor.objectId}")
        }
        
        // Reboot button - device configuration control
        publishRebootButtonDiscovery(macId, nodeId, prefix, deviceInfo)
        
        diagnosticsDiscoveryPublished = true
    }
    
    /**
     * Publish reboot button discovery to Home Assistant.
     * This creates a button entity that triggers thermostat reboot when pressed.
     */
    private fun publishRebootButtonDiscovery(macId: String, nodeId: String, prefix: String, deviceInfo: JSONObject) {
        val uniqueId = "easytouch_${macId}_reboot"
        val discoveryTopic = "$prefix/button/$nodeId/reboot/config"
        
        val payload = JSONObject().apply {
            put("name", "Reboot")
            put("unique_id", uniqueId)
            // Use device/ as pseudo-zone to match existing wildcard pattern: +/command/#
            put("command_topic", "$prefix/$baseTopic/device/command/reboot")
            put("payload_press", "PRESS")
            put("device_class", "restart")
            put("entity_category", "config")
            put("icon", "mdi:restart")
            put("device", deviceInfo)
            put("availability_topic", "$prefix/$baseTopic/availability")
        }.toString()
        
        mqttPublisher.publishDiscovery(discoveryTopic, payload)
        Log.i(TAG, "Published reboot button discovery")
    }
    
    /**
     * Publish current diagnostic state to MQTT and update UI.
     */
    private fun publishDiagnosticsState(isConnected: Boolean) {
        val isPaired = isAuthenticated  // Use protocol-level auth, not OS bonding
        val dataHealthy = isAuthenticated && isPollingActive
        
        // Publish binary diagnostic states
        mqttPublisher.publishState("easytouch/${device.address}/diag/authenticated", if (isPaired) "ON" else "OFF", true)
        mqttPublisher.publishState("easytouch/${device.address}/diag/connected", if (isConnected) "ON" else "OFF", true)
        mqttPublisher.publishState("easytouch/${device.address}/diag/data_healthy", if (dataHealthy) "ON" else "OFF", true)
        
        // Publish text diagnostic states (device info from BLE/protocol)
        deviceModel?.let { mqttPublisher.publishState("easytouch/${device.address}/diag/model_number", it, true) }
        bleSerialNumber?.let { mqttPublisher.publishState("easytouch/${device.address}/diag/serial_number", it, true) }
        bleFirmwareRevision?.let { mqttPublisher.publishState("easytouch/${device.address}/diag/firmware_version", it, true) }
        bleModelNumber?.let { mqttPublisher.publishState("easytouch/${device.address}/diag/device_type", it, true) }
        bleHardwareRevision?.let { mqttPublisher.publishState("easytouch/${device.address}/diag/config_index", it, true) }
        
        // Update UI status for this plugin
        mqttPublisher.updatePluginStatus(
            pluginId = "easytouch",
            connected = isConnected,
            authenticated = isPaired,
            dataHealthy = dataHealthy
        )
        
        DebugLog.d(TAG, "Published diagnostic state: authenticated=$isPaired, connected=$isConnected, dataHealthy=$dataHealthy, model=$deviceModel, serial=$bleSerialNumber, firmware=$bleFirmwareRevision")
    }
}

/**
 * Data class to hold configuration for a zone.
 * Parsed from "Get Config" response which includes MAV (Mode AVailable) bitmask.
 */
data class ZoneConfiguration(
    val zone: Int,
    val availableModesBitmask: Int,  // MAV - bitmask of supported modes
    val minCoolSetpoint: Int,
    val maxCoolSetpoint: Int,
    val minHeatSetpoint: Int,
    val maxHeatSetpoint: Int
)

/**
 * Data class to hold state for a single zone.
 * Field names match decompiled U_Thermostat.java
 */
data class ZoneState(
    val zoneNumber: Int,
    val ambientTemperature: Int,      // Index 12: Current ambient temp
    val coolSetpoint: Int,            // Index 2: Cool mode setpoint
    val heatSetpoint: Int,            // Index 3: Heat mode setpoint
    val autoCoolSetpoint: Int,        // Index 1: Auto mode cool setpoint
    val autoHeatSetpoint: Int,        // Index 0: Auto mode heat setpoint
    val drySetpoint: Int,             // Index 4: Dry mode setpoint
    val modeNum: Int,                 // Index 10: Current mode (0=off, 1=fan, 2=cool, 4=heat, 11=auto)
    val fanOnlySpeed: Int,            // Index 6: Fan speed in fan-only mode
    val coolFanSpeed: Int,            // Index 7: Fan speed in cool mode
    val electricFanSpeed: Int,        // Index 8: Fan speed for heat pump
    val autoFanSpeed: Int,            // Index 9: Fan speed in auto mode
    val gasFanSpeed: Int,             // Index 11: Fan speed for furnace
    val cycleActive: Boolean,         // Status flag bit 0
    val isCooling: Boolean,           // Status flag bit 1
    val isHeating: Boolean            // Status flag bit 2
)

/**
 * Data class to hold overall thermostat state (multi-zone aware).
 */
data class ThermostatState(
    val availableZones: List<Int>,           // e.g., [0] or [0, 1]
    val zones: Map<Int, ZoneState>,          // Zone number -> state
    val systemPower: Boolean = true,         // PRM[1] bit 3: System on/off
    val serialNumber: String? = null,
    val firmwareVersion: String? = null
) {
    // Convenience for single-zone access
    val primaryZone: ZoneState? get() = zones[0]
}
