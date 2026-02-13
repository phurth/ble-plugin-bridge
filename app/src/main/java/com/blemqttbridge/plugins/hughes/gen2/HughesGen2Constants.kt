package com.blemqttbridge.plugins.hughes.gen2

import java.util.UUID

/**
 * Constants for Hughes Power Watchdog Gen2 BLE protocol.
 *
 * Gen2 devices (E5-E9, V5-V9) use a framed binary protocol with a magic header,
 * command byte, body, and tail — completely different from the legacy Modbus-like
 * raw 40-byte frames used by Gen1 (E2-E4) devices.
 *
 * Protocol source: Decompiled com.yw.watchdog APK (Package.java, Protocol.java, Cmd.java)
 *
 * Packet frame format:
 *   [4 bytes] Magic: 0x247C2740
 *   [1 byte]  Version
 *   [1 byte]  MsgId (rolling 1-100)
 *   [1 byte]  Cmd
 *   [2 bytes] DataLen (big-endian uint16)
 *   [N bytes] Body
 *   [2 bytes] Tail: 0x7121
 *
 * BLE transport:
 *   Service: 000000FF
 *   Single characteristic for R/W/Notify: 0000FF01
 *   MTU: 80
 *   Init: Write ASCII "!%!%,protocol,open," to enable binary mode
 */
object HughesGen2Constants {

    // ===== BLE UUIDs =====

    /** Gen2 Power Watchdog BLE service UUID */
    val SERVICE_UUID: UUID = UUID.fromString("000000ff-0000-1000-8000-00805f9b34fb")

    /** Single characteristic for read/write/notify */
    val RW_CHARACTERISTIC_UUID: UUID = UUID.fromString("0000ff01-0000-1000-8000-00805f9b34fb")

    /** Client Characteristic Configuration Descriptor (CCCD) for notifications */
    val CCCD_UUID: UUID = UUID.fromString("00002902-0000-1000-8000-00805f9b34fb")

    // ===== DEVICE IDENTIFICATION =====

    /** Gen2 device BLE name prefix (format: WD_{type}_{serial}) */
    const val DEVICE_NAME_PREFIX = "WD_"

    // ===== PROTOCOL FRAMING =====

    /** Packet magic identifier (big-endian) */
    const val PACKET_MAGIC: Int = 0x247C2740  // 611940160

    /** Packet tail marker */
    const val PACKET_TAIL: Short = 0x7121.toShort()  // 28961

    /** Header size: 4 (magic) + 1 (version) + 1 (msgId) + 1 (cmd) + 2 (dataLen) = 9 */
    const val HEADER_SIZE = 9

    /** Tail size */
    const val TAIL_SIZE = 2

    /** Max buffer size for packet assembly */
    const val MAX_BUFFER_SIZE = 8192

    /** Protocol version byte */
    const val PROTOCOL_VERSION: Byte = 0x01

    /** ASCII command to enable binary protocol mode after BLE connect */
    const val PROTOCOL_OPEN_CMD = "!%!%,protocol,open,"

    /** Requested MTU for BLE connection */
    const val REQUESTED_MTU = 80

    // ===== COMMAND CODES =====

    /** Real-time telemetry report (device -> app), also carries version info */
    const val CMD_DL_REPORT: Byte = 0x01

    /** Error/fault history report (device -> app) */
    const val CMD_ERROR_REPORT: Byte = 0x02

    /** Reset energy counter (app -> device, no body) */
    const val CMD_ENERGY_RESET: Byte = 0x03

    /** Restart energy counting (app -> device, no body) */
    const val CMD_ENERGY_RESTART: Byte = 0x04

    /** Delete error record by ID (app -> device, 1 byte body) */
    const val CMD_ERROR_DEL: Byte = 0x05

    /** Set device clock (app -> device, 6 bytes: Y,M,D,H,Min,Sec) */
    const val CMD_SET_TIME: Byte = 0x06

    /** Set backlight level (app -> device, 1 byte body) */
    const val CMD_SET_BACKLIGHT: Byte = 0x07

    /** Read energy start timestamp (app -> device, no body) */
    const val CMD_READ_START_TIME: Byte = 0x08

    /** Initialize device with calibration data (app -> device, 15 bytes) */
    const val CMD_SET_INIT_DATA: Byte = 0x0A

