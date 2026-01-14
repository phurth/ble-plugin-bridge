package com.blemqttbridge.plugins.mopeka.protocol

/**
 * Mopeka Tank Sensor protocol constants
 * 
 * References:
 * - Manufacturer ID: 0x0059 (89 decimal, Mopeka)
 * - Service UUID: 0000fee5-0000-1000-8000-00805f9b34fb
 * - Protocol: Passive BLE advertisement scanning (no connection required)
 */
object MopekaConstants {
    // Manufacturer ID for Mopeka (used in BLE advertisement)
    const val MANUFACTURER_ID = 0x0059
    
    // Service UUID for Mopeka sensors
    const val SERVICE_UUID = "0000fee5-0000-1000-8000-00805f9b34fb"
    
    // Advertisement data byte positions
    // Reference: https://github.com/Bluetooth-Devices/mopeka-iot-ble
    object AdvertisementLayout {
        const val SYNC_BYTE_INDEX = 0         // Model ID (0x03-0x0C)
        const val BATTERY_RAW_INDEX = 1       // Battery raw value (NOT percentage)
        const val TEMPERATURE_AND_BUTTON_INDEX = 2  // Bits 0-6: temp raw (subtract 40 for °C), bit 7: button
        const val DISTANCE_LOW_INDEX = 3      // Distance low byte (14-bit little-endian)
        const val DISTANCE_HIGH_AND_QUALITY_INDEX = 4 // Bits 0-5: distance high, bits 6-7: quality (0-3)
        const val RESERVED_5_INDEX = 5        // Reserved/unused
        const val RESERVED_6_INDEX = 6        // Reserved/unused  
        const val RESERVED_7_INDEX = 7        // Reserved/unused
        const val ACCELEROMETER_X_INDEX = 8   // Accelerometer X
        const val ACCELEROMETER_Y_INDEX = 9   // Accelerometer Y
        
        const val MIN_LENGTH = 10
    }
    
    // Sensor models (identified by sync byte)
    object SensorModels {
        const val PRO_PLUS = 0x03        // M1015
        const val PRO_CHECK = 0x04       // M1017
        const val PRO_200 = 0x05
        const val PRO_H2O = 0x08         // Water sensor
        const val PRO_H2O_PLUS = 0x09
        const val LIPPERT_BOTTLE_CHECK = 0x0A
        const val TD40 = 0x0B
        const val TD200 = 0x0C
    }
    
    // Medium types for level calculation
    enum class MediumType(val id: String, val displayName: String) {
        PROPANE("propane", "Propane"),
        AIR("air", "Air (Tank Ratio)"),
        FRESH_WATER("fresh_water", "Fresh Water"),
        WASTE_WATER("waste_water", "Waste Water"),
        BLACK_WATER("black_water", "Black Water"),
        LIVE_WELL("live_well", "Live Well"),
        GASOLINE("gasoline", "Gasoline"),
        DIESEL("diesel", "Diesel"),
        LNG("lng", "LNG"),
        OIL("oil", "Oil"),
        HYDRAULIC_OIL("hydraulic_oil", "Hydraulic Oil"),
        CUSTOM("custom", "Custom");
        
        companion object {
            fun fromId(id: String): MediumType = values().find { it.id == id } ?: PROPANE
        }
    }
    
    // Sensor model names
    object ModelNames {
        fun getName(syncByte: Int): String = when (syncByte) {
            SensorModels.PRO_PLUS -> "Pro Plus (M1015)"
            SensorModels.PRO_CHECK -> "Pro Check (M1017)"
            SensorModels.PRO_200 -> "Pro 200"
            SensorModels.PRO_H2O -> "Pro H2O"
            SensorModels.PRO_H2O_PLUS -> "Pro H2O Plus"
            SensorModels.LIPPERT_BOTTLE_CHECK -> "Lippert BottleCheck"
            SensorModels.TD40 -> "TD40"
            SensorModels.TD200 -> "TD200"
            else -> "Unknown (0x${syncByte.toString(16)})"
        }
    }
    
