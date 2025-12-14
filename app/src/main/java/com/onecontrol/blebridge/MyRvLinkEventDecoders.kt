package com.onecontrol.blebridge

import android.util.Log

/**
 * MyRvLink Event Decoders
 * Based on decompiled C# event classes from OneControl.Direct.MyRvLink
 * 
 * Each decoder parses raw bytes from a specific event type into structured data
 */

// ============================================================================
// GatewayInformation Event (0x01)
// ============================================================================

data class GatewayInformationEvent(
    val protocolVersion: Int,
    val options: Int,
    val deviceCount: Int,
    val deviceTableId: Int,
    val deviceTableCrc: UInt,
    val deviceMetadataTableCrc: UInt
) {
    companion object {
        private const val MIN_LENGTH = 13
        
        fun decode(data: ByteArray): GatewayInformationEvent? {
            if (data.size < MIN_LENGTH) return null
            
            val protocolVersion = data[1].toInt() and 0xFF
            val options = data[2].toInt() and 0xFF
            val deviceCount = data[3].toInt() and 0xFF
            val deviceTableId = data[4].toInt() and 0xFF
            val deviceTableCrc = ((data[5].toUInt() and 0xFFu) shl 24) or
                                 ((data[6].toUInt() and 0xFFu) shl 16) or
                                 ((data[7].toUInt() and 0xFFu) shl 8) or
                                 (data[8].toUInt() and 0xFFu)
            val deviceMetadataTableCrc = ((data[9].toUInt() and 0xFFu) shl 24) or
                                        ((data[10].toUInt() and 0xFFu) shl 16) or
                                        ((data[11].toUInt() and 0xFFu) shl 8) or
                                        (data[12].toUInt() and 0xFFu)
            
            return GatewayInformationEvent(
                protocolVersion,
                options,
                deviceCount,
                deviceTableId,
                deviceTableCrc,
                deviceMetadataTableCrc
            )
        }
    }
    
    override fun toString(): String {
        return "GatewayInfo(v$protocolVersion, devices=$deviceCount, tableId=0x${deviceTableId.toString(16).padStart(2, '0')}, " +
               "tableCrc=0x${deviceTableCrc.toString(16).uppercase()}, metaCrc=0x${deviceMetadataTableCrc.toString(16).uppercase()})"
    }
}

// ============================================================================
// RvStatus Event (0x07) - System voltage and temperature
// ============================================================================

data class RvStatusEvent(
    val batteryVoltage: Float?,
    val externalTemperatureCelsius: Float?,
    val voltageAvailable: Boolean,
    val temperatureAvailable: Boolean
) {
    companion object {
        private const val MIN_LENGTH = 6
        private const val INVALID_VOLTAGE = 0xFFFF
        private const val INVALID_TEMPERATURE = 0x7FFF
        
        fun decode(data: ByteArray): RvStatusEvent? {
            if (data.size < MIN_LENGTH) return null
            
            // Voltage: Unsigned 8.8 fixed point, big-endian
            val voltageRaw = ((data[1].toInt() and 0xFF) shl 8) or (data[2].toInt() and 0xFF)
            val voltage = if (voltageRaw == INVALID_VOLTAGE) null else voltageRaw / 256.0f
            
            // Temperature: Signed 8.8 fixed point, big-endian
            val tempRaw = ((data[3].toInt() and 0xFF) shl 8) or (data[4].toInt() and 0xFF)
            val temperature = if (tempRaw == INVALID_TEMPERATURE) null else {
                // Handle signed value
                val signed = if (tempRaw and 0x8000 != 0) tempRaw - 0x10000 else tempRaw
                signed / 256.0f
            }
            
            // Feature flags
            val features = data[5].toInt() and 0xFF
            val voltageAvailable = (features and 0x01) != 0
            val temperatureAvailable = (features and 0x02) != 0
            
            return RvStatusEvent(voltage, temperature, voltageAvailable, temperatureAvailable)
        }
    }
    
    override fun toString(): String {
        val voltStr = if (batteryVoltage != null) String.format("%.2fV", batteryVoltage) else "N/A"
        val tempStr = if (externalTemperatureCelsius != null) String.format("%.1f°C", externalTemperatureCelsius) else "N/A"
        return "RvStatus(voltage=$voltStr, temp=$tempStr)"
    }
}

