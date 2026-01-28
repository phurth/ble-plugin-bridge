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
import com.blemqttbridge.core.MemoryManager
import com.blemqttbridge.core.discovery.DiscoveryBuilder
import com.blemqttbridge.core.discovery.DiscoveryBuilderFactory
import com.blemqttbridge.core.interfaces.OutputPluginInterface
import info.mqtt.android.service.MqttAndroidClient
import kotlinx.coroutines.suspendCancellableCoroutine
import org.eclipse.paho.client.mqttv3.IMqttActionListener
import org.eclipse.paho.client.mqttv3.IMqttDeliveryToken
import org.eclipse.paho.client.mqttv3.IMqttToken
import org.eclipse.paho.client.mqttv3.MqttConnectOptions
import org.eclipse.paho.client.mqttv3.MqttMessage
import org.eclipse.paho.client.mqttv3.MqttCallbackExtended
import java.io.RandomAccessFile
import kotlin.coroutines.resume
import kotlin.coroutines.resumeWithException

/**
 * MQTT output plugin using Eclipse Paho client.
 * Implements OutputPluginInterface for MQTT broker connectivity.
 */
class MqttOutputPlugin(
    private val context: Context,
    private val memoryManager: MemoryManager? = null
) : OutputPluginInterface {

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
                    btMac.replace(":", "").takeLast(6).lowercase()
                } else {
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
    private var _discoveryFormat: String = "homeassistant"
    private val commandCallbacks = mutableMapOf<String, (String, String) -> Unit>()
    private var connectionStatusListener: OutputPluginInterface.ConnectionStatusListener? = null

    override fun getTopicPrefix(): String = _topicPrefix
    private var connectOptions: MqttConnectOptions? = null

    override fun setConnectionStatusListener(listener: OutputPluginInterface.ConnectionStatusListener?) {
        connectionStatusListener = listener
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
            _discoveryFormat = config["discovery_format"] ?: "homeassistant"

            Log.i(TAG, "🔌 Initializing MQTT client: $brokerUrl (client: $clientId)")

            var continuationResumed = false

            // Start timeout timer to prevent hanging indefinitely
            val timeoutHandler = android.os.Handler(android.os.Looper.getMainLooper())
            val timeoutRunnable = Runnable {
                if (!continuationResumed) {
                    continuationResumed = true
                    Log.e(TAG, "⏱️ MQTT connection timeout after 35 seconds (no callback from Paho)")
                    connectionStatusListener?.onConnectionStatusChanged(false)
                    continuation.resumeWithException(
                        Exception("MQTT connection timeout - Paho callbacks not responding")
                    )
                }
            }
            timeoutHandler.postDelayed(timeoutRunnable, 35000)

            mqttClient = MqttAndroidClient(context, brokerUrl, clientId).apply {
                setCallback(object : MqttCallbackExtended {
                    override fun connectionLost(cause: Throwable?) {
                        Log.w(TAG, "❌ MQTT connection lost", cause)
                        connectionStatusListener?.onConnectionStatusChanged(false)
                        Log.i(TAG, "⏳ Automatic reconnect will be attempted...")
                    }

                    override fun connectComplete(reconnect: Boolean, serverURI: String?) {
                        Log.i(TAG, "✅ connectComplete callback: reconnect=$reconnect, serverURI=$serverURI")
                        
                        // If this is the first connection (not a reconnect), resume continuation if not already resumed
                        if (!reconnect && !continuationResumed) {
                            continuationResumed = true
                            timeoutHandler.removeCallbacks(timeoutRunnable)
                            Log.i(TAG, "✅ MQTT connected successfully on first connect!")
                            onMqttConnected()
                            connectionStatusListener?.onConnectionStatusChanged(true)
                            continuation.resume(Result.success(Unit))
                        } else if (reconnect) {
                            Log.i(TAG, "🔄 MQTT reconnected to $serverURI")
                            onMqttConnected()
                            resubscribeAll()
                            connectionStatusListener?.onConnectionStatusChanged(true)
                        }
                    }

                    override fun messageArrived(topic: String, message: MqttMessage) {
                        val payload = String(message.payload)
                        Log.w(TAG, "📨 MESSAGE ARRIVED: $topic = $payload")

                        commandCallbacks.forEach { (pattern, callback) ->
                            val regex = Regex(pattern.replace("+", "[^/]+").replace("#", ".*"))
                            val matches = topic.matches(regex)
                            if (matches) {
                                Log.i(TAG, "📨 Invoking callback for: $topic")
                                callback(topic, payload)
                            }
                        }
                    }

                    override fun deliveryComplete(token: IMqttDeliveryToken?) {
                        // Published successfully
                    }
                })
            }

            val options = MqttConnectOptions().apply {
                isAutomaticReconnect = true
                isCleanSession = true
                connectionTimeout = 30
                keepAliveInterval = 120
                maxInflight = 100

                if (!username.isNullOrBlank() && !password.isNullOrBlank()) {
                    userName = username
                    setPassword(password.toCharArray())
                }

                val availTopic = "$_topicPrefix/$AVAILABILITY_TOPIC"
                setWill(availTopic, "offline".toByteArray(), QOS, true)
            }

            connectOptions = options

            Log.d(TAG, "🚀 Calling mqttClient.connect() with 30s timeout...")
            mqttClient?.connect(options, null, object : IMqttActionListener {
                override fun onSuccess(asyncActionToken: IMqttToken?) {
                    if (continuationResumed) {
                        Log.w(TAG, "⚠️ MQTT onSuccess called but continuation already resumed, ignoring")
                        return
                    }
                    continuationResumed = true
                    timeoutHandler.removeCallbacks(timeoutRunnable)
                    Log.i(TAG, "✅ MQTT async connect operation succeeded (waiting for connectComplete callback)")
                    // NOTE: Don't call onMqttConnected() here - it will be called from connectComplete() callback
                    // when the actual MQTT connection is complete
                    continuation.resume(Result.success(Unit))
                }

                override fun onFailure(asyncActionToken: IMqttToken?, exception: Throwable?) {
                    // Ignore persistence exceptions - connection might still succeed
                    val isPersistenceError = exception?.message?.contains("MqttDefaultFilePersistence") == true ||
                                            exception?.javaClass?.simpleName?.contains("Persistence") == true
                    
                    if (isPersistenceError) {
                        Log.w(TAG, "⚠️ MQTT persistence error (ignoring, will wait for connectComplete): ${exception?.message}")
                        return
                    }
                    
                    if (continuationResumed) {
                        Log.w(TAG, "⚠️ MQTT onFailure called but continuation already resumed, ignoring", exception)
                        return
                    }
                    continuationResumed = true
                    timeoutHandler.removeCallbacks(timeoutRunnable)
                    Log.e(TAG, "❌ MQTT connection FAILED: ${exception?.message}", exception)
                    connectionStatusListener?.onConnectionStatusChanged(false)
                    continuation.resumeWithException(
                        exception ?: Exception("MQTT connection failed")
                    )
                }
            })

            continuation.invokeOnCancellation {
                if (!continuationResumed) {
                    continuationResumed = true
                    Log.w(TAG, "⚠️ MQTT initialize cancelled")
                    disconnect()
                }
            }

        } catch (e: Exception) {
            Log.e(TAG, "💥 MQTT initialization error", e)
            continuation.resumeWithException(e)
        }
    }

    override suspend fun publishState(topic: String, payload: String, retained: Boolean) {
        val fullTopic = "$_topicPrefix/$topic"
        publish(fullTopic, payload, retained)
    }

    override suspend fun publishDiscovery(topic: String, payload: String) {
        Log.i(TAG, "🔍 publishDiscovery() START: topic=$topic, payloadLen=${payload.length}")
        publishedDiscoveryTopics.add(topic)
        Log.d(TAG, "🔍 publishDiscovery() calling publish()...")
        publish(topic, payload, retained = true)
        Log.i(TAG, "🔍 publishDiscovery() COMPLETE: $topic")
    }

    override suspend fun subscribeToCommands(
        topicPattern: String,
        callback: (topic: String, payload: String) -> Unit
    ) = suspendCancellableCoroutine<Unit> { continuation ->
        try {
            val fullPattern = "$_topicPrefix/$topicPattern"
            Log.i(TAG, "📢 subscribeToCommands called: pattern=$fullPattern, mqttClient=${mqttClient != null}, connected=${mqttClient?.isConnected}")
            commandCallbacks[fullPattern] = callback

            val client = mqttClient
            if (client == null || !client.isConnected) {
                Log.e(TAG, "❌ Cannot subscribe - mqttClient is null or not connected!")
                continuation.resumeWithException(Exception("MQTT client is null or not connected"))
                return@suspendCancellableCoroutine
            }

            try {
                client.subscribe(fullPattern, QOS, null, object : IMqttActionListener {
                    override fun onSuccess(asyncActionToken: IMqttToken?) {
                        Log.i(TAG, "✅ Subscribed to: $fullPattern")
                        if (continuation.isActive) {
                            continuation.resume(Unit)
                        }
                    }

                    override fun onFailure(asyncActionToken: IMqttToken?, exception: Throwable?) {
                        Log.e(TAG, "❌ Subscribe failed: $fullPattern", exception)
                        if (continuation.isActive) {
                            continuation.resumeWithException(
                                exception ?: Exception("Subscribe failed")
                            )
                        }
                    }
                })
            } catch (e: Exception) {
                Log.e(TAG, "❌ Exception during subscribe call: $fullPattern", e)
                if (continuation.isActive) {
                    continuation.resumeWithException(e)
                }
                return@suspendCancellableCoroutine
            }

            continuation.invokeOnCancellation {
                try {
                    mqttClient?.unsubscribe(fullPattern)
                } catch (e: Exception) {
                    Log.w(TAG, "Failed to unsubscribe on cancellation: $fullPattern", e)
                }
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

    suspend fun publishAvailability(topic: String, online: Boolean) {
        val fullTopic = "$_topicPrefix/$topic"
        val payload = if (online) "online" else "offline"
        publish(fullTopic, payload, retained = true)
        Log.d(TAG, "📡 Published availability to $fullTopic: $payload")
    }

    private val publishedDiscoveryTopics = mutableSetOf<String>()

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

    suspend fun clearPluginDiscovery(pluginPattern: String): Int {
        val client = mqttClient
        if (client == null || !client.isConnected) {
            Log.w(TAG, "Cannot clear discovery - MQTT not connected")
            return 0
        }

        Log.i(TAG, "🧹 Clearing HA discovery for pattern: $pluginPattern")

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

            // Publish to both global and bridge-specific availability topics
            val globalAvailTopic = "$_topicPrefix/$AVAILABILITY_TOPIC"
            val deviceSuffix = getDeviceSuffix(context)
            val nodeId = "ble_mqtt_bridge_${deviceSuffix}"
            val bridgeAvailTopic = "$_topicPrefix/$nodeId/$AVAILABILITY_TOPIC"
            
            val onlineMessage = MqttMessage("online".toByteArray()).apply {
                qos = QOS
                isRetained = true
            }
            
            // Publish to global availability topic (for backward compatibility)
            Log.i(TAG, "📡 Publishing 'online' to global topic: $globalAvailTopic")
            client.publish(globalAvailTopic, onlineMessage)
            Log.i(TAG, "✅ Published availability: online to $globalAvailTopic")
            
            // Publish to bridge-specific availability topic (for bridge health sensor)
            Log.i(TAG, "📡 Publishing 'online' to bridge topic: $bridgeAvailTopic")
            client.publish(bridgeAvailTopic, onlineMessage)
            Log.i(TAG, "✅ Published availability: online to $bridgeAvailTopic")

            publishAvailabilityDiscovery()
            publishSystemDiagnostics()
        } catch (e: Exception) {
            Log.e(TAG, "❌ Error in onMqttConnected", e)
        }
    }

    private fun publishAvailabilityDiscovery() {
        try {
            val client = mqttClient ?: return
            val deviceSuffix = getDeviceSuffix(context)
            val nodeId = "ble_mqtt_bridge_${deviceSuffix}"
            val uniqueId = "ble_mqtt_bridge_${deviceSuffix}_availability"

            val appVersion = try {
                context.packageManager.getPackageInfo(context.packageName, 0).versionName ?: "unknown"
            } catch (e: Exception) {
                "unknown"
            }

            val btMac = try {
                android.bluetooth.BluetoothAdapter.getDefaultAdapter()?.address ?: "02:00:00:00:00:00"
            } catch (e: Exception) {
                "02:00:00:00:00:00"
            }

            val deviceName = "BLE MQTT Bridge ${deviceSuffix.uppercase()}"
            val builder: DiscoveryBuilder = DiscoveryBuilderFactory.create(
                format = _discoveryFormat,
                deviceMac = btMac,
                deviceName = deviceName,
                deviceManufacturer = "phurth",
                appVersion = appVersion
            )

            // Use bridge-specific availability topic instead of global availability topic
            val stateTopic = "$_topicPrefix/$nodeId/$AVAILABILITY_TOPIC"
            val baseTopic = nodeId
            val discoveryPayload = builder.buildBinarySensor(
                uniqueId = uniqueId,
                displayName = "BLE Bridge Availability",
                stateTopic = stateTopic,
                baseTopic = baseTopic,
                deviceIdentifier = nodeId,
                deviceClass = "connectivity",
                icon = null,
                payloadOn = "online",
                payloadOff = "offline"
            ).toString()

            val discoveryTopic = builder.buildDiscoveryTopic("binary_sensor", nodeId, "availability")
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

    private fun publishSystemDiagnostics() {
        try {
            val client = mqttClient ?: return
            val deviceSuffix = getDeviceSuffix(context)
            val nodeId = "ble_mqtt_bridge_${deviceSuffix}"

            val appVersion = try {
                context.packageManager.getPackageInfo(context.packageName, 0).versionName ?: "unknown"
            } catch (e: Exception) {
                "unknown"
            }

            val btMac = try {
                android.bluetooth.BluetoothAdapter.getDefaultAdapter()?.address ?: "02:00:00:00:00:00"
            } catch (e: Exception) {
                "02:00:00:00:00:00"
            }

            val deviceName = "BLE MQTT Bridge ${deviceSuffix.uppercase()}"
            val builder: DiscoveryBuilder = DiscoveryBuilderFactory.create(
                format = _discoveryFormat,
                deviceMac = btMac,
                deviceName = deviceName,
                deviceManufacturer = "phurth",
                appVersion = appVersion
            )

            publishDiagnosticSensor(
                builder = builder,
                nodeId = nodeId,
                objectId = "battery",
                name = "Battery Level",
                deviceClass = "battery",
                unitOfMeasurement = "%",
                stateClass = "measurement",
                icon = null,
                value = getBatteryLevel().toString()
            )

            publishDiagnosticSensor(
                builder = builder,
                nodeId = nodeId,
                objectId = "battery_status",
                name = "Battery Status",
                icon = "mdi:battery-charging",
                value = getBatteryStatus()
            )

            publishDiagnosticSensor(
                builder = builder,
                nodeId = nodeId,
                objectId = "battery_temp",
                name = "Battery Temperature",
                deviceClass = "temperature",
                unitOfMeasurement = "°C",
                stateClass = "measurement",
                icon = null,
                value = getBatteryTemperature().toString()
            )

            publishDiagnosticSensor(
                builder = builder,
                nodeId = nodeId,
                objectId = "ram_used",
                name = "RAM Used",
                icon = "mdi:memory",
                unitOfMeasurement = "%",
                stateClass = "measurement",
                value = getMemoryUsedPercent().toString()
            )

            publishDiagnosticSensor(
                builder = builder,
                nodeId = nodeId,
                objectId = "ram_available",
                name = "RAM Available",
                icon = "mdi:memory",
                unitOfMeasurement = "MB",
                stateClass = "measurement",
                value = getMemoryAvailableMB().toString()
            )

            publishDiagnosticSensor(
                builder = builder,
                nodeId = nodeId,
                objectId = "cpu_usage",
                name = "CPU Usage",
                icon = "mdi:cpu-64-bit",
                unitOfMeasurement = "%",
                stateClass = "measurement",
                value = getCpuUsage().toString()
            )

            publishDiagnosticSensor(
                builder = builder,
                nodeId = nodeId,
                objectId = "storage_available",
                name = "Storage Available",
                icon = "mdi:harddisk",
                unitOfMeasurement = "GB",
                stateClass = "measurement",
                value = getStorageAvailableGB().toString()
            )

            publishDiagnosticSensor(
                builder = builder,
                nodeId = nodeId,
                objectId = "storage_used",
                name = "Storage Used",
                icon = "mdi:harddisk",
                unitOfMeasurement = "%",
                stateClass = "measurement",
                value = getStorageUsedPercent().toString()
            )

            publishDiagnosticSensor(
                builder = builder,
                nodeId = nodeId,
                objectId = "wifi_ssid",
                name = "WiFi Network",
                icon = "mdi:wifi",
                stateClass = null,
                unitOfMeasurement = null,
                value = getWifiSSID()
            )

            publishDiagnosticSensor(
                builder = builder,
                nodeId = nodeId,
                objectId = "wifi_rssi",
                name = "WiFi Signal",
                deviceClass = "signal_strength",
                unitOfMeasurement = "dBm",
                stateClass = "measurement",
                icon = null,
                value = getWifiRSSI().toString()
            )

            publishDiagnosticSensor(
                builder = builder,
                nodeId = nodeId,
                objectId = "device_name",
                name = "Device Name",
                icon = "mdi:cellphone",
                stateClass = null,
                unitOfMeasurement = null,
                value = Build.MODEL ?: "unknown"
            )

            publishDiagnosticSensor(
                builder = builder,
                nodeId = nodeId,
                objectId = "device_manufacturer",
                name = "Device Manufacturer",
                icon = "mdi:factory",
                stateClass = null,
                unitOfMeasurement = null,
                value = Build.MANUFACTURER ?: "unknown"
            )

            publishDiagnosticSensor(
                builder = builder,
                nodeId = nodeId,
                objectId = "android_version",
                name = "Android Version",
                icon = "mdi:android",
                stateClass = null,
                unitOfMeasurement = null,
                value = "${Build.VERSION.RELEASE} (API ${Build.VERSION.SDK_INT})"
            )

            publishDiagnosticSensor(
                builder = builder,
                nodeId = nodeId,
                objectId = "device_uptime",
                name = "Device Uptime",
                icon = "mdi:timer",
                unitOfMeasurement = "h",
                stateClass = "measurement",
                value = getDeviceUptimeHours().toString()
            )

            Log.i(TAG, "📡 Published system diagnostic sensors")
        } catch (e: Exception) {
            Log.e(TAG, "Error publishing system diagnostics", e)
        }
    }

    private fun publishDiagnosticSensor(
        builder: DiscoveryBuilder,
        nodeId: String,
        objectId: String,
        name: String,
        deviceClass: String? = null,
        unitOfMeasurement: String? = null,
        stateClass: String? = null,
        icon: String? = null,
        value: String
    ) {
        try {
            val client = mqttClient ?: return
            val uniqueId = "${nodeId}_${objectId}"

            val stateTopic = "$_topicPrefix/sensor/$nodeId/$objectId/state"
            val baseTopic = nodeId

            val discoveryPayload = builder.buildSensor(
                uniqueId = uniqueId,
                displayName = name,
                stateTopic = stateTopic,
                baseTopic = baseTopic,
                deviceIdentifier = nodeId,
                unitOfMeasurement = unitOfMeasurement,
                deviceClass = deviceClass,
                icon = icon,
                stateClass = stateClass,
                valueTemplate = null,
                jsonAttributes = false
            ).toString()

            val discoveryTopic = builder.buildDiscoveryTopic("sensor", nodeId, objectId)
            val discoveryMessage = MqttMessage(discoveryPayload.toByteArray()).apply {
                qos = QOS
                isRetained = true
            }
            client.publish(discoveryTopic, discoveryMessage)

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
        // Use MemoryManager if available (single source of truth)
        return memoryManager?.let {
            it.memoryInfo.value.usagePercentage
        } ?: run {
            // Fallback to local calculation if MemoryManager not provided
            try {
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
    }

    private fun getMemoryAvailableMB(): Long {
        // Use MemoryManager if available (single source of truth)
        return memoryManager?.let {
            it.memoryInfo.value.freeMemoryMb
        } ?: run {
            // Fallback to local calculation if MemoryManager not provided
            try {
                val activityManager = context.getSystemService(Context.ACTIVITY_SERVICE) as ActivityManager
                val memInfo = ActivityManager.MemoryInfo()
                activityManager.getMemoryInfo(memInfo)
                memInfo.availMem / (1024 * 1024)
            } catch (e: Exception) {
                Log.w(TAG, "Error getting available memory", e)
                -1
            }
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
            val wasConnected = mqttClient?.isConnected == true
            if (wasConnected) {
                Log.i(TAG, "Disconnecting MQTT client")
                try {
                    val offlineMsg = MqttMessage("offline".toByteArray()).apply {
                        qos = QOS
                        isRetained = true
                    }
                    mqttClient?.publish("${_topicPrefix}/$AVAILABILITY_TOPIC", offlineMsg)
                } catch (e: Exception) {
                    Log.w(TAG, "Failed to publish offline before disconnect", e)
                }
                mqttClient?.disconnect()
            }
            mqttClient?.close()
            mqttClient = null
            commandCallbacks.clear()
            connectionStatusListener?.onConnectionStatusChanged(false)
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

    private suspend fun publish(topic: String, payload: String, retained: Boolean) = suspendCancellableCoroutine<Unit> { continuation ->
        try {
            val isDiscovery = topic.contains("/config") || payload.contains("\"availability_topic\"")
            val marker = if (isDiscovery) "🔍 DISCOVERY" else "📊 STATE"
            
            Log.d(TAG, "$marker publish() START: topic=$topic, payloadLen=${payload.length}, retained=$retained")
            
            val client = mqttClient
            Log.d(TAG, "$marker publish() client check: client=${client != null}")

            val connected = try {
                val result = client != null && client.isConnected
                Log.d(TAG, "$marker publish() connection: connected=$result")
                result
            } catch (e: Exception) {
                Log.e(TAG, "$marker publish() connection check FAILED: ${e.message}", e)
                false
            }

            if (!connected) {
                Log.w(TAG, "$marker ABORTED - not connected: $topic")
                continuation.resume(Unit)
                return@suspendCancellableCoroutine
            }

            val message = MqttMessage(payload.toByteArray()).apply {
                qos = QOS
                isRetained = retained
            }
            
            Log.d(TAG, "$marker publish() calling Paho client.publish(): $topic")

            client!!.publish(topic, message, null, object : IMqttActionListener {
                override fun onSuccess(asyncActionToken: IMqttToken?) {
                    Log.i(TAG, "$marker ✅ Paho onSuccess: $topic (${payload.length} bytes, retained=$retained)")
                    continuation.resume(Unit)
                }

                override fun onFailure(asyncActionToken: IMqttToken?, exception: Throwable?) {
                    Log.e(TAG, "$marker ❌ Paho onFailure: $topic - ${exception?.message}", exception)
                    continuation.resume(Unit)
                }
            })
            
            Log.d(TAG, "$marker publish() Paho call completed for: $topic")

        } catch (e: Exception) {
            Log.e(TAG, "💥 publish() EXCEPTION: ${e.message}", e)
            continuation.resume(Unit)
        }
    }

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
