package com.blemqttbridge.plugins.mopeka.protocol

import kotlin.math.roundToInt

/**
 * Mopeka tank sensor advertisement parser
 * 
 * Parses BLE manufacturer-specific advertisement data from Mopeka sensors.
 * 
 * Protocol decoding based on:
 * - https://github.com/Bluetooth-Devices/mopeka-iot-ble (Python reference implementation)
 * - https://github.com/spbrogan/mopeka_pro_check (Original protocol documentation)
 * 
 * Credits:
 * - sbrogan: Original work decoding the Mopeka sensor BLE protocol
 * - @bdraco and the Home Assistant community for the mopeka-iot-ble library
 * 
 * Advertisement format (10 bytes after manufacturer ID 0x0059):
 * Byte 0: Sync byte/Model ID (0x03-0x0C)
 * Byte 1: Battery raw value (NOT percentage - needs voltage conversion)
 * Byte 2: Temperature raw (bits 0-6) + button press (bit 7)
 * Byte 3-4: Distance reading (14-bit little-endian in millimeters) + quality (top 2 bits of byte 4)
 * Byte 5-7: Reserved/unused
 * Byte 8: Accelerometer X
 * Byte 9: Accelerometer Y
 * 
 * MIT License applies.
 */
object MopekaAdvertisementParser {
    
    private const val MIN_BATTERY = 0
    private const val MAX_BATTERY = 100
    private const val MIN_QUALITY = 0
    private const val MAX_QUALITY = 100
    private const val MIN_TEMPERATURE = -50
    private const val MAX_TEMPERATURE = 50
    
    /**
     * Parse Mopeka advertisement manufacturer data
     * 
     * @param macAddress Device MAC address
     * @param manufacturerData Raw bytes from advertisement (starting after manufacturer ID)
     * @return Parsed sensor data, or null if parsing failed
     */
    fun parse(macAddress: String, manufacturerData: ByteArray): MopekaSensorData? {
        if (manufacturerData.size < MopekaConstants.AdvertisementLayout.MIN_LENGTH) {
            return null
        }
        
        try {
            val syncByte = manufacturerData[MopekaConstants.AdvertisementLayout.SYNC_BYTE_INDEX].toInt() and 0xFF
            
            if (!isValidSyncByte(syncByte)) {
                return null
            }
            
            // Extract raw values
            val battery = extractBattery(manufacturerData)
            val distance = extractDistance(manufacturerData)
            val temperature = extractTemperature(manufacturerData)
            val quality = extractQuality(manufacturerData)
            val accelX = extractAccelerometerX(manufacturerData)
            val accelY = extractAccelerometerY(manufacturerData)
            
            // Validate ranges
            val warnings = mutableListOf<String>()
            if (battery < MIN_BATTERY || battery > MAX_BATTERY) {
                warnings.add("Battery out of range: $battery%")
            }
            if (temperature < MIN_TEMPERATURE || temperature > MAX_TEMPERATURE) {
                warnings.add("Temperature out of range: ${temperature}°C")
            }
            if (quality < MIN_QUALITY || quality > MAX_QUALITY) {
                warnings.add("Quality out of range: $quality%")
            }
            
            return MopekaSensorData(
                macAddress = macAddress,
                modelId = syncByte,
                modelName = MopekaConstants.ModelNames.getName(syncByte),
                batteryLevel = battery.coerceIn(MIN_BATTERY, MAX_BATTERY),
                distanceRaw = distance,
                temperature = temperature.coerceIn(MIN_TEMPERATURE, MAX_TEMPERATURE),
                quality = quality.coerceIn(MIN_QUALITY, MAX_QUALITY),
                accelerometerX = accelX,
                accelerometerY = accelY,
                tankLevel = 0f,  // Will be calculated after medium type is determined
                compensatedDistance = distance,  // Will be updated after temp compensation
                isValid = warnings.isEmpty(),
                validationWarnings = warnings
            )
        } catch (e: Exception) {
            return null
        }
    }
    
