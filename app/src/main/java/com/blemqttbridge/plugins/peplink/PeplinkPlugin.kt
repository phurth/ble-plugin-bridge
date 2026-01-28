package com.blemqttbridge.plugins.peplink

import android.content.Context
import android.util.Log
import com.blemqttbridge.core.interfaces.MqttPublisher
import com.blemqttbridge.core.interfaces.PluginConfig
import com.blemqttbridge.core.interfaces.PollingDevicePlugin
import kotlinx.coroutines.*
import kotlinx.coroutines.sync.Mutex
import org.json.JSONArray
import org.json.JSONObject
import java.time.Instant
import java.time.LocalDate
import java.time.ZoneId
import kotlin.math.roundToInt

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
 * - username: Admin username for router login
 * - password: Admin password for router login
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
    private var username: String = ""
    private var password: String = ""
    private var pollingIntervalMs: Long = DEFAULT_POLLING_INTERVAL_MS
    private var instanceName: String = "main"

    // ===== STATE =====

    private var hardwareConfig: PeplinkDiscovery.HardwareConfig? = null
    private var pollingJob: Job? = null
    private var pollingManager: PeplinkPollingManager? = null
    private var lastWanState: Map<Int, WanConnection> = emptyMap()
    private var lastWanUsage: Map<Int, WanUsage> = emptyMap()
    private var lastUsagePollAt: Long = 0L
    private var routerFirmwareVersion: String = "unknown"
    private var connectedDevicesCount: Int = 0
    private var pepVpnProfiles: Map<String, Triple<String, String, String>> = emptyMap()
    
    // ===== BANDWIDTH TRACKING =====
    
    private data class BandwidthSnapshot(
        val timestamp: Long,
        val rxBytes: Long,
        val txBytes: Long,
        val connId: Int
    )
    private val lastBandwidthSnapshot = mutableMapOf<Int, BandwidthSnapshot>()
    
    // ===== DIAGNOSTICS STATE =====
    
    private var lastSystemDiagnostics: SystemDiagnostics? = null
    private var lastDeviceInfo: DeviceInfo? = null
    
    private val commandMutex = Mutex()  // Serialize command execution to prevent concurrent API calls

    // ===== POLLING CONFIGURATION =====

    private var statusPollInterval: Int = 10          // seconds
    private var usagePollInterval: Int = 60           // seconds
    private var diagnosticsPollInterval: Int = 30     // seconds
    private var vpnPollInterval: Int = 60             // seconds
    private var gpsPollInterval: Int = 120            // seconds
    private var enableStatusPolling: Boolean = true
    private var enableUsagePolling: Boolean = true
    private var enableDiagnosticsPolling: Boolean = true
    private var enableVpnPolling: Boolean = false
    private var enableGpsPolling: Boolean = false

    // ===== LIFECYCLE =====

    override fun initialize(context: Context, config: PluginConfig) {
        this.context = context
        this.baseUrl = config.getString("base_url", "")
        this.username = config.getString("username", "")
        this.password = config.getString("password", "")
        this.instanceName = config.getString("instance_name", "main")
        this.pollingIntervalMs = (config.getString("polling_interval", "30").toLongOrNull() ?: 30) * 1000
        this.instanceId = "peplink_$instanceName"

        // Load polling configuration
        statusPollInterval = (config.getString("status_poll_interval", "10").toIntOrNull() ?: 10).coerceIn(5, 3600)
        usagePollInterval = (config.getString("usage_poll_interval", "60").toIntOrNull() ?: 60).coerceIn(5, 3600)
        diagnosticsPollInterval = (config.getString("diagnostics_poll_interval", "30").toIntOrNull() ?: 30).coerceIn(5, 3600)
        vpnPollInterval = (config.getString("vpn_poll_interval", "60").toIntOrNull() ?: 60).coerceIn(5, 3600)
        gpsPollInterval = (config.getString("gps_poll_interval", "120").toIntOrNull() ?: 120).coerceIn(5, 3600)

        enableStatusPolling = config.getString("enable_status_polling", "true").toBoolean()
        enableUsagePolling = config.getString("enable_usage_polling", "true").toBoolean()
        enableDiagnosticsPolling = config.getString("enable_diagnostics_polling", "true").toBoolean()
        enableVpnPolling = config.getString("enable_vpn_polling", "false").toBoolean()
        enableGpsPolling = config.getString("enable_gps_polling", "false").toBoolean()

        Log.i(TAG, "[$instanceId] Initialized - URL: $baseUrl, Polling: ${pollingIntervalMs}ms")
        Log.i(TAG, "[$instanceId] Polling config: status=${statusPollInterval}s(${if(enableStatusPolling) "ON" else "OFF"}), " +
                "usage=${usagePollInterval}s(${if(enableUsagePolling) "ON" else "OFF"}), " +
                "diag=${diagnosticsPollInterval}s(${if(enableDiagnosticsPolling) "ON" else "OFF"}), " +
                "vpn=${vpnPollInterval}s(${if(enableVpnPolling) "ON" else "OFF"}), " +
                "gps=${gpsPollInterval}s(${if(enableGpsPolling) "ON" else "OFF"})")
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
            this.apiClient = PeplinkApiClient(baseUrl, username, password)

            Log.i(TAG, "[$instanceId] Starting polling...")

            // Subscribe to command topics for this instance (prefix applied by publisher)
            // Subscribe only to command-specific topics, not state topics, to avoid flooding from polls
            val baseTopic = getMqttBaseTopic()
            mqttPublisher.subscribeToCommands("$baseTopic/wan/+/priority/set") { topic, payload ->
                GlobalScope.launch(Dispatchers.IO) {
                    try {
                        Log.d(TAG, "[$instanceId] Processing priority/set subscription callback")
                        handleCommandSerialized(topic, payload)
                    } catch (e: Exception) {
                        Log.e(TAG, "[$instanceId] Priority command handler failed", e)
                    }
                }
            }
            mqttPublisher.subscribeToCommands("$baseTopic/wan/+/cellular/reset") { topic, payload ->
                GlobalScope.launch(Dispatchers.IO) {
                    try {
                        Log.d(TAG, "[$instanceId] Processing cellular/reset subscription callback")
                        handleCommandSerialized(topic, payload)
                    } catch (e: Exception) {
                        Log.e(TAG, "[$instanceId] Reset command handler failed", e)
                    }
                }
            }

            // Start polling job (runs discovery and polling in background)
            pollingJob = CoroutineScope(Dispatchers.IO + SupervisorJob()).launch {
                // Step 1: Discover hardware configuration
                val discoveryResult = discoverHardware()
                if (discoveryResult.isFailure) {
                    Log.e(TAG, "[$instanceId] Hardware discovery failed: ${discoveryResult.exceptionOrNull()?.message}")
                    return@launch
                }

                // Fetch firmware version
                try {
                    val fw = apiClient.getFirmwareVersion()
                    if (fw.isSuccess) {
                        routerFirmwareVersion = fw.getOrThrow()
                        Log.i(TAG, "[$instanceId] Firmware version: $routerFirmwareVersion")
                    } else {
                        Log.w(TAG, "[$instanceId] Firmware fetch failed: ${fw.exceptionOrNull()?.message}")
                    }
                } catch (e: Exception) {
                    Log.w(TAG, "[$instanceId] Firmware fetch exception: ${e.message}")
                }

                // Fetch initial diagnostic data: clients and VPN profiles
                try {
                    apiClient.getConnectedDevicesCount().onSuccess { connectedDevicesCount = it }
                    apiClient.getPepVpnProfiles().onSuccess { pepVpnProfiles = it }
                } catch (_: Exception) {}

                // Step 2: Publish Home Assistant discovery payloads
                publishDiscoveryPayloads()

                // Publish firmware state once
                try {
                    val base = getMqttBaseTopic()
                    mqttPublisher.publishState("$base/firmware", routerFirmwareVersion)
                    mqttPublisher.publishState("$base/diagnostic/connected_devices", connectedDevicesCount.toString())
                    pepVpnProfiles.forEach { (id, triple) ->
                        mqttPublisher.publishState("$base/diagnostic/vpn/$id/status", triple.third)
                    }
                } catch (_: Exception) {}

                // Step 3: Initialize and start polling manager
                val pollingScope = CoroutineScope(Dispatchers.IO + SupervisorJob())
                pollingManager = PeplinkPollingManager(instanceId, pollingScope)

                // Configure polling intervals
                pollingManager!!.configure(
                    PeplinkPollingManager.PollingConfig(
                        statusInterval = statusPollInterval,
                        usageInterval = usagePollInterval,
                        diagnosticsInterval = diagnosticsPollInterval,
                        vpnInterval = vpnPollInterval,
                        gpsInterval = gpsPollInterval,
                        enableStatusPolling = enableStatusPolling,
                        enableUsagePolling = enableUsagePolling,
                        enableDiagnosticsPolling = enableDiagnosticsPolling,
                        enableVpnPolling = enableVpnPolling,
                        enableGpsPolling = enableGpsPolling
                    )
                )

                // Set up polling callbacks
                pollingManager!!.onStatusPoll = { poll() }
                pollingManager!!.onUsagePoll = { pollUsage() }
                pollingManager!!.onDiagnosticsPoll = { pollDiagnostics() }
                pollingManager!!.onVpnPoll = { pollVpn() }
                pollingManager!!.onGpsPoll = { pollGps() }

                // Force an immediate poll so data is available at startup
                try {
                    poll()
                } catch (e: Exception) {
                    Log.w(TAG, "[$instanceId] Initial poll failed: ${e.message}", e)
                }

                // Start the polling manager
                pollingManager!!.start()

                // Keep coroutine alive while polling manager is running
                while (isActive && pollingManager?.isRunning() == true) {
                    delay(1000)  // Check every second if polling manager is still running
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
            
            // Report API connectivity status
            val connected = statusResult.isSuccess
            val authenticated = true  // OAuth is handled by apiClient; if we got here, auth succeeded
            val dataHealthy = statusResult.isSuccess && statusResult.getOrNull()?.isNotEmpty() == true
            
            // Report status to the service for health tracking
            mqttPublisher.updatePluginStatus(instanceId, connected, authenticated, dataHealthy)
            
            if (statusResult.isFailure) {
                Log.w(TAG, "[$instanceId] API call failed: ${statusResult.exceptionOrNull()?.message}")
                return statusResult.map { Unit }
            }

            val wanStatus = statusResult.getOrThrow()

            // Publish state for each WAN connection
            for ((connId, connection) in wanStatus) {
                // Get enriched connection from hardware config (has simSlotCount set)
                val enrichedConnection = hardwareConfig?.wanConnections?.get(connId) ?: connection
                
                // Publish availability per WAN (enabled -> online/offline)
                val baseTopic = getMqttBaseTopic()
                mqttPublisher.publishAvailability("$baseTopic/wan/$connId/availability", connection.enabled)

                publishWanState(connId, enrichedConnection)
            }

            // Update cached state
            lastWanState = wanStatus

            Result.success(Unit)
        } catch (e: Exception) {
            Log.e(TAG, "[$instanceId] Status poll failed", e)
            // Report unhealthy status on exception
            mqttPublisher.updatePluginStatus(instanceId, false, false, false)
            Result.failure(e)
        }
    }

    private suspend fun pollUsage() {
        try {
            val usageResult = apiClient.getWanUsage()
            if (usageResult.isSuccess) {
                lastWanUsage = usageResult.getOrThrow()
                
                // Publish usage data for each WAN
                for ((connId, usage) in lastWanUsage) {
                    val baseTopic = getMqttBaseTopic()
                    mqttPublisher.publishState("$baseTopic/wan/$connId/usage/current", usage.usage.toString())
                    
                    // Publish per-SIM usage if available
                    usage.simSlots?.forEach { (slotId, simInfo) ->
                        mqttPublisher.publishState("$baseTopic/wan/$connId/sim/$slotId/usage", simInfo.usage.toString())
                    }
                }
                
                Log.d(TAG, "[$instanceId] Usage poll successful")
            } else {
                Log.w(TAG, "[$instanceId] Usage poll failed: ${usageResult.exceptionOrNull()?.message}")
            }
        } catch (e: Exception) {
            Log.e(TAG, "[$instanceId] Usage poll exception: ${e.message}")
        }
    }

    private suspend fun pollDiagnostics() {
        try {
            val base = getMqttBaseTopic()
            
            // Get system diagnostics (temperature, fans)
            apiClient.getSystemDiagnostics().onSuccess { diagnostics ->
                lastSystemDiagnostics = diagnostics
                
                // Publish temperature and threshold with availability
                if (diagnostics.temperature != null) {
                    mqttPublisher.publishState("$base/diagnostic/system/temperature", String.format("%.1f", diagnostics.temperature))
                    mqttPublisher.publishState("$base/diagnostic/system/temperature/availability", "online")
                } else {
                    mqttPublisher.publishState("$base/diagnostic/system/temperature/availability", "offline")
                }
                
                if (diagnostics.temperatureThreshold != null) {
                    mqttPublisher.publishState("$base/diagnostic/system/temperature_threshold", String.format("%.0f", diagnostics.temperatureThreshold))
                } else {
                    mqttPublisher.publishState("$base/diagnostic/system/temperature/availability", "offline")
                }
                
                // Publish fan information with availability
                diagnostics.fans.forEach { fan ->
                    val fanBase = "$base/diagnostic/fan/${fan.id}"
                    mqttPublisher.publishState("$fanBase/status", fan.status)
                    if (fan.speedRpm != null) {
                        mqttPublisher.publishState("$fanBase/speed_rpm", fan.speedRpm.toString())
                        mqttPublisher.publishState("$fanBase/availability", "online")
                    } else {
                        mqttPublisher.publishState("$fanBase/availability", "offline")
                    }
                    if (fan.speedPercent != null) {
                        mqttPublisher.publishState("$fanBase/speed_percent", fan.speedPercent.toString())
                    }
                }
            }
            
            // Get device information (serial number, model)
            apiClient.getDeviceInfo().onSuccess { deviceInfo ->
                lastDeviceInfo = deviceInfo
                mqttPublisher.publishState("$base/diagnostic/device/serial_number", deviceInfo.serialNumber)
                mqttPublisher.publishState("$base/diagnostic/device/model", deviceInfo.model)
                mqttPublisher.publishState("$base/diagnostic/device/availability", "online")
                deviceInfo.hardwareVersion?.let {
                    mqttPublisher.publishState("$base/diagnostic/device/hardware_version", it)
                }
            }.onFailure {
                mqttPublisher.publishState("$base/diagnostic/device/availability", "offline")
            }
            
            // Get connected devices count
            apiClient.getConnectedDevicesCount().onSuccess { count ->
                connectedDevicesCount = count
                mqttPublisher.publishState("$base/diagnostic/connected_devices", connectedDevicesCount.toString())
            }
            
            // Get bandwidth/traffic rates from the traffic API
            publishBandwidthFromTraffic()
            
            Log.d(TAG, "[$instanceId] Diagnostics poll successful")
        } catch (e: Exception) {
            Log.e(TAG, "[$instanceId] Diagnostics poll exception: ${e.message}")
        }
    }
    
    /**
     * Get bandwidth rates from the traffic API and publish to MQTT.
     * This is more reliable than calculating from byte counters.
     */
    private suspend fun publishBandwidthFromTraffic() {
        val base = getMqttBaseTopic()
        
        apiClient.getTrafficStats().onSuccess { stats ->
            if (stats.isNotEmpty()) {
                for ((connId, ratesDownloadUp) in stats) {
                    val downloadMbps = ratesDownloadUp.first
                    val uploadMbps = ratesDownloadUp.second
                    
                    // Only publish if we have non-zero rates
                    if (downloadMbps > 0 || uploadMbps > 0) {
                        mqttPublisher.publishState("$base/wan/$connId/bandwidth/download_mbps", String.format("%.1f", downloadMbps))
                        mqttPublisher.publishState("$base/wan/$connId/bandwidth/upload_mbps", String.format("%.1f", uploadMbps))
                    }
                }
                Log.d(TAG, "[$instanceId] Published bandwidth for ${stats.size} connections")
            }
        }
    }

    private suspend fun pollVpn() {
        try {
            apiClient.getPepVpnProfiles().onSuccess { profiles ->
                pepVpnProfiles = profiles
                val base = getMqttBaseTopic()
                pepVpnProfiles.forEach { (id, triple) ->
                    mqttPublisher.publishState("$base/diagnostic/vpn/$id/status", triple.third)
                    mqttPublisher.publishState("$base/diagnostic/vpn/$id/availability", "online")
                }
            }.onFailure {
                val base = getMqttBaseTopic()
                pepVpnProfiles.forEach { (id, _) ->
                    mqttPublisher.publishState("$base/diagnostic/vpn/$id/availability", "offline")
                }
            }
            Log.d(TAG, "[$instanceId] VPN poll successful")
        } catch (e: Exception) {
            Log.e(TAG, "[$instanceId] VPN poll exception: ${e.message}")
            val base = getMqttBaseTopic()
            pepVpnProfiles.forEach { (id, _) ->
                mqttPublisher.publishState("$base/diagnostic/vpn/$id/availability", "offline")
            }
        }
    }

    private suspend fun pollGps() {
        try {
            // GPS polling will be implemented in Phase 2
            Log.d(TAG, "[$instanceId] GPS poll - not yet implemented")
        } catch (e: Exception) {
            Log.e(TAG, "[$instanceId] GPS poll exception: ${e.message}")
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
            put("sw_version", routerFirmwareVersion)
        }

        // Get full topic prefix (e.g., "homeassistant")
        val topicPrefix = mqttPublisher.topicPrefix

        // Global diagnostic sensor: Firmware version
        run {
            val topicPrefix = mqttPublisher.topicPrefix
            val baseTopic = getMqttBaseTopic()
            val fullBaseTopic = "$topicPrefix/$baseTopic"
            payloads.add(
                "homeassistant/sensor/${instanceId}_firmware/config" to JSONObject().apply {
                    put("name", "Router Firmware Version")
                    put("unique_id", "${instanceId}_firmware")
                    put("state_topic", "$fullBaseTopic/firmware")
                    put("icon", "mdi:update")
                    put("entity_category", "diagnostic")
                    put("device", deviceInfo)
                }.toString()
            )

            // Connected devices (diagnostic)
            payloads.add(
                "homeassistant/sensor/${instanceId}_connected_devices/config" to JSONObject().apply {
                    put("name", "Connected Devices")
                    put("unique_id", "${instanceId}_connected_devices")
                    put("state_topic", "$fullBaseTopic/diagnostic/connected_devices")
                    put("icon", "mdi:devices")
                    put("entity_category", "diagnostic")
                    put("device", deviceInfo)
                }.toString()
            )

            // VPN profile statuses (diagnostic)
            pepVpnProfiles.forEach { (id, triple) ->
                payloads.add(
                    "homeassistant/sensor/${instanceId}_vpn_${id}_status/config" to JSONObject().apply {
                        put("name", "VPN: ${triple.first}")
                        put("unique_id", "${instanceId}_vpn_${id}_status")
                        put("state_topic", "$fullBaseTopic/diagnostic/vpn/$id/status")
                        put("availability_topic", "$fullBaseTopic/diagnostic/vpn/$id/availability")
                        put("payload_available", "online")
                        put("payload_not_available", "offline")
                        put("icon", "mdi:vpn")
                        put("entity_category", "diagnostic")
                        put("device", deviceInfo)
                    }.toString()
                )
            }
        }

        for ((connId, connection) in config.wanConnections) {
            val baseTopic = getMqttBaseTopic()
            val uniqueId = "${instanceId}_wan${connId}"

            // Full state topic includes the MQTT prefix
            val fullBaseTopic = "$topicPrefix/$baseTopic"
            val availabilityTopic = "$fullBaseTopic/wan/$connId/availability"

            // Sensor: Connection Status (raw message)
            payloads.add(
                "homeassistant/sensor/${uniqueId}_status/config" to JSONObject().apply {
                    put("name", "${connection.name} Status")
                    put("unique_id", "${uniqueId}_status")
                    put("state_topic", "$fullBaseTopic/wan/$connId/status")
                    put("icon", "mdi:lan")
                    put("availability_topic", availabilityTopic)
                    put("payload_available", "online")
                    put("payload_not_available", "offline")
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
                    put("availability_topic", availabilityTopic)
                    put("payload_available", "online")
                    put("payload_not_available", "offline")
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
                    put("availability_topic", availabilityTopic)
                    put("payload_available", "online")
                    put("payload_not_available", "offline")
                    put("device", deviceInfo)
                }.toString()
            )

            // Sensor: Uptime
            payloads.add(
                "homeassistant/sensor/${uniqueId}_uptime/config" to JSONObject().apply {
                    put("name", "${connection.name} Uptime")
                    put("unique_id", "${uniqueId}_uptime")
                    put("state_topic", "$fullBaseTopic/wan/$connId/uptime")
                    put("icon", "mdi:clock-outline")
                    put("availability_topic", availabilityTopic)
                    put("payload_available", "online")
                    put("payload_not_available", "offline")
                    put("device", deviceInfo)
                }.toString()
            )

            // Sensor: Status LED (color indicator)
            payloads.add(
                "homeassistant/sensor/${uniqueId}_status_led/config" to JSONObject().apply {
                    put("name", "${connection.name} Status LED")
                    put("unique_id", "${uniqueId}_status_led")
                    put("state_topic", "$fullBaseTopic/wan/$connId/status_led")
                    put("icon", "mdi:led-outline")
                    put("availability_topic", availabilityTopic)
                    put("payload_available", "online")
                    put("payload_not_available", "offline")
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
                    put("options", JSONArray(listOf("1", "2", "3", "4", "Disabled")))
                    put("icon", "mdi:priority-high")
                    put("device", deviceInfo)
                }.toString()
            )

            // Cellular-specific sensors
            if (connection.type == WanType.CELLULAR) {
                // Sensor: Signal Strength (formatted as "X/5" or "XdBm")
                payloads.add(
                    "homeassistant/sensor/${uniqueId}_signal/config" to JSONObject().apply {
                        put("name", "${connection.name} Signal")
                        put("unique_id", "${uniqueId}_signal")
                        put("state_topic", "$fullBaseTopic/wan/$connId/cellular/signal")
                        put("icon", "mdi:signal-cellular-3")
                        put("availability_topic", availabilityTopic)
                        put("payload_available", "online")
                        put("payload_not_available", "offline")
                        put("device", deviceInfo)
                    }.toString()
                )

                // Sensor: Signal dBm (raw)
                payloads.add(
                    "homeassistant/sensor/${uniqueId}_signal_dbm/config" to JSONObject().apply {
                        put("name", "${connection.name} Signal dBm")
                        put("unique_id", "${uniqueId}_signal_dbm")
                        put("state_topic", "$fullBaseTopic/wan/$connId/cellular/signal_dbm")
                        put("unit_of_measurement", "dBm")
                        put("device_class", "signal_strength")
                        put("icon", "mdi:signal" )
                        put("availability_topic", availabilityTopic)
                        put("payload_available", "online")
                        put("payload_not_available", "offline")
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
                        put("availability_topic", availabilityTopic)
                        put("payload_available", "online")
                        put("payload_not_available", "offline")
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
                        put("availability_topic", availabilityTopic)
                        put("payload_available", "online")
                        put("payload_not_available", "offline")
                        put("device", deviceInfo)
                    }.toString()
                )

                // Sensor: Carrier Aggregation
                payloads.add(
                    "homeassistant/binary_sensor/${uniqueId}_carrier_agg/config" to JSONObject().apply {
                        put("name", "${connection.name} Carrier Aggregation")
                        put("unique_id", "${uniqueId}_carrier_agg")
                        put("state_topic", "$fullBaseTopic/wan/$connId/cellular/carrier_aggregation")
                        put("payload_on", "true")
                        put("payload_off", "false")
                        put("icon", "mdi:signal-variant")
                        put("availability_topic", availabilityTopic)
                        put("payload_available", "online")
                        put("payload_not_available", "offline")
                        put("device", deviceInfo)
                    }.toString()
                )

                // Sensor: Bands (active cellular bands)
                payloads.add(
                    "homeassistant/sensor/${uniqueId}_bands/config" to JSONObject().apply {
                        put("name", "${connection.name} Bands")
                        put("unique_id", "${uniqueId}_bands")
                        put("state_topic", "$fullBaseTopic/wan/$connId/cellular/bands")
                        put("icon", "mdi:radio-tower")
                        put("availability_topic", availabilityTopic)
                        put("payload_available", "online")
                        put("payload_not_available", "offline")
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

            // Sensor: Usage - base per WAN (state=Enabled/Disabled, attributes carry usage/allowance/percent/start)
            // Skip for multi-SIM cellular connections (use per-SIM sensors instead)
            val hasMultipleSims = connection.simSlotCount > 1
            if (!hasMultipleSims) {
                payloads.add(
                    "homeassistant/sensor/${uniqueId}_usage/config" to JSONObject().apply {
                        put("name", "${connection.name}")
                        put("unique_id", "${uniqueId}_usage")
                        put("state_topic", "$fullBaseTopic/wan/$connId/usage_state")
                        put("json_attributes_topic", "$fullBaseTopic/wan/$connId/usage_attributes")
                        put("icon", "mdi:gauge")
                        put("availability_topic", availabilityTopic)
                        put("payload_available", "online")
                        put("payload_not_available", "offline")
                        put("device", deviceInfo)
                    }.toString()
                )
            }

            // Per-SIM slot sensors (for multi-SIM cellular modems)
            // Peplink supports up to 5 SIM slots: SIM A, SIM B, RemoteSIM, FusionSIM, Peplink eSIM
            if (connection.simSlotCount > 1) {
                for (slotId in 1..connection.simSlotCount) {
                    val simUniqueId = "${uniqueId}_sim${slotId}"
                    val slotName = getSimSlotName(slotId)

                    payloads.add(
                        "homeassistant/sensor/${simUniqueId}_usage/config" to JSONObject().apply {
                            put("name", "${connection.name} $slotName")
                            put("unique_id", "${simUniqueId}_usage")
                            put("state_topic", "$fullBaseTopic/wan/$connId/sim/$slotId/usage_state")
                            put("json_attributes_topic", "$fullBaseTopic/wan/$connId/sim/$slotId/usage_attributes")
                            put("icon", "mdi:sim")
                            put("availability_topic", availabilityTopic)
                            put("payload_available", "online")
                            put("payload_not_available", "offline")
                            put("device", deviceInfo)
                        }.toString()
                    )
                }
            }

            // Bandwidth sensors (per-WAN, regular sensors not in diagnostic)
            payloads.add(
                "homeassistant/sensor/${uniqueId}_download_rate/config" to JSONObject().apply {
                    put("name", "${connection.name} Download Rate")
                    put("unique_id", "${uniqueId}_download_rate")
                    put("state_topic", "$fullBaseTopic/wan/$connId/bandwidth/download_mbps")
                    put("unit_of_measurement", "Mbit/s")
                    put("device_class", "data_rate")
                    put("icon", "mdi:download")
                    put("availability_topic", availabilityTopic)
                    put("payload_available", "online")
                    put("payload_not_available", "offline")
                    put("device", deviceInfo)
                }.toString()
            )

            payloads.add(
                "homeassistant/sensor/${uniqueId}_upload_rate/config" to JSONObject().apply {
                    put("name", "${connection.name} Upload Rate")
                    put("unique_id", "${uniqueId}_upload_rate")
                    put("state_topic", "$fullBaseTopic/wan/$connId/bandwidth/upload_mbps")
                    put("unit_of_measurement", "Mbit/s")
                    put("device_class", "data_rate")
                    put("icon", "mdi:upload")
                    put("availability_topic", availabilityTopic)
                    put("payload_available", "online")
                    put("payload_not_available", "offline")
                    put("device", deviceInfo)
                }.toString()
            )
        }

        // System diagnostic sensors
        run {
            val baseTopic = getMqttBaseTopic()
            val fullBaseTopic = "$topicPrefix/$baseTopic"

            // System temperature sensor
            payloads.add(
                "homeassistant/sensor/${instanceId}_system_temperature/config" to JSONObject().apply {
                    put("name", "System Temperature")
                    put("unique_id", "${instanceId}_system_temperature")
                    put("state_topic", "$fullBaseTopic/diagnostic/system/temperature")
                    put("availability_topic", "$fullBaseTopic/diagnostic/system/temperature/availability")
                    put("payload_available", "online")
                    put("payload_not_available", "offline")
                    put("unit_of_measurement", "°C")
                    put("device_class", "temperature")
                    put("icon", "mdi:thermometer")
                    put("entity_category", "diagnostic")
                    put("enabled_by_default", false)
                    put("device", deviceInfo)
                }.toString()
            )

            // Temperature threshold sensor
            payloads.add(
                "homeassistant/sensor/${instanceId}_temperature_threshold/config" to JSONObject().apply {
                    put("name", "Temperature Threshold")
                    put("unique_id", "${instanceId}_temperature_threshold")
                    put("state_topic", "$fullBaseTopic/diagnostic/system/temperature_threshold")
                    put("availability_topic", "$fullBaseTopic/diagnostic/system/temperature/availability")
                    put("payload_available", "online")
                    put("payload_not_available", "offline")
                    put("unit_of_measurement", "°C")
                    put("device_class", "temperature")
                    put("icon", "mdi:alert-thermometer")
                    put("entity_category", "diagnostic")
                    put("enabled_by_default", false)
                    put("device", deviceInfo)
                }.toString()
            )

            // Dynamic fan sensors
            payloads.add(
                "homeassistant/sensor/${instanceId}_fan_1_speed/config" to JSONObject().apply {
                    put("name", "Fan 1 Speed")
                    put("unique_id", "${instanceId}_fan_1_speed")
                    put("state_topic", "$fullBaseTopic/diagnostic/fan/1/speed_rpm")
                    put("availability_topic", "$fullBaseTopic/diagnostic/fan/1/availability")
                    put("payload_available", "online")
                    put("payload_not_available", "offline")
                    put("unit_of_measurement", "rpm")
                    put("icon", "mdi:fan")
                    put("entity_category", "diagnostic")
                    put("enabled_by_default", false)
                    put("device", deviceInfo)
                }.toString()
            )

            payloads.add(
                "homeassistant/sensor/${instanceId}_fan_1_status/config" to JSONObject().apply {
                    put("name", "Fan 1 Status")
                    put("unique_id", "${instanceId}_fan_1_status")
                    put("state_topic", "$fullBaseTopic/diagnostic/fan/1/status")
                    put("availability_topic", "$fullBaseTopic/diagnostic/fan/1/availability")
                    put("payload_available", "online")
                    put("payload_not_available", "offline")
                    put("icon", "mdi:fan-alert")
                    put("entity_category", "diagnostic")
                    put("enabled_by_default", false)
                    put("device", deviceInfo)
                }.toString()
            )

            // Add up to 3 fans (most routers have 1-3)
            for (fanId in 2..3) {
                payloads.add(
                    "homeassistant/sensor/${instanceId}_fan_${fanId}_speed/config" to JSONObject().apply {
                        put("name", "Fan $fanId Speed")
                        put("unique_id", "${instanceId}_fan_${fanId}_speed")
                        put("state_topic", "$fullBaseTopic/diagnostic/fan/$fanId/speed_rpm")
                        put("availability_topic", "$fullBaseTopic/diagnostic/fan/$fanId/availability")
                        put("payload_available", "online")
                        put("payload_not_available", "offline")
                        put("unit_of_measurement", "rpm")
                        put("icon", "mdi:fan")
                        put("entity_category", "diagnostic")
                        put("enabled_by_default", false)
                        put("device", deviceInfo)
                    }.toString()
                )

                payloads.add(
                    "homeassistant/sensor/${instanceId}_fan_${fanId}_status/config" to JSONObject().apply {
                        put("name", "Fan $fanId Status")
                        put("unique_id", "${instanceId}_fan_${fanId}_status")
                        put("state_topic", "$fullBaseTopic/diagnostic/fan/$fanId/status")
                        put("availability_topic", "$fullBaseTopic/diagnostic/fan/$fanId/availability")
                        put("payload_available", "online")
                        put("payload_not_available", "offline")
                        put("icon", "mdi:fan-alert")
                        put("entity_category", "diagnostic")
                        put("enabled_by_default", false)
                        put("device", deviceInfo)
                    }.toString()
                )
            }

            // Serial number sensor
            payloads.add(
                "homeassistant/sensor/${instanceId}_serial_number/config" to JSONObject().apply {
                    put("name", "Serial Number")
                    put("unique_id", "${instanceId}_serial_number")
                    put("state_topic", "$fullBaseTopic/diagnostic/device/serial_number")
                    put("availability_topic", "$fullBaseTopic/diagnostic/device/availability")
                    put("payload_available", "online")
                    put("payload_not_available", "offline")
                    put("icon", "mdi:numeric")
                    put("entity_category", "diagnostic")
                    put("device", deviceInfo)
                }.toString()
            )

            // Model sensor
            payloads.add(
                "homeassistant/sensor/${instanceId}_model/config" to JSONObject().apply {
                    put("name", "Model")
                    put("unique_id", "${instanceId}_model")
                    put("state_topic", "$fullBaseTopic/diagnostic/device/model")
                    put("availability_topic", "$fullBaseTopic/diagnostic/device/availability")
                    put("payload_available", "online")
                    put("payload_not_available", "offline")
                    put("icon", "mdi:router-wireless")
                    put("entity_category", "diagnostic")
                    put("device", deviceInfo)
                }.toString()
            )


        }

        Log.i(TAG, "[$instanceId] Generated ${payloads.size} discovery payloads")
        return payloads
    }

    // ===== COMMAND HANDLING =====

    /**
     * Serialized command handler that acquires a mutex lock to prevent concurrent API calls.
     * This ensures that rapid successive commands (e.g., priority 3→2→1) are processed
     * sequentially rather than in parallel, avoiding race conditions.
     */
    private suspend fun handleCommandSerialized(topic: String, payload: String): Result<Unit> {
        Log.d(TAG, "[$instanceId] Waiting for command lock (topic=$topic)")
        commandMutex.lock()
        Log.d(TAG, "[$instanceId] Acquired command lock, executing (topic=$topic)")
        return try {
            handleCommand(topic, payload)
        } finally {
            Log.d(TAG, "[$instanceId] Releasing command lock (topic=$topic)")
            commandMutex.unlock()
        }
    }

    override suspend fun handleCommand(topic: String, payload: String): Result<Unit> {
        Log.i(TAG, "[$instanceId] handleCommand called: topic=$topic, payload=$payload")

        return try {
            val baseTopic = getMqttBaseTopic()
            val topicPrefix = mqttPublisher.topicPrefix
            val normalizedTopic = if (topicPrefix.isNotBlank() && topic.startsWith("$topicPrefix/")) {
                topic.removePrefix("$topicPrefix/")
            } else topic

            Log.d(TAG, "[$instanceId] normalized topic: $normalizedTopic")

            // Ignore non-command state updates that flow through the same subscription
            val priorityRegex = Regex("$baseTopic/wan/(\\d+)/priority/set")
            val resetRegex = Regex("$baseTopic/wan/(\\d+)/cellular/reset")
            
            val isPriorityCommand = priorityRegex.matches(normalizedTopic)
            val isResetCommand = resetRegex.matches(normalizedTopic)
            Log.d(TAG, "[$instanceId] isPriority=$isPriorityCommand, isReset=$isResetCommand")
            
            if (!isPriorityCommand && !isResetCommand) {
                Log.d(TAG, "[$instanceId] Ignoring non-command topic")
                return Result.success(Unit)
            }


            when {
                isPriorityCommand -> {
                    Log.i(TAG, "[$instanceId] Processing priority command")
                    val connId = priorityRegex.find(normalizedTopic)?.groupValues?.get(1)?.toIntOrNull()
                        ?: return Result.failure(IllegalArgumentException("Invalid connId in topic"))
                    val normalized = payload.trim()
                    Log.d(TAG, "[$instanceId] Setting WAN $connId priority to: $normalized")
                    val result = if (normalized.equals("Disabled", ignoreCase = true)) {
                        Log.d(TAG, "[$instanceId] Calling setWanPriority($connId, null)")
                        apiClient.setWanPriority(connId, null)
                    } else {
                        val priority = normalized.toIntOrNull()
                            ?: return Result.failure(IllegalArgumentException("Invalid priority value: $normalized"))
                        Log.d(TAG, "[$instanceId] Calling setWanPriority($connId, $priority)")
                        apiClient.setWanPriority(connId, priority)
                    }
                    Log.d(TAG, "[$instanceId] setWanPriority result: ${result.isSuccess}")

                    // On success, immediately flip availability to reflect enable/disable intent
                    if (result.isSuccess) {
                        val isEnabled = !normalized.equals("Disabled", ignoreCase = true)
                        mqttPublisher.publishAvailability("$baseTopic/wan/$connId/availability", isEnabled)
                    }
                    result
                }

                // Cellular reset: peplink/main/wan/1/cellular/reset
                isResetCommand -> {
                    Log.i(TAG, "[$instanceId] Processing reset command")
                    val connId = resetRegex.find(normalizedTopic)?.groupValues?.get(1)?.toIntOrNull()
                        ?: return Result.failure(IllegalArgumentException("Invalid connId in topic"))
                    Log.d(TAG, "[$instanceId] Calling resetCellularModem($connId)")
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

        // Status: Use the raw message if available, otherwise map the enum
        val status = connection.message ?: when (connection.status) {
            ConnectionStatus.CONNECTED -> "connected"
            ConnectionStatus.DISCONNECTED -> "disconnected"
            ConnectionStatus.DISABLED -> "disabled"
            else -> "unknown"
        }
        mqttPublisher.publishState("$baseTopic/wan/$connId/status", status)

        // Status LED: Publish color indicator (e.g., "green", "red", "yellow")
        val statusLed = connection.statusLed ?: ""
        mqttPublisher.publishState("$baseTopic/wan/$connId/status_led", statusLed)

        // Priority (always publish, even if null)
        val priority = connection.priority?.toString() ?: ""
        mqttPublisher.publishState("$baseTopic/wan/$connId/priority", priority)

        // IP Address (always publish, even if null)
        val ip = connection.ip ?: ""
        mqttPublisher.publishState("$baseTopic/wan/$connId/ip", ip)

        // Uptime: Format seconds to days:hours:minutes
        val uptimeFormatted = if (connection.uptime != null) formatUptime(connection.uptime) else "0:00:00"
        mqttPublisher.publishState("$baseTopic/wan/$connId/uptime", uptimeFormatted)

        // Cellular-specific data
        connection.cellular?.let { cellular ->
            // Signal strength: format as "X/5" if it's a level (1-5), otherwise show dBm
            if (cellular.signalStrength != null) {
                val signalFormatted = if (cellular.signalStrength in 1..5) {
                    "${cellular.signalStrength}/5"
                } else {
                    "${cellular.signalStrength} dBm"
                }
                mqttPublisher.publishState("$baseTopic/wan/$connId/cellular/signal", signalFormatted)
            }
            // Raw dBm signal sensor: prefer top-level dBm, else rsrp, else rssi
            run {
                var dbmValue: Int? = null
                val s = cellular.signalStrength
                if (s != null && s !in 1..5) {
                    dbmValue = s
                }
                if (dbmValue == null) {
                    dbmValue = cellular.rsrpDbm ?: cellular.rssiDbm
                }
                mqttPublisher.publishState("$baseTopic/wan/$connId/cellular/signal_dbm", dbmValue?.toString() ?: "")
            }
            // Carrier: parse JSON if present, extract name property
            val carrierName = parseCarrierName(cellular.carrier)
            mqttPublisher.publishState("$baseTopic/wan/$connId/cellular/carrier", carrierName)
            // Network type
            mqttPublisher.publishState("$baseTopic/wan/$connId/cellular/network", cellular.networkType ?: "")
            // Carrier aggregation
            mqttPublisher.publishState("$baseTopic/wan/$connId/cellular/carrier_aggregation", cellular.carrierAggregation.toString())
            // Bands: Publish active bands or "unavailable" if none
            val bandsValue = if (cellular.bands.isNotEmpty()) {
                cellular.bands.joinToString(", ")
            } else {
                "unavailable"
            }
            mqttPublisher.publishState("$baseTopic/wan/$connId/cellular/bands", bandsValue)
        }

        // Usage and attributes
        lastWanUsage[connId]?.let { usage ->
            // For non-multi-SIM connections
            val hasMultipleSims = usage.simSlots?.isNotEmpty() == true
            if (!hasMultipleSims) {
                val isEnabled = usage.enabled
                mqttPublisher.publishState("$baseTopic/wan/$connId/usage_state", if (isEnabled) "Enabled" else "Disabled")

                // Attributes
                val percent = usage.percent ?: computeUsagePercent(usage.usage, usage.limit)
                val startOrdinal = formatOrdinalDay(parseStartDay(usage.startDate))
                val attributesJson = JSONObject().apply {
                    put("usage", if (usage.usage != null) "${formatUsageGb(usage.usage)} GB" else "0 GB")
                    put("allowance", if (usage.limit != null) "${formatUsageGb(usage.limit)} GB" else "unlimited")
                    put("percent_used", if (percent != null) "$percent%" else "0%")
                    put("start_day", startOrdinal ?: "unknown")
                }
                mqttPublisher.publishState("$baseTopic/wan/$connId/usage_attributes", attributesJson.toString())
            }
        }

        // Per-SIM publishing: Always loop through all slots
        // This ensures disabled/missing slots always show "Disabled" state
        Log.d(TAG, "[$instanceId] WAN $connId: simSlotCount=${connection.simSlotCount}, type=${connection.type}")
        if (connection.simSlotCount > 1) {
            Log.d(TAG, "[$instanceId] WAN $connId: Publishing per-SIM states (slotCount=${connection.simSlotCount})")
            for (slotId in 1..connection.simSlotCount) {
                val slot = lastWanUsage[connId]?.simSlots?.get(slotId)
                Log.d(TAG, "[$instanceId] WAN $connId SIM $slotId: slot=$slot")
                if (slot != null) {
                    mqttPublisher.publishState("$baseTopic/wan/$connId/sim/$slotId/usage_state", if (slot.enabled) "Enabled" else "Disabled")
                    if (slot.enabled && slot.hasUsageTracking) {
                        val simPercent = slot.percent ?: computeUsagePercent(slot.usage, slot.limit)
                        val simStartOrdinal = formatOrdinalDay(parseStartDay(slot.startDate))
                        val simAttributes = JSONObject().apply {
                            put("usage", if (slot.usage != null) "${formatUsageGb(slot.usage)} GB" else "0 GB")
                            put("allowance", if (slot.limit != null) "${formatUsageGb(slot.limit)} GB" else "unlimited")
                            put("percent_used", if (simPercent != null) "$simPercent%" else "0%")
                            put("start_day", simStartOrdinal ?: "unknown")
                        }
                        mqttPublisher.publishState("$baseTopic/wan/$connId/sim/$slotId/usage_attributes", simAttributes.toString())
                    }
                } else {
                    // Slot not in usage data - always publish Disabled
                    mqttPublisher.publishState("$baseTopic/wan/$connId/sim/$slotId/usage_state", "Disabled")
                }
            }
        }
    }

    private fun formatUptime(seconds: Int): String {
        val totalSeconds = seconds
        val days = totalSeconds / 86400
        val hours = (totalSeconds % 86400) / 3600
        val minutes = (totalSeconds % 3600) / 60
        return "$days:${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}"
    }

    private fun formatUsageGb(mb: Long): String {
        return String.format("%.2f", mb / 1024.0)
    }

    private fun computeUsagePercent(usageMb: Long?, limitMb: Long?): Int? {
        if (usageMb == null || limitMb == null || limitMb <= 0) return null
        return ((usageMb.toDouble() / limitMb.toDouble()) * 100.0).roundToInt()
    }

    private fun parseCarrierName(carrierJson: String?): String {
        if (carrierJson.isNullOrBlank()) return ""
        return try {
            val json = JSONObject(carrierJson)
            json.optString("name", carrierJson)
        } catch (e: Exception) {
            // If parsing fails, return the original string
            carrierJson
        }
    }

    private fun parseStartDay(start: String?): Int? {
        if (start.isNullOrBlank()) return null
        return start.toIntOrNull()?.takeIf { it in 1..31 }
    }

    private fun formatOrdinalDay(day: Int?): String? {
        if (day == null) return null
        if (day in 11..13) return "${day}th"
        return when (day % 10) {
            1 -> "${day}st"
            2 -> "${day}nd"
            3 -> "${day}rd"
            else -> "${day}th"
        }
    }

    /**
     * Map SIM slot ID to Peplink slot name.
     * Peplink routers use these slot names in their UI.
     */
    private fun getSimSlotName(slotId: Int): String {
        return when (slotId) {
            1 -> "SIM A"
            2 -> "SIM B"
            3 -> "RemoteSIM"
            4 -> "FusionSIM"
            5 -> "Peplink eSIM"
            else -> "SIM $slotId"
        }
    }
}
