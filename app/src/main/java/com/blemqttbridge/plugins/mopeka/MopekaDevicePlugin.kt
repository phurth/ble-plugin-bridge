package com.blemqttbridge.plugins.mopeka

import android.bluetooth.BluetoothDevice
import android.bluetooth.BluetoothGatt
import android.bluetooth.BluetoothGattCallback
import android.bluetooth.le.ScanRecord
import android.content.Context
import com.blemqttbridge.core.interfaces.BleDevicePlugin
import com.blemqttbridge.core.interfaces.MqttPublisher
import com.blemqttbridge.core.interfaces.PluginConfig
import com.blemqttbridge.plugins.mopeka.protocol.MopekaConstants
import com.blemqttbridge.plugins.mopeka.protocol.MopekaAdvertisementParser
import com.blemqttbridge.plugins.mopeka.protocol.MopekaSensorData
import com.blemqttbridge.plugins.mopeka.protocol.MopekaHomeAssistantDiscovery
import com.blemqttbridge.util.DebugLog

/**
 * Mopeka Tank Sensor Plugin
 * 
 * Integrates Mopeka Pro Check/Pro Plus/Pro H2O Bluetooth tank level sensors.
 * Uses passive BLE advertisement scanning - no GATT connection required.
 * 
 * Key difference from GoPower: 
 * - Parses manufacturer-specific data directly from BLE advertisements
 * - No connection lifecycle needed
 * - Much simpler implementation (~300-400 lines vs GoPower's ~900)
 */
class MopekaDevicePlugin : BleDevicePlugin {
    
    companion object {
        const val PLUGIN_ID = "mopeka"
        const val PLUGIN_VERSION = "1.0.0"
        const val PLUGIN_DISPLAY_NAME = "Mopeka Tank Sensor"
    }
    
    override val pluginId: String = PLUGIN_ID
    override var instanceId: String = PLUGIN_ID  // Same as pluginId by default
    override val supportsMultipleInstances: Boolean = true
    override val displayName: String = PLUGIN_DISPLAY_NAME
    
    // Configuration
    private var configuredMacAddress: String? = null  // Optional: filter by MAC
    private var mediumType: MopekaConstants.MediumType = MopekaConstants.MediumType.PROPANE
    private var tankType: MopekaConstants.TankType = MopekaConstants.TankType.TANK_20LB_V
    private var minimumQuality: Int = MopekaConstants.QualityThresholds.ZERO
    
    // References
    private var context: Context? = null
    private var mqttPublisher: MqttPublisher? = null
    
    // Track which devices have had discovery published (to avoid republishing)
    private val discoveredDevices = mutableSetOf<String>()
    
    override fun initializeWithConfig(instanceId: String, config: Map<String, String>) {
        this.instanceId = instanceId
        
        // Extract device-specific configuration from instance config
        configuredMacAddress = config["sensor_mac"]?.ifEmpty { null }
        val mediumTypeId = config["medium_type"] ?: "propane"
        mediumType = MopekaConstants.MediumType.fromId(mediumTypeId)
        val tankTypeId = config["tank_type"] ?: "20lb_v"
        tankType = MopekaConstants.TankType.fromId(tankTypeId)
        minimumQuality = config["minimum_quality"]?.toIntOrNull() ?: MopekaConstants.QualityThresholds.ZERO
        
        DebugLog.i("Mopeka", "Initializing instance: $instanceId (MAC: $configuredMacAddress, medium: $mediumType, tank: $tankType, minQuality: $minimumQuality)")
    }
    
    override fun initialize(context: Context?, config: PluginConfig) {
        this.context = context
        
        DebugLog.d("Mopeka", "initialize() called with config parameters: ${config.parameters}")
        
        // Parse configuration (legacy path)
        configuredMacAddress = config.getString("sensor_mac", "").ifEmpty { null }
        val mediumTypeId = config.getString("medium_type", "propane")
        mediumType = MopekaConstants.MediumType.fromId(mediumTypeId)
        val tankTypeId = config.getString("tank_type", "20lb_v")
        tankType = MopekaConstants.TankType.fromId(tankTypeId)
        minimumQuality = config.getInt("minimum_quality", MopekaConstants.QualityThresholds.ZERO)
        
        DebugLog.i("Mopeka", "Initialized: MAC=$configuredMacAddress, medium=$mediumType, tank=$tankType, minQuality=$minimumQuality")
    }
    
