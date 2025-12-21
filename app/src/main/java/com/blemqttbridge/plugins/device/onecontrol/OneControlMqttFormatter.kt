package com.blemqttbridge.plugins.device.onecontrol

import android.util.Log
import com.blemqttbridge.core.interfaces.OutputPluginInterface
import org.json.JSONArray
import org.json.JSONObject

/**
 * MQTT formatter for OneControl device state updates.
 * 
 * Converts protocol-level state updates to MQTT topics and payloads,
 * including Home Assistant discovery configuration.
 * 
 * Based on working implementation from legacy android_ble_bridge app.
 */
class OneControlMqttFormatter(
    private val output: OutputPluginInterface,
    private val gatewayMac: String,
    private val topicPrefix: String = "onecontrol/ble"
) : OneControlStateListener {
    
    companion object {
        private const val TAG = "OneControlMqttFormatter"
        private const val DEVICE_ID_BASE = "onecontrol_ble"
    }
    
    // Track which devices have had discovery published
    private val discoveryPublished = mutableSetOf<Int>()
    
    // Track last known brightness for dimmers (for ON command without brightness)
    private val lastKnownBrightness = mutableMapOf<Int, Int>()
    
    override suspend fun onStateUpdate(gatewayMac: String, update: OneControlStateUpdate) {
        when (update) {
            is OneControlStateUpdate.DimmableLight -> publishDimmableLightState(update)
            is OneControlStateUpdate.Switch -> publishSwitchState(update)
            is OneControlStateUpdate.Cover -> publishCoverState(update)
            is OneControlStateUpdate.Tank -> publishTankState(update)
            is OneControlStateUpdate.Hvac -> publishHvacState(update)
            is OneControlStateUpdate.SystemStatus -> publishSystemStatus(update)
            is OneControlStateUpdate.GatewayInfo -> publishGatewayInfo(update)
            is OneControlStateUpdate.DeviceOnline -> publishDeviceOnline(update)
        }
    }
    
    override suspend fun onDeviceDiscovered(gatewayMac: String, update: OneControlStateUpdate) {
        if (discoveryPublished.contains(update.deviceAddress)) return
        
        when (update) {
            is OneControlStateUpdate.DimmableLight -> publishDimmableLightDiscovery(update)
            is OneControlStateUpdate.Switch -> publishSwitchDiscovery(update)
            is OneControlStateUpdate.Cover -> publishCoverDiscovery(update)
            is OneControlStateUpdate.Tank -> publishTankDiscovery(update)
            is OneControlStateUpdate.Hvac -> publishHvacDiscovery(update)
            is OneControlStateUpdate.SystemStatus -> publishSystemDiscovery()
            else -> { /* No discovery for other types */ }
        }
        
        discoveryPublished.add(update.deviceAddress)
    }
    
    // ========================================================================
    // State Publishing
    // ========================================================================
    
    private suspend fun publishDimmableLightState(update: OneControlStateUpdate.DimmableLight) {
        val baseTopic = "$topicPrefix/device/${update.tableId}/${update.deviceId}"
        
        // Classic MQTT light schema: state + brightness topics (matching legacy app)
        output.publishState("$baseTopic/state", if (update.isOn) "ON" else "OFF", retained = true)
        output.publishState("$baseTopic/brightness", update.brightnessRaw.toString(), retained = true)
        output.publishState("$baseTopic/type", "dimmable_light", retained = false)
        
        // Track last known brightness for dimmer recall
        if (update.brightnessRaw > 0) {
            lastKnownBrightness[update.deviceAddress] = update.brightnessRaw
        }
        
        Log.d(TAG, "💡 Published dimmable light ${update.tableId}:${update.deviceId} - ${if (update.isOn) "ON" else "OFF"} @ ${update.brightnessPct}%")
    }
    
    private suspend fun publishSwitchState(update: OneControlStateUpdate.Switch) {
        val baseTopic = "$topicPrefix/device/${update.tableId}/${update.deviceId}"
        
        output.publishState("$baseTopic/state", if (update.isOn) "ON" else "OFF", retained = true)
        output.publishState("$baseTopic/type", "relay", retained = false)
        
        Log.d(TAG, "⚡ Published switch ${update.tableId}:${update.deviceId} - ${if (update.isOn) "ON" else "OFF"}")
    }
    
    private suspend fun publishCoverState(update: OneControlStateUpdate.Cover) {
        val baseTopic = "$topicPrefix/device/${update.tableId}/${update.deviceId}"
        
        // Map status to HA cover state
        val haState = when (update.status) {
            0xC2 -> "opening"   // Extending
            0xC3 -> "closing"   // Retracting
            0xC0 -> {           // Stopped - infer from last direction
                when (update.lastDirection) {
                    0xC2 -> "open"
                    0xC3 -> "closed"
                    else -> "stopped"
                }
            }
            else -> "unknown"
        }
        
        output.publishState("$baseTopic/state", haState, retained = true)
        update.position?.let { 
            output.publishState("$baseTopic/position", it.toString(), retained = true) 
        }
        output.publishState("$baseTopic/type", "cover", retained = false)
        
        Log.d(TAG, "🪟 Published cover ${update.tableId}:${update.deviceId} - $haState")
    }
    
    private suspend fun publishTankState(update: OneControlStateUpdate.Tank) {
        val baseTopic = "$topicPrefix/device/${update.tableId}/${update.deviceId}"
        
        output.publishState("$baseTopic/level", update.level.toString(), retained = true)
        output.publishState("$baseTopic/type", "tank", retained = false)
        update.fluidType?.let {
            output.publishState("$baseTopic/fluid_type", it, retained = true)
        }
        
        Log.d(TAG, "🛢️ Published tank ${update.tableId}:${update.deviceId} - ${update.level}%")
    }
    
    private suspend fun publishHvacState(update: OneControlStateUpdate.Hvac) {
        val baseTopic = "$topicPrefix/device/${update.tableId}/${update.deviceId}"
        
        // Map heat mode to HA mode
        val haMode = when (update.heatMode) {
            0 -> "off"
            1 -> "heat"
            2 -> "cool"
            3 -> "heat_cool"
            else -> "off"
        }
        
        // Map fan mode
        val haFanMode = when (update.fanMode) {
            0 -> "auto"
            1 -> "high"
            2 -> "low"
            else -> "auto"
        }
        
        // Map zone status to action
        val haAction = when (update.zoneMode) {
            0 -> "off"
            1 -> "idle"
            2 -> "cooling"
            3, 4, 5, 6 -> "heating"  // Various heat modes
            else -> "idle"
        }
        
        // Publish JSON state (matching HA climate entity expectations)
        val stateJson = JSONObject().apply {
            put("mode", haMode)
            put("action", haAction)
            put("fan_mode", haFanMode)
            put("heat_setpoint", update.heatSetpointF)
            put("cool_setpoint", update.coolSetpointF)
            update.indoorTempF?.let { put("current_temperature", it) }
            update.outdoorTempF?.let { put("outdoor_temperature", it) }
        }
        
        output.publishState("$baseTopic/state", stateJson.toString(), retained = true)
        output.publishState("$baseTopic/type", "hvac", retained = false)
        
        Log.d(TAG, "🌡️ Published HVAC ${update.tableId}:${update.deviceId} - mode=$haMode, action=$haAction")
    }
    
    private suspend fun publishSystemStatus(update: OneControlStateUpdate.SystemStatus) {
        update.batteryVoltage?.let {
            output.publishState("$topicPrefix/system/voltage", String.format("%.2f", it), retained = true)
        }
        update.externalTempC?.let {
            output.publishState("$topicPrefix/system/temperature", String.format("%.1f", it), retained = true)
        }
    }
    
    private suspend fun publishGatewayInfo(update: OneControlStateUpdate.GatewayInfo) {
        output.publishState("$topicPrefix/gateway/protocol_version", update.protocolVersion.toString(), retained = true)
        output.publishState("$topicPrefix/gateway/device_count", update.deviceCount.toString(), retained = true)
        output.publishState("$topicPrefix/gateway/device_table_id", "0x${update.deviceTableId.toString(16)}", retained = true)
    }
    
    private suspend fun publishDeviceOnline(update: OneControlStateUpdate.DeviceOnline) {
        val baseTopic = "$topicPrefix/device/${update.tableId}/${update.deviceId}"
        output.publishState("$baseTopic/online", if (update.isOnline) "true" else "false", retained = true)
    }
    
    // ========================================================================
    // Home Assistant Discovery
    // ========================================================================
    
    private fun getDeviceInfo(): JSONObject {
        val cleanMac = gatewayMac.replace(":", "").lowercase()
        return JSONObject().apply {
            put("identifiers", JSONArray().put("${DEVICE_ID_BASE}_$cleanMac"))
            put("name", "OneControl BLE Gateway")
            put("model", "OneControl Gateway")
            put("manufacturer", "Lippert Components")
            put("sw_version", "BLE Bridge v1.0")
            put("connections", JSONArray().put(JSONArray().put("mac").put(gatewayMac)))
        }
    }
    
    /**
     * Get the availability topic for HA integration.
     */
    fun getAvailabilityTopic(): String = "$topicPrefix/availability"
    
    /**
     * Create availability configuration for HA discovery payloads.
     * When included, HA will show entities as unavailable when availability is 'offline'.
     */
    private fun getAvailabilityConfig(): JSONObject {
        return JSONObject().apply {
            put("topic", "$topicPrefix/availability")
            put("payload_available", "online")
            put("payload_not_available", "offline")
        }
    }
    
    /**
     * Publish online availability and return the pattern for command subscriptions.
     * Call this after successfully connecting to the gateway.
     */
    suspend fun publishOnline() {
        output.publishAvailability(true)
        Log.i(TAG, "📡 Published availability: online")
    }
    
    private fun sanitizeName(name: String): String {
        return name.lowercase().replace(Regex("[^a-z0-9]+"), "_").trim('_')
    }
    
    private suspend fun publishDimmableLightDiscovery(update: OneControlStateUpdate.DimmableLight) {
        val cleanMac = gatewayMac.replace(":", "").lowercase()
        val objectId = "light_${update.tableId.toString(16).padStart(2, '0')}_${update.deviceId.toString(16).padStart(2, '0')}"
        val uniqueId = "${DEVICE_ID_BASE}_${cleanMac}_$objectId"
        val topic = "homeassistant/light/$uniqueId/config"
        
        val baseTopic = "$topicPrefix/device/${update.tableId}/${update.deviceId}"
        val commandTopic = "$topicPrefix/command/dimmable/${update.tableId}/${update.deviceId}"
        
        val payload = JSONObject().apply {
            put("unique_id", uniqueId)
            put("name", "Light ${update.tableId}:${update.deviceId}")
            put("device", getDeviceInfo())
            put("availability", getAvailabilityConfig())
            
            // Classic MQTT light schema: state + brightness topics
            put("state_topic", "$baseTopic/state")
            put("command_topic", commandTopic)
            put("brightness_state_topic", "$baseTopic/brightness")
            put("brightness_command_topic", "$commandTopic/brightness")
            put("payload_on", "ON")
            put("payload_off", "OFF")
            put("on_command_type", "brightness")
            put("brightness_scale", 255)
            put("optimistic", false)
            put("icon", "mdi:lightbulb")
        }
        
        output.publishDiscovery(topic, payload.toString())
        Log.i(TAG, "📡 Published discovery for dimmable light ${update.tableId}:${update.deviceId}")
    }
    
    private suspend fun publishSwitchDiscovery(update: OneControlStateUpdate.Switch) {
        val cleanMac = gatewayMac.replace(":", "").lowercase()
        val objectId = "switch_${update.tableId.toString(16).padStart(2, '0')}_${update.deviceId.toString(16).padStart(2, '0')}"
        val uniqueId = "${DEVICE_ID_BASE}_${cleanMac}_$objectId"
        val topic = "homeassistant/switch/$uniqueId/config"
        
        val baseTopic = "$topicPrefix/device/${update.tableId}/${update.deviceId}"
        val commandTopic = "$topicPrefix/command/switch/${update.tableId}/${update.deviceId}"
        
        val payload = JSONObject().apply {
            put("unique_id", uniqueId)
            put("name", "Switch ${update.tableId}:${update.deviceId}")
            put("device", getDeviceInfo())
            put("availability", getAvailabilityConfig())
            put("state_topic", "$baseTopic/state")
            put("command_topic", commandTopic)
            put("payload_on", "ON")
            put("payload_off", "OFF")
            put("optimistic", false)
            put("icon", "mdi:electric-switch")
        }
        
        output.publishDiscovery(topic, payload.toString())
        Log.i(TAG, "📡 Published discovery for switch ${update.tableId}:${update.deviceId}")
    }
    
    private suspend fun publishCoverDiscovery(update: OneControlStateUpdate.Cover) {
        val cleanMac = gatewayMac.replace(":", "").lowercase()
        val objectId = "cover_${update.tableId.toString(16).padStart(2, '0')}_${update.deviceId.toString(16).padStart(2, '0')}"
        val uniqueId = "${DEVICE_ID_BASE}_${cleanMac}_$objectId"
        val topic = "homeassistant/cover/$uniqueId/config"
        
        val baseTopic = "$topicPrefix/device/${update.tableId}/${update.deviceId}"
        val commandTopic = "$topicPrefix/command/cover/${update.tableId}/${update.deviceId}"
        
        val payload = JSONObject().apply {
            put("unique_id", uniqueId)
            put("name", "Cover ${update.tableId}:${update.deviceId}")
            put("device", getDeviceInfo())
            put("availability", getAvailabilityConfig())
            put("state_topic", "$baseTopic/state")
            put("command_topic", commandTopic)
            put("position_topic", "$baseTopic/position")
            put("set_position_topic", "$commandTopic/position")
            put("payload_open", "OPEN")
            put("payload_close", "CLOSE")
            put("payload_stop", "STOP")
            put("icon", "mdi:window-shutter")
        }
        
        output.publishDiscovery(topic, payload.toString())
        Log.i(TAG, "📡 Published discovery for cover ${update.tableId}:${update.deviceId}")
    }
    
    private suspend fun publishTankDiscovery(update: OneControlStateUpdate.Tank) {
        val cleanMac = gatewayMac.replace(":", "").lowercase()
        val objectId = "tank_${update.tableId.toString(16).padStart(2, '0')}_${update.deviceId.toString(16).padStart(2, '0')}"
        val uniqueId = "${DEVICE_ID_BASE}_${cleanMac}_$objectId"
        val topic = "homeassistant/sensor/$uniqueId/config"
        
        val baseTopic = "$topicPrefix/device/${update.tableId}/${update.deviceId}"
        
        val payload = JSONObject().apply {
            put("unique_id", uniqueId)
            put("name", "Tank ${update.tableId}:${update.deviceId}")
            put("device", getDeviceInfo())
            put("availability", getAvailabilityConfig())
            put("state_topic", "$baseTopic/level")
            put("unit_of_measurement", "%")
            put("state_class", "measurement")
            put("icon", "mdi:gauge")
        }
        
        output.publishDiscovery(topic, payload.toString())
        Log.i(TAG, "📡 Published discovery for tank ${update.tableId}:${update.deviceId}")
    }
    
    private suspend fun publishHvacDiscovery(update: OneControlStateUpdate.Hvac) {
        val cleanMac = gatewayMac.replace(":", "").lowercase()
        val objectId = "hvac_${update.tableId.toString(16).padStart(2, '0')}_${update.deviceId.toString(16).padStart(2, '0')}"
        val uniqueId = "${DEVICE_ID_BASE}_${cleanMac}_$objectId"
        val topic = "homeassistant/climate/$uniqueId/config"
        
        val baseTopic = "$topicPrefix/device/${update.tableId}/${update.deviceId}"
        val commandTopic = "$topicPrefix/command/hvac/${update.tableId}/${update.deviceId}"
        
        val payload = JSONObject().apply {
            put("unique_id", uniqueId)
            put("name", "HVAC Zone ${update.deviceId}")
            put("device", getDeviceInfo())
            put("availability", getAvailabilityConfig())
            
            // State topics
            put("mode_state_topic", "$baseTopic/state")
            put("mode_state_template", "{{ value_json.mode }}")
            put("action_topic", "$baseTopic/state")
            put("action_template", "{{ value_json.action }}")
            put("current_temperature_topic", "$baseTopic/state")
            put("current_temperature_template", "{{ value_json.current_temperature }}")
            put("temperature_low_state_topic", "$baseTopic/state")
            put("temperature_low_state_template", "{{ value_json.heat_setpoint }}")
            put("temperature_high_state_topic", "$baseTopic/state")
            put("temperature_high_state_template", "{{ value_json.cool_setpoint }}")
            put("fan_mode_state_topic", "$baseTopic/state")
            put("fan_mode_state_template", "{{ value_json.fan_mode }}")
            
            // Command topics
            put("mode_command_topic", commandTopic)
            put("temperature_low_command_topic", "$commandTopic/heat_setpoint")
            put("temperature_high_command_topic", "$commandTopic/cool_setpoint")
            put("fan_mode_command_topic", "$commandTopic/fan_mode")
            
            // Modes
            put("modes", JSONArray().put("off").put("heat").put("cool").put("heat_cool"))
            put("fan_modes", JSONArray().put("auto").put("high").put("low"))
            
            // Temperature settings
            put("min_temp", 50)
            put("max_temp", 90)
            put("temp_step", 1)
            put("temperature_unit", "F")
        }
        
        output.publishDiscovery(topic, payload.toString())
        Log.i(TAG, "📡 Published discovery for HVAC zone ${update.tableId}:${update.deviceId}")
    }
    
    private suspend fun publishSystemDiscovery() {
        val cleanMac = gatewayMac.replace(":", "").lowercase()
        
        // Voltage sensor
        val voltageId = "${DEVICE_ID_BASE}_${cleanMac}_system_voltage"
        val voltageTopic = "homeassistant/sensor/$voltageId/config"
        val voltagePayload = JSONObject().apply {
            put("unique_id", voltageId)
            put("name", "RV Battery Voltage")
            put("device", getDeviceInfo())
            put("availability", getAvailabilityConfig())
            put("state_topic", "$topicPrefix/system/voltage")
            put("unit_of_measurement", "V")
            put("device_class", "voltage")
            put("state_class", "measurement")
            put("icon", "mdi:car-battery")
        }
        output.publishDiscovery(voltageTopic, voltagePayload.toString())
        
        // Temperature sensor
        val tempId = "${DEVICE_ID_BASE}_${cleanMac}_system_temperature"
        val tempTopic = "homeassistant/sensor/$tempId/config"
        val tempPayload = JSONObject().apply {
            put("unique_id", tempId)
            put("name", "RV External Temperature")
            put("device", getDeviceInfo())
            put("availability", getAvailabilityConfig())
            put("state_topic", "$topicPrefix/system/temperature")
            put("unit_of_measurement", "°C")
            put("device_class", "temperature")
            put("state_class", "measurement")
            put("icon", "mdi:thermometer")
        }
        output.publishDiscovery(tempTopic, tempPayload.toString())
        
        Log.i(TAG, "📡 Published discovery for system sensors")
    }
    
    /**
     * Get the command topic pattern for subscribing to MQTT commands.
     */
    fun getCommandTopicPattern(): String = "$topicPrefix/command/#"
    
    /**
     * Get last known brightness for a device (for dimmer ON without brightness).
     */
    fun getLastKnownBrightness(deviceAddress: Int): Int? = lastKnownBrightness[deviceAddress]
    
    /**
     * Clear discovery cache (forces re-publish on reconnect).
     */
    fun clearDiscoveryCache() {
        discoveryPublished.clear()
    }
    
    // ========================================================================
    // Command Parsing
    // ========================================================================
    
    /**
     * Parse an MQTT command topic and payload into a typed command.
     * 
     * Topic formats:
     * - onecontrol/ble/command/dimmable/{tableId}/{deviceId}  payload: ON/OFF
     * - onecontrol/ble/command/dimmable/{tableId}/{deviceId}/brightness  payload: 0-255
     * - onecontrol/ble/command/switch/{tableId}/{deviceId}  payload: ON/OFF
     * - onecontrol/ble/command/cover/{tableId}/{deviceId}  payload: OPEN/CLOSE/STOP
     * - onecontrol/ble/command/cover/{tableId}/{deviceId}/position  payload: 0-100
     * - onecontrol/ble/command/hvac/{tableId}/{deviceId}  payload: off/heat/cool/heat_cool
     * 
     * @param topic The MQTT topic
     * @param payload The MQTT payload
     * @return Parsed command, or null if topic doesn't match expected format
     */
    fun parseCommand(topic: String, payload: String): OneControlCommand? {
        // Remove prefix and split
        val withoutPrefix = topic.removePrefix("$topicPrefix/command/")
        val parts = withoutPrefix.split("/")
        
        if (parts.size < 3) {
            Log.w(TAG, "Invalid command topic format: $topic")
            return null
        }
        
        val deviceType = parts[0]  // dimmable, switch, cover, hvac
        val tableId = parts[1].toIntOrNull()?.toByte() ?: run {
            Log.w(TAG, "Invalid tableId in topic: $topic")
            return null
        }
        val deviceId = parts[2].toIntOrNull()?.toByte() ?: run {
            Log.w(TAG, "Invalid deviceId in topic: $topic")
            return null
        }
        val subTopic = parts.getOrNull(3)  // e.g., "brightness", "position"
        
        return when (deviceType) {
            "dimmable" -> parseDimmableCommand(tableId, deviceId, subTopic, payload)
            "switch" -> parseSwitchCommand(tableId, deviceId, payload)
            "cover" -> parseCoverCommand(tableId, deviceId, subTopic, payload)
            "hvac" -> parseHvacCommand(tableId, deviceId, subTopic, payload)
            else -> {
                Log.w(TAG, "Unknown device type in command: $deviceType")
                null
            }
        }
    }
    
    private fun parseDimmableCommand(
        tableId: Byte, 
        deviceId: Byte, 
        subTopic: String?, 
        payload: String
    ): OneControlCommand.DimmableLight? {
        return when (subTopic) {
            "brightness" -> {
                val brightness = payload.toIntOrNull()?.coerceIn(0, 255) ?: return null
                // Brightness command: turn on with specified brightness
                OneControlCommand.DimmableLight(tableId, deviceId, turnOn = true, brightness = brightness)
            }
            null -> {
                // State command: ON/OFF
                val turnOn = payload.uppercase() == "ON"
                val deviceAddress = ((tableId.toInt() and 0xFF) shl 8) or (deviceId.toInt() and 0xFF)
                val brightness = if (turnOn) {
                    // Use last known brightness or 255 (full)
                    lastKnownBrightness[deviceAddress] ?: 255
                } else null
                OneControlCommand.DimmableLight(tableId, deviceId, turnOn = turnOn, brightness = brightness)
            }
            else -> {
                Log.w(TAG, "Unknown dimmable subTopic: $subTopic")
                null
            }
        }
    }
    
    private fun parseSwitchCommand(tableId: Byte, deviceId: Byte, payload: String): OneControlCommand.Switch {
        val turnOn = payload.uppercase() == "ON"
        return OneControlCommand.Switch(tableId, deviceId, turnOn)
    }
    
    private fun parseCoverCommand(
        tableId: Byte, 
        deviceId: Byte, 
        subTopic: String?, 
        payload: String
    ): OneControlCommand.Cover? {
        return when (subTopic) {
            "position" -> {
                val position = payload.toIntOrNull()?.coerceIn(0, 100) ?: return null
                OneControlCommand.Cover(tableId, deviceId, command = "SET_POSITION", position = position)
            }
            null -> {
                // Command: OPEN/CLOSE/STOP
                val command = payload.uppercase()
                if (command !in listOf("OPEN", "CLOSE", "STOP")) {
                    Log.w(TAG, "Invalid cover command: $payload")
                    return null
                }
                OneControlCommand.Cover(tableId, deviceId, command = command)
            }
            else -> {
                Log.w(TAG, "Unknown cover subTopic: $subTopic")
                null
            }
        }
    }
    
    private fun parseHvacCommand(
        tableId: Byte, 
        deviceId: Byte, 
        subTopic: String?, 
        payload: String
    ): OneControlCommand.Hvac? {
        return when (subTopic) {
            "fan_mode" -> OneControlCommand.Hvac(tableId, deviceId, fanMode = payload.lowercase())
            "temperature" -> {
                // Heat setpoint
                val temp = payload.toIntOrNull() ?: return null
                OneControlCommand.Hvac(tableId, deviceId, heatSetpoint = temp)
            }
            "temperature_high" -> {
                // Cool setpoint
                val temp = payload.toIntOrNull() ?: return null
                OneControlCommand.Hvac(tableId, deviceId, coolSetpoint = temp)
            }
            null -> {
                // Mode command: off, heat, cool, heat_cool
                val mode = payload.lowercase()
                if (mode !in listOf("off", "heat", "cool", "heat_cool")) {
                    Log.w(TAG, "Invalid HVAC mode: $payload")
                    return null
                }
                OneControlCommand.Hvac(tableId, deviceId, mode = mode)
            }
            else -> {
                Log.w(TAG, "Unknown HVAC subTopic: $subTopic")
                null
            }
        }
    }
}
