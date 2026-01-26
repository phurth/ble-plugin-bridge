package com.blemqttbridge.plugins.peplink

import android.content.Context
import android.util.Log
import com.blemqttbridge.core.interfaces.MqttPublisher
import com.blemqttbridge.core.interfaces.PluginConfig
import com.blemqttbridge.core.interfaces.PollingDevicePlugin
import kotlinx.coroutines.*
import org.json.JSONArray
import org.json.JSONObject

/**
 * Peplink Router Plugin for BLE-MQTT Bridge.
 *
 * Features:
 * - Monitors WAN connection status (Ethernet, Cellular, WiFi, vWAN)
 * - Tracks cellular signal strength and data usage
 * - Supports priority switching for WAN failover
 * - Cellular modem reset capability
 * - Multi-instance support for multiple routers (main + towed vehicle)
 * - Home Assistant MQTT Discovery integration
 * - Dynamic hardware discovery for router-specific capabilities
 *
 * Configuration:
 * - base_url: Router IP/hostname (e.g., "http://192.168.1.1")
 * - client_id: OAuth client ID from router
 * - client_secret: OAuth client secret from router
 * - polling_interval: Poll interval in seconds (default: 30)
 * - instance_name: Unique identifier for this router (e.g., "main", "towed")
 */
class PeplinkPlugin : PollingDevicePlugin {

    companion object {
        private const val TAG = "PeplinkPlugin"
        private const val DEFAULT_POLLING_INTERVAL_MS = 30000L  // 30 seconds
    }

    // ===== IDENTIFICATION =====

    override val pluginId: String = "peplink"
    override var instanceId: String = "peplink_main"
    override val supportsMultipleInstances: Boolean = true
    override val displayName: String = "Peplink Router"

    // ===== CONFIGURATION =====

    private lateinit var context: Context
    private lateinit var apiClient: PeplinkApiClient
    private lateinit var mqttPublisher: MqttPublisher

    private var baseUrl: String = ""
    private var clientId: String = ""
    private var clientSecret: String = ""
    private var pollingIntervalMs: Long = DEFAULT_POLLING_INTERVAL_MS
    private var instanceName: String = "main"

    // ===== STATE =====

    private var hardwareConfig: PeplinkDiscovery.HardwareConfig? = null
    private var pollingJob: Job? = null
    private var lastWanState: Map<Int, WanConnection> = emptyMap()

    // ===== LIFECYCLE =====

    override fun initialize(context: Context, config: PluginConfig) {
        this.context = context
        this.baseUrl = config.getString("base_url", "")
        this.clientId = config.getString("client_id", "")
        this.clientSecret = config.getString("client_secret", "")
        this.instanceName = config.getString("instance_name", "main")
        this.pollingIntervalMs = (config.getString("polling_interval", "30").toLongOrNull() ?: 30) * 1000
        this.instanceId = "peplink_$instanceName"

        Log.i(TAG, "[$instanceId] Initialized - URL: $baseUrl, Polling: ${pollingIntervalMs}ms")
    }

    override fun destroy() {
        runBlocking {
            stopPolling()
        }
        Log.i(TAG, "[$instanceId] Destroyed")
    }

    // ===== POLLING =====

    override fun getPollingInterval(): Long = pollingIntervalMs

    override suspend fun startPolling(mqttPublisher: MqttPublisher): Result<Unit> {
        return try {
            this.mqttPublisher = mqttPublisher
            this.apiClient = PeplinkApiClient(baseUrl, clientId, clientSecret)

            Log.i(TAG, "[$instanceId] Starting polling...")

            // Step 1: Discover hardware configuration
            val discoveryResult = discoverHardware()
            if (discoveryResult.isFailure) {
                Log.e(TAG, "[$instanceId] Hardware discovery failed: ${discoveryResult.exceptionOrNull()?.message}")
                return discoveryResult
            }

            // Step 2: Publish Home Assistant discovery payloads
            publishDiscoveryPayloads()

            // Step 3: Start periodic polling
            pollingJob = CoroutineScope(Dispatchers.IO + SupervisorJob()).launch {
                while (isActive) {
                    try {
                        poll()
                    } catch (e: CancellationException) {
                        throw e  // Propagate cancellation
                    } catch (e: Exception) {
                        Log.e(TAG, "[$instanceId] Poll error: ${e.message}", e)
                    }
                    delay(pollingIntervalMs)
                }
            }

            Log.i(TAG, "[$instanceId] Polling started successfully")
            Result.success(Unit)
        } catch (e: Exception) {
            Log.e(TAG, "[$instanceId] Failed to start polling", e)
            Result.failure(e)
        }
    }

    override suspend fun stopPolling() {
        pollingJob?.cancel()
        pollingJob = null
        Log.i(TAG, "[$instanceId] Polling stopped")
    }