    /**
     * Set the MQTT publisher for this passive plugin.
     * Called by BaseBleService before processing advertisements.
     */
    fun setMqttPublisher(publisher: MqttPublisher) {
        this.mqttPublisher = publisher
        DebugLog.d("Mopeka", "MQTT publisher set for instance $instanceId")
    }    override fun destroy() {
        DebugLog.i("Mopeka", "Destroyed")
    }
    
    /**
     * Check if this device is a Mopeka sensor
     * 
     * Matches by:
     * 1. Configured MAC address if specified (for single sensor mode)
     * 2. Mopeka manufacturer ID (0x0059) in advertisement (for multi-sensor mode)
     */
    override fun matchesDevice(device: BluetoothDevice, scanRecord: ScanRecord?): Boolean {
        // If MAC is configured, match only that specific sensor
        if (!configuredMacAddress.isNullOrBlank()) {
            val matches = device.address.equals(configuredMacAddress, ignoreCase = true)
            DebugLog.d("Mopeka", "matchesDevice: Configured MAC=$configuredMacAddress, device=${device.address}, match=$matches")
            return matches
        }
        
        // No MAC configured - detect any Mopeka sensor by manufacturer ID
        if (scanRecord == null) {
            DebugLog.d("Mopeka", "matchesDevice: No scan record for ${device.address}")
            return false
        }
        
        // Check manufacturer-specific data for Mopeka ID (0x0059)
        val manufacturerData = scanRecord.getManufacturerSpecificData(MopekaConstants.MANUFACTURER_ID)
        val isMopeka = manufacturerData != null
        
        if (isMopeka) {
            DebugLog.i("Mopeka", "✅ Detected Mopeka sensor: ${device.address} (mfgId=0x${MopekaConstants.MANUFACTURER_ID.toString(16)})")
        } else {
            DebugLog.d("Mopeka", "matchesDevice: ${device.address} is not a Mopeka sensor")
        }
        
        return isMopeka
    }
    
    override fun getConfiguredDevices(): List<String> {
        // Mopeka uses passive scanning, no specific device list
        return emptyList()
    }
    
    /**
     * Mopeka doesn't use GATT connection - this is a no-op
     * The actual data parsing happens during BLE scan
     */
    override fun createGattCallback(
        device: BluetoothDevice,
        context: Context,
        mqttPublisher: MqttPublisher,
        onDisconnect: (BluetoothDevice, Int) -> Unit
    ): BluetoothGattCallback {
        // Store MQTT publisher for use in scan result handling
        this.mqttPublisher = mqttPublisher
        this.context = context
        
        // Publish Home Assistant discovery when we first see this device
        publishDiscovery(device.address, mqttPublisher)
        
        // Return a minimal GATT callback (never used for Mopeka)
        return object : BluetoothGattCallback() {
            override fun onConnectionStateChange(gatt: BluetoothGatt?, status: Int, newState: Int) {
                // Not used - Mopeka is passive scanning only
            }
        }
    }
    
    /**
     * Get the MQTT base topic for this device
     * Example: "mopeka/AA:BB:CC:DD:EE:FF"
     */
    override fun getMqttBaseTopic(device: BluetoothDevice): String {
        return "mopeka/${device.address}"
    }
    
    override suspend fun handleCommand(
        device: BluetoothDevice,
        commandTopic: String,
        payload: String
    ): Result<Unit> {
        // Mopeka sensors are read-only, no commands
        return Result.failure(Exception("Mopeka sensors do not support commands"))
    }
    
    override fun onGattConnected(device: BluetoothDevice, gatt: BluetoothGatt) {
        // Not used - Mopeka doesn't establish GATT connections
    }
    
    override fun getDiscoveryPayloads(device: BluetoothDevice): List<Pair<String, String>> {
        val baseTopic = getMqttBaseTopic(device)
        val discoveries = MopekaHomeAssistantDiscovery.generateAllDiscoveryPayloads(
            device.address,
            mediumType,
            baseTopic
        )
        return discoveries.toList()
    }
    
    override fun onDeviceDisconnected(device: BluetoothDevice) {
        // Clean up device-specific state if needed
        DebugLog.d("Mopeka", "Device disconnected: ${device.address}")
    }
    