    /** Turn relay on/off (app -> device, 1 byte: 1=ON, 2=OFF) */
    const val CMD_SET_OPEN: Byte = 0x0B

    /** Enable/disable neutral detection (app -> device, 1 byte) */
    const val CMD_NEUTRAL_DETECTION: Byte = 0x0D

    /** Alarm notification (device -> app, BLE only) */
    const val CMD_ALARM: Byte = 0x0E

    // ===== RELAY STATUS =====

    const val RELAY_ON: Byte = 0x01
    const val RELAY_OFF: Byte = 0x02

    // ===== DL REPORT BODY LAYOUT (34 bytes per line) =====

    /** Single-line report body size */
    const val DL_DATA_SIZE = 34

    /** Dual-line report body size (2 x 34) */
    const val DL_DATA_DUAL_SIZE = 68

    // DLData field offsets within each 34-byte block
    const val DL_OFFSET_INPUT_VOLTAGE = 0    // int32_be, /10000 -> V
    const val DL_OFFSET_CURRENT = 4          // int32_be, /10000 -> A
    const val DL_OFFSET_POWER = 8            // int32_be, /10000 -> W
    const val DL_OFFSET_ENERGY = 12          // int32_be, /10000 -> kWh
    const val DL_OFFSET_TEMP1 = 16           // int32_be (internal)
    const val DL_OFFSET_OUTPUT_VOLTAGE = 20  // int32_be, /10000 -> V (E8/V8+)
    const val DL_OFFSET_BACKLIGHT = 24       // byte
    const val DL_OFFSET_NEUTRAL_DET = 25     // byte (0/1)
    const val DL_OFFSET_BOOST = 26           // byte (1 = boosting, E8/V8+)
    const val DL_OFFSET_TEMPERATURE = 27     // byte (degrees, E8/V8+)
    const val DL_OFFSET_FREQUENCY = 28       // int32_be, /100 -> Hz
    const val DL_OFFSET_ERROR = 32           // byte (0-14)
    const val DL_OFFSET_STATUS = 33          // byte (1=ON, 2=OFF)

    // ===== ERROR RECORD LAYOUT (16 bytes each) =====

    const val ERROR_RECORD_SIZE = 16

    // ===== DEVICE TYPES =====

    /** Standard models */
    val STANDARD_TYPES = setOf("E5", "V5", "E6", "V6", "E7", "V7")

    /** Enhanced models with output voltage, boost, and temperature */
    val ENHANCED_TYPES = setOf("E8", "V8", "E9", "V9")

    /** All known device types */
    val ALL_TYPES = STANDARD_TYPES + ENHANCED_TYPES

    // ===== ERROR CODE LABELS =====

    val ERROR_LABELS = mapOf(
        0 to "OK",
        1 to "Voltage Error L1",
        2 to "Voltage Error L2",
        3 to "Over Current L1",
        4 to "Over Current L2",
        5 to "Neutral Reversed L1",
        6 to "Neutral Reversed L2",
        7 to "Missing Ground",
        8 to "Neutral Missing",
        9 to "Surge Protection Used Up",
        10 to "E10",
        11 to "Frequency Error L1",
        12 to "Frequency Error L2",
        13 to "F3",
        14 to "F4"
    )

    // ===== TIMING =====

    /** Delay before service discovery after connection */
    const val SERVICE_DISCOVERY_DELAY_MS = 200L

    /** Delay between BLE operations */
    const val OPERATION_DELAY_MS = 100L

    /** Delay before sending protocol open command */
    const val PROTOCOL_OPEN_DELAY_MS = 500L

    // ===== HELPERS =====

    /**
     * Parse device type from BLE name format WD_{type}_{serial}.
     * Returns the type string (e.g., "E8", "V5") or null if format doesn't match.
     */
    fun parseDeviceType(deviceName: String?): String? {
        if (deviceName == null || !deviceName.startsWith(DEVICE_NAME_PREFIX)) return null
        val parts = deviceName.split("_")
        return if (parts.size >= 3) parts[1] else null
    }

    /**
     * Check if a device type has enhanced features (output voltage, boost, temperature).
     */
    fun isEnhancedType(deviceType: String?): Boolean {
        return deviceType != null && deviceType in ENHANCED_TYPES
    }
}
