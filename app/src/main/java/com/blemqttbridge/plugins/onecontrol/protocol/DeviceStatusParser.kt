package com.blemqttbridge.plugins.onecontrol.protocol

import android.util.Log

/**
 * Parses device status events from MyRvLink protocol
 * Based on decompiled MyRvLink event classes
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
 */
data class RgbLightStatus(
    val deviceTableId: Byte,
    val deviceId: Byte,
    val statusBytes: ByteArray
) {
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