    // Reading quality thresholds
    object QualityThresholds {
        const val ZERO = 0        // Accept all readings
        const val LOW = 20        // >= 20%
        const val MEDIUM = 50     // >= 50%
        const val HIGH = 80       // >= 80%
    }
    
    /**
     * Temperature compensation coefficients for tank level measurement
     * Reference: MOPEKA_TANK_LEVEL_COEFFICIENTS from mopeka-iot-ble library
     * 
     * Formula: tank_level_mm = tank_level_raw * (c0 + (c1 * temp) + (c2 * temp^2))
     * where temp is the raw temperature value (NOT Celsius, before subtracting 40)
     * 
     * These coefficients account for speed-of-sound variations in different media
     * at different temperatures, improving measurement accuracy.
     */
    object TankLevelCoefficients {
        data class Coefficients(val c0: Double, val c1: Double, val c2: Double)
        
        val coefficientsMap = mapOf(
            MediumType.PROPANE to Coefficients(0.573045, -0.002822, -0.00000535),
            MediumType.AIR to Coefficients(0.153096, 0.000327, -0.000000294),
            MediumType.FRESH_WATER to Coefficients(0.600592, 0.003124, -0.00001368),
            MediumType.WASTE_WATER to Coefficients(0.600592, 0.003124, -0.00001368),
            MediumType.LIVE_WELL to Coefficients(0.600592, 0.003124, -0.00001368),
            MediumType.BLACK_WATER to Coefficients(0.600592, 0.003124, -0.00001368),
            MediumType.GASOLINE to Coefficients(0.7373417462, -0.001978229885, 0.00000202162),
            MediumType.DIESEL to Coefficients(0.7373417462, -0.001978229885, 0.00000202162),
            MediumType.LNG to Coefficients(0.7373417462, -0.001978229885, 0.00000202162),
            MediumType.OIL to Coefficients(0.7373417462, -0.001978229885, 0.00000202162),
            MediumType.HYDRAULIC_OIL to Coefficients(0.7373417462, -0.001978229885, 0.00000202162)
        )
        
        fun getCoefficients(mediumType: MediumType): Coefficients {
            return coefficientsMap[mediumType] ?: coefficientsMap[MediumType.PROPANE]!!
        }
    }
    
