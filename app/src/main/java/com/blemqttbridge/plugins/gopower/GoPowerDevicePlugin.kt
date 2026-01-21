package com.blemqttbridge.plugins.gopower

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
import com.blemqttbridge.plugins.gopower.protocol.GoPowerConstants
import org.json.JSONObject
import org.json.JSONArray
import java.nio.charset.StandardCharsets
import java.util.*

/**
 * GoPower Solar Charge Controller BLE Device Plugin
 * 
 * Supports: GP-PWM-30-SB and similar GoPower PWM charge controllers
 * 
 * Protocol: ASCII text over BLE with semicolon-delimited responses
 * - No authentication required
 * - Write space character (0x20) to poll status
 * - 32 fields in response containing voltage, current, SOC, temp, energy
 */
class GoPowerDevicePlugin : BleDevicePlugin {
    
    companion object {
        private const val TAG = "GoPowerPlugin"
        const val PLUGIN_ID = "gopower"
        const val PLUGIN_VERSION = "1.0.0"
    }
    
    override val pluginId: String = PLUGIN_ID
    override var instanceId: String = PLUGIN_ID  // Same as pluginId by default
    override val supportsMultipleInstances: Boolean = false
    override val displayName: String = "GoPower Solar Controller"
    
    private var context: Context? = null
    private var config: PluginConfig? = null
    
    // Configuration from settings
    private var controllerMac: String = ""
    
    // Strong reference to callback to prevent GC
    private var gattCallback: BluetoothGattCallback? = null
    
    // Current callback instance for command handling
    private var currentCallback: GoPowerGattCallback? = null
    
    override fun initialize(context: Context?, config: PluginConfig) {
        Log.i(TAG, "Initializing GoPower Device Plugin v$PLUGIN_VERSION")
        this.context = context
        this.config = config
        
        // Load configuration
        controllerMac = config.getString("controller_mac", controllerMac)
        
        Log.i(TAG, "Configured for controller: $controllerMac")
    }
    
    override fun matchesDevice(
        device: BluetoothDevice,
        scanRecord: ScanRecord?
    ): Boolean {
        // SECURITY: Only match on exact configured MAC address.
        // This prevents connecting to neighbors' devices in RV parks.
        // No auto-discovery by device name or service UUID.
        
        if (controllerMac.isBlank()) {
            return false  // No MAC configured = no matching
        }
        
        val deviceAddress = device.address
        if (deviceAddress.equals(controllerMac, ignoreCase = true)) {
            Log.d(TAG, "Device matched by configured MAC: $deviceAddress")
            return true
        }
        
        return false
    }
    
    override fun getConfiguredDevices(): List<String> {
        return if (controllerMac.isNotBlank()) listOf(controllerMac) else emptyList()
    }
    
    override fun createGattCallback(
        device: BluetoothDevice,
        context: Context,
        mqttPublisher: MqttPublisher,
        onDisconnect: (BluetoothDevice, Int) -> Unit
    ): BluetoothGattCallback {
        Log.i(TAG, "Creating GATT callback for ${device.address}")
        val callback = GoPowerGattCallback(device, context, mqttPublisher, instanceId, onDisconnect)
        Log.i(TAG, "Created callback with hashCode=${callback.hashCode()}")
        // Keep strong reference to prevent GC
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
        return "gopower/${device.address}"
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
        Log.i(TAG, "Command received: $commandTopic = $payload")
        val callback = currentCallback ?: return Result.failure(Exception("No active connection"))
        return callback.handleCommand(commandTopic, payload)
    }
    
    override fun destroy() {
        Log.i(TAG, "Destroying GoPower Plugin")
        currentCallback = null
        gattCallback = null
    }
}

/**
 * GATT Callback for GoPower Solar Charge Controller.
 * 
 * Handles:
 * - Connection establishment
 * - Service/characteristic discovery
 * - Notification subscription (no authentication needed)
 * - ASCII response parsing
 * - State parsing and MQTT publishing
 * - Reboot command handling
 */
