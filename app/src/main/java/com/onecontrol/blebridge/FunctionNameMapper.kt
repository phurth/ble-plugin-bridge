package com.onecontrol.blebridge

/**
 * Maps FUNCTION_NAME values (from IDS CAN protocol) to friendly device names.
 * Based on FUNCTION_NAME.cs from IDS.Core.IDS_CAN.Descriptors (4.6.4.0)
 */
object FunctionNameMapper {
    
    /**
     * Get a friendly name for a device based on its FunctionName and FunctionInstance.
     * 
     * @param functionName The numeric FUNCTION_NAME value (e.g., 41 = Living Room Ceiling Light)
     * @param functionInstance The instance number (used when name contains {0})
     * @return Friendly name like "Living Room Ceiling Light" or "Slide 1"
     */
    fun getFriendlyName(functionName: Int, functionInstance: Int): String {
        val baseName = functionNames[functionName] ?: "Unknown Device"
        
        // Replace {0} placeholder with instance number
        return if (baseName.contains("{0}")) {
            baseName.replace("{0}", functionInstance.toString())
        } else if (functionInstance > 0) {
            "$baseName $functionInstance"
        } else {
            baseName
        }
    }
    
    /**
     * Convert a friendly name to a valid MQTT topic segment.
     * Example: "Living Room Ceiling Light" -> "living_room_ceiling_light"
     */
    fun toMqttTopic(friendlyName: String): String {
        return friendlyName
            .lowercase()
            .replace(Regex("[^a-z0-9]+"), "_")
            .trim('_')
    }
    
