package com.blemqttbridge.plugins.onecontrol.protocol

/**
 * Builds MyRvLink commands for device control
 */
object MyRvLinkCommandBuilder {
    
    /**
     * Build GetDevices command
     * Format: [ClientCommandId (2 bytes)][CommandType (1)][DeviceTableId (1)][StartDeviceId (1)][MaxDeviceRequestCount (1)]
     */
    fun buildGetDevices(clientCommandId: UShort, deviceTableId: Byte, startDeviceId: Byte = 0.toByte(), maxDeviceCount: Byte = (-1).toByte()): ByteArray {
        return byteArrayOf(
            (clientCommandId.toInt() and 0xFF).toByte(),           // ClientCommandId LSB
            ((clientCommandId.toInt() shr 8) and 0xFF).toByte(),  // ClientCommandId MSB
            0x01.toByte(),  // CommandType: GetDevices
            deviceTableId,
            startDeviceId,
            maxDeviceCount
        )
    }
    
    /**
     * Build ActionSwitch command
     * Format: [ClientCommandId (2 bytes)][CommandType (1)][DeviceTableId (1)][SwitchState (1)][DeviceId1 (1)][DeviceId2 (1)]...
     * SwitchState: 0 = Off, 1 = On
     */
    fun buildActionSwitch(
        clientCommandId: UShort,
        deviceTableId: Byte,
        switchState: Boolean,
        deviceIds: List<Byte>
    ): ByteArray {
        if (deviceIds.isEmpty()) {
            throw IllegalArgumentException("At least one device ID required")
        }
        if (deviceIds.size > 255) {
            throw IllegalArgumentException("Too many devices (max 255)")
        }
        
        val stateByte = if (switchState) 0x01.toByte() else 0x00.toByte()
        val command = ByteArray(5 + deviceIds.size)
        
        command[0] = (clientCommandId.toInt() and 0xFF).toByte()
        command[1] = ((clientCommandId.toInt() shr 8) and 0xFF).toByte()
        command[2] = 0x40.toByte()  // CommandType: ActionSwitch (64 = 0x40)
        command[3] = deviceTableId
        command[4] = stateByte
        
        deviceIds.forEachIndexed { index, deviceId ->
            command[5 + index] = deviceId
        }
        
        return command
    }
    
    /**
     * Build ActionDimmable command
     * Wire format matching legacy app HCI capture:
     *   [CmdId_lo][CmdId_hi][CommandType=0x43][DeviceTableId][DeviceId][ModeByte][BrightnessByte][Reserved]
     * 
     * ModeByte: 0x00=Off, 0x01=On/Settings, 0x02=Blink, 0x03=Swell, 0x7F=Restore
     * BrightnessByte: 0-255
     */
    fun buildActionDimmable(
        clientCommandId: UShort,
        deviceTableId: Byte,
        deviceId: Byte,
        brightness: Int,
        mode: Int = -1
    ): ByteArray {
        val b = brightness.coerceIn(0, 255)
        
        // Mode: use explicit mode if provided, otherwise infer from brightness
        val modeByte = if (mode >= 0) {
            mode.coerceIn(0, 127).toByte()
        } else {
            if (b == 0) 0x00.toByte() else 0x01.toByte()
        }
        val brightnessByte = b.toByte()
        val reservedByte = 0x00.toByte()
        
        // 8 bytes total: matching legacy app format
        return byteArrayOf(
            (clientCommandId.toInt() and 0xFF).toByte(),            // CmdId low
            ((clientCommandId.toInt() shr 8) and 0xFF).toByte(),    // CmdId high
            0x43.toByte(),                                           // CommandType: ActionDimmable
            deviceTableId,
            deviceId,
            modeByte,
            brightnessByte,
            reservedByte
        )
    }
    
