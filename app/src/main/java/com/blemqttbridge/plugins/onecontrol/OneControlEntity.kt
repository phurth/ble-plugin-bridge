package com.blemqttbridge.plugins.onecontrol

/**
 * Entity models for OneControl devices.
 * 
 * This sealed class hierarchy provides type-safe representations of all entity types
 * supported by the OneControl gateway. Each entity type includes its identifying information
 * (tableId, deviceId) and current state.
 * 
 * Benefits:
 * - Type safety: Compile-time checking of entity properties
 * - Centralization: Single source of truth for entity data
 * - Testability: Easy to create entity instances for testing
 * - Maintainability: Clear documentation of what each entity type contains
 */
sealed class OneControlEntity {
    abstract val tableId: Int
    abstract val deviceId: Int
    
    /**
     * Unique key for this entity (used for discovery tracking, pending commands, etc.)
     */
    val key: String
        get() = "%02x%02x".format(tableId, deviceId)
    
    /**
     * Full address as "tableId:deviceId" (used for logging, maps)
     */
    val address: String
        get() = "$tableId:$deviceId"
    
    /**
     * Switch (relay) entity - simple ON/OFF control
     * 
     * @param tableId Device table ID
     * @param deviceId Device ID within table
     * @param isOn Current state (true = ON, false = OFF)
     */
    data class Switch(
        override val tableId: Int,
        override val deviceId: Int,
        val isOn: Boolean
    ) : OneControlEntity() {
        val state: String get() = if (isOn) "ON" else "OFF"
    }
    
    /**
     * Dimmable light entity - supports brightness control 0-255
     * 
     * @param tableId Device table ID
     * @param deviceId Device ID within table
     * @param brightness Current brightness level (0-255, 0 = OFF)
     * @param mode Mode byte from gateway (0=Off, 1=On, 2=Blink, 3=Swell)
     */
    data class DimmableLight(
        override val tableId: Int,
        override val deviceId: Int,
        val brightness: Int,
        val mode: Int
    ) : OneControlEntity() {
        val isOn: Boolean get() = mode > 0
        val state: String get() = if (isOn) "ON" else "OFF"

        /** Effect name for HA - mirrors RgbLight.effectName */
        val effectName: String get() = when (mode) {
            0 -> "Solid"   // Default when off
            1 -> "Solid"
            2 -> "Blink"
            3 -> "Swell"
            else -> "Solid"
        }

        companion object {
            /** Official app speed presets (milliseconds per half-cycle) */
            const val CYCLE_FAST = 220
            const val CYCLE_MEDIUM = 1055
            const val CYCLE_SLOW = 2447

            /** Map HA effect name → OneControl mode byte */
            fun effectToMode(effect: String): Int = when {
                effect.lowercase().startsWith("blink") -> 2
                effect.lowercase().startsWith("swell") -> 3
                else -> 1  // "Solid" or unknown → On
            }

            /** Map HA effect name → cycle time in ms */
            fun effectToCycleTime(effect: String): Int = when {
                effect.lowercase().contains("slow") -> CYCLE_SLOW
                effect.lowercase().contains("medium") -> CYCLE_MEDIUM
                else -> CYCLE_FAST  // Default to fast
            }

            val EFFECT_LIST = listOf(
                "Solid",
                "Blink Slow", "Blink Medium", "Blink Fast",
                "Swell Slow", "Swell Medium", "Swell Fast"
            )
        }
    }
    
    /**
     * RGB light entity - supports color and effects
     * 
     * @param tableId Device table ID
     * @param deviceId Device ID within table
     * @param red Red channel (0-255)
     * @param green Green channel (0-255)
     * @param blue Blue channel (0-255)
     * @param mode Mode byte (0=Off, 1=On/Solid, 2=Blink, 4=Jump3, 5=Jump7, 6=Fade3, 7=Fade7, 8=Rainbow)
     * @param autoOff Auto-off timer in minutes (0=disabled)
     * @param interval Effect interval in ms (for blink/transition effects)
     */
    data class RgbLight(
        override val tableId: Int,
        override val deviceId: Int,
        val red: Int,
        val green: Int,
        val blue: Int,
        val mode: Int,
        val autoOff: Int = 0,
        val interval: Int = 0
    ) : OneControlEntity() {
        val isOn: Boolean get() = mode > 0
        val state: String get() = if (isOn) "ON" else "OFF"
        
        /** Brightness computed from max RGB channel (for HA brightness slider) */
        val brightness: Int get() = maxOf(red, green, blue)
        
        /** Effect name for HA */
        val effectName: String get() = when (mode) {
            0 -> "Solid"   // When off, default effect is Solid (will turn on with Solid)
            1 -> "Solid"
            2 -> "Blink"
            4 -> "Jump 3"
            5 -> "Jump 7"
            6 -> "Fade 3"
            7 -> "Fade 7"
            8 -> "Rainbow"
            else -> "Solid"
        }
    }
    
    /**
     * Tank sensor entity - monitors tank level percentage
     * 
     * @param tableId Device table ID
     * @param deviceId Device ID within table
     * @param level Tank level percentage (0-100)
     */
    data class Tank(
        override val tableId: Int,
        override val deviceId: Int,
        val level: Int
    ) : OneControlEntity()
    
    /**
     * Cover (awning/slide) state sensor - reports motor state
     * 
     * SAFETY NOTE: Cover control is disabled. RV awnings/slides have no limit switches
     * or overcurrent protection - motors rely on operator judgment. This entity is
     * STATE-ONLY for safety reasons.
     * 
     * @param tableId Device table ID
     * @param deviceId Device ID within table
     * @param status Raw status byte from gateway
     * @param position Position byte (0xFF if not available)
     */
    data class Cover(
        override val tableId: Int,
        override val deviceId: Int,
        val status: Int,
        val position: Int = 0xFF
    ) : OneControlEntity() {
        /**
         * Home Assistant cover state based on status byte
         */
        val haState: String
            get() = when (status) {
                0xC2 -> "opening"   // Extending
                0xC3 -> "closing"   // Retracting
                0xC0 -> "stopped"   // Stopped
                else -> "unknown"
            }
    }
    
    /**
     * System voltage sensor - monitors RV battery voltage
     * 
     * @param voltage Voltage in volts (8.8 fixed point from gateway)
     */
    data class SystemVoltageSensor(
        val voltage: Float
    ) : OneControlEntity() {
        override val tableId: Int = 0xFF  // System sensors don't have device addresses
        override val deviceId: Int = 0xFF
    }
    
    /**
     * System temperature sensor - monitors ambient temperature
     * 
     * @param temperature Temperature in Celsius (8.8 fixed point from gateway)
     */
    data class SystemTemperatureSensor(
        val temperature: Float
    ) : OneControlEntity() {
        override val tableId: Int = 0xFF  // System sensors don't have device addresses
        override val deviceId: Int = 0xFF
    }
}
