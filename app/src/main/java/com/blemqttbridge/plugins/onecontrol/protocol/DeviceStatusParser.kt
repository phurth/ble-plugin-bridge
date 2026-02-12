package com.blemqttbridge.plugins.onecontrol.protocol

import android.util.Log

/**
 * Parses device status events from MyRvLink protocol
 */
object DeviceStatusParser {
    private const val TAG = "DeviceStatusParser"
    
    /**
     * Parse RelayBasicLatchingStatusType1 event
     * Format: [EventType (1)][DeviceTableId (1)][DeviceId (1)][State (1)]...
     * BytesPerDevice = 2
     */
    fun parseRelayBasicLatchingStatusType1(data: ByteArray): List<RelayStatus> {
        if (data.size < 2) {
            Log.w(TAG, "RelayBasicLatchingStatusType1 too short: ${data.size} bytes")
            return emptyList()
        }
        
        val deviceTableId = data[1]
        val statuses = mutableListOf<RelayStatus>()
        
        // Each device is 2 bytes: DeviceId (1) + State (1)
        var offset = 2
        while (offset + 1 < data.size) {
            val deviceId = data[offset]
            val state = data[offset + 1]
            statuses.add(RelayStatus(deviceTableId, deviceId, state))
            offset += 2
        }
        
        return statuses
    }
    
    /**
     * Parse DimmableLightStatus event
     * Format: [EventType (1)][DeviceTableId (1)][DeviceId (1)][Status (8 bytes)]...
     * BytesPerDevice = 9
     */
    fun parseDimmableLightStatus(data: ByteArray): List<DimmableLightStatus> {
        if (data.size < 2) {
            Log.w(TAG, "DimmableLightStatus too short: ${data.size} bytes")
            return emptyList()
        }
        
        val deviceTableId = data[1]
        val statuses = mutableListOf<DimmableLightStatus>()
        
        // Each device is 9 bytes: DeviceId (1) + Status (8 bytes)
        var offset = 2
        while (offset + 8 < data.size) {
            val deviceId = data[offset]
            val statusBytes = data.sliceArray(offset + 1 until offset + 9)
            statuses.add(DimmableLightStatus(deviceTableId, deviceId, statusBytes))
            offset += 9
        }
        
        return statuses
    }
    
    /**
     * Parse RgbLightStatus event
     * Format: [EventType (1)][DeviceTableId (1)][DeviceId (1)][Status (8 bytes)]...
     * BytesPerDevice = 9
     */
    fun parseRgbLightStatus(data: ByteArray): List<RgbLightStatus> {
        if (data.size < 2) {
            Log.w(TAG, "RgbLightStatus too short: ${data.size} bytes")
            return emptyList()
        }
        
        val deviceTableId = data[1]
        val statuses = mutableListOf<RgbLightStatus>()
        
        // Each device is 9 bytes: DeviceId (1) + Status (8 bytes)
        var offset = 2
        while (offset + 8 < data.size) {
            val deviceId = data[offset]
            val statusBytes = data.sliceArray(offset + 1 until offset + 9)
            statuses.add(RgbLightStatus(deviceTableId, deviceId, statusBytes))
            offset += 9
        }
        
        return statuses
    }
    
    /**
     * Extract brightness from dimmable light status (8 bytes)
     * Based on LogicalDeviceLightDimmableStatus structure:
     * - Data[0] = Mode (LightModeByteIndex)
     * - Data[1] = MaxBrightness
     * - Data[2] = Duration
     * - Data[3] = Brightness (BrightnessByteIndex) - THIS IS THE ACTUAL BRIGHTNESS
     */
    fun extractBrightness(statusBytes: ByteArray): Int? {
        if (statusBytes.size < 4) return null
        // Brightness is at index 3 (Data[3])
        return (statusBytes[3].toInt() and 0xFF)
    }
    
    /**
     * Extract on/off state from dimmable light status
     * Based on LogicalDeviceLightDimmableStatus.On property:
     * - On = Data[0] > 0 (Mode byte determines if light is on)
     */
    fun extractOnOffState(statusBytes: ByteArray): Boolean? {
        if (statusBytes.size < 1) return null
        // On is determined by Mode byte (Data[0]) > 0
        val mode = statusBytes[0].toInt() and 0xFF
        return mode > 0
    }
    
