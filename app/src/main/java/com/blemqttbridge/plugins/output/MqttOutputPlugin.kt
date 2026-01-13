package com.blemqttbridge.plugins.output

import android.app.ActivityManager
import android.content.Context
import android.content.Intent
import android.content.IntentFilter
import android.net.wifi.WifiManager
import android.os.BatteryManager
import android.os.Build
import android.os.Environment
import android.os.StatFs
import android.provider.Settings
import android.util.Log
import com.blemqttbridge.core.interfaces.OutputPluginInterface
import kotlinx.coroutines.suspendCancellableCoroutine
import info.mqtt.android.service.MqttAndroidClient
import org.eclipse.paho.client.mqttv3.*
import java.io.RandomAccessFile
import kotlin.coroutines.resume
import kotlin.coroutines.resumeWithException

/**
 * MQTT output plugin using Eclipse Paho client.
 * Implements OutputPluginInterface for MQTT broker connectivity.
 */
class MqttOutputPlugin(private val context: Context) : OutputPluginInterface {
    
    companion object {
        private const val TAG = "MqttOutputPlugin"
        private const val QOS = 1
        private const val AVAILABILITY_TOPIC = "availability"
        
        /**
         * Get a unique device suffix based on Bluetooth adapter MAC address.
         * This is stable across app upgrades, unlike Android ID which can change.
         * Returns last 6 characters to keep identifiers short.
         */
        private fun getDeviceSuffix(context: Context): String {
            return try {
                val bluetoothAdapter = android.bluetooth.BluetoothAdapter.getDefaultAdapter()
                val btMac = bluetoothAdapter?.address
                
                // Use BT MAC if available and not the dummy address
                if (btMac != null && btMac != "02:00:00:00:00:00") {
                    // Use last 6 chars of BT MAC (e.g., "0c9919" from "C0:09:25:0C:99:19")
                    btMac.replace(":", "").takeLast(6).lowercase()
                } else {
                    // Fallback to Android ID only if BT MAC unavailable
                    Log.w(TAG, "Bluetooth MAC unavailable, falling back to Android ID")
                    val androidId = Settings.Secure.getString(
                        context.contentResolver,
                        Settings.Secure.ANDROID_ID
                    )
                    androidId?.takeLast(6)?.lowercase() ?: "unknown"
                }
            } catch (e: Exception) {
                Log.e(TAG, "Failed to get device suffix", e)
                "unknown"
            }
        }
    }
    
    private var mqttClient: MqttAndroidClient? = null
    private var _topicPrefix: String = "homeassistant"
    private val commandCallbacks = mutableMapOf<String, (String, String) -> Unit>()
    private var connectionStatusListener: OutputPluginInterface.ConnectionStatusListener? = null
    
    override fun getTopicPrefix(): String = _topicPrefix
    private var connectOptions: MqttConnectOptions? = null
    
    override fun setConnectionStatusListener(listener: OutputPluginInterface.ConnectionStatusListener?) {
        connectionStatusListener = listener
        // Immediately notify current state
        listener?.onConnectionStatusChanged(isConnected())
    }
    
    override fun getOutputId() = "mqtt"
    
    override fun getOutputName() = "MQTT Broker"
    