// ============================================================================
// DimmableLightStatus Event (0x08)
// ============================================================================

data class DimmableLightStatusEvent(
    val deviceAddress: Int,
    val brightness: Int,  // 0-255 raw, converted to 0-100%
    val isOn: Boolean,
    val rawStatus: Int
) {
    companion object {
        private const val MIN_LENGTH = 5
        
        fun decode(data: ByteArray): DimmableLightStatusEvent? {
            if (data.size < MIN_LENGTH) return null
            
            // Device address: [DeviceTableId][DeviceId] - big-endian format
            // data[1] = DeviceTableId (0x08)
            // data[2] = DeviceId (0x09 or 0x0A)
            // Combined as 0x0809 or 0x080A
            val deviceAddress = ((data[1].toInt() and 0xFF) shl 8) or (data[2].toInt() and 0xFF)
            
            // Mode byte (Data[0] in C#): data[3] - indicates ON (>0) or OFF (0)
            val mode = data[3].toInt() and 0xFF
            
            // Brightness byte (Data[1] in C#): data[4] - raw 0-255
            val brightnessRaw = if (data.size > 4) data[4].toInt() and 0xFF else 0
            
            // Convert brightness to percentage (0-100)
            val brightness = (brightnessRaw * 100) / 255
            
            // Light is ON if mode > 0 (per decompiled code: On = Data[0] > 0)
            val isOn = mode > 0
            
            return DimmableLightStatusEvent(deviceAddress, brightness, isOn, mode)
        }
    }
    
    override fun toString(): String {
        return "DimmableLight(addr=0x${deviceAddress.toString(16).padStart(4, '0')}, " +
               "brightness=$brightness%, ${if (isOn) "ON" else "OFF"})"
    }
}

// ============================================================================
// RelayHBridgeMomentaryStatusType2 Event (0x0E) - Motors, slides, awnings
// ============================================================================

data class RelayHBridgeStatusEvent(
    val deviceAddress: Int,
    val position: Int,
    val status: Int,
    val rawData: ByteArray
) {
    companion object {
        private const val MIN_LENGTH = 5
        
        fun decode(data: ByteArray): RelayHBridgeStatusEvent? {
            if (data.size < MIN_LENGTH) return null
            
            // Device address: [DeviceTableId][DeviceId] - big-endian format
            val deviceAddress = ((data[1].toInt() and 0xFF) shl 8) or (data[2].toInt() and 0xFF)
            
            // Status and position
            val status = data[3].toInt() and 0xFF
            val position = if (data.size > 4) data[4].toInt() and 0xFF else 0
            
            return RelayHBridgeStatusEvent(deviceAddress, position, status, data)
        }
    }
    
    override fun toString(): String {
        return "HBridgeRelay(addr=0x${deviceAddress.toString(16).padStart(4, '0')}, " +
               "pos=$position, status=0x${status.toString(16)})"
    }
    
    override fun equals(other: Any?): Boolean {
        if (this === other) return true
        if (javaClass != other?.javaClass) return false
        other as RelayHBridgeStatusEvent
        if (deviceAddress != other.deviceAddress) return false
        if (position != other.position) return false
        if (status != other.status) return false
        if (!rawData.contentEquals(other.rawData)) return false
        return true
    }
    
    override fun hashCode(): Int {
        var result = deviceAddress
        result = 31 * result + position
        result = 31 * result + status
        result = 31 * result + rawData.contentHashCode()
        return result
    }
}

