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
     * Build ActionDimmable command per official OneControl app format.
     *
     * Official LogicalDeviceLightDimmableCommand payload (after 5-byte header):
     *   [Command][MaxBrightness][Duration(min)][CT1_hi][CT1_lo][CT2_hi][CT2_lo][Reserved]
     *
     * Minimum lengths per mode:
     *   Off(0), Restore(0x7F): 1 byte  (command only) → 6 total
     *   On(1), Settings(0x7E): 3 bytes (command + brightness + duration) → 8 total
     *   Blink(2), Swell(3):   7 bytes (+ 4 cycle time bytes) → 12 total
     *
     * This method builds On/Off/Restore format (no cycle times).
     * For Blink/Swell, use buildActionDimmableEffect().
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
        val modeInt = modeByte.toInt() and 0xFF
        
        val header = byteArrayOf(
            (clientCommandId.toInt() and 0xFF).toByte(),            // CmdId low
            ((clientCommandId.toInt() shr 8) and 0xFF).toByte(),    // CmdId high
            0x43.toByte(),                                           // CommandType: ActionDimmable
            deviceTableId,
            deviceId
        )
        
        // Official app variable-length payload per mode
        val payload = when (modeInt) {
            0, 0x7F -> byteArrayOf(modeByte)  // Off / Restore: just command byte
            else -> byteArrayOf(               // On / Settings: command + brightness + duration
                modeByte,
                b.toByte(),
                0x00.toByte()  // Duration = 0 (no auto-off)
            )
        }
        
        return header + payload
    }
    
    /**
     * Build ActionRgb command for RGB light control per official OneControl app.
     * CommandType: 0x44 (ActionRgb)
     *
     * Wire: CmdId_lo, CmdId_hi, 0x44, DeviceTableId, DeviceId, DataMinimum...
     *
     * DataMinimum varies by mode:
     *   Off(0):           0x00                                      (6 bytes total)
     *   On(1):            0x01, R, G, B, AutoOff                    (10 bytes total)
     *   Blink(2):         0x02, R, G, B, AutoOff, OnIntv, OffIntv   (12 bytes total)
     *   Transitions(4-8): Mode, AutoOff, IntvHi, IntvLo             (9 bytes total)
     *   Restore(127):     0x7F                                      (6 bytes total)
     *
     * IMPORTANT: Blink uses TWO SEPARATE single-byte intervals (on/off),
     * NOT a big-endian uint16. Transition modes (4-8) use big-endian uint16.
     *
     * @param autoOff Auto-off timer in minutes. 0xFF (255) = disabled (default).
     * @param blinkOnInterval Blink on-time (single byte). Official: Slow=207, Medium=87, Fast=15.
     * @param blinkOffInterval Blink off-time (single byte). Usually equal to onInterval.
     * @param transitionIntervalMs Transition interval (uint16 ms). Official: Slow=750, Medium=500, Fast=250.
     */
    fun buildActionRgb(
        clientCommandId: UShort,
        deviceTableId: Byte,
        deviceId: Byte,
        mode: Int,
        red: Int = 0,
        green: Int = 0,
        blue: Int = 0,
        autoOff: Int = 0xFF,
        blinkOnInterval: Int = 207,
        blinkOffInterval: Int = 207,
        transitionIntervalMs: Int = 750
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
            2 -> byteArrayOf(  // Blink: two separate single-byte intervals
                mode.toByte(),
                red.coerceIn(0, 255).toByte(),
                green.coerceIn(0, 255).toByte(),
                blue.coerceIn(0, 255).toByte(),
                autoOff.coerceIn(0, 255).toByte(),
                blinkOnInterval.coerceIn(0, 255).toByte(),   // OnInterval (single byte)
                blinkOffInterval.coerceIn(0, 255).toByte()   // OffInterval (single byte)
            )
            else -> byteArrayOf(  // Transition effects (4-8): uint16 interval
                mode.toByte(),
                autoOff.coerceIn(0, 255).toByte(),
                ((transitionIntervalMs shr 8) and 0xFF).toByte(),  // Interval MSB
                (transitionIntervalMs and 0xFF).toByte()           // Interval LSB
            )
        }
        
        return header + payload
    }
    
    /**
     * Build ActionDimmable for Blink/Swell effects per official OneControl app.
     *
     * Wire format (12 bytes):
     *   [CmdId_lo][CmdId_hi][0x43][TableId][DeviceId]
     *   [Command][MaxBrightness][Duration(min)][CT1_hi][CT1_lo][CT2_hi][CT2_lo]
     *
     * CycleTime values are big-endian uint16 in milliseconds.
     *   Blink: CT1 = on duration, CT2 = off duration
     *   Swell: CT1 = ramp-up duration, CT2 = ramp-down duration
     *
     * Official app defaults both to 220ms if either is 0.
     * Speed presets: Fast=220ms, Medium=1055ms, Slow=2447ms.
     *
     * @param cycleTime1Ms CT1 in milliseconds (big-endian uint16)
     * @param cycleTime2Ms CT2 in milliseconds (big-endian uint16)
     * @param durationMin Auto-off timer in minutes (0 = no auto-off)
     */
    fun buildActionDimmableEffect(
        clientCommandId: UShort,
        deviceTableId: Byte,
        deviceId: Byte,
        mode: Int,
        brightness: Int,
        cycleTime1Ms: Int = 220,
        cycleTime2Ms: Int = 220,
        durationMin: Int = 0
    ): ByteArray {
        val b = brightness.coerceIn(0, 255)
        // Force default 220ms if either cycle time is 0, matching official app
        val ct1 = if (cycleTime1Ms <= 0 || cycleTime2Ms <= 0) 220 else cycleTime1Ms.coerceIn(1, 65535)
        val ct2 = if (cycleTime1Ms <= 0 || cycleTime2Ms <= 0) 220 else cycleTime2Ms.coerceIn(1, 65535)
        
        return byteArrayOf(
            (clientCommandId.toInt() and 0xFF).toByte(),            // [0] CmdId low
            ((clientCommandId.toInt() shr 8) and 0xFF).toByte(),    // [1] CmdId high
            0x43.toByte(),                                           // [2] CommandType: ActionDimmable
            deviceTableId,                                           // [3] DeviceTableId
            deviceId,                                                // [4] DeviceId
            mode.coerceIn(2, 3).toByte(),                            // [5] Command: Blink(2) or Swell(3)
            b.toByte(),                                              // [6] MaxBrightness 0-255
            durationMin.coerceIn(0, 255).toByte(),                   // [7] Duration (auto-off minutes, 0=infinite)
            ((ct1 shr 8) and 0xFF).toByte(),                         // [8] CycleTime1 MSB
            (ct1 and 0xFF).toByte(),                                 // [9] CycleTime1 LSB
            ((ct2 shr 8) and 0xFF).toByte(),                         // [10] CycleTime2 MSB
            (ct2 and 0xFF).toByte()                                  // [11] CycleTime2 LSB
        )
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