    /**
     * Tank type configurations with geometric specifications
     * 
     * Percentage calculations use precise geometric formulas from HA community:
     * - Vertical tanks: Ellipsoid caps (2:1 ellipsoid) + cylinder middle section
     * - Horizontal tanks: Spherical caps + horizontal cylinder
     * 
     * References:
     * - https://community.home-assistant.io/t/add-tank-percentage-to-mopeka-integration/531322/34
     * - ESPHome mopeka_pro_check integration tank types
     */
    enum class TankType(
        val id: String,
        val displayName: String,
        val orientation: TankOrientation,
        val overallLengthMm: Double,      // Total tank height/length
        val overallDiameterMm: Double,    // Tank diameter
        val wallThicknessMm: Double = 3.175  // Default wall thickness
    ) {
        // Vertical propane tanks (USA standard BBQ tanks)
        TANK_20LB_V("20lb_v", "20lb Vertical", TankOrientation.VERTICAL, 
            overallLengthMm = 316.0,    // 12.44" side length + radius
            overallDiameterMm = 304.8   // 12" diameter
        ),
        TANK_30LB_V("30lb_v", "30lb Vertical", TankOrientation.VERTICAL,
            overallLengthMm = 422.0,
            overallDiameterMm = 304.8
        ),
        TANK_40LB_V("40lb_v", "40lb Vertical", TankOrientation.VERTICAL,
            overallLengthMm = 457.0,
            overallDiameterMm = 304.8
        ),
        
        // Horizontal propane tanks (common RV/home tanks)
        TANK_250GAL_H("250gal_h", "250 Gallon Horizontal", TankOrientation.HORIZONTAL,
            overallLengthMm = 2387.6,   // 94" length
            overallDiameterMm = 762.0   // 30" diameter
        ),
        TANK_500GAL_H("500gal_h", "500 Gallon Horizontal", TankOrientation.HORIZONTAL,
            overallLengthMm = 3022.6,   // 119" length
            overallDiameterMm = 952.5   // 37.5" diameter
        ),
        TANK_1000GAL_H("1000gal_h", "1000 Gallon Horizontal", TankOrientation.HORIZONTAL,
            overallLengthMm = 4877.5,   // 192" length
            overallDiameterMm = 1041.4  // 41" diameter
        ),
        
        // European propane tanks
        TANK_EUROPE_6KG("europe_6kg", "6kg European Vertical", TankOrientation.VERTICAL,
            overallLengthMm = 340.0,
            overallDiameterMm = 240.0
        ),
        TANK_EUROPE_11KG("europe_11kg", "11kg European Vertical", TankOrientation.VERTICAL,
            overallLengthMm = 390.0,
            overallDiameterMm = 290.0
        ),
        TANK_EUROPE_14KG("europe_14kg", "14kg European Vertical", TankOrientation.VERTICAL,
            overallLengthMm = 430.0,
            overallDiameterMm = 290.0
        ),
        
        // Custom tank with user-provided dimensions
        CUSTOM("custom", "Custom Tank", TankOrientation.VERTICAL,
            overallLengthMm = 300.0,    // Default values, user must configure
            overallDiameterMm = 300.0
        );
        
        // Internal dimensions (accounting for wall thickness)
        val internalDiameterMm: Double
            get() = overallDiameterMm - (2 * wallThicknessMm)
        
        val radiusMm: Double
            get() = internalDiameterMm / 2.0
        
        // For vertical tanks: side length of cylindrical middle section
        val sideLengthMm: Double
            get() = when (orientation) {
                TankOrientation.VERTICAL -> overallLengthMm - (internalDiameterMm / 2.0)
                TankOrientation.HORIZONTAL -> overallLengthMm - overallDiameterMm
            }
        
        companion object {
            fun fromId(id: String): TankType = values().find { it.id == id } ?: TANK_20LB_V
        }
    }
    
    enum class TankOrientation {
        VERTICAL,    // Sensor at bottom, measures upward (ellipsoid caps on top/bottom)
        HORIZONTAL   // Sensor at bottom of sideways tank (spherical caps on ends)
    }
    
    /**
     * Calculate tank fill percentage using precise geometric formulas
     * 
     * Vertical tanks (propane BBQ tanks):
     * - Shaped like ellipsoid caps (2:1 ratio) on top and bottom with cylinder in middle
     * - Formula: https://www.vcalc.com/wiki/Ellipsoid-Volume and Ellipsoid-Cap-Volume
     * 
     * Horizontal tanks (large propane tanks):
     * - Shaped like cylinder with hemispheres on ends
     * - Formula: https://www.vcalc.com/wiki/Volume-of-a-Sphere-Cap and volume-of-horizontal-cylinder
     * 
     * Credits:
     * - jrhelbert: Volumetric calculation formulas for accurate tank percentage
     *   (https://community.home-assistant.io/t/add-tank-percentage-to-mopeka-integration/531322/34)
     * 
     * @param tankType Tank configuration
     * @param measuredDepthMm Distance from sensor (at bottom) to liquid surface in mm
     * @return Fill percentage (0-100)
     */
    fun calculateTankPercentage(tankType: TankType, measuredDepthMm: Double): Double {
        // Adjust for wall thickness (sensor measures from inside bottom of tank)
        val fillDepth = measuredDepthMm - tankType.wallThicknessMm
        
        if (fillDepth < 0) return 0.0
        
        val R = tankType.radiusMm
        val sideLength = tankType.sideLengthMm
        
        return when (tankType.orientation) {
            TankOrientation.VERTICAL -> calculateVerticalTankPercentage(fillDepth, R, sideLength)
            TankOrientation.HORIZONTAL -> calculateHorizontalTankPercentage(fillDepth, R, sideLength)
        }
    }
    