    override suspend fun initialize(config: Map<String, String>): Result<Unit> = suspendCancellableCoroutine { continuation ->
        try {
            val brokerUrl = config["broker_url"] 
                ?: return@suspendCancellableCoroutine continuation.resumeWithException(
                    IllegalArgumentException("broker_url required")
                )
            val username = config["username"]
            val password = config["password"]
            val clientId = config["client_id"] ?: "ble_mqtt_bridge_${System.currentTimeMillis()}"
            _topicPrefix = config["topic_prefix"] ?: "homeassistant"
            
            Log.i(TAG, "Initializing MQTT client: $brokerUrl (client: $clientId)")
            
            // Guard flag to prevent double-resume of continuation
            var continuationResumed = false
            
            mqttClient = MqttAndroidClient(context, brokerUrl, clientId).apply {
                setCallback(object : MqttCallbackExtended {
                    override fun connectionLost(cause: Throwable?) {
                        Log.w(TAG, "MQTT connection lost", cause)
                        connectionStatusListener?.onConnectionStatusChanged(false)
                        Log.i(TAG, "Automatic reconnect will be attempted...")
                    }
                    
                    override fun connectComplete(reconnect: Boolean, serverURI: String?) {
                        if (reconnect) {
                            Log.i(TAG, "MQTT reconnected to $serverURI")
                            // Re-publish online status and re-subscribe after reconnect
                            onMqttConnected()
                            resubscribeAll()
                            connectionStatusListener?.onConnectionStatusChanged(true)
                        }
                    }
                    
                    override fun messageArrived(topic: String, message: MqttMessage) {
                        val payload = String(message.payload)
                        Log.w(TAG, "🚨 MESSAGE ARRIVED: $topic = $payload")
                        
                        // Find matching callback
                        Log.w(TAG, "🚨 Checking ${commandCallbacks.size} callback patterns")
                        commandCallbacks.forEach { (pattern, callback) ->
                            val regex = Regex(pattern.replace("+", "[^/]+").replace("#", ".*"))
                            val matches = topic.matches(regex)
                            Log.w(TAG, "🚨 Pattern '$pattern' matches '$topic': $matches")
                            if (matches) {
                                Log.w(TAG, "🚨 Invoking callback for: $topic")
                                callback(topic, payload)
                            }
                        }
                    }
                    
                    override fun deliveryComplete(token: IMqttDeliveryToken?) {
                        // Message published successfully
                    }
                })
            }
            
            val options = MqttConnectOptions().apply {
                isAutomaticReconnect = true
                isCleanSession = true  // Use clean sessions to avoid persistence issues
                connectionTimeout = 30
                keepAliveInterval = 120  // Increased to 2 minutes to prevent keep-alive timeouts
                maxInflight = 100  // Increased to handle initial discovery burst
                
                if (!username.isNullOrBlank() && !password.isNullOrBlank()) {
                    userName = username
                    setPassword(password.toCharArray())
                }
                
                // LWT: Mark offline on unexpected disconnect
                val availTopic = "$_topicPrefix/$AVAILABILITY_TOPIC"
                setWill(availTopic, "offline".toByteArray(), QOS, true)
            }
            
            // Store options for potential reconnection
            connectOptions = options
            
            mqttClient?.connect(options, null, object : IMqttActionListener {
                override fun onSuccess(asyncActionToken: IMqttToken?) {
                    if (continuationResumed) {
                        Log.w(TAG, "MQTT onSuccess called but continuation already resumed, ignoring")
                        return
                    }
                    continuationResumed = true
                    Log.i(TAG, "MQTT connected successfully")
                    connectionStatusListener?.onConnectionStatusChanged(true)
                    // Publish "online" to availability topic to clear any "offline" LWT message
                    onMqttConnected()
                    continuation.resume(Result.success(Unit))
                }
                
                override fun onFailure(asyncActionToken: IMqttToken?, exception: Throwable?) {
                    if (continuationResumed) {
                        Log.w(TAG, "MQTT onFailure called but continuation already resumed, ignoring", exception)
                        return
                    }
                    continuationResumed = true
                    Log.e(TAG, "MQTT connection failed", exception)
                    connectionStatusListener?.onConnectionStatusChanged(false)
                    continuation.resumeWithException(
                        exception ?: Exception("MQTT connection failed")
                    )
                }
            })
            
            continuation.invokeOnCancellation {
                if (!continuationResumed) {
                    continuationResumed = true
                    disconnect()
                }
            }
            
        } catch (e: Exception) {
            Log.e(TAG, "MQTT initialization error", e)
            continuation.resumeWithException(e)
        }
    }
    }
    
    override suspend fun publishState(topic: String, payload: String, retained: Boolean) {
        val fullTopic = "$_topicPrefix/$topic"
        publish(fullTopic, payload, retained)
    }
    
    override suspend fun publishDiscovery(topic: String, payload: String) {
        // Track discovery topics for cleanup when plugin is removed
        publishedDiscoveryTopics.add(topic)
        publish(topic, payload, retained = true)
    }
    
    override suspend fun subscribeToCommands(
        topicPattern: String,
        callback: (topic: String, payload: String) -> Unit
    ) = suspendCancellableCoroutine<Unit> { continuation ->
        try {
            val fullPattern = "$_topicPrefix/$topicPattern"
            Log.i(TAG, "📢 subscribeToCommands called: pattern=$fullPattern, mqttClient=${mqttClient != null}, connected=${mqttClient?.isConnected}")
            commandCallbacks[fullPattern] = callback
            
            if (mqttClient == null) {
                Log.e(TAG, "❌ Cannot subscribe - mqttClient is null!")
                continuation.resumeWithException(Exception("MQTT client is null"))
                return@suspendCancellableCoroutine
            }
            
            mqttClient?.subscribe(fullPattern, QOS, null, object : IMqttActionListener {
                override fun onSuccess(asyncActionToken: IMqttToken?) {
                    Log.i(TAG, "✅ Subscribed to: $fullPattern")
                    continuation.resume(Unit)
                }
                
                override fun onFailure(asyncActionToken: IMqttToken?, exception: Throwable?) {
                    Log.e(TAG, "❌ Subscribe failed: $fullPattern", exception)
                    continuation.resumeWithException(
                        exception ?: Exception("Subscribe failed")
                    )
                }
            })
            
            continuation.invokeOnCancellation {
                mqttClient?.unsubscribe(fullPattern)
            }
            
        } catch (e: Exception) {
            Log.e(TAG, "Subscribe error", e)
            continuation.resumeWithException(e)
        }
    }
    
    override suspend fun publishAvailability(online: Boolean) {
        val topic = "$_topicPrefix/$AVAILABILITY_TOPIC"
        val payload = if (online) "online" else "offline"
        publish(topic, payload, retained = true)
    }
    
    /**
     * Publish availability to a custom topic (for per-plugin availability).
     * The topic should NOT include the prefix - it will be added automatically.
     */
    suspend fun publishAvailability(topic: String, online: Boolean) {
        val fullTopic = "$_topicPrefix/$topic"
        val payload = if (online) "online" else "offline"
        publish(fullTopic, payload, retained = true)
        Log.d(TAG, "📡 Published availability to $fullTopic: $payload")
    }
    
    // Track published discovery topics so we can clear them when removing a plugin
    private val publishedDiscoveryTopics = mutableSetOf<String>()
    
    /**
     * Publish discovery and track the topic for later cleanup.
     */
    fun publishDiscoveryTracked(topic: String, payload: String) {
        publishedDiscoveryTopics.add(topic)
        try {
            val message = MqttMessage(payload.toByteArray()).apply {
                qos = QOS
                isRetained = true
            }
            mqttClient?.publish(topic, message)
        } catch (e: Exception) {
            Log.e(TAG, "Failed to publish discovery to $topic", e)
        }
    }
    
    /**
     * Clear all Home Assistant discovery configs for a specific plugin.
     * Publishing empty payload to config topics removes entities from HA.
     * 
     * @param pluginPattern Pattern to match (e.g., "onecontrol_ble_" to clear all OneControl entities)
     */
    suspend fun clearPluginDiscovery(pluginPattern: String): Int {
        val client = mqttClient
        if (client == null || !client.isConnected) {
            Log.w(TAG, "Cannot clear discovery - MQTT not connected")
            return 0
        }
        
        Log.i(TAG, "🧹 Clearing HA discovery for pattern: $pluginPattern")
        
        // Find all tracked topics matching the pattern
        val topicsToDelete = publishedDiscoveryTopics.filter { it.contains(pluginPattern) }
        
        Log.i(TAG, "🧹 Found ${topicsToDelete.size} discovery topics to clear")
        
        var cleared = 0
        for (topic in topicsToDelete) {
            try {
                val emptyMessage = MqttMessage(ByteArray(0)).apply {
                    qos = QOS
                    isRetained = true
                }
                client.publish(topic, emptyMessage)
                publishedDiscoveryTopics.remove(topic)
                cleared++
                Log.d(TAG, "🧹 Cleared: $topic")
            } catch (e: Exception) {
                Log.w(TAG, "Failed to clear $topic: ${e.message}")
            }
        }
        
        Log.i(TAG, "🧹 Cleared $cleared discovery topics")
        return cleared
    }

    /**
     * Called when MQTT connection is established.
     * Publishes "online" to availability topic and discovery payload.
     */
    private fun onMqttConnected() {
        Log.i(TAG, "🔌 onMqttConnected() called - publishing online status")
        try {
            val client = mqttClient
            if (client == null) {
                Log.e(TAG, "❌ onMqttConnected: mqttClient is null!")
                return
            }
            if (!client.isConnected) {
                Log.e(TAG, "❌ onMqttConnected: client not connected!")
                return
            }
            
            // Publish "online" to availability topic (clears LWT "offline")
            val availTopic = "$_topicPrefix/$AVAILABILITY_TOPIC"
            Log.i(TAG, "📡 Publishing 'online' to topic: $availTopic")
            val onlineMessage = MqttMessage("online".toByteArray()).apply {
                qos = QOS
                isRetained = true
            }
            client.publish(availTopic, onlineMessage)
            Log.i(TAG, "✅ Published availability: online to $availTopic")
            
            // Publish HA discovery for availability binary sensor
            publishAvailabilityDiscovery()
            
            // Publish system diagnostic sensors
            publishSystemDiagnostics()
            
        } catch (e: Exception) {
            Log.e(TAG, "❌ Error in onMqttConnected", e)
        }
    }
    
    /**
     * Publishes Home Assistant discovery for the bridge availability sensor.
     */
    private fun publishAvailabilityDiscovery() {
        try {
            val client = mqttClient ?: return
            val deviceSuffix = getDeviceSuffix(context)
            val nodeId = "ble_mqtt_bridge_${deviceSuffix}"
            val uniqueId = "ble_mqtt_bridge_${deviceSuffix}_availability"
            
            // Get app version
            val appVersion = try {
                context.packageManager.getPackageInfo(context.packageName, 0).versionName ?: "unknown"
            } catch (e: Exception) {
                "unknown"
            }
            
            val discoveryPayload = org.json.JSONObject().apply {
                put("name", "BLE Bridge Availability")
                put("unique_id", uniqueId)
                put("state_topic", "$_topicPrefix/$AVAILABILITY_TOPIC")
                put("payload_on", "online")
                put("payload_off", "offline")
                put("device_class", "connectivity")
                put("entity_category", "diagnostic")
                put("device", org.json.JSONObject().apply {
                    put("identifiers", org.json.JSONArray().put("ble_mqtt_bridge_${deviceSuffix}"))
                    put("name", "BLE MQTT Bridge ${deviceSuffix.uppercase()}")
                    put("model", "Android BLE to MQTT Bridge")
                    put("manufacturer", "phurth")
                    put("sw_version", appVersion)
                })
            }.toString()
            
            val discoveryTopic = "$_topicPrefix/binary_sensor/$nodeId/availability/config"
            val discoveryMessage = MqttMessage(discoveryPayload.toByteArray()).apply {
                qos = QOS
                isRetained = true
            }
            client.publish(discoveryTopic, discoveryMessage)
            Log.i(TAG, "📡 Published availability discovery to $discoveryTopic")
            
        } catch (e: Exception) {
            Log.e(TAG, "Error publishing availability discovery", e)
        }
    }
    
    /**
     * Publishes Home Assistant discovery and state for system diagnostic sensors.
     */
    private fun publishSystemDiagnostics() {
        try {
            val client = mqttClient ?: return
            val deviceSuffix = getDeviceSuffix(context)
            val nodeId = "ble_mqtt_bridge_${deviceSuffix}"
            
            // Get device info for the shared device definition
            val appVersion = try {
                context.packageManager.getPackageInfo(context.packageName, 0).versionName ?: "unknown"
            } catch (e: Exception) {
                "unknown"
            }
            
            val deviceInfo = org.json.JSONObject().apply {
                put("identifiers", org.json.JSONArray().put("ble_mqtt_bridge_${deviceSuffix}"))
                put("name", "BLE MQTT Bridge ${deviceSuffix.uppercase()}")
                put("model", "Android BLE to MQTT Bridge")
                put("manufacturer", "phurth")
                put("sw_version", appVersion)
            }
            
            // Battery Level
            publishDiagnosticSensor(
                nodeId = nodeId,
                objectId = "battery",
                name = "Battery Level",
                deviceClass = "battery",
                unitOfMeasurement = "%",
                stateClass = "measurement",
                deviceInfo = deviceInfo,
                value = getBatteryLevel().toString()
            )
            
            // Battery Status
            publishDiagnosticSensor(
                nodeId = nodeId,
                objectId = "battery_status",
                name = "Battery Status",
                icon = "mdi:battery-charging",
                deviceInfo = deviceInfo,
                value = getBatteryStatus()
            )
            
            // Battery Temperature
            publishDiagnosticSensor(
                nodeId = nodeId,
                objectId = "battery_temp",
                name = "Battery Temperature",
                deviceClass = "temperature",
                unitOfMeasurement = "°C",
                stateClass = "measurement",
                deviceInfo = deviceInfo,
                value = getBatteryTemperature().toString()
            )
            
            // RAM Used Percentage
            publishDiagnosticSensor(
                nodeId = nodeId,
                objectId = "ram_used",
                name = "RAM Used",
                icon = "mdi:memory",
                unitOfMeasurement = "%",
                stateClass = "measurement",
                deviceInfo = deviceInfo,
                value = getMemoryUsedPercent().toString()
            )
            
            // RAM Available
            publishDiagnosticSensor(
                nodeId = nodeId,
                objectId = "ram_available",
                name = "RAM Available",
                icon = "mdi:memory",
                unitOfMeasurement = "MB",
                stateClass = "measurement",
                deviceInfo = deviceInfo,
                value = getMemoryAvailableMB().toString()
            )
            
            // CPU Usage
            publishDiagnosticSensor(
                nodeId = nodeId,
                objectId = "cpu_usage",
                name = "CPU Usage",
                icon = "mdi:cpu-64-bit",
                unitOfMeasurement = "%",
                stateClass = "measurement",
                deviceInfo = deviceInfo,
                value = getCpuUsage().toString()
            )
            
            // Storage Available
            publishDiagnosticSensor(
                nodeId = nodeId,
                objectId = "storage_available",
                name = "Storage Available",
                icon = "mdi:harddisk",
                unitOfMeasurement = "GB",
                stateClass = "measurement",
                deviceInfo = deviceInfo,
                value = getStorageAvailableGB().toString()
            )
            
            // Storage Used Percentage
            publishDiagnosticSensor(
                nodeId = nodeId,
                objectId = "storage_used",
                name = "Storage Used",
                icon = "mdi:harddisk",
                unitOfMeasurement = "%",
                stateClass = "measurement",
                deviceInfo = deviceInfo,
                value = getStorageUsedPercent().toString()
            )
            
            // WiFi SSID
            publishDiagnosticSensor(
                nodeId = nodeId,
                objectId = "wifi_ssid",
                name = "WiFi Network",
                icon = "mdi:wifi",
                deviceInfo = deviceInfo,
                value = getWifiSSID()
            )
            
            // WiFi Signal Strength
            publishDiagnosticSensor(
                nodeId = nodeId,
                objectId = "wifi_rssi",
                name = "WiFi Signal",
                deviceClass = "signal_strength",
                unitOfMeasurement = "dBm",
                stateClass = "measurement",
                deviceInfo = deviceInfo,
                value = getWifiRSSI().toString()
            )
            
            // Device Name
            publishDiagnosticSensor(
                nodeId = nodeId,
                objectId = "device_name",
                name = "Device Name",
                icon = "mdi:cellphone",
                deviceInfo = deviceInfo,
                value = Build.MODEL ?: "unknown"
            )
            
            // Device Manufacturer
            publishDiagnosticSensor(
                nodeId = nodeId,
                objectId = "device_manufacturer",
                name = "Device Manufacturer",
                icon = "mdi:factory",
                deviceInfo = deviceInfo,
                value = Build.MANUFACTURER ?: "unknown"
            )
            
            // Android Version
            publishDiagnosticSensor(
                nodeId = nodeId,
                objectId = "android_version",
                name = "Android Version",
                icon = "mdi:android",
                deviceInfo = deviceInfo,
                value = "${Build.VERSION.RELEASE} (API ${Build.VERSION.SDK_INT})"
            )
            
            // Device Uptime
            publishDiagnosticSensor(
                nodeId = nodeId,
                objectId = "device_uptime",
                name = "Device Uptime",
                icon = "mdi:timer",
                unitOfMeasurement = "h",
                stateClass = "measurement",
                deviceInfo = deviceInfo,
                value = getDeviceUptimeHours().toString()
            )
            
            Log.i(TAG, "📡 Published system diagnostic sensors")
            
        } catch (e: Exception) {
            Log.e(TAG, "Error publishing system diagnostics", e)
        }
    }
    
    /**
     * Helper to publish a single diagnostic sensor discovery and state.
     */
    private fun publishDiagnosticSensor(
        nodeId: String,
        objectId: String,
        name: String,
        deviceClass: String? = null,
        unitOfMeasurement: String? = null,
        stateClass: String? = null,
        icon: String? = null,
        deviceInfo: org.json.JSONObject,
        value: String
    ) {
        try {
            val client = mqttClient ?: return
            val uniqueId = "${nodeId}_${objectId}"
            
            // Discovery payload
            val discoveryPayload = org.json.JSONObject().apply {
                put("name", name)
                put("unique_id", uniqueId)
                put("state_topic", "$_topicPrefix/sensor/$nodeId/$objectId/state")
                put("availability_topic", "$_topicPrefix/$AVAILABILITY_TOPIC")
                put("entity_category", "diagnostic")
                deviceClass?.let { put("device_class", it) }
                unitOfMeasurement?.let { put("unit_of_measurement", it) }
                stateClass?.let { put("state_class", it) }
                icon?.let { put("icon", it) }
                put("device", deviceInfo)
            }.toString()
            
            // Publish discovery
            val discoveryTopic = "$_topicPrefix/sensor/$nodeId/$objectId/config"
            val discoveryMessage = MqttMessage(discoveryPayload.toByteArray()).apply {
                qos = QOS
                isRetained = true
            }
            client.publish(discoveryTopic, discoveryMessage)
            
            // Publish state
            val stateTopic = "$_topicPrefix/sensor/$nodeId/$objectId/state"
            val stateMessage = MqttMessage(value.toByteArray()).apply {
                qos = QOS
                isRetained = true
            }
            client.publish(stateTopic, stateMessage)
            
            Log.d(TAG, "📊 Published diagnostic: $name = $value")
            
        } catch (e: Exception) {
            Log.e(TAG, "Error publishing diagnostic sensor $objectId", e)
        }
    }
    
    // System info getters
    
    private fun getBatteryLevel(): Int {
        return try {
            val batteryManager = context.getSystemService(Context.BATTERY_SERVICE) as BatteryManager
            batteryManager.getIntProperty(BatteryManager.BATTERY_PROPERTY_CAPACITY)
        } catch (e: Exception) {
            Log.w(TAG, "Error getting battery level", e)
            -1
        }
    }
    
    private fun getBatteryStatus(): String {
        return try {
            val intentFilter = IntentFilter(Intent.ACTION_BATTERY_CHANGED)
            val batteryStatus = context.registerReceiver(null, intentFilter)
            val status = batteryStatus?.getIntExtra(BatteryManager.EXTRA_STATUS, -1) ?: -1
            when (status) {
                BatteryManager.BATTERY_STATUS_CHARGING -> "charging"
                BatteryManager.BATTERY_STATUS_DISCHARGING -> "discharging"
                BatteryManager.BATTERY_STATUS_FULL -> "full"
                BatteryManager.BATTERY_STATUS_NOT_CHARGING -> "not_charging"
                else -> "unknown"
            }
        } catch (e: Exception) {
            Log.w(TAG, "Error getting battery status", e)
            "unknown"
        }
    }
    
    private fun getBatteryTemperature(): Float {
        return try {
            val intentFilter = IntentFilter(Intent.ACTION_BATTERY_CHANGED)
            val batteryStatus = context.registerReceiver(null, intentFilter)
            val temp = batteryStatus?.getIntExtra(BatteryManager.EXTRA_TEMPERATURE, -1) ?: -1
            if (temp > 0) temp / 10.0f else -1f
        } catch (e: Exception) {
            Log.w(TAG, "Error getting battery temperature", e)
            -1f
        }
    }
    
    private fun getMemoryUsedPercent(): Int {
        return try {
            val activityManager = context.getSystemService(Context.ACTIVITY_SERVICE) as ActivityManager
            val memInfo = ActivityManager.MemoryInfo()
            activityManager.getMemoryInfo(memInfo)
            val used = memInfo.totalMem - memInfo.availMem
            ((used.toDouble() / memInfo.totalMem) * 100).toInt()
        } catch (e: Exception) {
            Log.w(TAG, "Error getting memory usage", e)
            -1
        }
    }
    
    private fun getMemoryAvailableMB(): Long {
        return try {
            val activityManager = context.getSystemService(Context.ACTIVITY_SERVICE) as ActivityManager
            val memInfo = ActivityManager.MemoryInfo()
            activityManager.getMemoryInfo(memInfo)
            memInfo.availMem / (1024 * 1024)
        } catch (e: Exception) {
            Log.w(TAG, "Error getting available memory", e)
            -1
        }
    }
    
    private fun getCpuUsage(): Int {
        return try {
            val reader = RandomAccessFile("/proc/stat", "r")
            val load = reader.readLine()
            reader.close()
            
            val toks = load.split(" +".toRegex())
            val idle = toks[4].toLong()
            val total = toks.drop(1).take(7).sumOf { it.toLongOrNull() ?: 0 }
            
            val usage = if (total > 0) ((total - idle) * 100 / total).toInt() else -1
            usage
        } catch (e: Exception) {
            Log.w(TAG, "Error getting CPU usage", e)
            -1
        }
    }
    
    private fun getStorageAvailableGB(): Float {
        return try {
            val path = Environment.getDataDirectory()
            val stat = StatFs(path.path)
            val availableBytes = stat.availableBlocksLong * stat.blockSizeLong
            availableBytes / (1024f * 1024f * 1024f)
        } catch (e: Exception) {
            Log.w(TAG, "Error getting storage", e)
            -1f
        }
    }
    
    private fun getStorageUsedPercent(): Int {
        return try {
            val path = Environment.getDataDirectory()
            val stat = StatFs(path.path)
            val totalBytes = stat.blockCountLong * stat.blockSizeLong
            val availableBytes = stat.availableBlocksLong * stat.blockSizeLong
            val used = totalBytes - availableBytes
            ((used.toDouble() / totalBytes) * 100).toInt()
        } catch (e: Exception) {
            Log.w(TAG, "Error getting storage usage", e)
            -1
        }
    }
    
    private fun getWifiSSID(): String {
        return try {
            val wifiManager = context.applicationContext.getSystemService(Context.WIFI_SERVICE) as WifiManager
            val wifiInfo = wifiManager.connectionInfo
            wifiInfo.ssid?.replace("\"", "") ?: "unknown"
        } catch (e: Exception) {
            Log.w(TAG, "Error getting WiFi SSID", e)
            "unknown"
        }
    }
    
    private fun getWifiRSSI(): Int {
        return try {
            val wifiManager = context.applicationContext.getSystemService(Context.WIFI_SERVICE) as WifiManager
            val wifiInfo = wifiManager.connectionInfo
            wifiInfo.rssi
        } catch (e: Exception) {
            Log.w(TAG, "Error getting WiFi RSSI", e)
            -100
        }
    }
    
    private fun getDeviceUptimeHours(): Float {
        return try {
            val uptimeMillis = android.os.SystemClock.elapsedRealtime()
            uptimeMillis / (1000f * 60f * 60f)
        } catch (e: Exception) {
            Log.w(TAG, "Error getting device uptime", e)
            -1f
        }
    }
    
    override fun disconnect() {
        try {
            if (mqttClient?.isConnected == true) {
                Log.i(TAG, "Disconnecting MQTT client")
                mqttClient?.disconnect()
            }
            mqttClient?.close()
            mqttClient = null
            commandCallbacks.clear()
        } catch (e: Exception) {
            Log.e(TAG, "Error disconnecting MQTT", e)
        }
    }
    
    override fun isConnected(): Boolean {
        return try {
            mqttClient?.isConnected == true
        } catch (e: Exception) {
            Log.w(TAG, "Error checking isConnected: ${e.message}")
            false
        }
    }
    
    override fun getConnectionStatus(): String {
        return try {
            when {
                mqttClient == null -> "Not initialized"
                mqttClient?.isConnected == true -> "Connected"
                else -> "Disconnected"
            }
        } catch (e: Exception) {
            Log.w(TAG, "Error getting connection status: ${e.message}")
            "Error"
        }
    }
    
    /**
     * Internal publish helper with suspending coroutine.
     * Fails gracefully without throwing exceptions to prevent crashes.
     */
    private suspend fun publish(topic: String, payload: String, retained: Boolean) = suspendCancellableCoroutine<Unit> { continuation ->
        try {
            val client = mqttClient
            
            // Check if client is connected - wrap in try-catch as isConnected can throw
            val connected = try {
                client != null && client.isConnected
            } catch (e: Exception) {
                Log.w(TAG, "Error checking MQTT connection state: ${e.message}")
                false
            }
            
            if (!connected) {
                Log.w(TAG, "Cannot publish - MQTT not connected, skipping: $topic")
                continuation.resume(Unit) // Don't throw, just skip
                return@suspendCancellableCoroutine
            }
            
            val message = MqttMessage(payload.toByteArray()).apply {
                qos = QOS
                isRetained = retained
            }
            
            client!!.publish(topic, message, null, object : IMqttActionListener {
                override fun onSuccess(asyncActionToken: IMqttToken?) {
                    Log.d(TAG, "Published: $topic (${payload.length} bytes, retained=$retained)")
                    continuation.resume(Unit)
                }
                
                override fun onFailure(asyncActionToken: IMqttToken?, exception: Throwable?) {
                    Log.w(TAG, "Publish failed: $topic - ${exception?.message}")
                    continuation.resume(Unit) // Don't throw, just log
                }
            })
            
        } catch (e: Exception) {
            Log.e(TAG, "Publish error: ${e.message}")
            continuation.resume(Unit) // Don't throw, just log
        }
    }
    
    /**
     * Re-subscribe to all topics after reconnection.
     * Called automatically when connection is restored.
     */
    private fun resubscribeAll() {
        val client = mqttClient
        if (client == null || !client.isConnected) {
            Log.d(TAG, "Cannot resubscribe - client not connected yet")
            return
        }
        
        if (commandCallbacks.isEmpty()) {
            Log.d(TAG, "No subscriptions to restore")
            return
        }
        
        Log.i(TAG, "Resubscribing to ${commandCallbacks.size} topic(s)")
        commandCallbacks.keys.forEach { topic ->
            try {
                client.subscribe(topic, QOS, null, object : IMqttActionListener {
                    override fun onSuccess(asyncActionToken: IMqttToken?) {
                        Log.i(TAG, "Resubscribed to: $topic")
                    }
                    
                    override fun onFailure(asyncActionToken: IMqttToken?, exception: Throwable?) {
                        Log.e(TAG, "Failed to resubscribe to: $topic", exception)
                    }
                })
            } catch (e: Exception) {
                Log.e(TAG, "Error resubscribing to $topic", e)
            }
        }
    }
}