    override suspend fun poll(): Result<Unit> {
        return try {
            // Query WAN status
            val statusResult = apiClient.getWanStatus("1 2 3 4 5 6 7 8 9 10")
            if (statusResult.isFailure) {
                return statusResult.map { Unit }
            }

            val wanStatus = statusResult.getOrThrow()

            // Publish state for each WAN connection
            for ((connId, connection) in wanStatus) {
                publishWanState(connId, connection)
            }

            // Update cached state
            lastWanState = wanStatus

            Result.success(Unit)
        } catch (e: Exception) {
            Log.e(TAG, "[$instanceId] Poll failed", e)
            Result.failure(e)
        }
    }

    // ===== MQTT INTEGRATION =====

    override fun getMqttBaseTopic(): String = "peplink/$instanceName"

    override fun getDiscoveryPayloads(): List<Pair<String, String>> {
        val config = hardwareConfig ?: return emptyList()
        val payloads = mutableListOf<Pair<String, String>>()

        // Device info for Home Assistant
        val deviceInfo = JSONObject().apply {
            put("identifiers", JSONArray(listOf(instanceId)))
            put("name", "Peplink Router ($instanceName)")
            put("manufacturer", "Peplink")
            put("model", "Router")
            put("sw_version", "8.5.0+")
        }

        // Get full topic prefix (e.g., "homeassistant")
        val topicPrefix = mqttPublisher.topicPrefix

        for ((connId, connection) in config.wanConnections) {
            val baseTopic = getMqttBaseTopic()
            val uniqueId = "${instanceId}_wan${connId}"

            // Full state topic includes the MQTT prefix
            val fullBaseTopic = "$topicPrefix/$baseTopic"

            // Binary Sensor: Connection Status
            payloads.add(
                "homeassistant/binary_sensor/${uniqueId}_status/config" to JSONObject().apply {
                    put("name", "${connection.name} Status")
                    put("unique_id", "${uniqueId}_status")
                    put("state_topic", "$fullBaseTopic/wan/$connId/status")
                    put("payload_on", "connected")
                    put("payload_off", "disconnected")
                    put("device_class", "connectivity")
                    put("device", deviceInfo)
                }.toString()
            )

            // Sensor: Priority Level
            payloads.add(
                "homeassistant/sensor/${uniqueId}_priority/config" to JSONObject().apply {
                    put("name", "${connection.name} Priority")
                    put("unique_id", "${uniqueId}_priority")
                    put("state_topic", "$fullBaseTopic/wan/$connId/priority")
                    put("icon", "mdi:sort-numeric-ascending")
                    put("device", deviceInfo)
                }.toString()
            )

            // Sensor: IP Address
            payloads.add(
                "homeassistant/sensor/${uniqueId}_ip/config" to JSONObject().apply {
                    put("name", "${connection.name} IP")
                    put("unique_id", "${uniqueId}_ip")
                    put("state_topic", "$fullBaseTopic/wan/$connId/ip")
                    put("icon", "mdi:ip-network")
                    put("device", deviceInfo)
                }.toString()
            )

            // Sensor: Uptime
            payloads.add(
                "homeassistant/sensor/${uniqueId}_uptime/config" to JSONObject().apply {
                    put("name", "${connection.name} Uptime")
                    put("unique_id", "${uniqueId}_uptime")
                    put("state_topic", "$fullBaseTopic/wan/$connId/uptime")
                    put("unit_of_measurement", "s")
                    put("device_class", "duration")
                    put("icon", "mdi:clock-outline")
                    put("device", deviceInfo)
                }.toString()
            )

            // Select: Priority Control
            payloads.add(
                "homeassistant/select/${uniqueId}_priority_control/config" to JSONObject().apply {
                    put("name", "${connection.name} Priority Control")
                    put("unique_id", "${uniqueId}_priority_control")
                    put("command_topic", "$fullBaseTopic/wan/$connId/priority/set")
                    put("state_topic", "$fullBaseTopic/wan/$connId/priority")
                    put("options", JSONArray(listOf("1", "2", "3", "4")))
                    put("icon", "mdi:priority-high")
                    put("device", deviceInfo)
                }.toString()
            )

            // Cellular-specific sensors
            if (connection.type == WanType.CELLULAR) {
                // Sensor: Signal Strength
                payloads.add(
                    "homeassistant/sensor/${uniqueId}_signal/config" to JSONObject().apply {
                        put("name", "${connection.name} Signal")
                        put("unique_id", "${uniqueId}_signal")
                        put("state_topic", "$fullBaseTopic/wan/$connId/cellular/signal")
                        put("unit_of_measurement", "dBm")
                        put("device_class", "signal_strength")
                        put("icon", "mdi:signal-cellular-3")
                        put("device", deviceInfo)
                    }.toString()
                )

                // Sensor: Carrier
                payloads.add(
                    "homeassistant/sensor/${uniqueId}_carrier/config" to JSONObject().apply {
                        put("name", "${connection.name} Carrier")
                        put("unique_id", "${uniqueId}_carrier")
                        put("state_topic", "$fullBaseTopic/wan/$connId/cellular/carrier")
                        put("icon", "mdi:sim")
                        put("device", deviceInfo)
                    }.toString()
                )

                // Sensor: Network Type
                payloads.add(
                    "homeassistant/sensor/${uniqueId}_network/config" to JSONObject().apply {
                        put("name", "${connection.name} Network")
                        put("unique_id", "${uniqueId}_network")
                        put("state_topic", "$fullBaseTopic/wan/$connId/cellular/network")
                        put("icon", "mdi:network")
                        put("device", deviceInfo)
                    }.toString()
                )

                // Button: Reset Modem
                payloads.add(
                    "homeassistant/button/${uniqueId}_reset/config" to JSONObject().apply {
                        put("name", "${connection.name} Reset Modem")
                        put("unique_id", "${uniqueId}_reset")
                        put("command_topic", "$fullBaseTopic/wan/$connId/cellular/reset")
                        put("payload_press", "RESET")
                        put("icon", "mdi:restart")
                        put("device", deviceInfo)
                    }.toString()
                )
            }
        }

        Log.i(TAG, "[$instanceId] Generated ${payloads.size} discovery payloads")
        return payloads
    }

