package com.blemqttbridge.plugins.onecontrol.protocol

/**
 * Event type constants for MyRvLink events (copied from legacy app)
 * Based on MyRvLinkEventType enum from decompiled code
 */
object MyRvLinkEventType {
    const val Unknown: Byte = 0x00
    const val GatewayInformation: Byte = 0x01
    const val DeviceCommand: Byte = 0x02
    const val DeviceOnlineStatus: Byte = 0x03
    const val DeviceLockStatus: Byte = 0x04
    const val RelayBasicLatchingStatusType1: Byte = 0x05  // ADDED - was missing!
    const val RelayBasicLatchingStatusType2: Byte = 0x06
    const val RvStatus: Byte = 0x07
    const val DimmableLightStatus: Byte = 0x08
    const val RgbLightStatus: Byte = 0x09
    const val GeneratorGenieStatus: Byte = 0x0A
    const val HvacStatus: Byte = 0x0B
    const val TankSensorStatus: Byte = 0x0C
    const val RelayHBridgeMomentaryStatusType1: Byte = 0x0D  // ADDED
    const val RelayHBridgeMomentaryStatusType2: Byte = 0x0E
    const val HourMeterStatus: Byte = 0x0F
    const val Leveler4DeviceStatus: Byte = 0x10
    const val DeviceSessionStatus: Byte = 0x1A
    const val TankSensorStatusV2: Byte = 0x1B
    const val RealTimeClock: Byte = 0x20
}