    /**
     * Apply temperature compensation to tank level reading
     * Reference: tank_level_and_temp_to_mm() in mopeka-iot-ble
     * 
     * @param tankLevelRaw Raw distance reading (14-bit value from advertisement)
     * @param tempRaw Raw temperature value (before subtracting 40 for Celsius conversion)
     * @param mediumType Type of medium being measured
     * @return Temperature-compensated distance in millimeters
     */
    fun applyTemperatureCompensation(
        tankLevelRaw: Int,
        tempRaw: Int,
        mediumType: MopekaConstants.MediumType
    ): Int {
        val coefs = MopekaConstants.TankLevelCoefficients.getCoefficients(mediumType)
        val compensationFactor = coefs.c0 + (coefs.c1 * tempRaw) + (coefs.c2 * tempRaw * tempRaw)
        return (tankLevelRaw * compensationFactor).toInt()
    }
    
    /**
     * Extract battery percentage from advertisement
     * Reference: battery_to_percentage() in mopeka-iot-ble
     * Formula: ((raw / 32.0 - 2.2) / 0.65) * 100, clamped to 0-100
     */
    private fun extractBattery(data: ByteArray): Int {
        val rawValue = data[MopekaConstants.AdvertisementLayout.BATTERY_RAW_INDEX].toInt() and 0xFF
        val voltage = rawValue / 32.0
        val percentage = ((voltage - 2.2) / 0.65) * 100.0
        return percentage.roundToInt().coerceIn(0, 100)
    }
    
    /**
     * Extract ultrasonic distance from advertisement (millimeters)
     * Reference: tank_level = ((int(data[4]) << 8) + data[3]) & 0x3FFF
     * This is a 14-bit value (bits 0-13 from bytes 3-4, little-endian)
     * Top 2 bits of byte 4 are used for quality indicator
     */
    private fun extractDistance(data: ByteArray): Int {
        val low = data[MopekaConstants.AdvertisementLayout.DISTANCE_LOW_INDEX].toInt() and 0xFF
        val high = data[MopekaConstants.AdvertisementLayout.DISTANCE_HIGH_AND_QUALITY_INDEX].toInt() and 0xFF
        val raw16bit = (high shl 8) or low
        return raw16bit and 0x3FFF  // Mask to 14 bits (0-16383) - result is in millimeters
    }
    
    /**
     * Extract temperature from advertisement (Celsius)
     * Reference: temp = data[2] & 0x7F; temp_celsius = temp - 40
     * Byte 2 bits 0-6 contain raw temp value (bit 7 is button press)
     * Temperature in Celsius = (raw & 0x7F) - 40
     * 
     * Note: Home Assistant will convert to user's preferred unit (F/C)
     */
    private fun extractTemperature(data: ByteArray): Int {
        val rawTemp = data[MopekaConstants.AdvertisementLayout.TEMPERATURE_AND_BUTTON_INDEX].toInt() and 0x7F
        return rawTemp - 40  // Convert to Celsius
    }
    
    /**
     * Extract reading quality from advertisement
     * Reference: reading_quality = data[4] >> 6; percentage = (reading_quality / 3) * 100
     * Top 2 bits of byte 4 contain quality (0-3 raw value)
     * Convert to percentage: 0=0%, 1=33%, 2=67%, 3=100%
     */
    private fun extractQuality(data: ByteArray): Int {
        val qualityRaw = (data[MopekaConstants.AdvertisementLayout.DISTANCE_HIGH_AND_QUALITY_INDEX].toInt() and 0xFF) shr 6
        return ((qualityRaw / 3.0f) * 100f).roundToInt()
    }
    
    /**
     * Extract accelerometer X reading
     */
    private fun extractAccelerometerX(data: ByteArray): Int {
        return data[MopekaConstants.AdvertisementLayout.ACCELEROMETER_X_INDEX].toInt() and 0xFF
    }
    
    /**
     * Extract accelerometer Y reading
     */
    private fun extractAccelerometerY(data: ByteArray): Int {
        return data[MopekaConstants.AdvertisementLayout.ACCELEROMETER_Y_INDEX].toInt() and 0xFF
    }
    
    private fun isValidSyncByte(syncByte: Int): Boolean {
        return syncByte in 0x03..0x0C
    }
}
