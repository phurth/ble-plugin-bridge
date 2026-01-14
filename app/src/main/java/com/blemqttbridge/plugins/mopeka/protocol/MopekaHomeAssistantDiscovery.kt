package com.blemqttbridge.plugins.mopeka.protocol

/**
 * Home Assistant MQTT discovery helper for Mopeka sensors
 * 
 * Generates MQTT discovery payloads following Home Assistant's
 * MQTT discovery specification for automatic entity registration.
 */
object MopekaHomeAssistantDiscovery {
    
    /**
     * Generate a Home Assistant discovery entity
     * 
     * @param macAddress Device MAC address (used in unique_id)
     * @param entityType Type of entity (tank_level, battery, temperature, etc.)
     * @param displayName Human-readable name for Home Assistant UI
     * @param baseTopic MQTT base topic for this device (e.g., "mopeka/AA:BB:CC:DD:EE:FF")
     * @param topicPrefix MQTT topic prefix (e.g., "homeassistant")
     * @param unitOfMeasurement Unit (%, mm, °C, etc.)
     * @param deviceClass Home Assistant device class for icons/UI
     * @param icon Optional Material Design Icon
     * @return JSON discovery payload
     */
    fun generateDiscoveryJson(
        macAddress: String,
        entityType: String,
        displayName: String,
        baseTopic: String,
        topicPrefix: String = "homeassistant",
        unitOfMeasurement: String? = null,
        deviceClass: String? = null,
        icon: String? = null
    ): String {
        val cleanMac = macAddress.replace(":", "").uppercase()
        val uniqueId = "mopeka_${cleanMac}_$entityType"
        val stateTopic = "$topicPrefix/$baseTopic/$entityType"
        
        return buildString {
            append("{")
            append("\"name\":\"$displayName\",")
            append("\"state_topic\":\"$stateTopic\",")
            append("\"unique_id\":\"$uniqueId\",")
            
            if (unitOfMeasurement != null) {
                append("\"unit_of_measurement\":\"$unitOfMeasurement\",")
            }
            
            if (deviceClass != null) {
                append("\"device_class\":\"$deviceClass\",")
            }
            
            if (icon != null) {
                append("\"icon\":\"$icon\",")
            }
            
            // Device grouping (Home Assistant groups related entities)
            append("\"device\":{")
            append("\"identifiers\":[\"mopeka_$cleanMac\"],")
            append("\"name\":\"Mopeka Tank Sensor $macAddress\",")
            append("\"model\":\"Mopeka Pro Series\",")
            append("\"manufacturer\":\"Mopeka\"")
            append("},")
            
            // Home Assistant connection info
            append("\"availability_topic\":\"$topicPrefix/$baseTopic/availability\",")
            append("\"payload_available\":\"online\",")
            append("\"payload_not_available\":\"offline\"")
            
            append("}")
        }
    }
    
    /**
     * Generate all discovery payloads for a Mopeka sensor
     * 
     * Entities:
     * - Battery (%) 
     * - Temperature (°C) - HA converts to user preference
     * - Tank Level (%) - calculated using geometric volume formulas, with distance as JSON attribute
     * - Read Quality (0-3) - diagnostic sensor showing ultrasonic reading quality
     */
    fun generateAllDiscoveryPayloads(
        macAddress: String,
        mediumType: MopekaConstants.MediumType,
        baseTopic: String
    ): Map<String, String> {
        val cleanMac = macAddress.replace(":", "").uppercase()
        val discoveryPrefix = "homeassistant"
        
        return mapOf(
            // Battery percentage (diagnostic sensor)
            "$discoveryPrefix/sensor/mopeka_$cleanMac/battery/config" to
                generateBatteryDiscoveryJson(
                    macAddress,
                    baseTopic
                ),
            
            // Temperature (Celsius - HA handles conversion to F if user prefers)
            "$discoveryPrefix/sensor/mopeka_$cleanMac/temperature/config" to
                generateDiscoveryJson(
                    macAddress,
                    "temperature",
                    "Temperature",
                    baseTopic,
                    unitOfMeasurement = "°C",
                    deviceClass = "temperature",
                    icon = "mdi:thermometer"
                ),
            
            // Tank level as percentage (0-100%) calculated from geometric volume
            // More useful and accurate than distance due to non-linear tank shapes
            // Includes distance as JSON attribute
            "$discoveryPrefix/sensor/mopeka_$cleanMac/tank_level/config" to
                generateTankLevelDiscoveryJson(
                    macAddress,
                    baseTopic
                ),
            
            // Read quality (0-3 diagnostic sensor)
            "$discoveryPrefix/sensor/mopeka_$cleanMac/quality/config" to
                generateQualityDiscoveryJson(
                    macAddress,
                    baseTopic
                ),
            
            // Data healthy binary sensor (diagnostic)
            "$discoveryPrefix/binary_sensor/mopeka_$cleanMac/data_healthy/config" to
                generateDataHealthyDiscoveryJson(
                    macAddress,
                    baseTopic
                )
        )
    }
    
