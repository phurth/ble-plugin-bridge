package com.onecontrol.blebridge

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
        brightness: Int = 100
    ): ByteArray {
        val valueByte = when (command) {
            DimmableLightCommand.Off -> 0x00.toByte()
            DimmableLightCommand.Restore -> 0x7F.toByte()
            DimmableLightCommand.On, DimmableLightCommand.Settings -> brightness.coerceIn(1, 100).toByte()
            else -> brightness.coerceIn(1, 100).toByte()
        }

        // CommandId first, then CommandType (0x43), matching the capture payload order.
        val commandBytes = byteArrayOf(
            (commandId.toInt() and 0xFF).toByte(),            // CmdId low
            ((commandId.toInt() shr 8) and 0xFF).toByte(),   // CmdId high
            0x43.toByte(),                                   // CommandType: ActionDimmable
            deviceTableId,
            deviceId,
            valueByte
        )
        
        Log.d(TAG, "Encoded ActionDimmable (HCI format): cmdId=0x${commandId.toString(16)}, " +
                   "device=0x${deviceTableId.toString(16)}:${deviceId.toString(16)}, " +
                   "value=0x${(valueByte.toInt() and 0xFF).toString(16)}, " +
                   "size=6 bytes")
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

