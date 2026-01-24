package com.blemqttbridge.core.discovery

import org.json.JSONObject

/**
 * DiscoveryBuilder interface abstracts Home Assistant vs other MQTT discovery schemas.
 * Implementations produce JSON payloads for MQTT discovery topics.
 */
interface DiscoveryBuilder {
    fun buildSensor(
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
    ): JSONObject

    fun buildSwitch(
        uniqueId: String,
        displayName: String,
        stateTopic: String,
        commandTopic: String,
        baseTopic: String,
        deviceIdentifier: String,
        payloadOn: String,
        payloadOff: String,
        icon: String?
    ): JSONObject

    fun buildLight(
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
    ): JSONObject

    fun buildBinarySensor(
        uniqueId: String,
        displayName: String,
        stateTopic: String,
        baseTopic: String,
        deviceIdentifier: String,
        deviceClass: String?,
        icon: String?,
        payloadOn: String,
        payloadOff: String
    ): JSONObject

    fun buildNumber(
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
    ): JSONObject

    /** Helper: builder-specific topic and name utilities */
    fun buildDiscoveryTopic(component: String, nodeId: String, entityId: String): String
    fun sanitizeName(name: String): String
}
