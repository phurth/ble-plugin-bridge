package com.onecontrol.blebridge

import android.util.Log

/**
 * MyRvLink Event Factory - Decodes MyRvLink events from raw data
 * Based on MyRvLinkEventFactory.TryDecodeEvent() from decompiled code
 */
object MyRvLinkEventFactory {
    private const val TAG = "MyRvLinkEventFactory"
    private const val EVENT_TYPE_INDEX = 0

    /**
     * Try to decode a MyRvLink event from raw data
     * Returns the event type and data, or null if not a valid event
     */
    fun tryDecodeEvent(eventBytes: ByteArray): MyRvLinkEvent? {
        if (eventBytes.isEmpty()) {
            return null
        }

        try {
            val eventType = eventBytes[0].toInt() and 0xFF
            val myRvLinkEventType = MyRvLinkEventType.fromInt(eventType)

            // Don't accept Unknown or Invalid event types
            if (myRvLinkEventType == null || 
                myRvLinkEventType == MyRvLinkEventType.Unknown ||
                myRvLinkEventType == MyRvLinkEventType.Invalid) {
                // Not a valid MyRvLink event
                return null
            }

            return MyRvLinkEvent(
                eventType = myRvLinkEventType,
                rawData = eventBytes
            )
        } catch (e: Exception) {
            Log.e(TAG, "Error decoding event: ${e.message}")
            return null
        }
    }

    /**
     * Check if data looks like a MyRvLink command response
     * Format: [ClientCommandId (2 bytes)][CommandType (1 byte)][Response data...]
     */
    fun isCommandResponse(data: ByteArray): Boolean {
        if (data.size < 3) {
            return false
        }
        // Check if first 2 bytes form a reasonable command ID (1-0xFFFE)
        val commandId = ((data[1].toInt() and 0xFF) shl 8) or (data[0].toInt() and 0xFF)
        return commandId in 1..0xFFFE
    }

    /**
     * Extract command ID from command response
     */
    fun extractCommandId(data: ByteArray): UShort? {
        if (data.size < 2) {
            return null
        }
        val commandId = ((data[1].toInt() and 0xFF) shl 8) or (data[0].toInt() and 0xFF)
        return if (commandId in 1..0xFFFE) commandId.toUShort() else null
    }
}

/**
 * MyRvLink Event Types
 * Based on MyRvLinkEventType enum from decompiled code
 */
enum class MyRvLinkEventType(val value: Int) {
    Unknown(0),
    GatewayInformation(1),
    DeviceCommand(2),
    DeviceOnlineStatus(3),
    DeviceLockStatus(4),
    RelayBasicLatchingStatusType1(5),
    RelayBasicLatchingStatusType2(6),
    RvStatus(7),
    DimmableLightStatus(8),
    RgbLightStatus(9),
    GeneratorGenieStatus(10),
    HvacStatus(11),
    TankSensorStatus(12),
    RelayHBridgeMomentaryStatusType1(13),
    RelayHBridgeMomentaryStatusType2(14),
    HourMeterStatus(15),
    Leveler4DeviceStatus(16),
    LevelerConsoleText(17),
    Leveler1DeviceStatus(18),
    Leveler3DeviceStatus(19),
    Leveler5DeviceStatus(20),
    AutoOperationProgressStatus(21),
    DeviceSessionStatus(26),
    TankSensorStatusV2(27),
    RealTimeClock(32),
    CloudGatewayStatus(33),
    TemperatureSensorStatus(34),
    JaycoTbbStatus(35),
    MonitorPanelStatus(43),
    AccessoryGatewayStatus(44),
    AwningSensorStatus(47),
    BrakingSystemStatus(48),
    BatteryMonitorStatus(49),
    ReFlashBootloader(50),
    DoorLockStatus(51),
    DimmableLightExtendedStatus(53),
    LevelerType5ExtendedStatus(54),
    HostDebug(102),
    Invalid(255);

    companion object {
        fun fromInt(value: Int): MyRvLinkEventType? {
            return values().find { it.value == value }
        }
    }
}

/**
 * MyRvLink Event data class
 */
data class MyRvLinkEvent(
    val eventType: MyRvLinkEventType,
    val rawData: ByteArray
) {
    override fun equals(other: Any?): Boolean {
        if (this === other) return true
        if (javaClass != other?.javaClass) return false

        other as MyRvLinkEvent

        if (eventType != other.eventType) return false
        if (!rawData.contentEquals(other.rawData)) return false

        return true
    }

    override fun hashCode(): Int {
        var result = eventType.hashCode()
        result = 31 * result + rawData.contentHashCode()
        return result
    }
}