    /**
     * Generate battery sensor discovery (diagnostic sensor)
     */
    private fun generateBatteryDiscoveryJson(
        macAddress: String,
        baseTopic: String
    ): String {
        val cleanMac = macAddress.replace(":", "").uppercase()
        val uniqueId = "mopeka_${cleanMac}_battery"
        val discoveryPrefix = "homeassistant"
        val stateTopic = "$discoveryPrefix/$baseTopic/battery"
        
        return buildString {
            append("{")
            append("\"name\":\"Battery\",")
            append("\"state_topic\":\"$stateTopic\",")
            append("\"unique_id\":\"$uniqueId\",")
            append("\"unit_of_measurement\":\"%\",")
            append("\"device_class\":\"battery\",")
            append("\"entity_category\":\"diagnostic\",")  // Mark as diagnostic
            append("\"state_class\":\"measurement\",")
            
            // Device grouping
            append("\"device\":{")
            append("\"identifiers\":[\"mopeka_$cleanMac\"],")
            append("\"name\":\"Mopeka Tank Sensor $macAddress\",")
            append("\"model\":\"Mopeka Pro Series\",")
            append("\"manufacturer\":\"Mopeka\"")
            append("},")
            
            // Home Assistant connection info
            append("\"availability_topic\":\"$discoveryPrefix/$baseTopic/availability\",")
            append("\"payload_available\":\"online\",")
            append("\"payload_not_available\":\"offline\"")
            
            append("}")
        }
    }
    
    /**
     * Generate tank level discovery with JSON attributes for distance
     */
    private fun generateTankLevelDiscoveryJson(
        macAddress: String,
        baseTopic: String
    ): String {
        val cleanMac = macAddress.replace(":", "").uppercase()
        val uniqueId = "mopeka_${cleanMac}_tank_level"
        val discoveryPrefix = "homeassistant"
        val stateTopic = "$discoveryPrefix/$baseTopic/tank_level"
        
        return buildString {
            append("{")
            append("\"name\":\"Tank Level\",")
            append("\"state_topic\":\"$stateTopic\",")
            append("\"unique_id\":\"$uniqueId\",")
            append("\"unit_of_measurement\":\"%\",")
            append("\"icon\":\"mdi:propane-tank\",")
            append("\"value_template\":\"{{ value_json.value }}\",")  // Extract value from JSON
            append("\"json_attributes_topic\":\"$stateTopic\",")  // Enable JSON attributes
            
            // Device grouping
            append("\"device\":{")
            append("\"identifiers\":[\"mopeka_$cleanMac\"],")
            append("\"name\":\"Mopeka Tank Sensor $macAddress\",")
            append("\"model\":\"Mopeka Pro Series\",")
            append("\"manufacturer\":\"Mopeka\"")
            append("},")
            
            // Home Assistant connection info
            append("\"availability_topic\":\"$discoveryPrefix/$baseTopic/availability\",")
            append("\"payload_available\":\"online\",")
            append("\"payload_not_available\":\"offline\"")
            
            append("}")
        }
    }
    
    /**
     * Generate quality sensor discovery (diagnostic sensor, 0-3 scale)
     */
    private fun generateQualityDiscoveryJson(
        macAddress: String,
        baseTopic: String
    ): String {
        val cleanMac = macAddress.replace(":", "").uppercase()
        val uniqueId = "mopeka_${cleanMac}_quality"
        val discoveryPrefix = "homeassistant"
        val stateTopic = "$discoveryPrefix/$baseTopic/quality"
        
        return buildString {
            append("{")
            append("\"name\":\"Read Quality\",")
            append("\"state_topic\":\"$stateTopic\",")
            append("\"unique_id\":\"$uniqueId\",")
            append("\"icon\":\"mdi:signal\",")
            append("\"entity_category\":\"diagnostic\",")  // Mark as diagnostic
            append("\"state_class\":\"measurement\",")
            
            // Device grouping
            append("\"device\":{")
            append("\"identifiers\":[\"mopeka_$cleanMac\"],")
            append("\"name\":\"Mopeka Tank Sensor $macAddress\",")
            append("\"model\":\"Mopeka Pro Series\",")
            append("\"manufacturer\":\"Mopeka\"")
            append("},")
            
            // Home Assistant connection info
            append("\"availability_topic\":\"$discoveryPrefix/$baseTopic/availability\",")
            append("\"payload_available\":\"online\",")
            append("\"payload_not_available\":\"offline\"")
            
            append("}")
        }
    }
    
    /**
     * Generate data_healthy binary sensor discovery (diagnostic sensor)
     */
    private fun generateDataHealthyDiscoveryJson(
        macAddress: String,
        baseTopic: String
    ): String {
        val cleanMac = macAddress.replace(":", "").uppercase()
        val uniqueId = "mopeka_${cleanMac}_data_healthy"
        val discoveryPrefix = "homeassistant"
        val stateTopic = "$discoveryPrefix/$baseTopic/data_healthy"
        
        return buildString {
            append("{")
            append("\"name\":\"Data Healthy\",")
            append("\"state_topic\":\"$stateTopic\",")
            append("\"unique_id\":\"$uniqueId\",")
            append("\"device_class\":\"connectivity\",")
            append("\"entity_category\":\"diagnostic\",")  // Mark as diagnostic
            append("\"payload_on\":\"ON\",")
            append("\"payload_off\":\"OFF\",")
            
            // Device grouping
            append("\"device\":{")
            append("\"identifiers\":[\"mopeka_$cleanMac\"],")
            append("\"name\":\"Mopeka Tank Sensor $macAddress\",")
            append("\"model\":\"Mopeka Pro Series\",")
            append("\"manufacturer\":\"Mopeka\"")
            append("},")
            
            // Home Assistant connection info
            append("\"availability_topic\":\"$discoveryPrefix/$baseTopic/availability\",")
            append("\"payload_available\":\"online\",")
            append("\"payload_not_available\":\"offline\"")
            
            append("}")
        }
    }
}
