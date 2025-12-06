package com.onecontrol.blebridge

import android.util.Log
import org.json.JSONArray
import org.json.JSONObject

/**
 * Home Assistant MQTT Discovery helper
 * Publishes discovery configs for automatic entity creation in Home Assistant
 * https://www.home-assistant.io/integrations/mqtt/#mqtt-discovery
 */
object HomeAssistantMqttDiscovery {
    private const val TAG = "HADiscovery"
    
    /**
     * Generate device info object for Home Assistant
     * All entities will be grouped under this device
     */
    fun getDeviceInfo(gatewayMac: String): JSONObject {
        return JSONObject().apply {
            put("identifiers", JSONArray().put("onecontrol_ble_$gatewayMac"))
            put("name", "OneControl BLE ${formatMacForDisplay(gatewayMac)}")
            put("manufacturer", "Lippert Components Inc. (LCI)")
            put("model", "OneControl Gateway")
            put("sw_version", "MyRvLink Protocol v6")
            // Removed via_device to prevent "Unknown Device" parent device
        }
    }
    
    /**
     * Format MAC address for display: 24:DC:C3:ED:1E:0A
     * Handles both formats: "24:DC:C3:ED:1E:0A" and "24DCC3ED1E0A"
     */
    private fun formatMacForDisplay(mac: String): String {
        // If already formatted with colons, return as-is
        if (mac.contains(":")) {
            return mac
        }
        // Otherwise, chunk every 2 characters and join with colons
        return mac.chunked(2).joinToString(":")
    }
    
    /**
     * Sanitize name for Home Assistant object_id (lowercase, underscores only)
     */
    private fun sanitizeName(name: String): String {
        return name
            .lowercase()
            .replace(Regex("[^a-z0-9]+"), "_")
            .trim('_')
    }
    
    /**
     * Generate discovery topic for a device entity
     * Format: homeassistant/{component}/{node_id}/{object_id}/config
     */
    fun getDiscoveryTopic(component: String, gatewayMac: String, deviceAddr: Int): String {
        val nodeId = "onecontrol_ble_${gatewayMac.replace(":", "").lowercase()}"
        val objectId = "device_${deviceAddr.toString(16).padStart(4, '0')}"
        return "homeassistant/$component/$nodeId/$objectId/config"
    }
    
    /**
     * Generate discovery topic for a system sensor
     */
    fun getSystemDiscoveryTopic(component: String, gatewayMac: String, sensorName: String): String {
        val nodeId = "onecontrol_ble_${gatewayMac.replace(":", "").lowercase()}"
        val objectId = "system_${sanitizeName(sensorName)}"
        return "homeassistant/$component/$nodeId/$objectId/config"
    }
    
    /**
     * Generate discovery config for a dimmable light
     */
    fun getDimmableLightDiscovery(
        gatewayMac: String,
        deviceAddr: Int,
        deviceName: String,
        stateTopic: String,
        commandTopic: String,
        brightnessTopic: String
    ): JSONObject {
        val uniqueId = "onecontrol_ble_${gatewayMac.replace(":", "")}_light_${deviceAddr.toString(16)}"
        val objectId = "light_${deviceAddr.toString(16).padStart(4, '0')}"
        
        return JSONObject().apply {
            put("unique_id", uniqueId)
            put("name", deviceName)  // Entity name (not prefixed with device name)
            put("default_entity_id", "light.$objectId")  // Replace deprecated object_id
            put("device", getDeviceInfo(gatewayMac))
            
            // State
            put("state_topic", stateTopic)
            put("command_topic", commandTopic)
            
            // Brightness
            put("brightness_state_topic", brightnessTopic)
            // Route brightness commands to the same command topic we subscribe to
            put("brightness_command_topic", commandTopic)
            put("brightness_scale", 100)
            
            // Payloads
            put("payload_on", "ON")
            put("payload_off", "OFF")
            put("optimistic", false)  // Wait for state updates from gateway
            
            // Icon
            put("icon", "mdi:lightbulb")
        }
    }
    