    /**
     * Calculate percentage for vertical tank (ellipsoid caps + cylinder)
     * 
     * Reference: https://community.home-assistant.io/t/add-tank-percentage-to-mopeka-integration/531322/34
     * Credit: jrhelbert's vertical tank template
     */
    private fun calculateVerticalTankPercentage(fillDepth: Double, R: Double, sideLength: Double): Double {
        val pi = Math.PI
        
        // A 2:1 Ellipsoid has c value that is half of its a/b values (a and b are the radius)
        val E_a = R
        val E_b = R
        val E_c = R / 2.0
        
        // Tank height from bottom to top
        val H_tank = sideLength + E_c
        
        if (fillDepth > H_tank) return 100.0
        
        // Calculate max tank volume (mm^3)
        val hemiEllipsoidVolume = (2.0 / 3.0) * pi * E_a * E_b * E_c
        val cylinderVolume = sideLength * pi * R * R
        val maxVolume = 2 * hemiEllipsoidVolume + cylinderVolume
        
        // Calculate fill volume based on which region the level is in
        val fillVolume = when {
            // Bottom ellipsoid region
            fillDepth in 0.0..E_c -> {
                // Ellipsoidal cap volume: https://www.vcalc.com/wiki/ellipsoid-cap-volume
                pi * E_a * E_b * (
                    (2.0/3.0 * E_c) - E_c + fillDepth + 
                    (Math.pow(E_c - fillDepth, 3.0) / (3.0 * E_c * E_c))
                )
            }
            // Middle cylinder region
            fillDepth in E_c..(E_c + sideLength) -> {
                hemiEllipsoidVolume + (fillDepth - E_c) * pi * R * R
            }
            // Top ellipsoid region
            fillDepth in (E_c + sideLength)..H_tank -> {
                maxVolume - (pi * E_a * E_b * (
                    (2.0/3.0 * E_c) - E_c + (H_tank - fillDepth) + 
                    (Math.pow(E_c - (H_tank - fillDepth), 3.0) / (3.0 * E_c * E_c))
                ))
            }
            else -> -maxVolume / 100.0  // Invalid
        }
        
        if (fillVolume < 0) return 0.0
        
        return ((100.0 * fillVolume / maxVolume).coerceIn(0.0, 100.0))
    }
    
    /**
     * Calculate percentage for horizontal tank (spherical caps + cylinder)
     * 
     * Reference: https://community.home-assistant.io/t/add-tank-percentage-to-mopeka-integration/531322/34
     * User: jrhelbert's horizontal tank template
     */
    private fun calculateHorizontalTankPercentage(fillDepth: Double, R: Double, sideLength: Double): Double {
        val pi = Math.PI
        
        if (fillDepth > 2 * R) return 100.0
        if (fillDepth < 0) return 0.0
        
        // Calculate max tank volume (spherical ends + cylinder middle)
        val sphericalVolume = (4.0 / 3.0) * pi * R * R * R
        val cylinderVolume = sideLength * pi * R * R
        val maxVolume = sphericalVolume + cylinderVolume
        
        // Calculate fill volume
        // Spherical cap volume: https://www.vcalc.com/wiki/Volume-of-a-Sphere-Cap
        val fillSphericalVolume = (pi / 3.0) * fillDepth * fillDepth * (3 * R - fillDepth)
        
        // Horizontal cylinder partial volume: https://www.vcalc.com/wiki/volume-of-horizontal-cylinder
        val fillCylinderVolume = sideLength * (
            R * R * Math.acos((R - fillDepth) / R) - 
            (R - fillDepth) * Math.sqrt(2 * R * fillDepth - fillDepth * fillDepth)
        )
        
        val fillVolume = fillSphericalVolume + fillCylinderVolume
        
        return ((100.0 * fillVolume / maxVolume).coerceIn(0.0, 100.0))
    }
}
