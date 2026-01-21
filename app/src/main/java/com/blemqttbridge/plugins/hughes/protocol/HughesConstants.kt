package com.blemqttbridge.plugins.hughes.protocol

import java.util.UUID

/**
 * Constants for Hughes Power Watchdog BLE protocol.
 * 
 * Frame structure: Two 20-byte notifications concatenated into 40-byte frame (no checksum).
 * Header: 01 03 20 (first chunk)
 * Payload (big-endian 32-bit ints, divide by 10,000 unless noted):
 *   [3..6]   volts (V)
 *   [7..10]  amps (A)
 *   [11..14] watts (W)
 *   [15..18] cumulative energy (kWh, divide by 10,000)
 *   [19]     error code (0–9)
 *   [20..30] reserved/unknown (observed zeroes)
 *   [31..34] frequency (Hz, divide by 100)
 *   [37..39] line marker (0/0/0=line1, 1/1/1=line2; 30A only line1)
 */
object HughesConstants {
    
    // ===== BLE UUIDs =====
    
    /** Hughes Power Watchdog BLE service UUID */
    val SERVICE_UUID: UUID = UUID.fromString("0000ffe0-0000-1000-8000-00805f9b34fb")
    
    /** Notify characteristic - device sends data here */
    val NOTIFY_CHARACTERISTIC_UUID: UUID = UUID.fromString("0000ffe2-0000-1000-8000-00805f9b34fb")
    
    /** Write characteristic - send commands here (phase 2) */
    val WRITE_CHARACTERISTIC_UUID: UUID = UUID.fromString("0000fff5-0000-1000-8000-00805f9b34fb")
    
    /** Client Characteristic Configuration Descriptor (CCCD) for notifications */
    val CCCD_UUID: UUID = UUID.fromString("00002902-0000-1000-8000-00805f9b34fb")
    
    // ===== DEVICE IDENTIFICATION =====
    
    /** Legacy device name prefix */
    const val DEVICE_NAME_PREFIX_LEGACY = "PMD"
    
    /** Modern device name prefix */
    const val DEVICE_NAME_PREFIX_MODERN = "PWS"
    
    // ===== PROTOCOL CONSTANTS =====
    
    /** Chunk size (notifications arrive as two 20B chunks) */
    const val CHUNK_SIZE = 20
    
    /** Combined frame size */
    const val FRAME_SIZE = 40
    
    /** Expected header bytes for first chunk */
    val HEADER_BYTES = byteArrayOf(0x01, 0x03, 0x20)
    
    /** Timeout for assembling 40B frame from chunks (ms) */
    const val FRAME_TIMEOUT_MS = 1000L
    
    // ===== FRAME FIELD OFFSETS =====
    
    /** Volts (big-endian 32-bit int @ [3..6], divide by 10,000) */
    const val OFFSET_VOLTS = 3
    
    /** Amps (big-endian 32-bit int @ [7..10], divide by 10,000) */
    const val OFFSET_AMPS = 7
    
    /** Watts (big-endian 32-bit int @ [11..14], divide by 10,000) */
    const val OFFSET_WATTS = 11
    
    /** Cumulative energy (big-endian 32-bit int @ [15..18], divide by 10,000 for kWh) */
    const val OFFSET_ENERGY = 15
    
    /** Error code (1 byte @ [19], 0–9 map to error labels) */
    const val OFFSET_ERROR = 19
    
    /** Frequency (big-endian 32-bit int @ [31..34], divide by 100 for Hz) */
    const val OFFSET_FREQUENCY = 31
    
    /** Line marker bytes (@ [37..39]; 0/0/0=line1, 1/1/1=line2) */
    const val OFFSET_LINE_MARKER = 37
    
    // ===== ERROR CODE LABELS =====
    
    val ERROR_LABELS = mapOf(
        0 to "OK",
        1 to "Overvoltage L1",
        2 to "Overvoltage L2",
        3 to "Undervoltage L1",
        4 to "Undervoltage L2",
        5 to "Overcurrent L1",
        6 to "Overcurrent L2",
        7 to "Hot/Neutral Reversed",
        8 to "Lost Ground",
        9 to "No RV Neutral"
    )
    
    // ===== TIMING =====
    
    /** Delay before service discovery after connection */
    const val SERVICE_DISCOVERY_DELAY_MS = 200L
    
    /** Delay between BLE operations */
    const val OPERATION_DELAY_MS = 100L
}
