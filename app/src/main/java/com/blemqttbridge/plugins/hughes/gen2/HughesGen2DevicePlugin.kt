package com.blemqttbridge.plugins.hughes.gen2

import android.bluetooth.*
import android.bluetooth.le.ScanRecord
import android.content.Context
import android.os.Handler
import android.os.Looper
import android.util.Log
import com.blemqttbridge.core.interfaces.BleDevicePlugin
import com.blemqttbridge.core.interfaces.MqttPublisher
import com.blemqttbridge.core.interfaces.PluginConfig
import org.json.JSONArray
import org.json.JSONObject

/**
 * Hughes Power Watchdog Gen2 BLE Device Plugin
 *
 * Supports: Gen2 devices (E5-E9, V5-V9) identified by BLE name "WD_{type}_{serial}"
 *
 * Protocol: Framed binary packets over BLE characteristic 0xFF01.
 * After connection, the device must be put into binary mode by writing
 * the ASCII command "!%!%,protocol,open," to the characteristic.
 *
 * Key differences from Gen1 (legacy) plugin:
 * - Different BLE UUIDs (service 0x00FF, char 0xFF01 instead of 0xFFE0/FFE2/FFF5)
 * - Framed binary protocol with magic header, not raw Modbus-like frames
 * - 34-byte DLData (not 40-byte raw frames)
 * - Additional sensors: output voltage, boost, temperature, relay status
 * - Binary commands (framed packets, not ASCII strings)
 * - Relay on/off control (not just relay-on for EPO recovery)
 */
class HughesGen2DevicePlugin : BleDevicePlugin {

    companion object {
        private const val TAG = "HughesGen2Plugin"
        const val PLUGIN_ID = "hughes_gen2"
        const val PLUGIN_VERSION = "1.0.0"
    }

    override val pluginId: String = PLUGIN_ID
    override var instanceId: String = PLUGIN_ID
    override val supportsMultipleInstances: Boolean = false
    override val displayName: String = "Hughes Power Watchdog (Gen2)"

    private var context: Context? = null
    private var config: PluginConfig? = null

    // Configuration
    private var watchdogMac: String = ""
    private var expectedName: String? = null

    // Strong reference to callback to prevent GC
    private var gattCallback: BluetoothGattCallback? = null
    private var currentCallback: HughesGen2GattCallback? = null

    override fun initialize(context: Context?, config: PluginConfig) {
        Log.i(TAG, "Initializing Hughes Gen2 Plugin v$PLUGIN_VERSION")
        this.context = context
        this.config = config

        watchdogMac = config.getString("watchdog_gen2_mac", watchdogMac)
        expectedName = config.getString("expected_name", "").takeIf { it.isNotEmpty() }

        Log.i(TAG, "Configured for Gen2 Watchdog MAC: $watchdogMac (name: $expectedName)")
    }

    override fun matchesDevice(device: BluetoothDevice, scanRecord: ScanRecord?): Boolean {
        if (watchdogMac.isBlank()) return false

        val deviceAddress = device.address
        if (!deviceAddress.equals(watchdogMac, ignoreCase = true)) return false

        // Optional: verify device name matches Gen2 pattern
        if (expectedName != null) {
            val deviceName = device.name
            if (deviceName != expectedName) {
                Log.w(TAG, "Device $deviceAddress matched MAC but name mismatch (expected: $expectedName, got: $deviceName)")
                return false
            }
        }

        Log.d(TAG, "Gen2 device matched by configured MAC: $deviceAddress")
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
        Log.i(TAG, "Creating Gen2 GATT callback for ${device.address}")
        val callback = HughesGen2GattCallback(device, context, mqttPublisher, instanceId, onDisconnect)
        gattCallback = callback
        currentCallback = callback
        return callback
    }

    override fun onGattConnected(device: BluetoothDevice, gatt: BluetoothGatt) {
        Log.i(TAG, "GATT connected for ${device.address}")
        currentCallback?.onGattReady(gatt)
    }