class GoPowerGattCallback(
    private val device: BluetoothDevice,
    private val context: Context,
    private val mqttPublisher: MqttPublisher,
    private val instanceId: String,
    private val onDisconnect: (BluetoothDevice, Int) -> Unit
) : BluetoothGattCallback() {
    
    private var gatt: BluetoothGatt? = null
    private var writeChar: BluetoothGattCharacteristic? = null
    private var notifyChar: BluetoothGattCharacteristic? = null
    
    private val mainHandler = Handler(Looper.getMainLooper())
    private var discoveryPublished = false
    private var diagnosticsDiscoveryPublished = false
    
    // Current parsed state
    private var currentState: GoPowerState? = null
    
    // Device info extracted from status responses
    private var deviceModel: String = "GP-PWM-30-SB"  // Default model
    private var deviceSerial: String = "Unknown"
    private var deviceFirmware: String = "Unknown"
    
    // Feature detection - track if auxiliary battery is present
    private var hasAuxBattery: Boolean = false
    private var auxBatteryCheckCount: Int = 0
    private val AUX_BATTERY_CHECK_SAMPLES = 3  // Check first 3 readings
    
    // Response buffer for multi-packet responses
    private val responseBuffer = StringBuilder()
    
    // Status polling
    private var isPollingActive = false
    private var isConnected = false
    
    // GATT 133 retry tracking
    private var gatt133RetryCount = 0
    private val MAX_GATT_133_RETRIES = 3
    private val GATT_133_RETRY_DELAY_MS = 2000L
    
    // Connection watchdog
    private var watchdogRunnable: Runnable? = null
    private var lastSuccessfulOperationTime: Long = System.currentTimeMillis()
    private val WATCHDOG_INTERVAL_MS = 60000L  // Check connection health every 60s
    
    private val statusPollRunnable = object : Runnable {
        override fun run() {
            if (!isPollingActive || !isConnected) {
                DebugLog.d(TAG, "Polling stopped: active=$isPollingActive, connected=$isConnected")
                return
            }
            
            DebugLog.d(TAG, "Polling status...")
            pollStatus()
            
            // Schedule next poll
            mainHandler.postDelayed(this, GoPowerConstants.STATUS_POLL_INTERVAL_MS)
        }
    }
    
    companion object {
        private const val TAG = "GoPowerGattCallback"
    }
    
    // Base topic for MQTT
    private val baseTopic: String
        get() = "gopower/${device.address}"
    
    // ===== LIFECYCLE CALLBACKS =====
    
    override fun onConnectionStateChange(gatt: BluetoothGatt, status: Int, newState: Int) {
        Log.i(TAG, "Connection state: $newState (status: $status)")
        val stateStr = when (newState) {
            BluetoothProfile.STATE_CONNECTED -> "CONNECTED"
            BluetoothProfile.STATE_DISCONNECTED -> "DISCONNECTED"
            BluetoothProfile.STATE_CONNECTING -> "CONNECTING"
            BluetoothProfile.STATE_DISCONNECTING -> "DISCONNECTING"
            else -> "UNKNOWN($newState)"
        }
        mqttPublisher.logBleEvent("STATE_CHANGE: $stateStr (status=$status)")
        
        when (status) {
            BluetoothGatt.GATT_SUCCESS -> {
                when (newState) {
                    BluetoothProfile.STATE_CONNECTED -> {
                        Log.i(TAG, "Connected to ${device.address}")
                        this.gatt = gatt
                        isConnected = true
                        // Reset retry counter on successful connection
                        gatt133RetryCount = 0
                        lastSuccessfulOperationTime = System.currentTimeMillis()
                        publishAvailability(true)
                        publishDiagnosticsDiscovery()
                        publishDiagnosticsState()
                        
                        // Discover services after brief delay
                        mainHandler.postDelayed({
                            gatt.discoverServices()
                        }, GoPowerConstants.SERVICE_DISCOVERY_DELAY_MS)
                    }
                    BluetoothProfile.STATE_DISCONNECTED -> {
                        Log.w(TAG, "Disconnected from ${device.address}")
                        cleanup()
                        onDisconnect(device, status)
                    }
                }
            }
            133 -> {
                gatt133RetryCount++
                Log.e(TAG, "⚠️ GATT_ERROR (133) - Connection failed (attempt $gatt133RetryCount/$MAX_GATT_133_RETRIES)")
                cleanup()
                
                if (gatt133RetryCount < MAX_GATT_133_RETRIES) {
                    Log.i(TAG, "🔄 Retrying connection in ${GATT_133_RETRY_DELAY_MS}ms...")
                    mainHandler.postDelayed({
                        try {
                            Log.i(TAG, "🔄 Attempting reconnection (retry $gatt133RetryCount)...")
                            val newGatt = device.connectGatt(
                                context,
                                false,
                                this,
                                BluetoothDevice.TRANSPORT_LE
                            )
                            if (newGatt != null) {
                                this.gatt = newGatt
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
                    onDisconnect(device, status)
                }
            }
            8 -> {
                Log.e(TAG, "⏱️ Connection timeout (status 8)")
                cleanup()
                onDisconnect(device, status)
            }
            19 -> {
                Log.e(TAG, "🚫 Peer terminated connection (status 19)")
                cleanup()
                onDisconnect(device, status)
            }
            else -> {
                Log.e(TAG, "❌ Connection failed with status: $status")
                cleanup()
                onDisconnect(device, status)
            }
        }
    }
    
    override fun onServicesDiscovered(gatt: BluetoothGatt, status: Int) {
        mqttPublisher.logBleEvent("SERVICES_DISCOVERED: status=$status, count=${gatt.services.size}")
        if (status != BluetoothGatt.GATT_SUCCESS) {
            Log.e(TAG, "Service discovery failed: $status")
            return
        }
        
        lastSuccessfulOperationTime = System.currentTimeMillis()
        Log.i(TAG, "Services discovered")
        
        // Find GoPower service
        val service = gatt.getService(GoPowerConstants.SERVICE_UUID)
        if (service == null) {
            Log.e(TAG, "GoPower service not found!")
            return
        }
        
        // Get characteristics
        writeChar = service.getCharacteristic(GoPowerConstants.WRITE_CHARACTERISTIC_UUID)
        notifyChar = service.getCharacteristic(GoPowerConstants.NOTIFY_CHARACTERISTIC_UUID)
        
        if (writeChar == null || notifyChar == null) {
            Log.e(TAG, "Missing characteristics! write=${writeChar != null}, notify=${notifyChar != null}")
            return
        }
        
        Log.i(TAG, "All characteristics found")
        
        // Enable notifications on FFF1
        mainHandler.postDelayed({
            enableNotifications()
        }, GoPowerConstants.OPERATION_DELAY_MS)
    }
    
    /**
     * Enable notifications on the notify characteristic (FFF1)
     */
    private fun enableNotifications() {
        val char = notifyChar ?: return
        val g = gatt ?: return
        
        Log.i(TAG, "Enabling notifications on FFF1...")
        
        // Enable local notifications
        if (!g.setCharacteristicNotification(char, true)) {
            Log.e(TAG, "Failed to set characteristic notification")
            return
        }
        
        // Write to CCCD to enable remote notifications
        val descriptor = char.getDescriptor(GoPowerConstants.CCCD_UUID)
        if (descriptor != null) {
            descriptor.value = BluetoothGattDescriptor.ENABLE_NOTIFICATION_VALUE
            if (!g.writeDescriptor(descriptor)) {
                Log.e(TAG, "Failed to write CCCD descriptor")
            } else {
                Log.i(TAG, "CCCD write initiated")
            }
        } else {
            Log.w(TAG, "CCCD descriptor not found, notifications may not work properly")
            // Some devices work without explicit CCCD write
            startStatusPolling()
        }
    }
    
    override fun onDescriptorWrite(
        gatt: BluetoothGatt,
        descriptor: BluetoothGattDescriptor,
        status: Int
    ) {
        if (descriptor.uuid == GoPowerConstants.CCCD_UUID) {
            if (status == BluetoothGatt.GATT_SUCCESS) {
                Log.i(TAG, "Notifications enabled")
                // No authentication needed - start polling immediately
                publishDiagnosticsState()
                startStatusPolling()
            } else {
                Log.e(TAG, "Failed to enable notifications: $status")
            }
        }
    }
    
    // ===== STATUS POLLING =====
    
    private fun startStatusPolling() {
        if (isPollingActive) {
            Log.d(TAG, "Polling already active")
            return
        }
        
        Log.i(TAG, "Starting status polling loop (${GoPowerConstants.STATUS_POLL_INTERVAL_MS}ms interval)")
        isPollingActive = true
        publishDiagnosticsState()
        startWatchdog()
        
        // Request first status immediately
        mainHandler.postDelayed({
            statusPollRunnable.run()
        }, GoPowerConstants.OPERATION_DELAY_MS)
    }
    
    private fun stopStatusPolling() {
        Log.i(TAG, "Stopping status polling loop")
        isPollingActive = false
        mainHandler.removeCallbacks(statusPollRunnable)
    }
    
    private fun cleanup() {
        stopStatusPolling()
        stopWatchdog()
        isConnected = false
        publishAvailability(false)
        publishDiagnosticsState()
    }
    
    /**
     * Send poll command (ASCII space) to request status
     */
    private fun pollStatus() {
        val char = writeChar ?: run {
            Log.e(TAG, "Cannot poll: writeChar is null")
            return
        }
        val g = gatt ?: run {
            Log.e(TAG, "Cannot poll: gatt is null")
            return
        }
        
        char.value = GoPowerConstants.POLL_COMMAND
        if (g.writeCharacteristic(char)) {
            Log.d(TAG, "Poll command sent (0x20)")
        } else {
            Log.e(TAG, "Failed to send poll command")
        }
    }
    
    override fun onCharacteristicWrite(
        gatt: BluetoothGatt,
        characteristic: BluetoothGattCharacteristic,
        status: Int
    ) {
        val hex = characteristic.value?.joinToString(" ") { "%02X".format(it) } ?: "(null)"
        mqttPublisher.logBleEvent("WRITE ${characteristic.uuid}: $hex (status=$status)")
        if (status == BluetoothGatt.GATT_SUCCESS) {
            DebugLog.d(TAG, "Characteristic write complete: ${characteristic.uuid}")
        } else {
            Log.e(TAG, "Characteristic write failed: ${characteristic.uuid}, status=$status")
        }
    }
    
    // Android 13+ (API 33+) callback with direct value parameter
    override fun onCharacteristicChanged(
        gatt: BluetoothGatt,
        characteristic: BluetoothGattCharacteristic,
        value: ByteArray
    ) {
        if (characteristic.uuid != GoPowerConstants.NOTIFY_CHARACTERISTIC_UUID) return
        
        handleCharacteristicData(value)
    }
    
    // Older Android versions - deprecated in API 33
    @Deprecated("Deprecated in API 33")
    override fun onCharacteristicChanged(
        gatt: BluetoothGatt,
        characteristic: BluetoothGattCharacteristic
    ) {
        if (characteristic.uuid != GoPowerConstants.NOTIFY_CHARACTERISTIC_UUID) return
        
        val data = characteristic.value
        if (data != null) {
            handleCharacteristicData(data)
        }
    }
    
    /**
     * Handle characteristic data for both API 33+ and older Android versions
     */
    private fun handleCharacteristicData(data: ByteArray) {
        if (data.isEmpty()) return
        
        lastSuccessfulOperationTime = System.currentTimeMillis()
        val hex = data.joinToString(" ") { "%02X".format(it) }
        mqttPublisher.logBleEvent("NOTIFY ${GoPowerConstants.NOTIFY_CHARACTERISTIC_UUID}: $hex")
        val chunk = String(data, StandardCharsets.UTF_8)
        DebugLog.d(TAG, "Received chunk: $chunk")
        
        responseBuffer.append(chunk)
        
        // Check if we have a complete response (contains enough semicolons)
        val bufferStr = responseBuffer.toString()
        val fieldCount = bufferStr.count { it == ';' }
        
        if (fieldCount >= GoPowerConstants.EXPECTED_FIELD_COUNT - 1) {
            // Likely complete response - try to parse
            responseBuffer.clear()
            parseAndPublishStatus(bufferStr)
        } else {
            DebugLog.d(TAG, "Waiting for more data (have $fieldCount fields)")
        }
    }
    
    /**
     * Parse ASCII response and publish to MQTT
     */
    private fun parseAndPublishStatus(response: String) {
        DebugLog.d(TAG, "Parsing response: ${response.take(100)}...")
        
        val fields = response.split(GoPowerConstants.FIELD_DELIMITER)
        if (fields.size < GoPowerConstants.EXPECTED_FIELD_COUNT) {
            Log.w(TAG, "Incomplete response: ${fields.size} fields (expected ${GoPowerConstants.EXPECTED_FIELD_COUNT})")
            return
        }
        
        // Debug: Log ALL 32 fields to identify which contains resettable Ah counter
        Log.d(TAG, "ALL FIELDS: ${fields.mapIndexed { index, value -> "[$index]=$value" }.joinToString(", ")}")
        
        try {
            // Parse fields with scaling
            val solarCurrentMa = fields.getOrNull(GoPowerConstants.FIELD_SOLAR_CURRENT)?.trim()?.toIntOrNull() ?: 0
            val batteryVoltageMv = fields.getOrNull(GoPowerConstants.FIELD_BATTERY_VOLTAGE)?.trim()?.toIntOrNull() ?: 0
            val soc = fields.getOrNull(GoPowerConstants.FIELD_SOC)?.trim()?.toIntOrNull() ?: 0
            val solarVoltageMv = fields.getOrNull(GoPowerConstants.FIELD_SOLAR_VOLTAGE)?.trim()?.toIntOrNull() ?: 0
            val tempC = parseSignedInt(fields.getOrNull(GoPowerConstants.FIELD_TEMP_C)?.trim())
            val tempF = parseSignedInt(fields.getOrNull(GoPowerConstants.FIELD_TEMP_F)?.trim())
            
            // Parse amp-hours field (current accumulated charge)
            val ampHours = fields.getOrNull(GoPowerConstants.FIELD_AMP_HOURS_TODAY)?.trim()?.toIntOrNull() ?: 0
            
            Log.d(TAG, "Amp-hours: $ampHours Ah")
            
            // Parse device info
            val firmware = fields.getOrNull(GoPowerConstants.FIELD_FIRMWARE)?.trim()?.toIntOrNull()
            val serialHex = fields.getOrNull(GoPowerConstants.FIELD_SERIAL)?.trim()
            
            // Update device info fields if available
            if (firmware != null) {
                deviceFirmware = firmware.toString()
            }
            if (serialHex != null) {
                // Serial is hex string - convert to decimal (e.g., "177" hex = 375 decimal)
                try {
                    deviceSerial = serialHex.toInt(16).toString()
                } catch (e: NumberFormatException) {
                    Log.w(TAG, "Failed to parse serial number: $serialHex")
                }
            }
            
            // Convert to display units
            val solarCurrent = solarCurrentMa / 1000.0
            val batteryVoltage = batteryVoltageMv / 1000.0
            val solarVoltage = solarVoltageMv / 1000.0
            val solarPower = solarVoltage * solarCurrent  // Calculate solar power in watts
            
            // Convert Ah to Wh for energy dashboard: Wh = Ah × Voltage
            val energyWh = (ampHours * batteryVoltage).toInt()
            
            val state = GoPowerState(
                solarVoltage = solarVoltage,
                solarCurrent = solarCurrent,
                solarPower = solarPower,
                batteryVoltage = batteryVoltage,
                stateOfCharge = soc,
                temperatureC = tempC,
                temperatureF = tempF,
                ampHours = energyWh,  // Now contains Wh instead of Ah
                ampHoursAux = 0  // Not used
            )
            
            Log.i(TAG, "Parsed: PV=${solarVoltage}V ${solarCurrent}A ${solarPower}W, Batt=${batteryVoltage}V ${soc}%, Energy=${energyWh}Wh, Temp=${tempC}°C")
            
            currentState = state
            
            // All models use single daily amp-hour counter - no aux battery
            hasAuxBattery = false
            
            // Publish discovery on first data
            if (!discoveryPublished) {
                publishDiscovery()
                discoveryPublished = true
            }
            
            // Publish current state
            publishState(state)
            publishDiagnosticsState()
            
        } catch (e: Exception) {
            Log.e(TAG, "Error parsing response: ${e.message}", e)
        }
    }
    
    /**
     * Parse signed integer (e.g., "+06" or "-05")
     */
    private fun parseSignedInt(str: String?): Int {
        if (str.isNullOrBlank()) return 0
        return try {
            str.replace("+", "").toInt()
        } catch (e: NumberFormatException) {
            0
        }
    }
    
    // ===== MQTT PUBLISHING =====
    
    private fun publishAvailability(online: Boolean) {
        val topic = "$baseTopic/availability"
        mqttPublisher.publishAvailability(topic, online)
        DebugLog.d(TAG, "Published availability: $online")
    }
    
    private fun publishDiscovery() {
        val macId = device.address.replace(":", "").lowercase()
        val nodeId = "gopower_$macId"
        val prefix = mqttPublisher.topicPrefix
        
        // Get app version
        val appVersion = try {
            context.packageManager.getPackageInfo(context.packageName, 0).versionName ?: "unknown"
        } catch (e: Exception) {
            "unknown"
        }
        
        // Device info for HA discovery
        val deviceInfo = JSONObject().apply {
            put("identifiers", JSONArray().put("gopower_$macId"))
            put("name", "GoPower ${device.address}")
            put("manufacturer", "phurth")
            put("model", "GoPower solar controller plugin for the Android BLE to MQTT Bridge")
            put("sw_version", appVersion)
            put("connections", JSONArray().put(JSONArray().put("mac").put(device.address)))
        }
        
        // Availability configuration
        val availabilityConfig = JSONArray().apply {
            put(JSONObject().apply {
                put("topic", "$prefix/$baseTopic/availability")
                put("payload_available", "online")
                put("payload_not_available", "offline")
            })
        }
        
        // Define sensors (common to all models)
        val commonSensors = listOf(
            SensorConfig("solar_voltage", "Solar Voltage", "V", "voltage", "measurement", "mdi:solar-panel", "$baseTopic/solar_voltage"),
            SensorConfig("solar_current", "Solar Current", "A", "current", "measurement", "mdi:current-dc", "$baseTopic/solar_current"),
            SensorConfig("solar_power", "Solar Power", "W", "power", "measurement", "mdi:solar-power", "$baseTopic/solar_power"),
            SensorConfig("battery_voltage", "Battery Voltage", "V", "voltage", "measurement", "mdi:car-battery", "$baseTopic/battery_voltage"),
            SensorConfig("state_of_charge", "State of Charge", "%", "battery", "measurement", "mdi:battery", "$baseTopic/state_of_charge"),
            SensorConfig("temperature", "Temperature", "°C", "temperature", "measurement", "mdi:thermometer", "$baseTopic/temperature"),
            SensorConfig("energy", "Energy", "Wh", "energy", "total_increasing", "mdi:lightning-bolt", "$baseTopic/energy")
        )
        
        // Build final sensor list
        val sensors = commonSensors
        
        // Publish sensor discovery
        sensors.forEach { sensor ->
            val uniqueId = "gopower_${macId}_${sensor.objectId}"
            val discoveryTopic = "$prefix/sensor/$nodeId/${sensor.objectId}/config"
            
            val payload = JSONObject().apply {
                put("name", sensor.name)
                put("unique_id", uniqueId)
                put("state_topic", "$prefix/${sensor.stateTopic}")
                put("unit_of_measurement", sensor.unit)
                if (sensor.deviceClass != null) {
                    put("device_class", sensor.deviceClass)
                }
                if (sensor.stateClass != null) {
                    put("state_class", sensor.stateClass)
                }
                put("icon", sensor.icon)
                put("availability", availabilityConfig)
                put("device", deviceInfo)
            }.toString()
            
            mqttPublisher.publishDiscovery(discoveryTopic, payload)
            Log.i(TAG, "Published sensor discovery: ${sensor.objectId}")
        }
        
        // Publish reboot button discovery
        val rebootUniqueId = "gopower_${macId}_reboot"
        val rebootDiscoveryTopic = "$prefix/button/$nodeId/reboot/config"
        
        val rebootPayload = JSONObject().apply {
            put("name", "Reboot Controller")
            put("unique_id", rebootUniqueId)
            put("command_topic", "$prefix/$baseTopic/command/reboot")
            put("payload_press", "PRESS")
            put("icon", "mdi:restart")
            put("entity_category", "config")
            put("availability", availabilityConfig)
            put("device", deviceInfo)
        }.toString()
        
        mqttPublisher.publishDiscovery(rebootDiscoveryTopic, rebootPayload)
        Log.i(TAG, "Published button discovery: reboot")
        
        // Subscribe to commands
        mqttPublisher.subscribeToCommands("$baseTopic/command/#") { topic, payload ->
            mainHandler.post {
                handleCommand(topic, payload)
            }
        }
    }
    
    private fun publishState(state: GoPowerState) {
        // Publish each sensor value
        mqttPublisher.publishState("$baseTopic/solar_voltage", "%.3f".format(state.solarVoltage), true)
        mqttPublisher.publishState("$baseTopic/solar_current", "%.3f".format(state.solarCurrent), true)
        mqttPublisher.publishState("$baseTopic/solar_power", "%.1f".format(state.solarPower), true)
        mqttPublisher.publishState("$baseTopic/battery_voltage", "%.3f".format(state.batteryVoltage), true)
        mqttPublisher.publishState("$baseTopic/state_of_charge", state.stateOfCharge.toString(), true)
        mqttPublisher.publishState("$baseTopic/temperature", state.temperatureC.toString(), true)
        mqttPublisher.publishState("$baseTopic/energy", state.ampHours.toString(), true)  // ampHours field now contains Wh
        
        DebugLog.d(TAG, "Published state")
    }
    
    // ===== COMMAND HANDLING =====
    
    fun handleCommand(commandTopic: String, payload: String): Result<Unit> {
        Log.i(TAG, "Handling command: $commandTopic = $payload")
        
        return when {
            commandTopic.endsWith("/command/reboot") -> {
                if (payload == "PRESS") {
                    sendRebootCommand()
                } else {
                    Result.failure(Exception("Invalid payload for reboot: $payload"))
                }
            }
            else -> {
                Log.w(TAG, "Unknown command topic: $commandTopic")
                Result.failure(Exception("Unknown command: $commandTopic"))
            }
        }
    }
    
    /**
     * Send soft reset/reboot command to controller.
     * This reboots the controller without clearing any settings.
     * IMPORTANT: Must send unlock sequence first!
     */
    private fun sendRebootCommand(): Result<Unit> {
        val char = writeChar ?: return Result.failure(Exception("Write characteristic not available"))
        val g = gatt ?: return Result.failure(Exception("GATT not connected"))
        
        Log.i(TAG, "Sending unlock sequence before reboot...")
        
        // Step 1: Send unlock command
        char.value = GoPowerConstants.UNLOCK_COMMAND
        if (!g.writeCharacteristic(char)) {
            Log.e(TAG, "Failed to send unlock command")
            return Result.failure(Exception("Failed to write unlock command"))
        }
        
        Log.i(TAG, "Unlock command sent, waiting ${GoPowerConstants.UNLOCK_DELAY_MS}ms before reboot...")
        
        // Step 2: Send reboot command after delay (must be on main thread)
        mainHandler.postDelayed({
            val writeCharDelay = writeChar
            val gattDelay = gatt
            
            if (writeCharDelay == null || gattDelay == null) {
                Log.e(TAG, "Lost connection before reboot command could be sent")
                return@postDelayed
            }
            
            writeCharDelay.value = GoPowerConstants.REBOOT_COMMAND
            if (gattDelay.writeCharacteristic(writeCharDelay)) {
                Log.i(TAG, "Soft reset/reboot command sent (after unlock)")
            } else {
                Log.e(TAG, "Failed to send reboot command after unlock")
            }
        }, GoPowerConstants.UNLOCK_DELAY_MS)
        
        return Result.success(Unit)
    }
    
    // ===== DIAGNOSTIC HEALTH STATUS =====
    
    /**
     * Publish diagnostic sensor discovery to Home Assistant.
     * GoPower only has Connected and Data (no authentication needed)
     */
    private fun publishDiagnosticsDiscovery() {
        if (diagnosticsDiscoveryPublished) return
        
        val macId = device.address.replace(":", "").lowercase()
        val nodeId = "gopower_$macId"
        val prefix = mqttPublisher.topicPrefix
        
        // Get app version
        val appVersion = try {
            context.packageManager.getPackageInfo(context.packageName, 0).versionName ?: "unknown"
        } catch (e: Exception) {
            "unknown"
        }
        
        // Device info for HA discovery
        val deviceInfo = JSONObject().apply {
            put("identifiers", JSONArray().put("gopower_$macId"))
            put("name", "GoPower ${device.address}")
            put("manufacturer", "phurth")
            put("model", "GoPower solar controller plugin for the Android BLE to MQTT Bridge")
            put("sw_version", appVersion)
            put("connections", JSONArray().put(JSONArray().put("mac").put(device.address)))
        }
        
        // Diagnostic sensors to publish (no authentication needed for GoPower)
        val binarySensors = listOf(
            Triple("connected", "Connected", "diag/connected"),
            Triple("data_healthy", "Data Healthy", "diag/data_healthy")
        )
        
        val textSensors = listOf(
            Triple("model_number", "Model Number", "diag/model_number"),
            Triple("firmware_version", "Firmware Version", "diag/firmware_version")
        )
        
        // Binary sensors (connected, data_healthy)
        binarySensors.forEach { (objectId, name, stateTopic) ->
            val uniqueId = "gopower_${macId}_diag_$objectId"
            val discoveryTopic = "$prefix/binary_sensor/$nodeId/$objectId/config"
            
            val payload = JSONObject().apply {
                put("name", name)
                put("unique_id", uniqueId)
                put("state_topic", "$prefix/$baseTopic/$stateTopic")
                put("payload_on", "ON")
                put("payload_off", "OFF")
                put("availability_topic", "$prefix/availability")
                put("payload_available", "online")
                put("payload_not_available", "offline")
                put("entity_category", "diagnostic")
                put("device", deviceInfo)
            }.toString()
            
            mqttPublisher.publishDiscovery(discoveryTopic, payload)
            Log.i(TAG, "Published diagnostic discovery: $objectId")
        }
        
        // Text sensors (model, serial, firmware, fault_code, fault_description)
        textSensors.forEach { (objectId, name, stateTopic) ->
            val uniqueId = "gopower_${macId}_diag_$objectId"
            val discoveryTopic = "$prefix/sensor/$nodeId/$objectId/config"
            
            val payload = JSONObject().apply {
                put("name", name)
                put("unique_id", uniqueId)
                put("state_topic", "$prefix/$baseTopic/$stateTopic")
                put("entity_category", "diagnostic")
                put("device", deviceInfo)
            }.toString()
            
            mqttPublisher.publishDiscovery(discoveryTopic, payload)
            Log.i(TAG, "Published diagnostic discovery: $objectId")
        }
        
        diagnosticsDiscoveryPublished = true
    }
    
    /**
     * Publish current diagnostic state to MQTT and update UI.
     */
    private fun publishDiagnosticsState() {
        val dataHealthy = isConnected && isPollingActive && currentState != null
        
        // Publish to MQTT
        mqttPublisher.publishState("$baseTopic/diag/connected", if (isConnected) "ON" else "OFF", true)
        mqttPublisher.publishState("$baseTopic/diag/data_healthy", if (dataHealthy) "ON" else "OFF", true)
        
        // Publish device info
        mqttPublisher.publishState("$baseTopic/diag/model_number", deviceModel, true)
        mqttPublisher.publishState("$baseTopic/diag/firmware_version", deviceFirmware, true)
        
        // Update UI status for this plugin (no auth for GoPower)
        mqttPublisher.updatePluginStatus(
            pluginId = instanceId,
            connected = isConnected,
            authenticated = isConnected,  // No separate auth for GoPower
            dataHealthy = dataHealthy
        )
        
        DebugLog.d(TAG, "Published diagnostic state: connected=$isConnected, dataHealthy=$dataHealthy, model=$deviceModel, serial=$deviceSerial, fw=$deviceFirmware")
    }    
    /**
     * Start connection watchdog - detects zombie states and stale connections
     */
    private fun startWatchdog() {
        stopWatchdog()
        
        watchdogRunnable = object : Runnable {
            override fun run() {
                val currentGatt = gatt
                val timeSinceLastOp = System.currentTimeMillis() - lastSuccessfulOperationTime
                
                Log.d(TAG, "🐕 Watchdog check: isConnected=$isConnected, gatt=${currentGatt != null}, timeSince=${timeSinceLastOp}ms")
                
                // Detect zombie state: should be connected but isn't
                if (currentGatt != null && !isConnected) {
                    Log.e(TAG, "🐕 ZOMBIE STATE DETECTED: GATT exists but isConnected=false")
                    Log.e(TAG, "🐕 Forcing cleanup and reconnection...")
                    cleanup()
                    onDisconnect(device, -1)
                }
                // Detect stale connection: connected but no operations for 5 minutes
                else if (isConnected && isPollingActive && timeSinceLastOp > 300000) {
                    Log.e(TAG, "🐕 STALE CONNECTION DETECTED: No operations for ${timeSinceLastOp/1000}s")
                    Log.e(TAG, "🐕 Forcing reconnection...")
                    cleanup()
                    onDisconnect(device, -1)
                }
                
                mainHandler.postDelayed(this, WATCHDOG_INTERVAL_MS)
            }
        }
        
        mainHandler.postDelayed(watchdogRunnable!!, WATCHDOG_INTERVAL_MS)
        Log.i(TAG, "🐕 Connection watchdog started (every ${WATCHDOG_INTERVAL_MS/1000}s)")
    }
    
    private fun stopWatchdog() {
        watchdogRunnable?.let {
            mainHandler.removeCallbacks(it)
            watchdogRunnable = null
            Log.d(TAG, "🐕 Watchdog stopped")
        }
    }
}

/**
 * Data class to hold parsed GoPower state
 */
data class GoPowerState(
    val solarVoltage: Double,      // PV panel voltage (V)
    val solarCurrent: Double,      // PV panel current (A)
    val solarPower: Double,        // Calculated PV power (W)
    val batteryVoltage: Double,    // Battery voltage (V)
    val stateOfCharge: Int,        // Battery SOC (%)
    val temperatureC: Int,         // Temperature (°C)
    val temperatureF: Int,         // Temperature (°F)
    val ampHours: Int,             // Energy in Wh (converted from Ah × voltage)
    val ampHoursAux: Int           // Amp-hours Battery 2 (not used)
)

/**
 * Helper data class for sensor configuration
 */
private data class SensorConfig(
    val objectId: String,
    val name: String,
    val unit: String,
    val deviceClass: String?,
    val stateClass: String?,
    val icon: String,
    val stateTopic: String
)
