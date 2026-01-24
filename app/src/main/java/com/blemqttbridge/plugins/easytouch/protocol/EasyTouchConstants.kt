package com.blemqttbridge.plugins.easytouch.protocol

import java.util.UUID

/**
 * Constants for EasyTouch thermostat BLE protocol.
 * Reference: k3vmcd/ha-micro-air-easytouch HACS integration
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
    
    // ===== STANDARD BLE DEVICE INFORMATION SERVICE (0x180A) =====
    
    /** Device Information Service UUID */
    val DEVICE_INFO_SERVICE_UUID: UUID = UUID.fromString("0000180A-0000-1000-8000-00805F9B34FB")
    
    /** Manufacturer Name String characteristic */
    val MANUFACTURER_NAME_UUID: UUID = UUID.fromString("00002A29-0000-1000-8000-00805F9B34FB")
    
    /** Model Number String characteristic */
    val MODEL_NUMBER_UUID: UUID = UUID.fromString("00002A24-0000-1000-8000-00805F9B34FB")
    
    /** Serial Number String characteristic */
    val SERIAL_NUMBER_UUID: UUID = UUID.fromString("00002A25-0000-1000-8000-00805F9B34FB")
    
    /** Hardware Revision String characteristic */
    val HARDWARE_REVISION_UUID: UUID = UUID.fromString("00002A27-0000-1000-8000-00805F9B34FB")
    
    /** Firmware Revision String characteristic */
    val FIRMWARE_REVISION_UUID: UUID = UUID.fromString("00002A26-0000-1000-8000-00805F9B34FB")
    
    /** Software Revision String characteristic */
    val SOFTWARE_REVISION_UUID: UUID = UUID.fromString("00002A28-0000-1000-8000-00805F9B34FB")
    
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
        const val HEAT = 3              // Generic heat (uses gas fan)
        const val FURNACE = 4           // AquaHot/Diesel furnace (uses gas fan)
        const val HEAT_PUMP = 5         // Heat pump (uses electric fan)
        const val DRY = 6
        const val HEAT_STRIP = 7        // Heat strip (uses electric fan)
        const val AUTO = 8              // Auto mode
        const val AUTO_HS = 9           // Auto with heat strip backup
        const val AUTO_HP = 10          // Auto with heat pump backup
        const val AUTO_FURNACE = 11     // Auto with furnace backup
        const val ELECTRIC_HEAT = 12    // Electric heat (uses electric fan)
        const val GAS_HEAT = 13         // Gas heat (uses gas fan)
    }
    
    /** Map device mode values to Home Assistant mode strings */
    val DEVICE_TO_HA_MODE = mapOf(
        DeviceMode.OFF to "off",
        DeviceMode.FAN_ONLY to "fan_only",
        DeviceMode.COOL to "cool",
        DeviceMode.HEAT to "heat",
        DeviceMode.FURNACE to "heat",
        DeviceMode.HEAT_PUMP to "heat",
        DeviceMode.DRY to "dry",
        DeviceMode.HEAT_STRIP to "heat",
        DeviceMode.AUTO to "auto",
        DeviceMode.AUTO_HS to "auto",
        DeviceMode.AUTO_HP to "auto",
        DeviceMode.AUTO_FURNACE to "auto",
        DeviceMode.ELECTRIC_HEAT to "heat",
        DeviceMode.GAS_HEAT to "heat"
    )
    
    /** Map Home Assistant mode strings to device mode values */
    val HA_MODE_TO_DEVICE = mapOf(
        "off" to DeviceMode.OFF,
        "fan_only" to DeviceMode.FAN_ONLY,
        "cool" to DeviceMode.COOL,
        "heat" to DeviceMode.HEAT_PUMP,  // Default to heat pump (most common)
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
    
    // ===== HEAT TYPE PRESET MODES =====
    // Preset modes allow users to select heat source when in heat mode
    
    /** Map preset names to device mode numbers */
    val HEAT_TYPE_PRESETS = mapOf(
        "Heat Pump" to DeviceMode.HEAT_PUMP,
        "Furnace" to DeviceMode.FURNACE,
        "Heat Strip" to DeviceMode.HEAT_STRIP,
        "Electric Heat" to DeviceMode.ELECTRIC_HEAT,
        "Gas Heat" to DeviceMode.GAS_HEAT,
        "Heat" to DeviceMode.HEAT
    )
    
    /** Reverse map: device mode numbers to preset names */
    val HEAT_TYPE_REVERSE = mapOf(
        DeviceMode.HEAT_PUMP to "Heat Pump",
        DeviceMode.FURNACE to "Furnace",
        DeviceMode.HEAT_STRIP to "Heat Strip",
        DeviceMode.ELECTRIC_HEAT to "Electric Heat",
        DeviceMode.GAS_HEAT to "Gas Heat",
        DeviceMode.HEAT to "Heat"
    )
    
    /**
     * Get the correct fan JSON field name for a given heat mode.
     * Based on official EasyTouch app logic (U_Thermostat.java:807)
     */
    fun getFanFieldForMode(modeNum: Int): String {
        return when (modeNum) {
            DeviceMode.HEAT_PUMP, DeviceMode.HEAT_STRIP, DeviceMode.ELECTRIC_HEAT -> "eleFan"
            DeviceMode.HEAT, DeviceMode.FURNACE, DeviceMode.GAS_HEAT -> "gasFan"
            else -> "coolFan"  // Default for non-heat modes
        }
    }
    
    // ===== Z_sts ARRAY INDICES =====
    // Status array indices for thermostat state data
    
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
    
    /** Delay before reading response after writing command */
    const val READ_DELAY_MS = 50L
    const val STATUS_POLL_INTERVAL_MS = 60000L  // 60 seconds
    
    /** Maximum time to wait for status response */
    const val STATUS_TIMEOUT_MS = 10000L
    
    // ===== TEMPERATURE LIMITS =====
    
    const val MIN_TEMP_F = 60
    const val MAX_TEMP_F = 90
    const val TEMP_STEP = 1.0
}