// ============================================================================
// RelayBasicLatchingStatusType2 Event (0x06) - Simple ON/OFF relays
// ============================================================================

data class RelayBasicLatchingStatusEvent(
    val deviceAddress: Int,
    val isOn: Boolean,
    val status: Int
) {
    companion object {
        private const val MIN_LENGTH = 5
        
        fun decode(data: ByteArray): RelayBasicLatchingStatusEvent? {
            if (data.size < MIN_LENGTH) return null
            
            // Device address: [DeviceTableId][DeviceId] - big-endian format
            val deviceAddress = ((data[1].toInt() and 0xFF) shl 8) or (data[2].toInt() and 0xFF)
            
            // Status byte (this is Data[0] in LogicalDeviceRelayStatusType2)
            val status = data[3].toInt() and 0xFF
            
            // RawOutputState is lower 4 bits: (byte)(Data[0] & 0xFu)
            // RelayBasicOutputState.On = 1, RelayBasicOutputState.Off = 0
            val rawOutputState = status and 0x0F
            val isOn = rawOutputState == 1
            
            return RelayBasicLatchingStatusEvent(deviceAddress, isOn, status)
        }
    }
    
    override fun toString(): String {
        return "LatchingRelay(addr=0x${deviceAddress.toString(16).padStart(4, '0')}, ${if (isOn) "ON" else "OFF"})"
    }
}

// ============================================================================
// DeviceOnlineStatus Event (0x03)
// ============================================================================

data class DeviceOnlineStatusEvent(
    val deviceAddress: Int,
    val isOnline: Boolean,
    val rawStatus: Int
) {
    companion object {
        private const val MIN_LENGTH = 5
        
        fun decode(data: ByteArray): DeviceOnlineStatusEvent? {
            if (data.size < MIN_LENGTH) return null
            
            // Device address: [DeviceTableId][DeviceId] - big-endian format
            val deviceAddress = ((data[1].toInt() and 0xFF) shl 8) or (data[2].toInt() and 0xFF)
            
            // Status byte
            val status = data[3].toInt() and 0xFF
            
            // Online if status is not 0xFF (typical "offline" marker)
            val isOnline = status != 0xFF
            
            return DeviceOnlineStatusEvent(deviceAddress, isOnline, status)
        }
    }
    
    override fun toString(): String {
        return "DeviceOnline(addr=0x${deviceAddress.toString(16).padStart(4, '0')}, ${if (isOnline) "ONLINE" else "OFFLINE"})"
    }
}

// ============================================================================
// DeviceLockStatus Event (0x04)
// ============================================================================

data class DeviceLockStatusEvent(
    val deviceAddress: Int,
    val isLocked: Boolean,
    val rawStatus: Int
) {
    companion object {
        private const val MIN_LENGTH = 5
        
        fun decode(data: ByteArray): DeviceLockStatusEvent? {
            if (data.size < MIN_LENGTH) return null
            
            // Device address: [DeviceTableId][DeviceId] - big-endian format
            val deviceAddress = ((data[1].toInt() and 0xFF) shl 8) or (data[2].toInt() and 0xFF)
            
            // Status byte
            val status = data[3].toInt() and 0xFF
            
            // Locked if bit is set (exact bit TBD)
            val isLocked = (status and 0x01) != 0
            
            return DeviceLockStatusEvent(deviceAddress, isLocked, status)
        }
    }
    
    override fun toString(): String {
        return "DeviceLock(addr=0x${deviceAddress.toString(16).padStart(4, '0')}, ${if (isLocked) "LOCKED" else "UNLOCKED"})"
    }
}

// ============================================================================
// RealTimeClock Event (0x20)
// ============================================================================

