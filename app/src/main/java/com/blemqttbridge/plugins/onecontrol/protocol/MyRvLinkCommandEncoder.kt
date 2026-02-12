package com.blemqttbridge.plugins.onecontrol.protocol

import android.util.Log

/**
 * MyRvLink Command Encoder
 * Encodes control commands for sending to the gateway
 */
object MyRvLinkCommandEncoder {
    private const val TAG = "MyRvLinkCommandEncoder"
    
    /**
     * Dimmable Light Command types
     */
    enum class DimmableLightCommand(val value: Byte) {
        Off(0),
        On(1),
        Blink(2),
        Swell(3),
        Settings(126),
        Restore(127)
    }
    
    /**
     * RGB Light Mode/Command types
     * From official app's LogicalDeviceLightRGBCommand
     */
    enum class RgbLightMode(val value: Byte) {
        Off(0),
        On(1),        // Solid color
        Blink(2),     // Blink effect
        Jump3(4),     // 3-color jump transition
        Jump7(5),     // 7-color jump transition
        Fade3(6),     // 3-color fade transition
        Fade7(7),     // 7-color fade transition
        Rainbow(8),   // Rainbow cycle
        Restore(127.toByte())  // Restore last settings
    }
    
    /**
     * Encode ActionDimmable command for controlling dimmable lights.
     *
     * Wire format to match the HCI capture (commandId first, then CommandType):
     *   [CmdId_lo][CmdId_hi][CommandType=0x43][DeviceTableId][DeviceId][Value]
     *
     * For the interior light capture, Value was 0x01 (On).
     * Off = 0x00, Restore = 0x7F. For On/Settings we allow a caller-supplied byte.
     */
    fun encodeActionDimmable(
        commandId: UShort,
        deviceTableId: Byte,
        deviceId: Byte,
        command: DimmableLightCommand,
        brightness: Int = 255  // expect 0-255
    ): ByteArray {
        val b = brightness.coerceIn(0, 255)
        val modeByte = when (command) {
            DimmableLightCommand.Off -> 0x00.toByte()
            DimmableLightCommand.Restore -> 0x7F.toByte()
            else -> 0x01.toByte() // On/Settings use 0x01 per capture
        }
        val brightnessByte = b.toByte()
        val reservedByte = 0x00.toByte()

        // CommandId first, then CommandType (0x43), matching the capture payload order.
        val commandBytes = byteArrayOf(
            (commandId.toInt() and 0xFF).toByte(),            // CmdId low
            ((commandId.toInt() shr 8) and 0xFF).toByte(),   // CmdId high
            0x43.toByte(),                                   // CommandType: ActionDimmable
            deviceTableId,
            deviceId,
            modeByte,
            brightnessByte,
            reservedByte
        )
        
        Log.d(TAG, "Encoded ActionDimmable (HCI format): cmdId=0x${commandId.toString(16)}, " +
                   "device=0x${deviceTableId.toString(16)}:${deviceId.toString(16)}, " +
                   "mode=0x${(modeByte.toInt() and 0xFF).toString(16)}, brightness=0x${(brightnessByte.toInt() and 0xFF).toString(16)}, size=8 bytes")
        Log.d(TAG, "Raw command bytes: ${commandBytes.joinToString(" ") { "%02X".format(it) }}")
        
        return commandBytes
    }
    
