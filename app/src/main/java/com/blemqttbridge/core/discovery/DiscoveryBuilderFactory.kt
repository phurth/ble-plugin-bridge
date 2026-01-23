package com.blemqttbridge.core.discovery

/**
 * Factory for creating discovery builders by format.
 *
 * Supported formats:
 * - "homeassistant" (default)
 * - aliases: "ha"
 *
 * Future: add other formats (openhab, domoticz) implementing DiscoveryBuilder.
 */
object DiscoveryBuilderFactory {
    fun create(
        format: String?,
        deviceMac: String,
        deviceName: String,
        deviceManufacturer: String,
        appVersion: String? = null
    ): DiscoveryBuilder {
        return when (format?.lowercase()) {
            null, "", "homeassistant", "ha" ->
                HomeAssistantDiscoveryBuilder(deviceMac, deviceName, deviceManufacturer, appVersion)
            else ->
                // Default to Home Assistant until other formats are implemented
                HomeAssistantDiscoveryBuilder(deviceMac, deviceName, deviceManufacturer, appVersion)
        }
    }
}