data class RealTimeClockEvent(
    val year: Int,
    val month: Int,
    val day: Int,
    val hour: Int,
    val minute: Int,
    val second: Int
) {
    companion object {
        private const val MIN_LENGTH = 7
        
        fun decode(data: ByteArray): RealTimeClockEvent? {
            if (data.size < MIN_LENGTH) return null
            
            // Typical RTC format (needs verification from actual data)
            val year = 2000 + (data[1].toInt() and 0xFF)
            val month = data[2].toInt() and 0xFF
            val day = data[3].toInt() and 0xFF
            val hour = data[4].toInt() and 0xFF
            val minute = data[5].toInt() and 0xFF
            val second = data[6].toInt() and 0xFF
            
            return RealTimeClockEvent(year, month, day, hour, minute, second)
        }
    }
    
    override fun toString(): String {
        return "RTC($year-${month.toString().padStart(2, '0')}-${day.toString().padStart(2, '0')} " +
               "${hour.toString().padStart(2, '0')}:${minute.toString().padStart(2, '0')}:${second.toString().padStart(2, '0')})"
    }
}

// ============================================================================
// Tank Sensor Status Events
// ============================================================================

/**
 * Tank Sensor Status (V1) - Multiple tanks per event
 * Format: [EventType=0x0C][DeviceTableId][DeviceId1][Percent1][DeviceId2][Percent2]...
 * 2 bytes per tank: DeviceId + Percent (0-100)
 */
data class TankSensorStatusEvent(
    val deviceTableId: Byte,
    val tanks: List<TankLevel>
) {
    data class TankLevel(
        val deviceId: Byte,
        val percent: Int  // 0-100
    )
    
    companion object {
        fun decode(data: ByteArray): TankSensorStatusEvent? {
            if (data.size < 3) return null
            if (data[0].toInt() and 0xFF != 0x0C) return null  // TankSensorStatus
            
            val deviceTableId = data[1]
            val tanks = mutableListOf<TankLevel>()
            
            // Parse tank data: pairs of [DeviceId, Percent]
            var index = 2
            while (index + 1 < data.size) {
                val deviceId = data[index]
                val percent = data[index + 1].toInt() and 0xFF
                tanks.add(TankLevel(deviceId, percent))
                index += 2
            }
            
            return TankSensorStatusEvent(deviceTableId, tanks)
        }
    }
    
    override fun toString(): String {
        return "TankSensorStatus(tableId=$deviceTableId, tanks=${tanks.size})"
    }
}

/**
 * Tank Sensor Status (V2) - Single tank per event
 * Format: [EventType=0x1B][DeviceTableId][DeviceId][Status bytes (1 or 8 bytes)]
 * Status can be 1 byte (simple percent) or 8 bytes (extended)
 */
data class TankSensorStatusV2Event(
    val deviceTableId: Byte,
    val deviceId: Byte,
    val percent: Int?  // Extracted from status bytes if available
) {
    companion object {
        fun decode(data: ByteArray): TankSensorStatusV2Event? {
            if (data.size < 4) return null
            if (data[0].toInt() and 0xFF != 0x1B) return null  // TankSensorStatusV2
            
            val deviceTableId = data[1]
            val deviceId = data[2]
            
            // Status can be 1 byte (percent) or 8 bytes (extended)
            val percent = when (data.size) {
                4 -> data[3].toInt() and 0xFF  // Simple 1-byte percent
                11 -> data[3].toInt() and 0xFF  // Extended format, first byte is percent
                else -> null
            }
            
            return TankSensorStatusV2Event(deviceTableId, deviceId, percent)
        }
    }
    
    override fun toString(): String {
        return "TankSensorStatusV2(tableId=$deviceTableId, deviceId=$deviceId, percent=$percent%)"
    }
}

// ============================================================================
// HvacStatus Event (0x0B)
// ============================================================================

/**
 * HVAC Status Event - Multiple zones per event
 * Format: [EventType=0x0B][DeviceTableId][DeviceId1][8-byte status1][2-byte extended1][DeviceId2][8-byte status2][2-byte extended2]...
 * 11 bytes per zone: DeviceId + 8-byte status + 2-byte extended
 */