    /**
     * Build ActionRgb command for RGB light control
     * CommandType: 0x44 (ActionRgb)
     * Format: CmdId_lo, CmdId_hi, 0x44, DeviceTableId, DeviceId, Mode, payload...
     * 
     * Payload varies by mode:
     *   Off(0): no additional bytes
     *   On(1): [R][G][B][AutoOff]
     *   Blink(2): [R][G][B][AutoOff][IntervalHi][IntervalLo]
     *   Transitions(4-8): [AutoOff][IntervalHi][IntervalLo]
     *   Restore(127): no additional bytes
     */
    fun buildActionRgb(
        clientCommandId: UShort,
        deviceTableId: Byte,
        deviceId: Byte,
        mode: Int,
        red: Int = 0,
        green: Int = 0,
        blue: Int = 0,
        autoOff: Int = 0,
        intervalMs: Int = 500
    ): ByteArray {
        val header = byteArrayOf(
            (clientCommandId.toInt() and 0xFF).toByte(),
            ((clientCommandId.toInt() shr 8) and 0xFF).toByte(),
            0x44.toByte(),  // CommandType: ActionRgb
            deviceTableId,
            deviceId
        )
        
        val payload = when (mode) {
            0, 127 -> byteArrayOf(mode.toByte())  // Off or Restore
            1 -> byteArrayOf(  // On (Solid)
                mode.toByte(),
                red.coerceIn(0, 255).toByte(),
                green.coerceIn(0, 255).toByte(),
                blue.coerceIn(0, 255).toByte(),
                autoOff.coerceIn(0, 255).toByte()
            )
            2 -> byteArrayOf(  // Blink
                mode.toByte(),
                red.coerceIn(0, 255).toByte(),
                green.coerceIn(0, 255).toByte(),
                blue.coerceIn(0, 255).toByte(),
                autoOff.coerceIn(0, 255).toByte(),
                ((intervalMs shr 8) and 0xFF).toByte(),
                (intervalMs and 0xFF).toByte()
            )
            else -> byteArrayOf(  // Transition effects (4-8)
                mode.toByte(),
                autoOff.coerceIn(0, 255).toByte(),
                ((intervalMs shr 8) and 0xFF).toByte(),
                (intervalMs and 0xFF).toByte()
            )
        }
        
        return header + payload
    }
    
    /**
     * Build ActionDimmable with full command structure
     * This is a simplified version - full implementation would use LogicalDeviceLightDimmableCommand
     */
    fun buildActionDimmableFull(
        clientCommandId: UShort,
        deviceTableId: Byte,
        deviceId: Byte,
        brightness: Int,
        mode: Byte = 0,
        onDuration: Byte = 0,
        blinkSwellCycleTime1: Byte = 0,
        blinkSwellCycleTime2: Byte = 0
    ): ByteArray {
        // Full command structure (8 bytes minimum for dimmable command)
        val commandData = byteArrayOf(
            brightness.coerceIn(0, 100).toByte(),
            mode,
            onDuration,
            blinkSwellCycleTime1,
            blinkSwellCycleTime2,
            0.toByte(),  // Reserved
            0.toByte(),  // Reserved
            0.toByte()   // Reserved
        )
        
        val command = ByteArray(5 + commandData.size)
        command[0] = (clientCommandId.toInt() and 0xFF).toByte()
        command[1] = ((clientCommandId.toInt() shr 8) and 0xFF).toByte()
        command[2] = 0x43.toByte()  // CommandType: ActionDimmable
        command[3] = deviceTableId
        command[4] = deviceId
        
        commandData.forEachIndexed { index, byte ->
            command[5 + index] = byte
        }
        
        return command
    }
    
    /**
     * Build ActionHvac command
     * Format: [ClientCommandId (2 bytes)][CommandType=0x45][DeviceTableId][DeviceId][Command (3 bytes)]
     * Command payload: [command_byte][low_trip_temp][high_trip_temp]
     * 
     * @param clientCommandId Client command ID (incremented per command)
     * @param deviceTableId Device table ID
     * @param deviceId Device ID (zone ID)
     * @param heatMode Heat mode (0=Off, 1=Heating, 2=Cooling, 3=Both, 4=RunSchedule)
     * @param heatSource Heat source (0=PreferGas, 1=PreferHeatPump, 2=Other, 3=Reserved)
     * @param fanMode Fan mode (0=Auto, 1=High, 2=Low)
     * @param lowTripTempF Heat setpoint (°F, 0-255)
     * @param highTripTempF Cool setpoint (°F, 0-255)
     */
    fun buildActionHvac(
        clientCommandId: UShort,
        deviceTableId: Byte,
        deviceId: Byte,
        heatMode: Int,
        heatSource: Int,
        fanMode: Int,
        lowTripTempF: Int,
        highTripTempF: Int
    ): ByteArray {
        // Pack command byte: HeatMode (bits 0-2), HeatSource (bits 4-5), FanMode (bits 6-7)
        val commandByte = ((heatMode and 0x07) or
                          ((heatSource and 0x03) shl 4) or
                          ((fanMode and 0x03) shl 6)).toByte()
        
        val command = ByteArray(8)
        command[0] = (clientCommandId.toInt() and 0xFF).toByte()
        command[1] = ((clientCommandId.toInt() shr 8) and 0xFF).toByte()
        command[2] = 0x45.toByte()  // CommandType: ActionHvac (69 = 0x45)
        command[3] = deviceTableId
        command[4] = deviceId
        command[5] = commandByte
        command[6] = lowTripTempF.coerceIn(0, 255).toByte()
        command[7] = highTripTempF.coerceIn(0, 255).toByte()
        
        return command
    }
    
