package com.blemqttbridge.plugins.easytouch

import android.bluetooth.*
import android.bluetooth.le.ScanRecord
import android.content.Context
import android.os.Handler
import android.os.Looper
import android.util.Log
import com.blemqttbridge.core.interfaces.BleDevicePlugin
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
            Log.d(TAG, "Device matched by MAC: $deviceAddress")
            return true
        }
        
        // Match by device name prefix
        if (deviceName?.startsWith(EasyTouchConstants.DEVICE_NAME_PREFIX, ignoreCase = true) == true) {
            Log.d(TAG, "Device matched by name: $deviceName")
            return true
        }
        
        // Match by advertised service UUID
        val advertisedServices = scanRecord?.serviceUuids
        if (advertisedServices != null) {
            for (uuid in advertisedServices) {
                if (uuid.uuid == EasyTouchConstants.SERVICE_UUID) {
                    Log.d(TAG, "Device matched by service UUID: ${uuid.uuid}")
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
    
    // Current state (multi-zone aware)
    private var currentState: ThermostatState? = null
    
    // Response buffer for multi-packet JSON responses
    private val responseBuffer = StringBuilder()
    
    // Status polling (matches official app's 4-second interval)
    private var isPollingActive = false
    private val statusPollRunnable = object : Runnable {
        override fun run() {
            if (!isPollingActive || !isAuthenticated) {
                Log.d(TAG, "Polling stopped: active=$isPollingActive, auth=$isAuthenticated")
                return
            }
            
            Log.d(TAG, "📡 Polling status...")
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
                Log.i(TAG, "✅ Connected to ${device.address}")
                this.gatt = gatt
                publishAvailability(true)
                
                // Discover services after brief delay
                mainHandler.postDelayed({
                    gatt.discoverServices()
                }, EasyTouchConstants.SERVICE_DISCOVERY_DELAY_MS)
            }
            BluetoothProfile.STATE_DISCONNECTED -> {
                Log.w(TAG, "❌ Disconnected from ${device.address}")
                stopStatusPolling()  // Stop polling on disconnect
                publishAvailability(false)
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
        
        Log.i(TAG, "✅ Services discovered")
        
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
        
        Log.i(TAG, "✅ All characteristics found")
        
        // Start authentication
        mainHandler.postDelayed({
            authenticate()
        }, EasyTouchConstants.AUTH_STEP_DELAY_MS)
    }
    
    // ===== STATUS POLLING =====
    
    /**
     * Start continuous status polling.
     * Matches the official EasyTouch app behavior: polls every 4 seconds.
     */
    private fun startStatusPolling() {
        if (isPollingActive) {
            Log.d(TAG, "Polling already active")
            return
        }
        
        Log.i(TAG, "🔄 Starting status polling loop (${STATUS_POLL_INTERVAL_MS}ms interval)")
        isPollingActive = true
        
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
        Log.i(TAG, "⏹ Stopping status polling loop")
        isPollingActive = false
        mainHandler.removeCallbacks(statusPollRunnable)
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
        Log.d(TAG, "Characteristic write complete: $uuid, status=$status")
        
        when (uuid) {
            EasyTouchConstants.PASSWORD_CMD_UUID -> {
                if (status == BluetoothGatt.GATT_SUCCESS) {
                    Log.i(TAG, "✅ Password written successfully")
                    // Mark as authenticated and start continuous polling
                    isAuthenticated = true
                    Log.i(TAG, "🎉 Fully authenticated! Starting status polling loop...")
                    startStatusPolling()
                } else {
                    Log.e(TAG, "Password write failed: $status")
                }
            }
            EasyTouchConstants.JSON_CMD_UUID -> {
                if (status == BluetoothGatt.GATT_SUCCESS) {
                    Log.d(TAG, "✅ JSON command written successfully")
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
        
        Log.d(TAG, "Reading jsonReturn characteristic...")
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
            Log.e(TAG, "Characteristic read failed: ${characteristic.uuid}, status=$status")
            return
        }
        
        if (characteristic.uuid == EasyTouchConstants.JSON_RETURN_UUID) {
            processReceivedData(characteristic.value)
        }
    }
    
    /**
     * Process data received from either read or notification
     */
    private fun processReceivedData(data: ByteArray?) {
        if (data == null || data.isEmpty()) {
            Log.d(TAG, "Empty response from jsonReturn")
            return
        }
        
        val chunk = String(data, StandardCharsets.UTF_8)
        Log.d(TAG, "Received data: $chunk")
        
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
            Log.d(TAG, "Waiting for more data (buffer: ${responseBuffer.length} chars)")
            // Schedule another read to get more data
            mainHandler.postDelayed({
                readJsonResponse()
            }, 100)
        }
    }
    
    // ===== JSON COMMUNICATION =====
    
    private fun requestStatus(zone: Int) {
        val command = JSONObject().apply {
            put("Type", "Get Status")
            put("Zone", zone)
            put("EM", "x")  // Email placeholder (required by protocol)
            put("TM", 0)    // Time placeholder
        }
        writeJsonCommand(command)
    }
    
    private fun writeJsonCommand(json: JSONObject) {
        val char = jsonCmdChar ?: return
        val g = gatt ?: return
        
        val jsonString = json.toString()
        Log.d(TAG, "Writing JSON command: $jsonString")
        
        char.value = jsonString.toByteArray(StandardCharsets.UTF_8)
        if (!g.writeCharacteristic(char)) {
            Log.e(TAG, "Failed to write JSON command")
        }
    }
    
    override fun onCharacteristicChanged(
        gatt: BluetoothGatt,
        characteristic: BluetoothGattCharacteristic
    ) {
        if (characteristic.uuid != EasyTouchConstants.JSON_RETURN_UUID) return
        
        val data = characteristic.value
        if (data == null || data.isEmpty()) return
        
        val chunk = String(data, StandardCharsets.UTF_8)
        Log.d(TAG, "Received chunk: $chunk")
        
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
            Log.d(TAG, "Waiting for more data (buffer: ${responseBuffer.length} chars)")
        }
    }
    
    private fun handleJsonResponse(json: JSONObject) {
        Log.d(TAG, "Received JSON: ${json.toString().take(200)}...")
        
        val type = json.optString("Type", "")
        val responseType = json.optString("RT", "")  // Response type field
        
        when {
            // Status response: Type="Response", RT="Status"
            type == "Response" && responseType == "Status" -> {
                val state = parseMultiZoneStatus(json)
                currentState = state
                publishState(state)
                Log.i(TAG, "✅ Status update received and published")
            }
            // Legacy format: Type="Status"
            type == "Status" -> {
                val state = parseMultiZoneStatus(json)
                currentState = state
                publishState(state)
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
    
    // ===== STATE PARSING (Multi-Zone) =====
    
    private fun parseMultiZoneStatus(json: JSONObject): ThermostatState {
        val zSts = json.optJSONObject("Z_sts")
        
        if (zSts == null) {
            Log.e(TAG, "No Z_sts in status response")
            return ThermostatState(emptyList(), emptyMap())
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
        Log.d(TAG, "System power: $systemPower (PRM[1]=${prmArray?.optInt(1, 0) ?: "N/A"})")
        
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
            subscribeToCommands()
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
        when (mode) {
            "cool" -> {
                mqttPublisher.publishState("$zoneTopic/state/target_temperature", state.coolSetpoint.toString(), true)
            }
            "heat" -> {
                mqttPublisher.publishState("$zoneTopic/state/target_temperature", state.heatSetpoint.toString(), true)
            }
            "auto" -> {
                mqttPublisher.publishState("$zoneTopic/state/target_temperature_high", state.autoCoolSetpoint.toString(), true)
                mqttPublisher.publishState("$zoneTopic/state/target_temperature_low", state.autoHeatSetpoint.toString(), true)
            }
            "dry" -> {
                mqttPublisher.publishState("$zoneTopic/state/target_temperature", state.drySetpoint.toString(), true)
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
        
        Log.d(TAG, "Published zone $zoneNum: temp=${state.ambientTemperature}°F, mode=$mode, fan=$fanMode, action=$action")
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
        Log.d(TAG, "Published availability: $payload to $baseTopic/availability")
    }
    
    // ===== MQTT COMMAND SUBSCRIPTION =====
    
    /**
     * Subscribe to MQTT command topics for all zones.
     * Topic pattern: easytouch/{MAC}/+/command/# matches all zone commands
     * The + wildcard matches zone_0, zone_1, etc.
     */
    private fun subscribeToCommands() {
        // Subscribe to all command topics for this device
        // Pattern: easytouch/{MAC}/+/command/# (+ matches zone_N)
        val commandPattern = "$baseTopic/+/command/#"
        
        mqttPublisher.subscribeToCommands(commandPattern) { topic, payload ->
            Log.d(TAG, "📥 Received command: $topic = $payload")
            val result = handleCommand(topic, payload)
            if (result.isFailure) {
                Log.e(TAG, "Command failed: ${result.exceptionOrNull()?.message}")
            }
        }
        
        Log.i(TAG, "✅ Subscribed to command topic: $commandPattern")
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
            
            val payload = JSONObject().apply {
                put("name", entityName)
                put("unique_id", uniqueId)
                put("object_id", "easytouch_${mac}_zone_$zone")  // Explicit entity ID base
                put("device", JSONObject().apply {
                    put("identifiers", JSONArray().put("easytouch_$mac"))
                    put("name", deviceName)
                    put("manufacturer", "Micro-Air")
                    put("model", "EasyTouch")
                })
                put("modes", JSONArray(EasyTouchConstants.SUPPORTED_HVAC_MODES))
                put("fan_modes", JSONArray(EasyTouchConstants.SUPPORTED_FAN_MODES))
                put("temperature_unit", "F")
                put("temp_step", EasyTouchConstants.TEMP_STEP)
                put("min_temp", EasyTouchConstants.MIN_TEMP_F)
                put("max_temp", EasyTouchConstants.MAX_TEMP_F)
                
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
        Log.d(TAG, "Command for zone $zone: $commandTopic = $payload")
        
        return try {
            when {
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
        
        val command = JSONObject().apply {
            put("Type", "Change")
            put("Changes", JSONObject().apply {
                put("zone", zone)
                put("power", if (payload == "off") 0 else 1)
                put("mode", modeValue)
            })
        }
        
        writeJsonCommand(command)
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
            writeJsonCommand(command)
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
        
        writeJsonCommand(command)
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
        
        writeJsonCommand(command)
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
        
        writeJsonCommand(command)
        return Result.success(Unit)
    }
}

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