    /**
     * Generate discovery config for a switch (relay)
     */
    fun getSwitchDiscovery(
        gatewayMac: String,
        deviceAddr: Int,
        deviceName: String,
        stateTopic: String,
        commandTopic: String
    ): JSONObject {
        val uniqueId = "onecontrol_ble_${gatewayMac.replace(":", "")}_switch_${deviceAddr.toString(16)}"
        val objectId = "switch_${deviceAddr.toString(16).padStart(4, '0')}"
        
        return JSONObject().apply {
            put("unique_id", uniqueId)
            put("name", deviceName)  // Entity name (not prefixed with device name)
            put("default_entity_id", "switch.$objectId")  // Replace deprecated object_id
            put("device", getDeviceInfo(gatewayMac))
            
            // State
            put("state_topic", stateTopic)
            put("command_topic", commandTopic)
            
            // Payloads
            put("payload_on", "ON")
            put("payload_off", "OFF")
            put("optimistic", false)
            
            // Icon
            put("icon", "mdi:electric-switch")
        }
    }
    
    /**
     * Generate discovery config for a cover (slide/awning)
     */
    fun getCoverDiscovery(
        gatewayMac: String,
        deviceAddr: Int,
        deviceName: String,
        stateTopic: String,
        commandTopic: String,
        positionTopic: String
    ): JSONObject {
        val uniqueId = "onecontrol_ble_${gatewayMac.replace(":", "")}_cover_${deviceAddr.toString(16)}"
        val objectId = "cover_${deviceAddr.toString(16).padStart(4, '0')}"
        
        return JSONObject().apply {
            put("unique_id", uniqueId)
            put("name", deviceName)  // Entity name (not prefixed with device name)
            put("default_entity_id", "cover.$objectId")  // Replace deprecated object_id
            put("device", getDeviceInfo(gatewayMac))
            
            // State
            put("state_topic", stateTopic)
            put("command_topic", commandTopic)
            
            // Position
            put("position_topic", positionTopic)
            put("set_position_topic", "${positionTopic}/set")
            
            // Payloads
            put("payload_open", "OPEN")
            put("payload_close", "CLOSE")
            put("payload_stop", "STOP")
            
            // Icon
            put("icon", "mdi:window-shutter")
        }
    }
    
    /**
     * Generate discovery config for a sensor (voltage, temperature, etc.)
     */
    fun getSensorDiscovery(
        gatewayMac: String,
        sensorName: String,
        stateTopic: String,
        unit: String? = null,
        deviceClass: String? = null,
        icon: String? = null
    ): JSONObject {
        val uniqueId = "onecontrol_ble_${gatewayMac.replace(":", "")}_${sanitizeName(sensorName)}"
        
        return JSONObject().apply {
            put("unique_id", uniqueId)
            put("name", sensorName)
            put("default_entity_id", "sensor.${sanitizeName(sensorName)}")  // Replace deprecated object_id
            put("device", getDeviceInfo(gatewayMac))
            
            // State
            put("state_topic", stateTopic)
            put("state_class", "measurement")
            
            // Optional attributes
            unit?.let { put("unit_of_measurement", it) }
            deviceClass?.let { put("device_class", it) }
            icon?.let { put("icon", it) }
        }
    }
    
    /**
     * Get generic device name based on device type
     */
    fun getGenericDeviceName(deviceType: String, deviceAddr: Int): String {
        val shortAddr = deviceAddr.toString(16).uppercase().padStart(4, '0')
        
        return when (deviceType) {
            "dimmable_light" -> "Dimmable Light $shortAddr"
            "relay", "latching_relay" -> "Switch $shortAddr"
            "hbridge_relay" -> "Motor $shortAddr"
            "rgb_light" -> "RGB Light $shortAddr"
            else -> "Device $shortAddr"
        }
    }
    
    /**
     * Get Home Assistant component type for a device type
     */
    fun getComponentType(deviceType: String): String {
        return when (deviceType) {
            "dimmable_light", "rgb_light" -> "light"
            "relay", "latching_relay" -> "switch"
            "hbridge_relay" -> "cover"
            else -> "sensor"
        }
    }
}