    // ===== COMMAND HANDLING =====

    override suspend fun handleCommand(topic: String, payload: String): Result<Unit> {
        Log.i(TAG, "[$instanceId] Handling command: $topic = $payload")

        return try {
            val baseTopic = getMqttBaseTopic()

            when {
                // Priority switching: peplink/main/wan/1/priority/set
                topic.matches(Regex("$baseTopic/wan/(\\d+)/priority/set")) -> {
                    val connId = Regex("$baseTopic/wan/(\\d+)/priority/set").find(topic)?.groupValues?.get(1)?.toIntOrNull()
                        ?: return Result.failure(IllegalArgumentException("Invalid connId in topic"))
                    val priority = payload.toIntOrNull()
                        ?: return Result.failure(IllegalArgumentException("Invalid priority value"))

                    apiClient.setWanPriority(connId, priority)
                }

                // Cellular reset: peplink/main/wan/1/cellular/reset
                topic.matches(Regex("$baseTopic/wan/(\\d+)/cellular/reset")) -> {
                    val connId = Regex("$baseTopic/wan/(\\d+)/cellular/reset").find(topic)?.groupValues?.get(1)?.toIntOrNull()
                        ?: return Result.failure(IllegalArgumentException("Invalid connId in topic"))

                    apiClient.resetCellularModem(connId)
                }

                else -> {
                    Log.w(TAG, "[$instanceId] Unknown command topic: $topic")
                    Result.failure(IllegalArgumentException("Unknown command topic"))
                }
            }
        } catch (e: Exception) {
            Log.e(TAG, "[$instanceId] Command failed", e)
            Result.failure(e)
        }
    }

    // ===== HARDWARE DISCOVERY =====

    override suspend fun discoverHardware(): Result<Unit> {
        return try {
            val result = PeplinkDiscovery.discoverHardware(apiClient)
            if (result.isSuccess) {
                hardwareConfig = result.getOrThrow()
                Log.i(TAG, "[$instanceId] Hardware discovery complete: ${hardwareConfig?.wanConnections?.size} WAN connections")
            }
            result.map { Unit }
        } catch (e: Exception) {
            Log.e(TAG, "[$instanceId] Hardware discovery failed", e)
            Result.failure(e)
        }
    }

    // ===== PRIVATE HELPERS =====

    private suspend fun publishDiscoveryPayloads() {
        val payloads = getDiscoveryPayloads()
        for ((topic, payload) in payloads) {
            mqttPublisher.publishDiscovery(topic, payload)
        }
        Log.i(TAG, "[$instanceId] Published ${payloads.size} discovery payloads")
    }

    private suspend fun publishWanState(connId: Int, connection: WanConnection) {
        val baseTopic = getMqttBaseTopic()

        // Status
        val status = when (connection.status) {
            ConnectionStatus.CONNECTED -> "connected"
            ConnectionStatus.DISCONNECTED -> "disconnected"
            ConnectionStatus.DISABLED -> "disabled"
            else -> "unknown"
        }
        mqttPublisher.publishState("$baseTopic/wan/$connId/status", status)

        // Priority (always publish, even if null)
        val priority = connection.priority?.toString() ?: ""
        mqttPublisher.publishState("$baseTopic/wan/$connId/priority", priority)

        // IP Address (always publish, even if null)
        val ip = connection.ip ?: ""
        mqttPublisher.publishState("$baseTopic/wan/$connId/ip", ip)

        // Uptime (always publish, even if null)
        val uptime = connection.uptime?.toString() ?: "0"
        mqttPublisher.publishState("$baseTopic/wan/$connId/uptime", uptime)

        // Cellular-specific data
        connection.cellular?.let { cellular ->
            mqttPublisher.publishState("$baseTopic/wan/$connId/cellular/signal", cellular.signalStrength.toString())
            mqttPublisher.publishState("$baseTopic/wan/$connId/cellular/carrier", cellular.carrier ?: "")
            mqttPublisher.publishState("$baseTopic/wan/$connId/cellular/network", cellular.networkType ?: "")
        }
    }
}
