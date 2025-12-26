package com.blemqttbridge.plugins.gopower.protocol

import java.util.UUID

/**
 * Constants for GoPower GP-PWM-30-SB Solar Charge Controller BLE protocol.
 * 
 * Protocol analysis from HCI trace capture:
 * - ASCII text protocol with semicolon-delimited responses
 * - Write ASCII space (0x20) to trigger status poll
 * - Response contains 32 semicolon-delimited fields
 * - No authentication required
 */
object GoPowerConstants {
    
    // ===== BLE UUIDs =====
    
    /** GoPower BLE service UUID */
    val SERVICE_UUID: UUID = UUID.fromString("0000fff0-0000-1000-8000-00805f9b34fb")
    
    /** Write characteristic - send poll command here */
    val WRITE_CHARACTERISTIC_UUID: UUID = UUID.fromString("0000fff2-0000-1000-8000-00805f9b34fb")
    
    /** Notify characteristic - receive status data here */
    val NOTIFY_CHARACTERISTIC_UUID: UUID = UUID.fromString("0000fff1-0000-1000-8000-00805f9b34fb")
    
    /** Client Characteristic Configuration Descriptor (CCCD) for notifications */
    val CCCD_UUID: UUID = UUID.fromString("00002902-0000-1000-8000-00805f9b34fb")
    
    // ===== DEVICE IDENTIFICATION =====
    
    /** Device name prefix for scan matching */
    const val DEVICE_NAME_PREFIX = "GP-PWM"
    
    /** Alternative device name prefix */
    const val DEVICE_NAME_PREFIX_ALT = "GoPower"
    
    // ===== PROTOCOL CONSTANTS =====
    
    /** Poll command: ASCII space character */
    val POLL_COMMAND: ByteArray = byteArrayOf(0x20)  // ' ' space character
    
    /** Response field delimiter */
    const val FIELD_DELIMITER = ";"
    
    /** Expected number of fields in response */
    const val EXPECTED_FIELD_COUNT = 32
    
    /** Polling interval in milliseconds (same as official app) */
    const val STATUS_POLL_INTERVAL_MS = 4000L
    
    // ===== TIMING CONSTANTS =====
    
    /** Delay before service discovery after connection */
    const val SERVICE_DISCOVERY_DELAY_MS = 200L
    
    /** Delay between BLE operations */
    const val OPERATION_DELAY_MS = 100L
    
    /** Read delay after write */
    const val READ_DELAY_MS = 150L
    
    // ===== RESPONSE FIELD INDICES =====
    // Based on analysis of PwmResponseParser.java and HCI trace
    
    /** Field 0: Solar/PV Panel Current (mA, divide by 1000 for A) */
    const val FIELD_SOLAR_CURRENT = 0
    
    /** Field 2: Battery Voltage (mV, divide by 1000 for V) */
    const val FIELD_BATTERY_VOLTAGE = 2
    
    /** Field 10: State of Charge (%) */
    const val FIELD_SOC = 10
    
    /** Field 11: Solar/PV Panel Voltage (mV, divide by 1000 for V) */
    const val FIELD_SOLAR_VOLTAGE = 11
    
    /** Field 16: Temperature Celsius (signed, e.g., +06 = 6°C) */
    const val FIELD_TEMP_C = 16
    
    /** Field 17: Temperature Fahrenheit (signed, e.g., +43 = 43°F) */
    const val FIELD_TEMP_F = 17
    
    /** Field 19: Amp-hours (Ah) */
    const val FIELD_AMP_HOURS = 19
    
    /** Field 24: Energy Today (Wh) */
    const val FIELD_ENERGY_TODAY = 24
    
    /** Field 8: Firmware Version (Integer) */
    const val FIELD_FIRMWARE = 8
    
    /** Field 14: Serial Number (Integer) */
    const val FIELD_SERIAL = 14
    
    // ===== MODBUS COMMANDS =====
    // Some commands use Modbus binary frames
    
    /** Reboot/Factory Reset command (Modbus frame) */
    val REBOOT_COMMAND: ByteArray = byteArrayOf(1, 120, 0, 0, 0, 1, 96, 0)
}
