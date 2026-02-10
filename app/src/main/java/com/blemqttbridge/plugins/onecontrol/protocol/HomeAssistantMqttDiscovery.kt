package com.blemqttbridge.plugins.onecontrol.protocol

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
    private const val DEVICE_ID_BASE = "onecontrol_ble_v2"
    
    /**
     * Discovery builder class - simplifies creating discovery payloads
     * with consistent device info and common fields.
     */
    class DiscoveryBuilder(
        private val gatewayMac: String,
        private val appVersion: String? = null
    ) {
        private val macNormalized = gatewayMac.replace(":", "").lowercase()
        
        /**
         * Build switch discovery payload
         */
        fun buildSwitch(
            deviceAddr: Int,
            deviceName: String,
            stateTopic: String,
            commandTopic: String,
            attributesTopic: String? = null
        ): JSONObject {
            return getSwitchDiscovery(
                gatewayMac, deviceAddr, deviceName,
                stateTopic, commandTopic, attributesTopic, appVersion
            )
        }
        
        /**
         * Build dimmable light discovery payload
         */
        fun buildDimmableLight(
            deviceAddr: Int,
            deviceName: String,
            stateTopic: String,
            commandTopic: String,
            brightnessTopic: String
        ): JSONObject {
            return getDimmableLightDiscovery(
                gatewayMac, deviceAddr, deviceName,
                stateTopic, commandTopic, brightnessTopic, appVersion
            )
        }
        
        /**
         * Build cover state sensor discovery payload (state-only, no control)
         */
        fun buildCoverStateSensor(
            deviceAddr: Int,
            deviceName: String,
            stateTopic: String
        ): JSONObject {
            return getCoverStateSensorDiscovery(
                gatewayMac, deviceAddr, deviceName,
                stateTopic, appVersion
            )
        }
        
        /**
         * Build sensor discovery payload
         */
        fun buildSensor(
            sensorName: String,
            stateTopic: String,
            unit: String? = null,
            deviceClass: String? = null,
            icon: String? = null
        ): JSONObject {
            return getSensorDiscovery(
                gatewayMac, sensorName, stateTopic,
                unit, deviceClass, icon, appVersion
            )
        }
        
        /**
         * Build generator state sensor discovery payload
         * Includes json_attributes_topic for quiet hours, raw status byte, etc.
         */
        fun buildGeneratorStateSensor(
            deviceAddr: Int,
            deviceName: String,
            stateTopic: String,
            attributesTopic: String? = null
        ): JSONObject {
            return getGeneratorStateSensorDiscovery(
                gatewayMac, deviceAddr, deviceName,
                stateTopic, attributesTopic, appVersion
            )
        }
        
        /**
         * Build binary sensor discovery payload
         */
        fun buildBinarySensor(
            deviceAddr: Int,
            deviceName: String,
            stateTopic: String,
            deviceClass: String? = null,
            icon: String? = null
        ): JSONObject {
            return getBinarySensorDiscovery(
                gatewayMac, deviceAddr, deviceName,
                stateTopic, deviceClass, icon, appVersion
            )
        }
        
        /**
         * Get discovery topic for an entity
         */
        fun getDiscoveryTopic(component: String, topicPrefix: String, deviceAddr: Int): String {
            val objectId = "${topicPrefix}_${deviceAddr.toString(16).padStart(4, '0')}"
            return "homeassistant/$component/onecontrol_ble_$macNormalized/$objectId/config"
        }
        
        /**
         * Get discovery topic for a system sensor
         */
        fun getSystemSensorTopic(sensorName: String): String {
            val objectId = "system_${sanitizeName(sensorName)}"
            return "homeassistant/sensor/onecontrol_ble_$macNormalized/$objectId/config"
        }
    }
    
    /**
     * Generate device info object for Home Assistant
     * All entities will be grouped under this device
     */
    fun getDeviceInfo(gatewayMac: String, appVersion: String? = null): JSONObject {
        return JSONObject().apply {
            put("identifiers", JSONArray().put("${DEVICE_ID_BASE}_$gatewayMac"))
            put("name", "OneControl BLE Gateway")
            put("model", "OneControl plugin for the Android BLE to MQTT Bridge")
            put("manufacturer", "phurth")
            appVersion?.let { put("sw_version", it) }
            put("connections", JSONArray().put(JSONArray().put("mac").put(formatMacForDisplay(gatewayMac))))
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
        brightnessTopic: String,
        appVersion: String? = null
    ): JSONObject {
        val uniqueId = "onecontrol_ble_${gatewayMac.replace(":", "")}_light_${deviceAddr.toString(16)}"
        val objectId = "light_${deviceAddr.toString(16).padStart(4, '0')}"
        
        return JSONObject().apply {
            put("unique_id", uniqueId)
            put("name", deviceName)  // Entity name (not prefixed with device name)
            put("default_entity_id", "light.$objectId")  // Replace deprecated object_id
            put("device", getDeviceInfo(gatewayMac, appVersion))

            // Classic MQTT light schema: state + brightness topics
            put("state_topic", stateTopic)
            put("command_topic", commandTopic)
            put("brightness_state_topic", brightnessTopic)
            put("brightness_command_topic", "$commandTopic/brightness")
            put("payload_on", "ON")
            put("payload_off", "OFF")
            put("on_command_type", "brightness")
            put("optimistic", false)  // Wait for state updates from gateway
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
        commandTopic: String,
        attributesTopic: String? = null,
        appVersion: String? = null
    ): JSONObject {
        val uniqueId = "onecontrol_ble_${gatewayMac.replace(":", "")}_switch_${deviceAddr.toString(16)}"
        val objectId = "switch_${deviceAddr.toString(16).padStart(4, '0')}"
        
        return JSONObject().apply {
            put("unique_id", uniqueId)
            put("name", deviceName)  // Entity name (not prefixed with device name)
            put("default_entity_id", "switch.$objectId")  // Replace deprecated object_id
            put("device", getDeviceInfo(gatewayMac, appVersion))
            
            // State
            put("state_topic", stateTopic)
            put("command_topic", commandTopic)
            
            // Attributes (DTC codes, fault status, current draw)
            if (attributesTopic != null) {
                put("json_attributes_topic", attributesTopic)
            }
            
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
     * Note: Position is not supported for momentary H-Bridge devices (always 0xFF)
     */
    /**
     * Generate discovery config for a cover (awning, slide-out, etc.)
     * 
     * SAFETY NOTE: RV awnings and slides typically lack limit switches on the extend direction.
     * Extending too far can damage the awning (fabric rolls wrong way) or slide mechanism.
     * However, RETRACTING is safe because:
     * - Physical stop when fully retracted (hits the RV body)
     * - Controller auto-stops via current spike detection
     * 
     * By default, only CLOSE (retract) and STOP commands are enabled.
     * Set retractOnly=false to enable OPEN (extend) - USE WITH CAUTION.
     * 
     * @param retractOnly If true (default), only CLOSE and STOP are available. OPEN is disabled.
     */
    fun getCoverDiscovery(
        gatewayMac: String,
        deviceAddr: Int,
        deviceName: String,
        stateTopic: String,
        commandTopic: String,
        attributesTopic: String? = null,
        appVersion: String? = null,
        retractOnly: Boolean = true
    ): JSONObject {
        val uniqueId = "onecontrol_ble_${gatewayMac.replace(":", "")}_cover_${deviceAddr.toString(16)}"
        val objectId = "cover_${deviceAddr.toString(16).padStart(4, '0')}"
        
        return JSONObject().apply {
            put("unique_id", uniqueId)
            put("name", deviceName)  // Entity name (not prefixed with device name)
            put("default_entity_id", "cover.$objectId")  // Replace deprecated object_id
            put("device", getDeviceInfo(gatewayMac, appVersion))
            
            // State
            put("state_topic", stateTopic)
            put("command_topic", commandTopic)
            
            // Attributes (hazard flags, fault status)
            if (attributesTopic != null) {
                put("json_attributes_topic", attributesTopic)
            }
            
            // Payloads - CLOSE and STOP always available
            put("payload_close", "CLOSE")
            put("payload_stop", "STOP")
            
            // OPEN is dangerous for awnings/slides without limit switches
            // Only enable if explicitly requested
            if (!retractOnly) {
                put("payload_open", "OPEN")
            }
            // Note: Not including payload_open disables the open button in HA
            
            // State values
            put("state_open", "open")
            put("state_opening", "opening")
            put("state_closed", "closed")
            put("state_closing", "closing")
            put("state_stopped", "stopped")
            
            // Icon based on mode
            put("icon", if (retractOnly) "mdi:arrow-collapse-horizontal" else "mdi:window-shutter")
        }
    }
    
    /**
     * Generate discovery config for a cover STATE SENSOR (state-only, no control)
     * 
     * SAFETY NOTE: RV awnings and slides have no limit switches or overcurrent protection.
     * The motors rely entirely on operator judgment to avoid damage.
     * Therefore, we expose these as state-only sensors rather than controllable covers.
     * 
     * States: open, opening, closed, closing, stopped
     */
    fun getCoverStateSensorDiscovery(
        gatewayMac: String,
        deviceAddr: Int,
        deviceName: String,
        stateTopic: String,
        appVersion: String? = null
    ): JSONObject {
        val uniqueId = "onecontrol_ble_${gatewayMac.replace(":", "")}_cover_state_${deviceAddr.toString(16)}"
        val objectId = "cover_state_${deviceAddr.toString(16).padStart(4, '0')}"
        
        return JSONObject().apply {
            put("unique_id", uniqueId)
            put("name", deviceName)
            put("default_entity_id", "sensor.$objectId")
            put("device", getDeviceInfo(gatewayMac, appVersion))
            
            // State only - no commands
            put("state_topic", stateTopic)
            
            // Icon shows awning/slide
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
        icon: String? = null,
        appVersion: String? = null
    ): JSONObject {
        val uniqueId = "onecontrol_ble_${gatewayMac.replace(":", "")}_${sanitizeName(sensorName)}"
        
        return JSONObject().apply {
            put("unique_id", uniqueId)
            put("name", sensorName)
            put("default_entity_id", "sensor.${sanitizeName(sensorName)}")  // Replace deprecated object_id
            put("device", getDeviceInfo(gatewayMac, appVersion))
            
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
     * Generate discovery config for a generator state sensor
     * State values: off, priming, starting, running, stopping
     */
    fun getGeneratorStateSensorDiscovery(
        gatewayMac: String,
        deviceAddr: Int,
        deviceName: String,
        stateTopic: String,
        attributesTopic: String? = null,
        appVersion: String? = null
    ): JSONObject {
        val uniqueId = "onecontrol_ble_${gatewayMac.replace(":", "")}_gen_state_${deviceAddr.toString(16)}"
        val objectId = "gen_state_${deviceAddr.toString(16).padStart(4, '0')}"
        
        return JSONObject().apply {
            put("unique_id", uniqueId)
            put("name", deviceName)
            put("default_entity_id", "sensor.$objectId")
            put("device", getDeviceInfo(gatewayMac, appVersion))
            
            put("state_topic", stateTopic)
            put("icon", "mdi:engine")
            
            attributesTopic?.let { put("json_attributes_topic", it) }
        }
    }
    
    /**
     * Generate discovery config for a binary sensor
     */
    fun getBinarySensorDiscovery(
        gatewayMac: String,
        deviceAddr: Int,
        deviceName: String,
        stateTopic: String,
        deviceClass: String? = null,
        icon: String? = null,
        appVersion: String? = null
    ): JSONObject {
        val uniqueId = "onecontrol_ble_${gatewayMac.replace(":", "")}_bin_${sanitizeName(deviceName)}"
        val objectId = "bin_${sanitizeName(deviceName)}"
        
        return JSONObject().apply {
            put("unique_id", uniqueId)
            put("name", deviceName)
            put("default_entity_id", "binary_sensor.$objectId")
            put("device", getDeviceInfo(gatewayMac, appVersion))
            
            put("state_topic", stateTopic)
            put("payload_on", "ON")
            put("payload_off", "OFF")
            
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

