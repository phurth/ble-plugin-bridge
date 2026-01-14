package com.blemqttbridge.plugins.mopeka.protocol

/**
 * Structured data from a Mopeka sensor advertisement
 * 
 * Note: tankLevel is the calculated fill percentage (0-100%) using geometric
 * volume formulas based on tank type. Distance is stored in distanceRaw.
 */
data class MopekaSensorData(
    // Device identification
    val macAddress: String,
    val modelId: Int,
    val modelName: String,
    
    // Sensor readings
    val batteryLevel: Int,        // 0-100 %
    val distanceRaw: Int,         // Raw ultrasonic reading in mm (before temp compensation)
    val temperature: Int,         // Signed, Celsius
    val quality: Int,             // 0-100 %
    val accelerometerX: Int,      // For orientation detection
    val accelerometerY: Int,
    
    // Calculated values
    val tankLevel: Float,         // Tank fill percentage (0-100%) calculated from geometric volume
    val compensatedDistance: Int = distanceRaw,  // Temperature-compensated distance in mm
    val mediumType: MopekaConstants.MediumType = MopekaConstants.MediumType.PROPANE,
    
    // Metadata
    val timestamp: Long = System.currentTimeMillis(),
    val isValid: Boolean = true,
    val validationWarnings: List<String> = emptyList()
) {
    fun toMqttJson(): String {
        return buildString {
            append("{")
            append("\"mac_address\":\"$macAddress\",")
            append("\"model\":\"$modelName\",")
            append("\"tank_level\":$tankLevel,")
            append("\"distance_mm\":$distanceRaw,")
            append("\"temperature_c\":$temperature,")
            append("\"battery_percent\":$batteryLevel,")
            append("\"quality_percent\":$quality,")
            append("\"medium_type\":\"${mediumType.id}\",")
            append("\"timestamp\":$timestamp")
            append("}")
        }
    }
    
    companion object {
        fun invalid(macAddress: String, reason: String): MopekaSensorData {
            return MopekaSensorData(
                macAddress = macAddress,
                modelId = 0,
                modelName = "Invalid",
                batteryLevel = 0,
                distanceRaw = 0,
                temperature = 0,
                quality = 0,
                accelerometerX = 0,
                accelerometerY = 0,
                tankLevel = 0f,
                isValid = false,
                validationWarnings = listOf(reason)
            )
        }
    }
}
