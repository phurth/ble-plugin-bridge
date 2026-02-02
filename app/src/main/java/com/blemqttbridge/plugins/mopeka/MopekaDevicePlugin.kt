package com.blemqttbridge.plugins.mopeka

import android.bluetooth.BluetoothDevice
import android.bluetooth.BluetoothGatt
import android.bluetooth.BluetoothGattCallback
import android.bluetooth.le.ScanRecord
import android.content.Context
import android.util.Log
import com.blemqttbridge.core.interfaces.BleDevicePlugin
import com.blemqttbridge.core.interfaces.MqttPublisher
import com.blemqttbridge.core.interfaces.PluginConfig
import com.blemqttbridge.core.discovery.HomeAssistantDiscoveryBuilder
import com.blemqttbridge.plugins.mopeka.protocol.MopekaConstants
import com.blemqttbridge.plugins.mopeka.protocol.MopekaAdvertisementParser
import com.blemqttbridge.plugins.mopeka.protocol.MopekaSensorData

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
        private const val TAG = "Mopeka"
        const val PLUGIN_ID = "mopeka"
        const val PLUGIN_VERSION = "1.0.0"
        const val PLUGIN_DISPLAY_NAME = "Mopeka Tank Sensor"
        const val OFFLINE_TIMEOUT_MS = 30 * 60 * 1000L  // 30 minutes
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
    
    // Track device offline status with timeout (30 minutes = 1800000 ms)
    private val lastSeenTimestamp = mutableMapOf<String, Long>()
    private val offlineDevices = mutableSetOf<String>()
    
    // Track last successful health update to detect stale data
    private var lastSuccessfulUpdateMs: Long = 0L
    private val HEALTH_STALE_TIMEOUT_MS = 2 * 60 * 1000L  // 2 minutes - if no update, health goes bad
    
    override fun initializeWithConfig(instanceId: String, config: Map<String, String>) {
        this.instanceId = instanceId
        
        // Extract device-specific configuration from instance config
        configuredMacAddress = config["sensor_mac"]?.ifEmpty { null }
        val mediumTypeId = config["medium_type"] ?: "propane"
        mediumType = MopekaConstants.MediumType.fromId(mediumTypeId)
        val tankTypeId = config["tank_type"] ?: "20lb_v"
        tankType = MopekaConstants.TankType.fromId(tankTypeId)
        minimumQuality = config["minimum_quality"]?.toIntOrNull() ?: MopekaConstants.QualityThresholds.ZERO
        
        Log.i(TAG, "Initializing instance: $instanceId (MAC: $configuredMacAddress, medium: $mediumType, tank: $tankType, minQuality: $minimumQuality)")
    }
    
    override fun initialize(context: Context?, config: PluginConfig) {
        this.context = context
        
        Log.d(TAG, "initialize() called with config parameters: ${config.parameters}")
        
        // Parse configuration (legacy path)
        configuredMacAddress = config.getString("sensor_mac", "").ifEmpty { null }
        val mediumTypeId = config.getString("medium_type", "propane")
        mediumType = MopekaConstants.MediumType.fromId(mediumTypeId)
        val tankTypeId = config.getString("tank_type", "20lb_v")
        tankType = MopekaConstants.TankType.fromId(tankTypeId)
        minimumQuality = config.getInt("minimum_quality", MopekaConstants.QualityThresholds.ZERO)
        
        Log.i(TAG, "Initialized: MAC=$configuredMacAddress, medium=$mediumType, tank=$tankType, minQuality=$minimumQuality")
    }
    
    /**
     * Set the MQTT publisher for this passive plugin.
     * Called by BaseBleService before processing advertisements.
     */
    fun setMqttPublisher(publisher: MqttPublisher) {
        this.mqttPublisher = publisher
        Log.d(TAG, "MQTT publisher set for instance $instanceId")
    }    override fun destroy() {
        Log.i(TAG, "Destroyed")
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
            Log.d(TAG, "matchesDevice: Configured MAC=$configuredMacAddress, device=${device.address}, match=$matches")
            return matches
        }
        
        // No MAC configured - detect any Mopeka sensor by manufacturer ID
        if (scanRecord == null) {
            Log.d(TAG, "matchesDevice: No scan record for ${device.address}")
            return false
        }
        
        // Check manufacturer-specific data for Mopeka ID (0x0059)
        val manufacturerData = scanRecord.getManufacturerSpecificData(MopekaConstants.MANUFACTURER_ID)
        val isMopeka = manufacturerData != null
        
        if (isMopeka) {
            Log.i(TAG, "✅ Detected Mopeka sensor: ${device.address} (mfgId=0x${MopekaConstants.MANUFACTURER_ID.toString(16)})")
        } else {
            Log.d(TAG, "matchesDevice: ${device.address} is not a Mopeka sensor")
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
        val macAddress = device.address
        val cleanMac = macAddress.replace(":", "").uppercase()
        val appVersion = try {
            context?.packageManager?.getPackageInfo(context?.packageName ?: "", 0)?.versionName ?: "unknown"
        } catch (e: Exception) {
            "unknown"
        }
        
        val builder = HomeAssistantDiscoveryBuilder(
            deviceMac = macAddress,
            deviceName = "Mopeka Tank Sensor $macAddress",
            deviceManufacturer = "Mopeka",
            appVersion = appVersion
        )
        
        val deviceId = "mopeka_$cleanMac"
        val discoveries = mutableListOf<Pair<String, String>>()
        
        // Battery percentage (diagnostic)
        discoveries.add(
            HomeAssistantDiscoveryBuilder.buildDiscoveryTopic("sensor", deviceId, "battery") to
            builder.buildSensor(
                uniqueId = "mopeka_${cleanMac}_battery",
                displayName = "Battery",
                stateTopic = "${mqttPublisher?.topicPrefix}/$baseTopic/battery",
                baseTopic = baseTopic,
                deviceIdentifier = deviceId,
                unitOfMeasurement = "%",
                deviceClass = "battery",
                stateClass = "measurement",
                icon = "mdi:battery",
                valueTemplate = null,
                jsonAttributes = false
            ).toString()
        )
        
        // Temperature (Celsius)
        discoveries.add(
            HomeAssistantDiscoveryBuilder.buildDiscoveryTopic("sensor", deviceId, "temperature") to
            builder.buildSensor(
                uniqueId = "mopeka_${cleanMac}_temperature",
                displayName = "Temperature",
                stateTopic = "${mqttPublisher?.topicPrefix}/$baseTopic/temperature",
                baseTopic = baseTopic,
                deviceIdentifier = deviceId,
                unitOfMeasurement = "°C",
                deviceClass = "temperature",
                stateClass = "measurement",
                icon = "mdi:thermometer",
                valueTemplate = null,
                jsonAttributes = false
            ).toString()
        )
        
        // Tank Level (%)
        discoveries.add(
            HomeAssistantDiscoveryBuilder.buildDiscoveryTopic("sensor", deviceId, "tank_level") to
            builder.buildSensor(
                uniqueId = "mopeka_${cleanMac}_tank_level",
                displayName = "Tank Level",
                stateTopic = "${mqttPublisher?.topicPrefix}/$baseTopic/tank_level",
                baseTopic = baseTopic,
                deviceIdentifier = deviceId,
                unitOfMeasurement = "%",
                deviceClass = null,
                icon = "mdi:propane-tank",
                stateClass = "measurement",
                valueTemplate = "{{ value_json.value }}",
                jsonAttributes = true
            ).toString()
        )
        
        // Quality (0-3 diagnostic)
        discoveries.add(
            HomeAssistantDiscoveryBuilder.buildDiscoveryTopic("sensor", deviceId, "quality") to
            builder.buildSensor(
                uniqueId = "mopeka_${cleanMac}_quality",
                displayName = "Read Quality",
                stateTopic = "${mqttPublisher?.topicPrefix}/$baseTopic/quality",
                baseTopic = baseTopic,
                deviceIdentifier = deviceId,
                unitOfMeasurement = null,
                deviceClass = null,
                stateClass = "measurement",
                icon = "mdi:signal",
                valueTemplate = null,
                jsonAttributes = false
            ).toString()
        )
        
        return discoveries
    }
    
    override fun onDeviceDisconnected(device: BluetoothDevice) {
        // Clean up device-specific state if needed
        Log.d(TAG, "Device disconnected: ${device.address}")
    }
    
    /**
     * Parse advertisement data and publish to MQTT
     * This would be called from the BLE scan handler when a Mopeka device is detected
     */
    fun handleScanResult(device: BluetoothDevice, scanRecord: ScanRecord?) {
        Log.d(TAG, "📥 handleScanResult called for ${device.address}, scanRecord=${scanRecord != null}")
        
        // Check if plugin health has gone stale (no successful updates in timeout period)
        val currentTime = System.currentTimeMillis()
        if (lastSuccessfulUpdateMs > 0 && (currentTime - lastSuccessfulUpdateMs) > HEALTH_STALE_TIMEOUT_MS) {
            Log.w(TAG, "⚠️ Health stale! No successful update for ${currentTime - lastSuccessfulUpdateMs}ms, marking unhealthy")
            mqttPublisher?.updatePluginStatus(instanceId, connected = false, authenticated = false, dataHealthy = false)
        }
        
        // Update last-seen timestamp for this device
        lastSeenTimestamp[device.address] = currentTime
        
        // If device was previously offline, mark it as back online
        if (offlineDevices.contains(device.address)) {
            offlineDevices.remove(device.address)
            Log.i(TAG, "✅ Device ${device.address} back online after timeout")
        }
        
        // Check for devices that have timed out (no advertisements in 30 minutes)
        checkForOfflineDevices(currentTime)
        
        val data = scanRecord?.bytes ?: run {
            Log.d(TAG, "⚠️ No scan record data")
            return
        }
        Log.d(TAG, "📄 Scan record data length: ${data.size} bytes")
        val manufacturerData = parseManufacturerData(data, MopekaConstants.MANUFACTURER_ID)
            ?: run {
                Log.d(TAG, "⚠️ No manufacturer data for ID ${MopekaConstants.MANUFACTURER_ID}")
                return
            }
        Log.d(TAG, "✅ Found manufacturer data: ${manufacturerData.size} bytes")
        
        var sensorData = MopekaAdvertisementParser.parse(device.address, manufacturerData) ?: run {
            Log.d(TAG, "⚠️ Failed to parse sensor data")
            return
        }
        Log.d(TAG, "✅ Parsed sensor data: temp=${sensorData.temperature}°C, quality=${sensorData.quality}")
        
        // Quality check
        if (sensorData.quality < minimumQuality) {
            Log.d(TAG, "Skipping ${device.address}: quality ${sensorData.quality} < $minimumQuality")
            return
        }
        Log.d(TAG, "✅ Quality check passed (${sensorData.quality} >= $minimumQuality)")
        
        // Extract raw temperature for temperature compensation (before -40 conversion)
        val rawTemp = (manufacturerData[MopekaConstants.AdvertisementLayout.TEMPERATURE_AND_BUTTON_INDEX].toInt() and 0x7F)
        
        // Apply temperature compensation to get accurate distance in mm
        val compensatedDistance = MopekaAdvertisementParser.applyTemperatureCompensation(
            sensorData.distanceRaw,
            rawTemp,
            mediumType
        )
        Log.d(TAG, "🌡️ Temperature compensation: raw=${sensorData.distanceRaw}mm, compensated=${compensatedDistance}mm (temp_raw=$rawTemp)")
        
        // Calculate tank level percentage using geometric formulas (from HA community)
        // Uses precise volume calculations for vertical/horizontal tank shapes
        val tankPercentage = MopekaConstants.calculateTankPercentage(tankType, compensatedDistance.toDouble())
        Log.d(TAG, "📊 Tank percentage: ${String.format("%.1f", tankPercentage)}% (${compensatedDistance}mm, tank=${tankType.displayName})")
        
        // Store percentage as tank level (not mm anymore)
        val tankLevel = tankPercentage.toFloat()
        Log.d(TAG, "✅ Tank level (percentage): ${String.format("%.1f", tankLevel)}% (medium=$mediumType)")
        
        // Update with calculated tank level and compensated distance
        sensorData = sensorData.copy(
            tankLevel = tankLevel,
            compensatedDistance = compensatedDistance,
            mediumType = mediumType
        )
        
        // Publish Home Assistant discovery on first detection
        Log.d(TAG, "🔍 Discovery check: device=${device.address}, alreadyDiscovered=${discoveredDevices.contains(device.address)}, mqttPublisher=$mqttPublisher")
        if (!discoveredDevices.contains(device.address)) {
            Log.d(TAG, "🔍 Device NOT in discoveredDevices set, checking mqttPublisher...")
            mqttPublisher?.let { publisher ->
                Log.i(TAG, "📢 Publishing Home Assistant discovery for ${device.address}")
                publishDiscovery(device.address, publisher)
                discoveredDevices.add(device.address)
            } ?: Log.w(TAG, "⚠️ mqttPublisher is null! Cannot publish discovery for ${device.address}")
        } else {
            Log.d(TAG, "✅ Device ${device.address} already in discoveredDevices, skipping discovery publish")
        }
        
        Log.d(TAG, "📡 Publishing to MQTT...")
        publishToMqtt(sensorData)
        
        // Record successful update for health monitoring
        lastSuccessfulUpdateMs = System.currentTimeMillis()
        
        // Update plugin status to show green health indicator ONLY after successful publish
        // This ensures health is only green when we're actually receiving and publishing valid data
        mqttPublisher?.updatePluginStatus(
            pluginId = instanceId,
            connected = false,  // Passive plugin, no connection
            authenticated = false,  // Passive plugin, no authentication
            dataHealthy = true  // Successfully processed and published data
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
        Log.d(TAG, "publishToMqtt called, mqttPublisher=$mqttPublisher")
        val publisher = mqttPublisher ?: run {
            Log.w(TAG, "⚠️ mqttPublisher is null! Cannot publish to MQTT")
            return
        }
        
        val actualBaseTopic = "mopeka/${sensorData.macAddress}"
        Log.d(TAG, "Publishing to topic: $actualBaseTopic")
        
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
        
        Log.d(TAG, "Published: $actualBaseTopic - Tank: ${String.format("%.1f", sensorData.tankLevel)}%, Temp: ${sensorData.temperature}°C, Battery: ${sensorData.batteryLevel}%")
    }
    
    /**
     * Publish Home Assistant MQTT discovery payloads for this device
     */
    private fun publishDiscovery(macAddress: String, publisher: MqttPublisher) {
        val baseTopic = "mopeka/$macAddress"
        val cleanMac = macAddress.replace(":", "").uppercase()
        val appVersion = try {
            context?.packageManager?.getPackageInfo(context?.packageName ?: "", 0)?.versionName ?: "unknown"
        } catch (e: Exception) {
            "unknown"
        }
        
        val builder = HomeAssistantDiscoveryBuilder(
            deviceMac = macAddress,
            deviceName = "Mopeka Tank Sensor $macAddress",
            deviceManufacturer = "Mopeka",
            appVersion = appVersion
        )
        
        val deviceId = "mopeka_$cleanMac"
        
        // Battery
        publisher.publishDiscovery(
            HomeAssistantDiscoveryBuilder.buildDiscoveryTopic("sensor", deviceId, "battery"),
            builder.buildSensor(
                uniqueId = "mopeka_${cleanMac}_battery",
                displayName = "Battery",
                stateTopic = "${mqttPublisher?.topicPrefix}/$baseTopic/battery",
                baseTopic = baseTopic,
                deviceIdentifier = deviceId,
                unitOfMeasurement = "%",
                deviceClass = "battery",
                stateClass = "measurement",
                icon = "mdi:battery",
                valueTemplate = null,
                jsonAttributes = false
            ).toString()
        )
        
        // Temperature
        publisher.publishDiscovery(
            HomeAssistantDiscoveryBuilder.buildDiscoveryTopic("sensor", deviceId, "temperature"),
            builder.buildSensor(
                uniqueId = "mopeka_${cleanMac}_temperature",
                displayName = "Temperature",
                stateTopic = "${mqttPublisher?.topicPrefix}/$baseTopic/temperature",
                baseTopic = baseTopic,
                deviceIdentifier = deviceId,
                unitOfMeasurement = "°C",
                deviceClass = "temperature",
                stateClass = "measurement",
                icon = null,
                valueTemplate = null,
                jsonAttributes = false
            ).toString()
        )
        
        // Tank Level
        publisher.publishDiscovery(
            HomeAssistantDiscoveryBuilder.buildDiscoveryTopic("sensor", deviceId, "tank_level"),
            builder.buildSensor(
                uniqueId = "mopeka_${cleanMac}_tank_level",
                displayName = "Tank Level",
                stateTopic = "${mqttPublisher?.topicPrefix}/$baseTopic/tank_level",
                baseTopic = baseTopic,
                deviceIdentifier = deviceId,
                unitOfMeasurement = "%",
                icon = "mdi:propane-tank",
                stateClass = "measurement",
                deviceClass = null,
                valueTemplate = "{{ value_json.value }}",
                jsonAttributes = true
            ).also { Log.d(TAG, "Tank Level discovery JSON: $it") }.toString()
        )
        
        // Quality
        publisher.publishDiscovery(
            HomeAssistantDiscoveryBuilder.buildDiscoveryTopic("sensor", deviceId, "quality"),
            builder.buildSensor(
                uniqueId = "mopeka_${cleanMac}_quality",
                displayName = "Read Quality",
                stateTopic = "${mqttPublisher?.topicPrefix}/$baseTopic/quality",
                baseTopic = baseTopic,
                deviceIdentifier = deviceId,
                unitOfMeasurement = null,
                deviceClass = null,
                stateClass = "measurement",
                icon = "mdi:signal",
                valueTemplate = null,
                jsonAttributes = false
            ).toString()
        )
        
        Log.i(TAG, "Discovery published for $macAddress (medium: ${mediumType.displayName})")
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
    
    /**
     * Check for devices that haven't been seen recently and publish offline status
     * Called periodically to detect when devices stop advertising
     */
    private fun checkForOfflineDevices(currentTime: Long) {
        val devicesToMarkOffline = mutableListOf<String>()
        
        for ((deviceMac, lastSeen) in lastSeenTimestamp) {
            val timeSinceLastSeen = currentTime - lastSeen
            
            // If device hasn't been seen in 30 minutes and isn't already marked offline
            if (timeSinceLastSeen > OFFLINE_TIMEOUT_MS && !offlineDevices.contains(deviceMac)) {
                devicesToMarkOffline.add(deviceMac)
            }
        }
        
        // Publish offline status for devices that timed out
        for (deviceMac in devicesToMarkOffline) {
            offlineDevices.add(deviceMac)
            val topic = "mopeka/$deviceMac/availability"
            Log.w(TAG, "📡 Device $deviceMac timed out - publishing offline")
            mqttPublisher?.publishAvailability(topic, false)
        }
    }}