    override fun onDeviceDisconnected(device: BluetoothDevice) {
        Log.i(TAG, "Device disconnected: ${device.address}")
        currentCallback = null
    }

    override fun getMqttBaseTopic(device: BluetoothDevice): String {
        return "hughes/${device.address}"
    }

    override fun getDiscoveryPayloads(device: BluetoothDevice): List<Pair<String, String>> {
        return emptyList() // Discovery handled by callback
    }

    override suspend fun handleCommand(
        device: BluetoothDevice,
        commandTopic: String,
        payload: String
    ): Result<Unit> {
        val callback = currentCallback
            ?: return Result.failure(Exception("Device not connected"))

        return callback.handleCommand(commandTopic, payload)
    }

    override fun destroy() {
        Log.i(TAG, "Destroying Hughes Gen2 Plugin")
        currentCallback = null
        gattCallback = null
    }
}

/**
 * GATT Callback for Hughes Power Watchdog Gen2 devices.
 *
 * Handles:
 * - BLE connection and service discovery
 * - Protocol initialization (MTU + "!%!%,protocol,open,")
 * - Packet framing and reassembly via HughesGen2PacketFramer
 * - DLReport telemetry parsing for all sensor fields
 * - Error report parsing
 * - Relay on/off, energy reset, backlight, time sync commands
 * - MQTT publishing and Home Assistant discovery
 */
