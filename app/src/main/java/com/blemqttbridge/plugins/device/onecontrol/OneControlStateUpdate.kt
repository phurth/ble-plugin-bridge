package com.blemqttbridge.plugins.device.onecontrol

/**
 * Typed device state updates from OneControl protocol processing.
 * 
 * These are protocol-level state changes, independent of output format (MQTT, REST, etc.).
 * A separate formatter/bridge layer converts these to output-specific payloads.
 */
sealed class OneControlStateUpdate {
    /** Device address = (tableId << 8) | deviceId */
    abstract val tableId: Byte
    abstract val deviceId: Byte
    val deviceAddress: Int get() = ((tableId.toInt() and 0xFF) shl 8) or (deviceId.toInt() and 0xFF)
    
    /**
     * Dimmable light state update.
     * @param tableId Device table ID
     * @param deviceId Device ID within table
     * @param isOn Whether light is on
     * @param brightnessRaw Raw brightness 0-255 (as reported by gateway)
     * @param brightnessPct Brightness as percentage 0-100
     */
    data class DimmableLight(
        override val tableId: Byte,
        override val deviceId: Byte,
        val isOn: Boolean,
        val brightnessRaw: Int,  // 0-255
        val brightnessPct: Int   // 0-100
    ) : OneControlStateUpdate()
    
    /**
     * Simple relay/switch state update.
     */
    data class Switch(
        override val tableId: Byte,
        override val deviceId: Byte,
        val isOn: Boolean
    ) : OneControlStateUpdate()
    
    /**
     * H-bridge motor/cover state update (slides, awnings).
     * @param status Raw status byte: 0xC2=extending, 0xC3=retracting, 0xC0=stopped
     * @param position Position 0-100 if available
     */
    data class Cover(
        override val tableId: Byte,
        override val deviceId: Byte,
        val status: Int,
        val position: Int?,
        val lastDirection: Int?  // Last non-stopped direction for open/closed inference
    ) : OneControlStateUpdate()
    
    /**
     * Tank sensor level update.
     * @param level Normalized level 0-100 percent
     * @param fluidType "water" or "fuel" if known
     */
    data class Tank(
        override val tableId: Byte,
        override val deviceId: Byte,
        val level: Int,  // 0-100
        val fluidType: String? = null
    ) : OneControlStateUpdate()
    
    /**
     * HVAC zone status update.
     */
    data class Hvac(
        override val tableId: Byte,
        override val deviceId: Byte,
        val heatMode: Int,      // 0=Off, 1=Heat, 2=Cool, 3=Both
        val heatSource: Int,    // 0=PreferGas, 1=PreferHeatPump, 2=Other
        val fanMode: Int,       // 0=Auto, 1=High, 2=Low
        val zoneMode: Int,      // 0=Off, 1=Idle, 2=Cooling, 3=HeatPump, etc.
        val heatSetpointF: Int,
        val coolSetpointF: Int,
        val indoorTempF: Float?,
        val outdoorTempF: Float?
    ) : OneControlStateUpdate()
    
    /**
     * RV system status (voltage, temperature).
     */
    data class SystemStatus(
        override val tableId: Byte,
        override val deviceId: Byte,
        val batteryVoltage: Float?,
        val externalTempC: Float?
    ) : OneControlStateUpdate() {
        companion object {
            // System status uses tableId=0, deviceId=0
            val SYSTEM_TABLE_ID: Byte = 0
            val SYSTEM_DEVICE_ID: Byte = 0
        }
    }
    
    /**
     * Gateway information received.
     */
    data class GatewayInfo(
        override val tableId: Byte,
        override val deviceId: Byte,
        val protocolVersion: Int,
        val deviceCount: Int,
        val deviceTableId: Int
    ) : OneControlStateUpdate() {
        companion object {
            val GATEWAY_TABLE_ID: Byte = 0
            val GATEWAY_DEVICE_ID: Byte = 0
        }
    }
    
    /**
     * Device online/offline status.
     */
    data class DeviceOnline(
        override val tableId: Byte,
        override val deviceId: Byte,
        val isOnline: Boolean
    ) : OneControlStateUpdate()
}

/**
 * Listener interface for receiving state updates from OneControl plugin.
 * Implementations (e.g., MQTT formatter) convert these to output-specific format.
 */
interface OneControlStateListener {
    /**
     * Called when a device state update is received.
     * @param gatewayMac MAC address of the gateway device
     * @param update The typed state update
     */
    suspend fun onStateUpdate(gatewayMac: String, update: OneControlStateUpdate)
    
    /**
     * Called when a new device is discovered and needs HA discovery published.
     * @param gatewayMac MAC address of the gateway device
     * @param update The device state (used to determine device type for discovery)
     */
    suspend fun onDeviceDiscovered(gatewayMac: String, update: OneControlStateUpdate)
}