data class HvacStatusEvent(
    val deviceTableId: Byte,
    val zones: List<HvacZoneStatus>
) {
    data class HvacZoneStatus(
        val deviceId: Byte,
        val commandByte: Byte,  // HeatMode, HeatSource, FanMode bitfields
        val lowTripTempF: Int,  // Heat setpoint (°F)
        val highTripTempF: Int, // Cool setpoint (°F)
        val zoneStatus: Byte,   // Zone mode + failed thermistor flag
        val indoorTempF: Float?, // Indoor temperature (°F) or null if invalid
        val outdoorTempF: Float?, // Outdoor temperature (°F) or null if invalid
        val dtc: Int  // Diagnostic Trouble Code (ushort)
    ) {
        // Decode command byte bitfields
        val heatMode: Int get() = commandByte.toInt() and 0x07  // bits 0-2
        val heatSource: Int get() = (commandByte.toInt() shr 4) and 0x03  // bits 4-5
        val fanMode: Int get() = (commandByte.toInt() shr 6) and 0x03  // bits 6-7
        val zoneMode: Int get() = zoneStatus.toInt() and 0x8F  // bits 0-6 (mask 0x8F)
        val failedThermistor: Boolean get() = (zoneStatus.toInt() and 0x80) != 0
    }

    companion object {
        private const val MIN_LENGTH = 13  // EventType (1) + DeviceTableId (1) + DeviceId (1) + Status (8) + Extended (2)
        private const val BYTES_PER_ZONE = 11  // DeviceId (1) + Status (8) + Extended (2)
        private val INVALID_TEMP_SENTINELS = setOf(0x8000, 0x2FF0)

        fun decode(data: ByteArray): HvacStatusEvent? {
            if (data.size < MIN_LENGTH) return null
            if (data[0].toInt() and 0xFF != 0x0B) return null  // HvacStatus

            val deviceTableId = data[1]
            val zones = mutableListOf<HvacZoneStatus>()

            // Parse zones: each zone is 11 bytes (DeviceId + 8-byte status + 2-byte extended)
            var index = 2  // Start after EventType and DeviceTableId
            while (index + BYTES_PER_ZONE <= data.size) {
                val deviceId = data[index]
                val commandByte = data[index + 1]  // First byte of 8-byte status
                val lowTripTempF = data[index + 2].toInt() and 0xFF
                val highTripTempF = data[index + 3].toInt() and 0xFF
                val zoneStatus = data[index + 4]
                
                // Indoor temp: signed 8.8 fixed point, big-endian (bytes 5-6 of status)
                val indoorTempRaw = ((data[index + 5].toInt() and 0xFF) shl 8) or (data[index + 6].toInt() and 0xFF)
                val indoorTempF = decodeTemperature(indoorTempRaw)
                
                // Outdoor temp: signed 8.8 fixed point, big-endian (bytes 7-8 of status)
                val outdoorTempRaw = ((data[index + 7].toInt() and 0xFF) shl 8) or (data[index + 8].toInt() and 0xFF)
                val outdoorTempF = decodeTemperature(outdoorTempRaw)
                
                // DTC: ushort, big-endian (2-byte extended)
                val dtc = ((data[index + 9].toInt() and 0xFF) shl 8) or (data[index + 10].toInt() and 0xFF)

                zones.add(HvacZoneStatus(
                    deviceId = deviceId,
                    commandByte = commandByte,
                    lowTripTempF = lowTripTempF,
                    highTripTempF = highTripTempF,
                    zoneStatus = zoneStatus,
                    indoorTempF = indoorTempF,
                    outdoorTempF = outdoorTempF,
                    dtc = dtc
                ))

                index += BYTES_PER_ZONE
            }

            return if (zones.isNotEmpty()) HvacStatusEvent(deviceTableId, zones) else null
        }

        private fun decodeTemperature(raw: Int): Float? {
            if (raw in INVALID_TEMP_SENTINELS) return null
            // Signed 8.8 fixed point: if bit 15 is set, it's negative
            val signed = if (raw and 0x8000 != 0) raw - 0x10000 else raw
            return signed / 256.0f
        }
    }

    override fun toString(): String {
        return "HvacStatus(tableId=$deviceTableId, zones=${zones.size})"
    }
}