    /**
     * Encode ActionRgb command for controlling RGB lights.
     *
     * Wire format:
     *   CmdId_lo, CmdId_hi, CommandType=0x44, DeviceTableId, DeviceId, Mode, payload...
     *
     * Payload varies by mode:
     *   Off(0):      [Mode]  (1 byte, no additional payload)
     *   On(1):       [Mode][R][G][B][AutoOff]  (5 bytes)
     *   Blink(2):    [Mode][R][G][B][AutoOff][OnIntervalHi][OnIntervalLo]  (7 bytes - simplified, off interval mirrors on)
     *   Transitions: [Mode][AutoOff][IntervalHi][IntervalLo]  (4 bytes, modes 4-8)
     *   Restore(127):[Mode]  (1 byte)
     */
    fun encodeActionRgb(
        commandId: UShort,
        deviceTableId: Byte,
        deviceId: Byte,
        mode: RgbLightMode,
        red: Int = 0,
        green: Int = 0,
        blue: Int = 0,
        autoOff: Int = 0,
        intervalMs: Int = 500
    ): ByteArray {
        val header = byteArrayOf(
            (commandId.toInt() and 0xFF).toByte(),            // CmdId low
            ((commandId.toInt() shr 8) and 0xFF).toByte(),   // CmdId high
            0x44.toByte(),                                   // CommandType: ActionRgb
            deviceTableId,
            deviceId
        )
        
        val payload = when (mode) {
            RgbLightMode.Off, RgbLightMode.Restore -> byteArrayOf(mode.value)
            RgbLightMode.On -> byteArrayOf(
                mode.value,
                red.coerceIn(0, 255).toByte(),
                green.coerceIn(0, 255).toByte(),
                blue.coerceIn(0, 255).toByte(),
                autoOff.coerceIn(0, 255).toByte()
            )
            RgbLightMode.Blink -> byteArrayOf(
                mode.value,
                red.coerceIn(0, 255).toByte(),
                green.coerceIn(0, 255).toByte(),
                blue.coerceIn(0, 255).toByte(),
                autoOff.coerceIn(0, 255).toByte(),
                ((intervalMs shr 8) and 0xFF).toByte(),      // Interval high
                (intervalMs and 0xFF).toByte()                // Interval low
            )
            // Transition effects (Jump3/Jump7/Fade3/Fade7/Rainbow) — no color, just timing
            else -> byteArrayOf(
                mode.value,
                autoOff.coerceIn(0, 255).toByte(),
                ((intervalMs shr 8) and 0xFF).toByte(),      // Interval high
                (intervalMs and 0xFF).toByte()                // Interval low
            )
        }
        
        val commandBytes = header + payload
        
        Log.d(TAG, "Encoded ActionRgb: cmdId=0x${commandId.toString(16)}, " +
                   "device=0x${deviceTableId.toString(16)}:${deviceId.toString(16)}, " +
                   "mode=${mode.name}, R=$red G=$green B=$blue, size=${commandBytes.size} bytes")
        Log.d(TAG, "Raw command bytes: ${commandBytes.joinToString(" ") { "%02X".format(it) }}")
        
        return commandBytes
    }
    
    /**
     * Encode ActionSwitch command for controlling switches/relays
     * 
     * HCI format: [CommandType=0x40][ClientCommandId (2 bytes LE)][DeviceTableId][DeviceId][SwitchCommand]
     * NOTE: CommandType comes FIRST (matching HCI capture pattern)
     * 
     * @param commandId Client command ID
     * @param deviceTableId Device table ID
     * @param deviceId Device ID
     * @param turnOn true to turn on, false to turn off
     * @return Encoded command bytes
     */
    fun encodeActionSwitch(
        commandId: UShort,
        deviceTableId: Byte,
        deviceId: Byte,
        turnOn: Boolean
    ): ByteArray {
        val switchCommand: Byte = if (turnOn) 1 else 0
        
        // CommandType FIRST (matching HCI capture)
        val commandBytes = ByteArray(6)
        commandBytes[0] = 0x40.toByte()  // ActionSwitch - FIRST!
        commandBytes[1] = (commandId.toInt() and 0xFF).toByte()  // Command ID low byte
        commandBytes[2] = ((commandId.toInt() shr 8) and 0xFF).toByte()  // Command ID high byte
        commandBytes[3] = deviceTableId
        commandBytes[4] = deviceId
        commandBytes[5] = switchCommand
        
        Log.d(TAG, "Encoded ActionSwitch (HCI format): cmdId=0x${commandId.toString(16)}, " +
                   "device=0x${deviceTableId.toString(16)}:${deviceId.toString(16)}, " +
                   "turnOn=$turnOn")
        Log.d(TAG, "Raw command bytes: ${commandBytes.joinToString(" ") { "%02X".format(it) }}")
        
        return commandBytes
    }
}

