package com.onecontrol.blebridge

/**
 * Builds MyRvLink commands for device control
 * Based on decompiled MyRvLinkCommand classes
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
     * Format: [ClientCommandId (2 bytes)][CommandType (1)][DeviceTableId (1)][DeviceId (1)][Command data...]
     * Minimum command data: 1 byte (brightness 0-100)
     */
    fun buildActionDimmable(
        clientCommandId: UShort,
        deviceTableId: Byte,
        deviceId: Byte,
        brightness: Int? = null,
        turnOn: Boolean? = null
    ): ByteArray {
        // Build command data
        // For simple brightness control, we use a single byte (0-100)
        // For more complex commands, we'd need LogicalDeviceLightDimmableCommand structure
        val commandData = mutableListOf<Byte>()
        
        if (brightness != null) {
            // Set brightness (0-100)
            commandData.add(brightness.coerceIn(0, 100).toByte())
        } else if (turnOn != null) {
            // Turn on/off (100 = on, 0 = off)
            commandData.add(if (turnOn) 100.toByte() else 0.toByte())
        } else {
            // Default: turn on at 100%
            commandData.add(100.toByte())
        }
        
        val command = ByteArray(5 + commandData.size)
        command[0] = (clientCommandId.toInt() and 0xFF).toByte()
        command[1] = ((clientCommandId.toInt() shr 8) and 0xFF).toByte()
        command[2] = 0x43.toByte()  // CommandType: ActionDimmable (67 = 0x43)
        command[3] = deviceTableId
        command[4] = deviceId
        
        commandData.forEachIndexed { index, byte ->
            command[5 + index] = byte
        }
        
        return command
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
}