    /**
     * Parse advertisement data and publish to MQTT
     * This would be called from the BLE scan handler when a Mopeka device is detected
     */
    fun handleScanResult(device: BluetoothDevice, scanRecord: ScanRecord?) {
        DebugLog.d("Mopeka", "📥 handleScanResult called for ${device.address}, scanRecord=${scanRecord != null}")
        val data = scanRecord?.bytes ?: run {
            DebugLog.d("Mopeka", "⚠️ No scan record data")
            return
        }
        DebugLog.d("Mopeka", "📄 Scan record data length: ${data.size} bytes")
        val manufacturerData = parseManufacturerData(data, MopekaConstants.MANUFACTURER_ID)
            ?: run {
                DebugLog.d("Mopeka", "⚠️ No manufacturer data for ID ${MopekaConstants.MANUFACTURER_ID}")
                return
            }
        DebugLog.d("Mopeka", "✅ Found manufacturer data: ${manufacturerData.size} bytes")
        
        var sensorData = MopekaAdvertisementParser.parse(device.address, manufacturerData) ?: run {
            DebugLog.d("Mopeka", "⚠️ Failed to parse sensor data")
            return
        }
        DebugLog.d("Mopeka", "✅ Parsed sensor data: temp=${sensorData.temperature}°C, quality=${sensorData.quality}")
        
        // Quality check
        if (sensorData.quality < minimumQuality) {
            DebugLog.d("Mopeka", "Skipping ${device.address}: quality ${sensorData.quality} < $minimumQuality")
            return
        }
        DebugLog.d("Mopeka", "✅ Quality check passed (${sensorData.quality} >= $minimumQuality)")
        
        // Extract raw temperature for temperature compensation (before -40 conversion)
        val rawTemp = (manufacturerData[MopekaConstants.AdvertisementLayout.TEMPERATURE_AND_BUTTON_INDEX].toInt() and 0x7F)
        
        // Apply temperature compensation to get accurate distance in mm
        val compensatedDistance = MopekaAdvertisementParser.applyTemperatureCompensation(
            sensorData.distanceRaw,
            rawTemp,
            mediumType
        )
        DebugLog.d("Mopeka", "🌡️ Temperature compensation: raw=${sensorData.distanceRaw}mm, compensated=${compensatedDistance}mm (temp_raw=$rawTemp)")
        
        // Calculate tank level percentage using geometric formulas (from HA community)
        // Uses precise volume calculations for vertical/horizontal tank shapes
        val tankPercentage = MopekaConstants.calculateTankPercentage(tankType, compensatedDistance.toDouble())
        DebugLog.d("Mopeka", "📊 Tank percentage: ${String.format("%.1f", tankPercentage)}% (${compensatedDistance}mm, tank=${tankType.displayName})")
        
        // Store percentage as tank level (not mm anymore)
        val tankLevel = tankPercentage.toFloat()
        DebugLog.d("Mopeka", "✅ Tank level (percentage): ${String.format("%.1f", tankLevel)}% (medium=$mediumType)")
        
        // Update with calculated tank level and compensated distance
        sensorData = sensorData.copy(
            tankLevel = tankLevel,
            compensatedDistance = compensatedDistance,
            mediumType = mediumType
        )
        
        // Publish Home Assistant discovery on first detection
        if (!discoveredDevices.contains(device.address)) {
            mqttPublisher?.let { publisher ->
                DebugLog.i("Mopeka", "📢 Publishing Home Assistant discovery for ${device.address}")
                publishDiscovery(device.address, publisher)
                discoveredDevices.add(device.address)
            }
        }
        
        DebugLog.d("Mopeka", "📡 Publishing to MQTT...")
        publishToMqtt(sensorData)
        
        // Update plugin status to show green health indicator
        mqttPublisher?.updatePluginStatus(
            pluginId = instanceId,
            connected = false,  // Passive plugin, no connection
            authenticated = false,  // Passive plugin, no authentication
            dataHealthy = true  // We're receiving data
        )
    }
    
    /**
     * Parse Mopeka advertisement manufacturer data
     */
    private fun parseAdvertisement(macAddress: String, data: ByteArray): MopekaSensorData? {
        return MopekaAdvertisementParser.parse(macAddress, data)
    }
    