class HughesGen2GattCallback(
    private val device: BluetoothDevice,
    private val context: Context,
    private val mqttPublisher: MqttPublisher,
    private val instanceId: String,
    private val onDisconnect: (BluetoothDevice, Int) -> Unit
) : BluetoothGattCallback() {

    companion object {
        private const val TAG = "HughesGen2Gatt"
    }

    private var gatt: BluetoothGatt? = null
    private var rwChar: BluetoothGattCharacteristic? = null

    private val mainHandler = Handler(Looper.getMainLooper())
    private val packetFramer = HughesGen2PacketFramer()

    private var discoveryPublished = false
    private var protocolOpened = false
    private var isConnected = false

    // Device info parsed from name
    private var deviceType: String? = null
    private var isEnhanced = false

    // Current telemetry state (line 1 and optional line 2)
    private var line1: HughesGen2PacketFramer.DLData? = null
    private var line2: HughesGen2PacketFramer.DLData? = null
    private var isDualLine = false

    private val baseTopic: String
        get() = "hughes_gen2_${device.address.replace(":", "").lowercase()}"

    /**
     * Called by the plugin after connectGatt returns successfully.
     */
    fun onGattReady(gatt: BluetoothGatt) {
        this.gatt = gatt
    }

    // ===== LIFECYCLE =====

    override fun onConnectionStateChange(gatt: BluetoothGatt, status: Int, newState: Int) {
        Log.i(TAG, "Connection state: $newState (status: $status)")

        when (status) {
            BluetoothGatt.GATT_SUCCESS -> {
                when (newState) {
                    BluetoothProfile.STATE_CONNECTED -> {
                        Log.i(TAG, "Connected to Gen2 device ${device.address}")
                        this.gatt = gatt
                        isConnected = true

                        // Parse device type from name
                        deviceType = HughesGen2Constants.parseDeviceType(device.name)
                        isEnhanced = HughesGen2Constants.isEnhancedType(deviceType)
                        Log.i(TAG, "Device type: $deviceType (enhanced: $isEnhanced)")

                        publishAvailability(true)

                        // Request MTU before service discovery
                        Log.i(TAG, "Requesting MTU ${HughesGen2Constants.REQUESTED_MTU}")
                        gatt.requestMtu(HughesGen2Constants.REQUESTED_MTU)
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

    override fun onMtuChanged(gatt: BluetoothGatt, mtu: Int, status: Int) {
        Log.i(TAG, "MTU changed to $mtu (status: $status)")

        // Proceed with service discovery regardless of MTU result
        mainHandler.postDelayed({
            gatt.discoverServices()
        }, HughesGen2Constants.SERVICE_DISCOVERY_DELAY_MS)
    }

    override fun onServicesDiscovered(gatt: BluetoothGatt, status: Int) {
        Log.i(TAG, "Services discovered: status=$status, count=${gatt.services.size}")
        if (status != BluetoothGatt.GATT_SUCCESS) {
            Log.e(TAG, "Service discovery failed: $status")
            return
        }

        // Find Gen2 service
        val service = gatt.getService(HughesGen2Constants.SERVICE_UUID)
        if (service == null) {
            Log.e(TAG, "Gen2 service (000000FF) not found! Available services: ${gatt.services.map { it.uuid }}")
            return
        }

        // Get the single R/W/Notify characteristic
        rwChar = service.getCharacteristic(HughesGen2Constants.RW_CHARACTERISTIC_UUID)
        if (rwChar == null) {
            Log.e(TAG, "R/W characteristic (0000FF01) not found!")
            return
        }

        Log.i(TAG, "Characteristic found, enabling notifications...")

        mainHandler.postDelayed({
            enableNotifications()
        }, HughesGen2Constants.OPERATION_DELAY_MS)
    }

    private fun enableNotifications() {
        val char = rwChar ?: return
        val g = gatt ?: return

        if (!g.setCharacteristicNotification(char, true)) {
            Log.e(TAG, "Failed to set characteristic notification")
            return
        }

        val descriptor = char.getDescriptor(HughesGen2Constants.CCCD_UUID)
        if (descriptor != null) {
            descriptor.value = BluetoothGattDescriptor.ENABLE_NOTIFICATION_VALUE
            if (!g.writeDescriptor(descriptor)) {
                Log.e(TAG, "Failed to write CCCD descriptor")
            } else {
                Log.i(TAG, "CCCD write initiated")
            }
        } else {
            Log.w(TAG, "CCCD descriptor not found, notifications may still work")
            // Some devices work without explicit CCCD write
            sendProtocolOpen()
        }
    }

    override fun onDescriptorWrite(gatt: BluetoothGatt, descriptor: BluetoothGattDescriptor, status: Int) {
        if (descriptor.uuid == HughesGen2Constants.CCCD_UUID) {
            if (status == BluetoothGatt.GATT_SUCCESS) {
                Log.i(TAG, "Notifications enabled successfully")
                // Now send the protocol open command to enter binary mode
                sendProtocolOpen()
            } else {
                Log.e(TAG, "Failed to enable notifications: $status")
            }
        }
    }

    /**
     * Send the ASCII protocol open command to switch the device into binary mode.
     */
    private fun sendProtocolOpen() {
        if (protocolOpened) return

        mainHandler.postDelayed({
            val char = rwChar ?: return@postDelayed
            val g = gatt ?: return@postDelayed

            val cmd = HughesGen2Constants.PROTOCOL_OPEN_CMD.toByteArray(Charsets.US_ASCII)
            Log.i(TAG, "Sending protocol open command: ${HughesGen2Constants.PROTOCOL_OPEN_CMD}")

            char.value = cmd
            char.writeType = BluetoothGattCharacteristic.WRITE_TYPE_DEFAULT
            if (g.writeCharacteristic(char)) {
                Log.i(TAG, "Protocol open command sent (${cmd.size} bytes)")
                protocolOpened = true
            } else {
                Log.e(TAG, "Failed to send protocol open command")
            }
        }, HughesGen2Constants.PROTOCOL_OPEN_DELAY_MS)
    }

    // ===== DATA HANDLING =====

    // Android 13+ (API 33+)
    override fun onCharacteristicChanged(
        gatt: BluetoothGatt,
        characteristic: BluetoothGattCharacteristic,
        value: ByteArray
    ) {
        if (characteristic.uuid != HughesGen2Constants.RW_CHARACTERISTIC_UUID) return
        handleIncomingData(value)
    }

    // Older Android
    @Deprecated("Deprecated in API 33")
    override fun onCharacteristicChanged(gatt: BluetoothGatt, characteristic: BluetoothGattCharacteristic) {
        if (characteristic.uuid != HughesGen2Constants.RW_CHARACTERISTIC_UUID) return
        characteristic.value?.let { handleIncomingData(it) }
    }

    override fun onCharacteristicWrite(gatt: BluetoothGatt, characteristic: BluetoothGattCharacteristic, status: Int) {
        val statusStr = if (status == BluetoothGatt.GATT_SUCCESS) "SUCCESS" else "FAILED($status)"
        Log.d(TAG, "Characteristic write: $statusStr")
        mqttPublisher.logBleEvent("WRITE ${characteristic.uuid}: status=$status")
    }

    /**
     * Process raw bytes from BLE notification through the packet framer.
     */
    private fun handleIncomingData(data: ByteArray) {
        Log.d(TAG, "Received ${data.size} bytes: ${data.joinToString(" ") { String.format("%02X", it) }}")

        // Check if this is an ASCII response (e.g., "ok" from protocol open)
        if (!protocolOpened || data.size < 4) {
            val text = String(data, Charsets.US_ASCII).trim()
            if (text.contains("ok", ignoreCase = true)) {
                Log.i(TAG, "Received ASCII response: $text")
                return
            }
        }

        val packet = packetFramer.feedData(data) ?: return

        Log.d(TAG, "Parsed packet: cmd=0x${String.format("%02X", packet.cmd)}, bodyLen=${packet.dataLen}")

        when (packet.cmd) {
            HughesGen2Constants.CMD_DL_REPORT -> handleDLReport(packet.body)
            HughesGen2Constants.CMD_ERROR_REPORT -> handleErrorReport(packet.body)
            HughesGen2Constants.CMD_ALARM -> handleAlarm(packet.body)
            else -> Log.d(TAG, "Received response for cmd 0x${String.format("%02X", packet.cmd)}")
        }
    }

    private fun handleDLReport(body: ByteArray) {
        val readings = packetFramer.parseDLReport(body) ?: return

        isDualLine = readings.size > 1
        line1 = readings[0]
        if (readings.size > 1) {
            line2 = readings[1]
        }

        Log.d(TAG, "DLReport: V=${String.format("%.1f", line1!!.inputVoltage)}V, " +
            "A=${String.format("%.1f", line1!!.current)}A, " +
            "W=${String.format("%.0f", line1!!.power)}W, " +
            "E=${String.format("%.1f", line1!!.energy)}kWh, " +
            "F=${String.format("%.0f", line1!!.frequency)}Hz, " +
            "err=${line1!!.errorCode}, relay=${line1!!.relayStatus}" +
            if (isEnhanced) ", outV=${String.format("%.1f", line1!!.outputVoltage)}V, " +
                "boost=${line1!!.boost}, temp=${line1!!.temperature}°" else ""
        )

        publishMetrics()
        publishDiagnosticsState()

        if (!discoveryPublished) {
            publishDiscovery()
            discoveryPublished = true
        }

        mqttPublisher.updatePluginStatus(
            pluginId = instanceId,
            connected = true,
            authenticated = false,
            dataHealthy = true
        )
    }

    private fun handleErrorReport(body: ByteArray) {
        val records = packetFramer.parseErrorReport(body)
        Log.i(TAG, "Received ${records.size} error records")

        val jsonArray = JSONArray()
        for (record in records) {
            jsonArray.put(JSONObject().apply {
                put("error_code", record.errorCode)
                put("error", HughesGen2Constants.ERROR_LABELS[record.errorCode] ?: "Unknown")
                put("sub_code", record.subCode)
                put("start_time", record.startTime)
                put("end_time", record.endTime)
            })
        }
        mqttPublisher.publishState("$baseTopic/error_history", jsonArray.toString(), true)
    }

    private fun handleAlarm(body: ByteArray) {
        Log.w(TAG, "ALARM received: ${body.joinToString(" ") { String.format("%02X", it) }}")
        mqttPublisher.publishState("$baseTopic/alarm", "triggered", false)
    }

    // ===== COMMANDS =====

    /**
     * Handle MQTT command from Home Assistant.
     */
    fun handleCommand(commandTopic: String, payload: String): Result<Unit> {
        val g = gatt ?: return Result.failure(Exception("Not connected"))
        val char = rwChar ?: return Result.failure(Exception("Characteristic not found"))

        val cmd = when {
            commandTopic.endsWith("/relay/set") || commandTopic.endsWith("/switch/set") -> {
                val on = payload.equals("ON", ignoreCase = true) || payload == "1"
                Log.i(TAG, "Relay command: ${if (on) "ON" else "OFF"}")
                packetFramer.buildSetOpen(on)
            }
            commandTopic.endsWith("/energy_reset") -> {
                Log.i(TAG, "Energy reset command")
                packetFramer.buildEnergyReset()
            }
            commandTopic.endsWith("/backlight/set") -> {
                val level = payload.toIntOrNull() ?: return Result.failure(Exception("Invalid backlight level: $payload"))
                Log.i(TAG, "Backlight command: level=$level")
                packetFramer.buildSetBacklight(level)
            }
            commandTopic.endsWith("/time_sync") -> {
                Log.i(TAG, "Time sync command")
                packetFramer.buildSetTime()
            }
            commandTopic.endsWith("/neutral_detection/set") -> {
                val enable = payload.equals("ON", ignoreCase = true) || payload == "1"
                Log.i(TAG, "Neutral detection: ${if (enable) "enable" else "disable"}")
                packetFramer.buildNeutralDetection(enable)
            }
            else -> {
                Log.w(TAG, "Unknown command topic: $commandTopic")
                return Result.failure(Exception("Unknown command: $commandTopic"))
            }
        }

        return writeCommand(g, char, cmd)
    }

    private fun writeCommand(gatt: BluetoothGatt, char: BluetoothGattCharacteristic, data: ByteArray): Result<Unit> {
        char.value = data
        char.writeType = BluetoothGattCharacteristic.WRITE_TYPE_DEFAULT
        return if (gatt.writeCharacteristic(char)) {
            Log.d(TAG, "Command written: ${data.size} bytes")
            mqttPublisher.logBleEvent("WRITE cmd ${data.joinToString(" ") { String.format("%02X", it) }}")
            Result.success(Unit)
        } else {
            Log.e(TAG, "Failed to write command")
            Result.failure(Exception("BLE write failed"))
        }
    }

    // ===== MQTT PUBLISHING =====

    private fun publishMetrics() {
        val l1 = line1 ?: return

        // Line 1 metrics
        publishLineMetrics(l1, "_l1")

        // Line 2 metrics (if dual-line)
        if (isDualLine && line2 != null) {
            publishLineMetrics(line2!!, "_l2")
        }

        // Error status (shared across lines)
        val errorLabel = HughesGen2Constants.ERROR_LABELS[l1.errorCode] ?: "Unknown"
        mqttPublisher.publishState("$baseTopic/error", errorLabel, false)
        mqttPublisher.publishState("$baseTopic/error_code", l1.errorCode.toString(), false)

        // Relay status
        val relayState = if (l1.relayStatus == 1) "ON" else "OFF"
        mqttPublisher.publishState("$baseTopic/relay", relayState, false)
    }

    private fun publishLineMetrics(data: HughesGen2PacketFramer.DLData, suffix: String) {
        mqttPublisher.publishState("$baseTopic/volts$suffix", String.format("%.2f", data.inputVoltage), false)
        mqttPublisher.publishState("$baseTopic/amps$suffix", String.format("%.2f", data.current), false)
        mqttPublisher.publishState("$baseTopic/watts$suffix", String.format("%.2f", data.power), false)
        mqttPublisher.publishState("$baseTopic/energy$suffix", String.format("%.2f", data.energy), false)
        mqttPublisher.publishState("$baseTopic/frequency$suffix", String.format("%.2f", data.frequency), false)

        // Enhanced fields (E8/V8+)
        if (isEnhanced) {
            mqttPublisher.publishState("$baseTopic/output_volts$suffix", String.format("%.2f", data.outputVoltage), false)
            mqttPublisher.publishState("$baseTopic/boost$suffix", if (data.boost) "ON" else "OFF", false)
            mqttPublisher.publishState("$baseTopic/temperature$suffix", data.temperature.toString(), false)
        }

        // Combined state JSON
        val stateJson = JSONObject().apply {
            put("voltage", String.format("%.2f", data.inputVoltage))
            put("current", String.format("%.2f", data.current))
            put("power", String.format("%.2f", data.power))
            put("energy", String.format("%.2f", data.energy))
            put("frequency", String.format("%.2f", data.frequency))
            put("error_code", data.errorCode)
            put("error", HughesGen2Constants.ERROR_LABELS[data.errorCode] ?: "Unknown")
            put("relay", if (data.relayStatus == 1) "ON" else "OFF")
            if (isEnhanced) {
                put("output_voltage", String.format("%.2f", data.outputVoltage))
                put("boost", data.boost)
                put("temperature", data.temperature)
            }
            put("timestamp", System.currentTimeMillis())
        }
        mqttPublisher.publishState("$baseTopic/state$suffix", stateJson.toString(), false)
    }

    private fun publishAvailability(online: Boolean) {
        mqttPublisher.publishAvailability("$baseTopic/availability", online)
    }

    private fun publishDiagnosticsState() {
        mqttPublisher.publishState("$baseTopic/diag/data_healthy", "ON", true)
    }

    // ===== HA DISCOVERY =====

    private fun publishDiscovery() {
        Log.i(TAG, "Publishing HA discovery for $baseTopic")

        val macId = device.address.replace(":", "").lowercase()
        val deviceId = "hughes_gen2_$macId"
        val typeLabel = deviceType ?: "Gen2"
        val deviceName = "Hughes Watchdog $typeLabel ${device.address}"

        val appVersion = try {
            context.packageManager?.getPackageInfo(context.packageName ?: "", 0)?.versionName ?: "unknown"
        } catch (e: Exception) { "unknown" }

        val deviceInfo = JSONObject().apply {
            put("identifiers", JSONArray().put(deviceId))
            put("name", deviceName)
            put("model", "Power Watchdog $typeLabel")
            put("manufacturer", "Hughes")
            put("sw_version", appVersion)
            put("connections", JSONArray().put(JSONArray().put("mac").put(device.address)))
        }

        val prefix = mqttPublisher.topicPrefix

        // Line 1 sensors
        publishSensorDiscovery("volts_l1", "L1 Voltage", "V", "voltage", "measurement", deviceInfo, prefix)
        publishSensorDiscovery("amps_l1", "L1 Current", "A", "current", "measurement", deviceInfo, prefix)
        publishSensorDiscovery("watts_l1", "L1 Power", "W", "power", "measurement", deviceInfo, prefix)
        publishSensorDiscovery("energy_l1", "Energy", "kWh", "energy", "total_increasing", deviceInfo, prefix)
        publishSensorDiscovery("frequency_l1", "L1 Frequency", "Hz", "frequency", "measurement", deviceInfo, prefix)

        // Line 2 sensors (always publish; HA will show unavailable if single-line)
        publishSensorDiscovery("volts_l2", "L2 Voltage", "V", "voltage", "measurement", deviceInfo, prefix)
        publishSensorDiscovery("amps_l2", "L2 Current", "A", "current", "measurement", deviceInfo, prefix)
        publishSensorDiscovery("watts_l2", "L2 Power", "W", "power", "measurement", deviceInfo, prefix)
        publishSensorDiscovery("energy_l2", "L2 Energy", "kWh", "energy", "total_increasing", deviceInfo, prefix)
        publishSensorDiscovery("frequency_l2", "L2 Frequency", "Hz", "frequency", "measurement", deviceInfo, prefix)

        // Enhanced sensors (E8/V8+)
        if (isEnhanced) {
            publishSensorDiscovery("output_volts_l1", "L1 Output Voltage", "V", "voltage", "measurement", deviceInfo, prefix)
            publishSensorDiscovery("temperature_l1", "Temperature", "°F", "temperature", "measurement", deviceInfo, prefix)

            // Boost binary sensor
            publishBinarySensorDiscovery("boost_l1", "Boost Active", deviceInfo, prefix)
        }

        // Error sensor
        publishSensorDiscovery("error", "Error Status", null, null, null, deviceInfo, prefix)
        publishSensorDiscovery("error_code", "Error Code", null, null, null, deviceInfo, prefix)

        // Relay switch
        publishSwitchDiscovery("relay", "Relay", deviceInfo, prefix)

        // Diagnostic health binary sensor
        publishDiagnosticDiscovery(deviceInfo, prefix, macId)

        publishAvailability(true)
    }

    private fun publishSensorDiscovery(
        field: String, name: String, unit: String?, deviceClass: String?,
        stateClass: String?, deviceInfo: JSONObject, prefix: String
    ) {
        val discoveryTopic = "$prefix/sensor/${instanceId}_$field/config"
        val payload = JSONObject().apply {
            put("name", name)
            put("state_topic", "$prefix/$baseTopic/$field")
            if (unit != null) put("unit_of_measurement", unit)
            if (deviceClass != null) put("device_class", deviceClass)
            if (stateClass != null) put("state_class", stateClass)
            put("unique_id", "${instanceId}_$field")
            put("device", deviceInfo)
            put("availability_topic", "$prefix/$baseTopic/availability")
        }
        mqttPublisher.publishDiscovery(discoveryTopic, payload.toString())
    }

    private fun publishBinarySensorDiscovery(
        field: String, name: String, deviceInfo: JSONObject, prefix: String
    ) {
        val discoveryTopic = "$prefix/binary_sensor/${instanceId}_$field/config"
        val payload = JSONObject().apply {
            put("name", name)
            put("state_topic", "$prefix/$baseTopic/$field")
            put("payload_on", "ON")
            put("payload_off", "OFF")
            put("unique_id", "${instanceId}_$field")
            put("device", deviceInfo)
            put("availability_topic", "$prefix/$baseTopic/availability")
        }
        mqttPublisher.publishDiscovery(discoveryTopic, payload.toString())
    }

    private fun publishSwitchDiscovery(
        field: String, name: String, deviceInfo: JSONObject, prefix: String
    ) {
        val discoveryTopic = "$prefix/switch/${instanceId}_$field/config"
        val payload = JSONObject().apply {
            put("name", name)
            put("state_topic", "$prefix/$baseTopic/$field")
            put("command_topic", "$prefix/$baseTopic/$field/set")
            put("payload_on", "ON")
            put("payload_off", "OFF")
            put("unique_id", "${instanceId}_$field")
            put("device", deviceInfo)
            put("availability_topic", "$prefix/$baseTopic/availability")
        }
        mqttPublisher.publishDiscovery(discoveryTopic, payload.toString())
    }

    private fun publishDiagnosticDiscovery(deviceInfo: JSONObject, prefix: String, macId: String) {
        val nodeId = "${instanceId}_data_healthy"
        val uniqueId = "hughes_gen2_${macId}_diag_data_healthy"
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
        }
        mqttPublisher.publishDiscovery(discoveryTopic, payload.toString())
    }

    // ===== CLEANUP =====

    private fun cleanup() {
        isConnected = false
        protocolOpened = false
        packetFramer.clear()
        mainHandler.removeCallbacksAndMessages(null)
        publishAvailability(false)
        mqttPublisher.publishState("$baseTopic/diag/data_healthy", "OFF", true)
    }
}