// ============================================================================
// Device State Tracker
// ============================================================================

/**
 * Device metadata (from GetDevices and GetDevicesMetadata commands)
 */
data class DeviceMetadata(
    val deviceTableId: Byte,
    val deviceId: Byte,
    val deviceAddress: Int,
    val functionName: Int? = null,
    val functionInstance: Int? = null,
    val friendlyName: String? = null,
    val mqttTopic: String? = null
)

/**
 * Tracks the current state of all devices
 */
class DeviceStateTracker {
    private val TAG = "DeviceStateTracker"
    
    var gatewayInfo: GatewayInformationEvent? = null
    var rvStatus: RvStatusEvent? = null
    
    private val dimmableLights = mutableMapOf<Int, DimmableLightStatusEvent>()
    private val hBridgeRelays = mutableMapOf<Int, RelayHBridgeStatusEvent>()
    private val latchingRelays = mutableMapOf<Int, RelayBasicLatchingStatusEvent>()
    private val deviceOnlineStatus = mutableMapOf<Int, DeviceOnlineStatusEvent>()
    private val deviceLockStatus = mutableMapOf<Int, DeviceLockStatusEvent>()
    
    // Device metadata from GetDevicesMetadata
    private val deviceMetadata = mutableMapOf<Int, DeviceMetadata>()
    
    var rtc: RealTimeClockEvent? = null
    
    /**
     * Process an incoming event and update device states
     */
    fun processEvent(eventType: MyRvLinkEventType, data: ByteArray) {
        when (eventType) {
            MyRvLinkEventType.GatewayInformation -> {
                gatewayInfo = GatewayInformationEvent.decode(data)
                gatewayInfo?.let {
                    Log.i(TAG, "📊 Gateway: $it")
                }
            }
            
            MyRvLinkEventType.RvStatus -> {
                rvStatus = RvStatusEvent.decode(data)
                rvStatus?.let {
                    Log.i(TAG, "🔋 $it")
                }
            }
            
            MyRvLinkEventType.DimmableLightStatus -> {
                DimmableLightStatusEvent.decode(data)?.let {
                    dimmableLights[it.deviceAddress] = it
                    Log.d(TAG, "💡 $it")
                }
            }
            
            MyRvLinkEventType.RelayHBridgeMomentaryStatusType2 -> {
                RelayHBridgeStatusEvent.decode(data)?.let {
                    hBridgeRelays[it.deviceAddress] = it
                    Log.d(TAG, "🔌 $it")
                }
            }
            
            MyRvLinkEventType.RelayBasicLatchingStatusType2 -> {
                RelayBasicLatchingStatusEvent.decode(data)?.let {
                    latchingRelays[it.deviceAddress] = it
                    Log.d(TAG, "⚡ $it")
                }
            }
            
            MyRvLinkEventType.DeviceOnlineStatus -> {
                DeviceOnlineStatusEvent.decode(data)?.let {
                    deviceOnlineStatus[it.deviceAddress] = it
                    Log.d(TAG, "📡 $it")
                }
            }
            
            MyRvLinkEventType.DeviceLockStatus -> {
                DeviceLockStatusEvent.decode(data)?.let {
                    deviceLockStatus[it.deviceAddress] = it
                    Log.d(TAG, "🔒 $it")
                }
            }
            
            MyRvLinkEventType.RealTimeClock -> {
                rtc = RealTimeClockEvent.decode(data)
                rtc?.let {
                    Log.i(TAG, "⏰ $it")
                }
            }
            
            else -> {
                Log.d(TAG, "❓ Unhandled event type: $eventType")
            }
        }
    }
    