    /**
     * Publish sensor data to MQTT
     * 
     * Publishes only the 3 primary entities to match Home Assistant core integration:
     * - Battery (%)
     * - Temperature (°C) 
     * - Tank Level (%) - calculated using geometric volume formulas
     * 
     * Additional data (quality, model, distance, etc.) available in the combined state JSON.
     */
    private fun publishToMqtt(sensorData: MopekaSensorData) {
        DebugLog.d("Mopeka", "publishToMqtt called, mqttPublisher=$mqttPublisher")
        val publisher = mqttPublisher ?: run {
            DebugLog.w("Mopeka", "⚠️ mqttPublisher is null! Cannot publish to MQTT")
            return
        }
        
        val actualBaseTopic = "mopeka/${sensorData.macAddress}"
        DebugLog.d("Mopeka", "Publishing to topic: $actualBaseTopic")
        
        // Publish the primary sensor values
        // Battery percentage
        publisher.publishState("$actualBaseTopic/battery", sensorData.batteryLevel.toString(), false)
        
        // Temperature in Celsius (HA will convert to F if user prefers)
        publisher.publishState("$actualBaseTopic/temperature", sensorData.temperature.toString(), false)
        
        // Tank level as percentage with JSON attributes for distance
        val distanceInches = sensorData.compensatedDistance / 25.4
        val tankLevelJson = buildString {
            append("{")
            append("\"value\":${String.format("%.1f", sensorData.tankLevel)},")
            append("\"distance_mm\":${sensorData.compensatedDistance},")
            append("\"distance_in\":${String.format("%.1f", distanceInches)}")
            append("}")
        }
        publisher.publishState("$actualBaseTopic/tank_level", tankLevelJson, false)
        
        // Read quality as 0-3 diagnostic sensor
        val qualityRaw = (sensorData.quality / 33).coerceIn(0, 3)  // Convert 0-100% back to 0-3
        publisher.publishState("$actualBaseTopic/quality", qualityRaw.toString(), false)
        
        // Data healthy diagnostic (always ON when we receive data)
        publisher.publishState("$actualBaseTopic/data_healthy", "ON", true)
        
        // Publish combined state JSON (includes all data for debugging/advanced users)
        publisher.publishState("$actualBaseTopic/state", sensorData.toMqttJson(), false)
        
        // Publish availability (always online while we're receiving advertisements)
        publisher.publishAvailability("$actualBaseTopic/availability", true)
        
        DebugLog.d("Mopeka", "Published: $actualBaseTopic - Tank: ${String.format("%.1f", sensorData.tankLevel)}%, Temp: ${sensorData.temperature}°C, Battery: ${sensorData.batteryLevel}%")
    }
    
    /**
     * Publish Home Assistant MQTT discovery payloads for this device
     */
    private fun publishDiscovery(macAddress: String, publisher: MqttPublisher) {
        val baseTopic = "mopeka/$macAddress"
        val discoveries = MopekaHomeAssistantDiscovery.generateAllDiscoveryPayloads(
            macAddress,
            mediumType,
            baseTopic
        )
        
        for ((discoveryTopic, payload) in discoveries) {
            publisher.publishDiscovery(discoveryTopic, payload)
            DebugLog.d("Mopeka", "Published discovery: $discoveryTopic")
        }
        
        DebugLog.i("Mopeka", "Discovery published for $macAddress (medium: ${mediumType.displayName})")
    }
    
    /**
     * Helper: Parse manufacturer-specific data from BLE advertisement
     */
    private fun parseManufacturerData(scanRecord: ByteArray, manufacturerId: Int): ByteArray? {
        // BLE advertisement structure: type-length-value
        var i = 0
        while (i < scanRecord.size) {
            val length = scanRecord[i].toInt() and 0xFF
            if (length == 0) break
            
            if (i + length >= scanRecord.size) break
            
            val type = scanRecord[i + 1].toInt() and 0xFF
            
            // Type 0xFF = Manufacturer Specific Data
            if (type == 0xFF && length >= 3) {
                // Manufacturer ID is little-endian in bytes 2-3
                val mfgId = (scanRecord[i + 2].toInt() and 0xFF) or
                           ((scanRecord[i + 3].toInt() and 0xFF) shl 8)
                
                if (mfgId == manufacturerId) {
                    // Return data after manufacturer ID bytes
                    return scanRecord.sliceArray((i + 4) until (i + 1 + length))
                }
            }
            
            i += length + 1
        }
        
        return null
    }
    
    private fun isValidSyncByte(syncByte: Int): Boolean {
        return syncByte in 0x03..0x0C
    }
}
