package com.blemqttbridge.core.discovery

import com.blemqttbridge.util.DebugLog
import org.json.JSONArray
import org.json.JSONObject

/**
 * Unified Home Assistant MQTT Discovery Builder
 * 
 * Consolidates discovery payload generation across all plugins:
 * - OneControl gateway switches/lights
 * - Mopeka tank sensors
 * - Hughes power sensors
 * - Mock/test devices
 * 
 * Follows Home Assistant MQTT discovery spec:
 * https://www.home-assistant.io/integrations/mqtt/#mqtt-discovery
 */
class HomeAssistantDiscoveryBuilder(
    private val deviceMac: String,
    private val deviceName: String,
    private val deviceManufacturer: String,
    private val appVersion: String? = null
) : DiscoveryBuilder {
    private val macNormalized = deviceMac.replace(":", "").lowercase()
    private val macClean = deviceMac.replace(":", "").uppercase()
    
    /**
     * Build device info object for HA device grouping
     */
    private fun buildDeviceInfo(deviceIdentifier: String): JSONObject {
        return JSONObject().apply {
            put("identifiers", JSONArray().put(deviceIdentifier))
            put("name", deviceName)
            put("manufacturer", deviceManufacturer)
            if (appVersion != null) {
                put("sw_version", appVersion)
            }
        }
    }
    
    /**
     * Build availability config (online/offline status)
     */
    private fun buildAvailability(baseTopic: String): JSONObject {
        return JSONObject().apply {
            put("topic", "homeassistant/$baseTopic/availability")
            put("payload_available", "online")
            put("payload_not_available", "offline")
        }
    }
    
    /**
     * Build a generic sensor discovery payload
     */
    override fun buildSensor(
        uniqueId: String,
        displayName: String,
        stateTopic: String,
        baseTopic: String,
        deviceIdentifier: String,
        unitOfMeasurement: String?,
        deviceClass: String?,
        icon: String?,
        stateClass: String?,
        valueTemplate: String?,
        jsonAttributes: Boolean
    ): JSONObject {
        return JSONObject().apply {
            put("unique_id", uniqueId)
            put("name", displayName)
            put("state_topic", stateTopic)
            put("device", buildDeviceInfo(deviceIdentifier))
            
            if (unitOfMeasurement != null) {
                put("unit_of_measurement", unitOfMeasurement)
            }
            if (deviceClass != null) {
                put("device_class", deviceClass)
            }
            if (icon != null) {
                put("icon", icon)
            }
            if (stateClass != null) {
                put("state_class", stateClass)
            }
            if (valueTemplate != null) {
                DebugLog.d("HA Discovery", "Adding value_template: $valueTemplate")
                put("value_template", valueTemplate)
            }
            if (jsonAttributes) {
                put("json_attributes_topic", stateTopic)
            }
            
            put("availability", buildAvailability(baseTopic))
        }
    }
    
    /**
     * Build a switch discovery payload
     */
    override fun buildSwitch(
        uniqueId: String,
        displayName: String,
        stateTopic: String,
        commandTopic: String,
        baseTopic: String,
        deviceIdentifier: String,
        payloadOn: String,
        payloadOff: String,
        icon: String?
    ): JSONObject {
        return JSONObject().apply {
            put("unique_id", uniqueId)
            put("name", displayName)
            put("state_topic", stateTopic)
            put("command_topic", commandTopic)
            put("payload_on", payloadOn)
            put("payload_off", payloadOff)
            put("optimistic", false)
            put("device", buildDeviceInfo(deviceIdentifier))
            
            if (icon != null) {
                put("icon", icon)
            }
            
            put("availability", buildAvailability(baseTopic))
        }
    }
    
    /**
     * Build a light discovery payload (brightness support)
     */
    override fun buildLight(
        uniqueId: String,
        displayName: String,
        stateTopic: String,
        commandTopic: String,
        brightnessTopic: String,
        brightnessCommandTopic: String,
        baseTopic: String,
        deviceIdentifier: String,
        payloadOn: String,
        payloadOff: String,
        brightnessScale: Int
    ): JSONObject {
        return JSONObject().apply {
            put("unique_id", uniqueId)
            put("name", displayName)
            put("state_topic", stateTopic)
            put("command_topic", commandTopic)
            put("brightness_state_topic", brightnessTopic)
            put("brightness_command_topic", brightnessCommandTopic)
            put("brightness_scale", brightnessScale)
            put("payload_on", payloadOn)
            put("payload_off", payloadOff)
            put("optimistic", false)
            put("device", buildDeviceInfo(deviceIdentifier))
            
            put("availability", buildAvailability(baseTopic))
        }
    }
    
    /**
     * Build a binary sensor discovery payload
     */
    override fun buildBinarySensor(
        uniqueId: String,
        displayName: String,
        stateTopic: String,
        baseTopic: String,
        deviceIdentifier: String,
        deviceClass: String?,
        icon: String?,
        payloadOn: String,
        payloadOff: String
    ): JSONObject {
        return JSONObject().apply {
            put("unique_id", uniqueId)
            put("name", displayName)
            put("state_topic", stateTopic)
            put("payload_on", payloadOn)
            put("payload_off", payloadOff)
            put("device", buildDeviceInfo(deviceIdentifier))
            
            if (deviceClass != null) {
                put("device_class", deviceClass)
            }
            if (icon != null) {
                put("icon", icon)
            }
            
            put("availability", buildAvailability(baseTopic))
        }
    }
    
    /**
     * Build a number/slider discovery payload
     */
    override fun buildNumber(
        uniqueId: String,
        displayName: String,
        stateTopic: String,
        commandTopic: String,
        baseTopic: String,
        deviceIdentifier: String,
        min: Number,
        max: Number,
        step: Number,
        unitOfMeasurement: String?,
        icon: String?
    ): JSONObject {
        return JSONObject().apply {
            put("unique_id", uniqueId)
            put("name", displayName)
            put("state_topic", stateTopic)
            put("command_topic", commandTopic)
            put("min", min)
            put("max", max)
            put("step", step)
            put("device", buildDeviceInfo(deviceIdentifier))
            
            if (unitOfMeasurement != null) {
                put("unit_of_measurement", unitOfMeasurement)
            }
            if (icon != null) {
                put("icon", icon)
            }
            
            put("availability", buildAvailability(baseTopic))
        }
    }
    
    companion object {
        /**
         * Build a discovery topic for an entity
         */
        fun buildDiscoveryTopic(
            component: String,
            nodeId: String,
            entityId: String
        ): String {
            return "homeassistant/$component/$nodeId/$entityId/config"
        }
        
        /**
         * Sanitize a name for use as part of a topic/ID
         */
        fun sanitizeName(name: String): String {
            return name.lowercase()
                .replace(Regex("[^a-z0-9_]"), "_")
                .replace(Regex("_+"), "_")
                .trim('_')
        }
    }

    // Instance-level overrides delegating to companion helpers
    override fun buildDiscoveryTopic(component: String, nodeId: String, entityId: String): String {
        return Companion.buildDiscoveryTopic(component, nodeId, entityId)
    }

    override fun sanitizeName(name: String): String {
        return Companion.sanitizeName(name)
    }

    // No convenience overloads: use explicit arguments for clarity
}
