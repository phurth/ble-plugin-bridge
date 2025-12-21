package com.blemqttbridge.plugins.device.onecontrol

/**
 * Typed commands for OneControl devices.
 * 
 * These are protocol-level commands, independent of input format (MQTT, REST, etc.).
 * A separate parser/handler converts input-specific format to these typed commands.
 */
sealed class OneControlCommand {
    /** Device address = (tableId << 8) | deviceId */
    abstract val tableId: Byte
    abstract val deviceId: Byte
    val deviceAddress: Int get() = ((tableId.toInt() and 0xFF) shl 8) or (deviceId.toInt() and 0xFF)
    
    /**
     * Command to control a dimmable light.
     * @param tableId Device table ID
     * @param deviceId Device ID within table
     * @param turnOn true to turn on, false to turn off
     * @param brightness Brightness 0-255 (null to use last known or 255)
     */
    data class DimmableLight(
        override val tableId: Byte,
        override val deviceId: Byte,
        val turnOn: Boolean,
        val brightness: Int? = null  // 0-255, null = use last known or 255
    ) : OneControlCommand()
    
    /**
     * Command to control a simple switch/relay.
     */
    data class Switch(
        override val tableId: Byte,
        override val deviceId: Byte,
        val turnOn: Boolean
    ) : OneControlCommand()
    
    /**
     * Command to control a cover (slide, awning).
     * @param command "OPEN", "CLOSE", "STOP", or position 0-100
     */
    data class Cover(
        override val tableId: Byte,
        override val deviceId: Byte,
        val command: String,  // "OPEN", "CLOSE", "STOP"
        val position: Int? = null  // 0-100 for set_position
    ) : OneControlCommand()
    
    /**
     * Command to control HVAC.
     */
    data class Hvac(
        override val tableId: Byte,
        override val deviceId: Byte,
        val mode: String? = null,           // "off", "heat", "cool", "heat_cool"
        val fanMode: String? = null,        // "auto", "high", "low"
        val heatSetpoint: Int? = null,      // Temperature in F
        val coolSetpoint: Int? = null       // Temperature in F
    ) : OneControlCommand()
}

/**
 * Handler interface for commands to OneControl devices.
 */
interface OneControlCommandHandler {
    /**
     * Handle a typed command.
     * @param gatewayMac MAC address of the gateway to send command to
     * @param command The typed command to execute
     * @return Result with Unit on success, Exception on failure
     */
    suspend fun handleCommand(gatewayMac: String, command: OneControlCommand): Result<Unit>
}