    /**
     * Extract on/off state from relay status (1 byte)
     */
    fun extractRelayState(state: Byte): Boolean {
        // State byte: bit 0 = on/off
        return (state.toInt() and 0x01) != 0
    }
}

/**
 * Relay status data class
 */
data class RelayStatus(
    val deviceTableId: Byte,
    val deviceId: Byte,
    val state: Byte
) {
    val isOn: Boolean
        get() = DeviceStatusParser.extractRelayState(state)
}

/**
 * Dimmable light status data class
 */
data class DimmableLightStatus(
    val deviceTableId: Byte,
    val deviceId: Byte,
    val statusBytes: ByteArray
) {
    val brightness: Int?
        get() = DeviceStatusParser.extractBrightness(statusBytes)
    
    val isOn: Boolean?
        get() = DeviceStatusParser.extractOnOffState(statusBytes)
    
    override fun equals(other: Any?): Boolean {
        if (this === other) return true
        if (javaClass != other?.javaClass) return false
        
        other as DimmableLightStatus
        
        if (deviceTableId != other.deviceTableId) return false
        if (deviceId != other.deviceId) return false
        if (!statusBytes.contentEquals(other.statusBytes)) return false
        
        return true
    }
    
    override fun hashCode(): Int {
        var result = deviceTableId.toInt()
        result = 31 * result + deviceId.toInt()
        result = 31 * result + statusBytes.contentHashCode()
        return result
    }
}

/**
 * RGB light status data class
 * Status bytes layout (8 bytes from official app LogicalDeviceLightRGBStatus):
 *   [0] Mode:      0=Off, 1=On, 2=Blink, 4=Jump3, 5=Jump7, 6=Fade3, 7=Fade7, 8=Rainbow, 127=Restore
 *   [1] Red:       0-255
 *   [2] Green:     0-255
 *   [3] Blue:      0-255
 *   [4] AutoOff:   0-255 (minutes, 0=disabled)
 *   [5] IntervalHi: interval high byte (big-endian)
 *   [6] IntervalLo: interval low byte (big-endian)
 *   [7] Reserved
 */
data class RgbLightStatus(
    val deviceTableId: Byte,
    val deviceId: Byte,
    val statusBytes: ByteArray
) {
    /** Mode byte: 0=Off, 1=On, 2=Blink, 4=Jump3, 5=Jump7, 6=Fade3, 7=Fade7, 8=Rainbow */
    val mode: Int get() = if (statusBytes.isNotEmpty()) statusBytes[0].toInt() and 0xFF else 0

    /** Red channel 0-255 */
    val red: Int get() = if (statusBytes.size > 1) statusBytes[1].toInt() and 0xFF else 0

    /** Green channel 0-255 */
    val green: Int get() = if (statusBytes.size > 2) statusBytes[2].toInt() and 0xFF else 0

    /** Blue channel 0-255 */
    val blue: Int get() = if (statusBytes.size > 3) statusBytes[3].toInt() and 0xFF else 0

    /** Auto-off timer in minutes (0 = disabled) */
    val autoOff: Int get() = if (statusBytes.size > 4) statusBytes[4].toInt() and 0xFF else 0

    /** Effect interval in milliseconds (big-endian 16-bit) */
    val interval: Int get() = if (statusBytes.size > 6) {
        ((statusBytes[5].toInt() and 0xFF) shl 8) or (statusBytes[6].toInt() and 0xFF)
    } else 0

    /** Whether the light is on (mode > 0) */
    val isOn: Boolean get() = mode > 0

    /** Effect name for HA effect_list */
    val effectName: String get() = when (mode) {
        0 -> "Off"
        1 -> "Solid"
        2 -> "Blink"
        4 -> "Jump 3"
        5 -> "Jump 7"
        6 -> "Fade 3"
        7 -> "Fade 7"
        8 -> "Rainbow"
        else -> "Unknown"
    }

    override fun equals(other: Any?): Boolean {
        if (this === other) return true
        if (javaClass != other?.javaClass) return false
        
        other as RgbLightStatus
        
        if (deviceTableId != other.deviceTableId) return false
        if (deviceId != other.deviceId) return false
        if (!statusBytes.contentEquals(other.statusBytes)) return false
        
        return true
    }
    
    override fun hashCode(): Int {
        var result = deviceTableId.toInt()
        result = 31 * result + deviceId.toInt()
        result = 31 * result + statusBytes.contentHashCode()
        return result
    }
}