    /**
     * Mapping of FUNCTION_NAME numeric values to friendly names.
     * Only includes common RV device types - unknown values will show as "Unknown Device".
     */
    private val functionNames = mapOf(
        // Water & Plumbing
        3 to "Gas Water Heater",
        4 to "Electric Water Heater",
        5 to "Water Pump",
        67 to "Fresh Tank",
        68 to "Grey Tank",
        69 to "Black Tank",
        295 to "Water Heater",
        296 to "Water Heaters",
        428 to "Fresh Tank Valve",
        429 to "Grey Tank Valve",
        430 to "Black Tank Valve",
        
        // Ventilation & Climate
        6 to "Bath Vent",
        143 to "Roof Vent",
        145 to "Kitchen Vent",
        281 to "Living Room Climate Zone",
        284 to "Front Bedroom Climate Zone",
        285 to "Rear Bedroom Climate Zone",
        398 to "Computer Fan",
        399 to "Battery Fan",
        
        // Awnings
        70 to "Front Awning",
        71 to "Rear Awning",
        72 to "Patio Awning",
        73 to "Door Awning",
        164 to "Driver Awning",
        165 to "Passenger Awning",
        300 to "Awning {0} (if equip)",
        319 to "Garage Awning",
        379 to "Front Awning",
        390 to "Slide Awning",
        
        // Slides
        74 to "Front Slide",
        75 to "Rear Slide",
        76 to "Driver Slide",
        77 to "Passenger Slide",
        78 to "Bedroom Slide",
        79 to "Living Slide",
        80 to "Kitchen Slide",
        81 to "Main Slide",
        170 to "Driver Side Slide",
        171 to "Off Door Side Slide",
        172 to "Main Room Slide",
        173 to "Driver Side Rear Slide",
        174 to "Off Door Side Rear Slide",
        175 to "Right Slide Room",
        176 to "Left Slide Room",
        299 to "Slide {0} (if equip)",
        315 to "Bathroom Slide",
        380 to "Dinette Slide",
        400 to "Right Slide Room",
        401 to "Left Slide Room",
        407 to "Door Side Sofa Slide",
        408 to "Off Door Side Sofa Slide",
        409 to "Rear Bed Slide",
        
        // Jacks & Leveling
        82 to "Front Jack",
        83 to "Rear Jack",
        84 to "Driver Side Jack",
        85 to "Passenger Side Jack",
        86 to "Leveling Jack",
        87 to "Landing Gear",
        404 to "Base Camp Leveler",
        
        // General Lights
        7 to "Light",
        8 to "Flood Light",
        9 to "Work Light",
        47 to "Security Light",
        48 to "Porch Light",
        61 to "Ceiling Light",
        62 to "Entry Light",
        65 to "Shower Light",
        88 to "Interior Light",
        89 to "Exterior Light",
        94 to "Step Light",
        95 to "Loading Light",
        96 to "Cargo Light",
        97 to "Accent Light",
        98 to "Cabinet Light",
        133 to "Exterior Lighting",
        177 to "Night Light",
        260 to "Reading Light",
        279 to "Front Reading Light",
        280 to "Rear Reading Light",
        289 to "Men's Light",
        290 to "Women's Light",
        291 to "Service Light",
        292 to "ODS Flood Light",
        293 to "Underbody Accent Light",
        294 to "Speaker Light",
        301 to "Awning Light {0} (if equip)",
        302 to "Interior Light {0} (if equip)",
        308 to "Rock Light",
        309 to "Chassis Light",
        310 to "Exterior Shower Light",
        360 to "Jack Lights",
        362 to "Interior Step Light",
        363 to "Exterior Step Light",
        366 to "Soffit Light",
        392 to "Central Lights",
        393 to "Right Side Lights",
        394 to "Left Side Lights",
        395 to "Right Scene Lights",
        396 to "Left Scene Lights",
        397 to "Rear Scene Lights",
        402 to "Dump Light",
        410 to "Theater Lights",
        411 to "Utility Cabinet Light",
        412 to "Chase Light",
        413 to "Floor Lights",
        414 to "Roof Top Tent Light",
        
        // Living Room Lights
        41 to "Living Room Ceiling Light",
        42 to "Living Room Sconce Light",
        43 to "Living Room Pendants Light",
        44 to "Living Room Bar Light",
        58 to "Living Room Light",
        311 to "Living Room Accent Light",
        
        // Kitchen Lights
        32 to "Kitchen Ceiling Light",
        33 to "Kitchen Sconce Light",
        34 to "Kitchen Pendants Light",
        35 to "Kitchen Range Light",
        36 to "Kitchen Counter Light",
        37 to "Kitchen Bar Light",
        38 to "Kitchen Island Light",
        39 to "Kitchen Chandelier Light",
        40 to "Kitchen Under Cabinet Light",
        59 to "Kitchen Light",
        406 to "Kitchen Pendant Light",
        
        // Bedroom Lights
        10 to "Front Bedroom Ceiling Light",
        11 to "Front Bedroom Overhead Light",
        12 to "Front Bedroom Vanity Light",
        13 to "Front Bedroom Sconce Light",
        14 to "Front Bedroom Loft Light",
        15 to "Rear Bedroom Ceiling Light",
        16 to "Rear Bedroom Overhead Light",
        17 to "Rear Bedroom Vanity Light",
        18 to "Rear Bedroom Sconce Light",
        19 to "Rear Bedroom Loft Light",
        20 to "Loft Light",
        56 to "Bunk Room Light",
        57 to "Bedroom Light",
        63 to "Bed Ceiling Light",
        64 to "Bedroom Lav Light",
        
        // Bathroom Lights
        23 to "Front Bathroom Light",
        24 to "Front Bathroom Vanity Light",
        25 to "Front Bathroom Ceiling Light",
        26 to "Front Bathroom Shower Light",
        27 to "Front Bathroom Sconce Light",
        28 to "Rear Bathroom Vanity Light",
        29 to "Rear Bathroom Ceiling Light",
        30 to "Rear Bathroom Shower Light",
        31 to "Rear Bathroom Sconce Light",
        50 to "Bathroom Light",
        51 to "Bathroom Vanity Light",
        52 to "Bathroom Ceiling Light",
        53 to "Bathroom Shower Light",
        54 to "Bathroom Sconce Light",
        
        // Hall Lights
        21 to "Front Hall Light",
        22 to "Rear Hall Light",
        55 to "Hall Light",
        
        // Garage Lights
        45 to "Garage Ceiling Light",
        46 to "Garage Cabinet Light",
        
        // Awning Lights
        49 to "Awning Light",
        385 to "Patio Awning Light",
        386 to "Garage Awning Light",
        387 to "Rear Awning Light",
        388 to "Side Awning Light",
        389 to "Slide Awning Light",
        391 to "Front Awning Light",
        
        // Locker Lights
        305 to "Front Locker Light",
        306 to "Rear Locker Light",
        
        // Galley/Lounge
        60 to "Lounge Light",
        66 to "Galley Light",
        
        // Flood Lights
        312 to "Rear Flood Light",
        313 to "Passenger Flood Light",
        314 to "Driver Flood Light",
        
        // Power & Battery
        92 to "Inverter Charger",
        93 to "Generator",
        154 to "Solar Panel",
        355 to "Auxiliary Battery",
        356 to "Chassis Battery",
        357 to "House Battery",
        358 to "Kitchen Battery",
        367 to "Battery Bank",
        368 to "RV Battery",
        369 to "Solar Battery",
        370 to "Tongue Battery",
        382 to "Inverter",
        383 to "Battery Heat",
        
        // Pumps & Motors
        90 to "Macerator Pump",
        91 to "Sump Pump",
        
        // Heaters
        135 to "Tank Heater",
        136 to "Furnace",
        137 to "Electric Heat",
        317 to "Yeti Package",
        318 to "Propane Locker",
        381 to "Holding Tanks Heater",
        
        // Other
        99 to "Entry Step",
        101 to "Utility",
        129 to "Switches",
        130 to "Lights",
        131 to "Levelers",
        132 to "Slides",
        134 to "Step",
        286 to "Bed Tilt",
        287 to "Front Bed Tilt",
        288 to "Rear Bed Tilt",
        307 to "Rear Aux Power",
        316 to "Roof Lift",
        320 to "Monitor Panel",
        321 to "Camera",
        384 to "Camera Power",
        405 to "Refrigerator",
        
        // Power Shades
        415 to "Upper Power Shades",
        416 to "Lower Power Shades",
        417 to "Living Room Power Shades",
        418 to "Bedroom Power Shades",
        419 to "Bathroom Power Shades",
        420 to "Bunk Power Shades",
        421 to "Loft Power Shades",
        422 to "Front Power Shades",
        423 to "Rear Power Shades",
        424 to "Main Power Shades",
        425 to "Garage Power Shades",
        426 to "Door Side Power Shades",
        427 to "Off Door Side Power Shades",
        
        // Temperature Sensors
        324 to "Accessory Temperature",
        325 to "Accessory Refrigerator",
        326 to "Accessory Fridge",
        327 to "Accessory Freezer",
        328 to "Accessory External",
        330 to "Refrigerator Temperature",
        331 to "Refrigerator Temperature Home",
        332 to "Freezer Temperature",
        333 to "Freezer Temperature Home",
        334 to "Cooler Temperature",
        335 to "Kitchen Temperature",
        336 to "Living Room Temperature",
        337 to "Bedroom Temperature",
        338 to "Master Bedroom Temperature",
        339 to "Garage Temperature",
        340 to "Basement Temperature",
        341 to "Bathroom Temperature",
        342 to "Storage Area Temperature",
        343 to "Drivers Area Temperature",
        344 to "Bunks Temperature"
    )
}