    /**
     * Build ActionHBridge (cover/slide/awning) command
     * CommandType: 0x41 (ActionMovement)
     * Format: [ClientCommandId (2 bytes)][CommandType=0x41][DeviceTableId][DeviceId][Command]
     * 
     * Command byte values (HBridgeCommand enum):
     *   0x00 = Stop
     *   0x01 = Forward (retract/close)
     *   0x02 = Reverse (extend/open)
     *   0x03 = ClearUserClearRequiredLatch (clear fault)
     *   0x04 = HomeReset
     *   0x05 = AutoForward
     *   0x06 = AutoReverse
     */
    fun buildActionHBridge(
        clientCommandId: UShort,
        deviceTableId: Byte,
        deviceId: Byte,
        command: Byte
    ): ByteArray {
        return byteArrayOf(
            (clientCommandId.toInt() and 0xFF).toByte(),           // CmdId low
            ((clientCommandId.toInt() shr 8) and 0xFF).toByte(),  // CmdId high
            0x41.toByte(),  // CommandType: ActionMovement (65 = 0x41)
            deviceTableId,
            deviceId,
            command
        )
    }
    
    /**
     * Build GetDevicesMetadata command
     * Format: [ClientCommandId (2 bytes)][CommandType (2)][DeviceTableId (1)][StartDeviceId (1)][MaxDeviceRequestCount (1)]
     */
    fun buildGetDevicesMetadata(clientCommandId: UShort, deviceTableId: Byte, startDeviceId: Byte = 0.toByte(), maxDeviceCount: Byte = (-1).toByte()): ByteArray {
        return byteArrayOf(
            (clientCommandId.toInt() and 0xFF).toByte(),           // ClientCommandId LSB
            ((clientCommandId.toInt() shr 8) and 0xFF).toByte(),  // ClientCommandId MSB
            0x02.toByte(),  // CommandType: GetDevicesMetadata
            deviceTableId,
            startDeviceId,
            maxDeviceCount
        )
    }
    
    /**
     * Build ActionGeneratorGenie command
     * CommandType: 0x42 (ActionGeneratorGenie)
     * Format: [ClientCommandId (2 bytes)][CommandType=0x42][DeviceTableId][DeviceId][RvLinkGeneratorCommand]
     *
     * RvLinkGeneratorCommand: 0x00 = Off, 0x01 = On
     * The gateway handles the state machine (priming → starting → running) internally.
     */
    fun buildActionGeneratorGenie(
        clientCommandId: UShort,
        deviceTableId: Byte,
        deviceId: Byte,
        turnOn: Boolean
    ): ByteArray {
        val commandByte = if (turnOn) 0x01.toByte() else 0x00.toByte()
        return byteArrayOf(
            (clientCommandId.toInt() and 0xFF).toByte(),           // CmdId low
            ((clientCommandId.toInt() shr 8) and 0xFF).toByte(),  // CmdId high
            0x42.toByte(),  // CommandType: ActionGeneratorGenie (66 = 0x42)
            deviceTableId,
            deviceId,
            commandByte
        )
    }

    // H-Bridge command constants
    object HBridgeCommand {
        const val STOP: Byte = 0x00
        const val FORWARD: Byte = 0x01      // Retract/Close
        const val REVERSE: Byte = 0x02      // Extend/Open
        const val CLEAR_FAULT: Byte = 0x03
        const val HOME_RESET: Byte = 0x04
        const val AUTO_FORWARD: Byte = 0x05
        const val AUTO_REVERSE: Byte = 0x06
    }
}

