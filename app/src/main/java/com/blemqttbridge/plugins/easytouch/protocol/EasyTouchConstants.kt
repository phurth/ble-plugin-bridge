package com.blemqttbridge.plugins.easytouch.protocol

import java.util.UUID

/**
 * Constants for EasyTouch thermostat BLE protocol.
 * Based on analysis of HACS integration: k3vmcd/ha-micro-air-easytouch
 */
object EasyTouchConstants {
    
    // ===== BLE UUIDs =====
    
    /** EasyTouch service UUID */
    val SERVICE_UUID: UUID = UUID.fromString("000000FF-0000-1000-8000-00805F9B34FB")
    
    /** Write password for authentication */
    val PASSWORD_CMD_UUID: UUID = UUID.fromString("0000DD01-0000-1000-8000-00805F9B34FB")
    
    /** Write JSON commands */
    val JSON_CMD_UUID: UUID = UUID.fromString("0000EE01-0000-1000-8000-00805F9B34FB")
    
    /** Read/notify JSON responses */
    val JSON_RETURN_UUID: UUID = UUID.fromString("0000FF01-0000-1000-8000-00805F9B34FB")
    
    /** Client Characteristic Configuration Descriptor (CCCD) for notifications */
    val CCCD_UUID: UUID = UUID.fromString("00002902-0000-1000-8000-00805f9b34fb")
    
    // ===== DEVICE IDENTIFICATION =====
    
    /** Device name prefix for scan matching */
    const val DEVICE_NAME_PREFIX = "EasyTouch"
    
    // ===== DEVICE MODE MAPPINGS =====
    // These map between EasyTouch protocol values and Home Assistant modes
    
    /** EasyTouch device mode values */
    object DeviceMode {
        const val OFF = 0
        const val FAN_ONLY = 1
        const val COOL = 2
        const val HEAT = 4
        const val DRY = 6
        const val AUTO = 11
    }
    
    /** Map device mode values to Home Assistant mode strings */
    val DEVICE_TO_HA_MODE = mapOf(
        DeviceMode.OFF to "off",
        DeviceMode.FAN_ONLY to "fan_only",
        DeviceMode.COOL to "cool",
        DeviceMode.HEAT to "heat",
        DeviceMode.DRY to "dry",
        DeviceMode.AUTO to "auto"
    )
    
    /** Map Home Assistant mode strings to device mode values */
    val HA_MODE_TO_DEVICE = mapOf(
        "off" to DeviceMode.OFF,
        "fan_only" to DeviceMode.FAN_ONLY,
        "cool" to DeviceMode.COOL,
        "heat" to DeviceMode.HEAT,
        "dry" to DeviceMode.DRY,
        "auto" to DeviceMode.AUTO
    )
    
    /** Supported HVAC modes for Home Assistant discovery */
    val SUPPORTED_HVAC_MODES = listOf("off", "heat", "cool", "auto", "fan_only", "dry")
    
    // ===== FAN MODE MAPPINGS =====
    
    /** Fan mode values for standard modes (cool/heat/auto) */
    object FanMode {
        const val OFF = 0
        const val MANUAL_LOW = 1
        const val MANUAL_HIGH = 2
        const val CYCLED_LOW = 65
        const val CYCLED_HIGH = 66
        const val FULL_AUTO = 128
    }
    
    /** Fan mode values for fan-only mode */
    object FanOnlyMode {
        const val OFF = 0
        const val LOW = 1
        const val HIGH = 2
    }
    
    /** Map fan values to Home Assistant fan mode strings (standard modes) */
    val FAN_VALUE_TO_HA = mapOf(
        FanMode.OFF to "off",
        FanMode.MANUAL_LOW to "low",
        FanMode.MANUAL_HIGH to "high",
        FanMode.CYCLED_LOW to "low",
        FanMode.CYCLED_HIGH to "high",
        FanMode.FULL_AUTO to "auto"
    )
    
    /** Map Home Assistant fan mode to device values */
    val HA_FAN_TO_VALUE = mapOf(
        "off" to FanMode.OFF,
        "low" to FanMode.MANUAL_LOW,
        "high" to FanMode.MANUAL_HIGH,
        "auto" to FanMode.FULL_AUTO
    )
    
    /** Supported fan modes for Home Assistant discovery */
    val SUPPORTED_FAN_MODES = listOf("auto", "low", "high")
    
    // ===== Z_sts ARRAY INDICES =====
    // From decompiled U_Thermostat.java - these are the correct indices
    
    object StatusIndex {
        const val AUTO_HEAT_SETPOINT = 0   // autoHeatSP
        const val AUTO_COOL_SETPOINT = 1   // autoCoolSP
        const val COOL_SETPOINT = 2        // coolSP
        const val HEAT_SETPOINT = 3        // heatSP
        const val DRY_SETPOINT = 4         // drySP
        const val RH_SETPOINT = 5          // rhSP (humidity)
        const val FAN_ONLY_SPEED = 6       // fanOnlySpeed
        const val COOL_FAN_SPEED = 7       // coolFanSpeed
        const val ELECTRIC_FAN_SPEED = 8   // electricFanSpeed (heat pump)
        const val AUTO_FAN_SPEED = 9       // autoFanSpeed
        const val MODE = 10                // Current mode setting
        const val GAS_FAN_SPEED = 11       // gasFanSpeed (furnace)
        const val AMBIENT_TEMP = 12        // Current ambient temperature
        const val OUTSIDE_TEMP = 13        // Outside air temp (255 = invalid)
        const val FAULT = 14               // Fault code
        const val STATUS_FLAGS = 15        // Bit flags: 1=cycleActive, 2=isCooling, 4=isHeating
    }
    
    // ===== CONNECTION SETTINGS =====
    
    /** Delay before service discovery after connection */
    const val SERVICE_DISCOVERY_DELAY_MS = 500L
    
    /** Delay between authentication steps */
    const val AUTH_STEP_DELAY_MS = 200L
    
    /** Delay before requesting initial status */
    const val INITIAL_STATUS_DELAY_MS = 500L
    
    /** Delay before reading response after writing command (official app uses immediate read) */
    const val READ_DELAY_MS = 50L
    const val STATUS_POLL_INTERVAL_MS = 60000L  // 60 seconds
    
    /** Maximum time to wait for status response */
    const val STATUS_TIMEOUT_MS = 10000L
    
    // ===== TEMPERATURE LIMITS =====
    
    const val MIN_TEMP_F = 60
    const val MAX_TEMP_F = 90
    const val TEMP_STEP = 1.0
}