    /**
     * Store or update device metadata (from GetDevicesMetadata command)
     */
    fun updateDeviceMetadata(
        deviceTableId: Byte,
        deviceId: Byte,
        functionName: Int?,
        functionInstance: Int?
    ) {
        val deviceAddress = ((deviceId.toInt() and 0xFF) or ((deviceTableId.toInt() and 0xFF) shl 8))
        
        val friendlyName = if (functionName != null && functionInstance != null) {
            FunctionNameMapper.getFriendlyName(functionName, functionInstance)
        } else {
            null
        }
        
        val mqttTopic = friendlyName?.let { FunctionNameMapper.toMqttTopic(it) }
        
        deviceMetadata[deviceAddress] = DeviceMetadata(
            deviceTableId = deviceTableId,
            deviceId = deviceId,
            deviceAddress = deviceAddress,
            functionName = functionName,
            functionInstance = functionInstance,
            friendlyName = friendlyName,
            mqttTopic = mqttTopic
        )
        
        Log.i(TAG, "📝 Device metadata updated: addr=0x${deviceAddress.toString(16).padStart(4, '0')}, " +
                   "name=$friendlyName, topic=$mqttTopic")
    }
    
    /**
     * Get friendly name for a device address
     */
    fun getDeviceFriendlyName(address: Int): String? {
        return deviceMetadata[address]?.friendlyName
    }
    
    /**
     * Get MQTT topic for a device address
     */
    fun getDeviceMqttTopic(address: Int): String? {
        return deviceMetadata[address]?.mqttTopic
    }
    
    /**
     * Get all known devices
     */
    fun getAllDevices(): Map<Int, String> {
        val devices = mutableMapOf<Int, String>()
        
        dimmableLights.forEach { (addr, _) -> devices[addr] = "dimmable_light" }
        hBridgeRelays.forEach { (addr, _) -> devices[addr] = "hbridge_relay" }
        latchingRelays.forEach { (addr, _) -> devices[addr] = "latching_relay" }
        
        return devices
    }
    
    /**
     * Get device state for MQTT publishing
     */
    fun getDeviceState(address: Int): Map<String, Any>? {
        dimmableLights[address]?.let {
            return mapOf(
                "type" to "dimmable_light",
                "address" to "0x${address.toString(16).padStart(4, '0')}",
                "state" to if (it.isOn) "ON" else "OFF",
                "brightness" to it.brightness
            )
        }
        
        hBridgeRelays[address]?.let {
            return mapOf(
                "type" to "hbridge_relay",
                "address" to "0x${address.toString(16).padStart(4, '0')}",
                "position" to it.position,
                "status" to it.status
            )
        }
        
        latchingRelays[address]?.let {
            return mapOf(
                "type" to "latching_relay",
                "address" to "0x${address.toString(16).padStart(4, '0')}",
                "state" to if (it.isOn) "ON" else "OFF"
            )
        }
        
        return null
    }
    
    /**
     * Get system state for MQTT publishing
     */
    fun getSystemState(): Map<String, Any> {
        val state = mutableMapOf<String, Any>()
        
        gatewayInfo?.let {
            state["gateway_version"] = it.protocolVersion
            state["device_count"] = it.deviceCount
            state["device_table_id"] = "0x${it.deviceTableId.toString(16)}"
        }
        
        rvStatus?.let {
            it.batteryVoltage?.let { v -> state["battery_voltage"] = v }
            it.externalTemperatureCelsius?.let { t -> state["external_temperature"] = t }
        }
        
        rtc?.let {
            state["datetime"] = "$it"
        }
        
        state["device_addresses"] = getAllDevices().keys.map { "0x${it.toString(16).padStart(4, '0')}" }
        
        return state
    }
}

