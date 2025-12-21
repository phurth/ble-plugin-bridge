package com.blemqttbridge.plugins.device.onecontrol.protocol

/**
 * Event type constants for MyRvLink events (copied from legacy app)
 */
object MyRvLinkEventType {
    const val GatewayInformation: Byte = 0x10
    const val RvStatus: Byte = 0x07
    const val DimmableLightStatus: Byte = 0x08
    const val RelayHBridgeMomentaryStatusType2: Byte = 0x0E
    const val RelayBasicLatchingStatusType2: Byte = 0x06
    const val DeviceOnlineStatus: Byte = 0x03
    const val DeviceLockStatus: Byte = 0x04
    const val RealTimeClock: Byte = 0x20
    const val TankSensorStatus: Byte = 0x09
    const val HvacStatus: Byte = 0x0F
    // Add more as needed
}
