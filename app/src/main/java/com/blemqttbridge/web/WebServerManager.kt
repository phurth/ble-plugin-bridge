package com.blemqttbridge.web

import android.content.Context
import android.util.Base64
import android.util.Log
import com.blemqttbridge.BuildConfig
import com.blemqttbridge.core.BaseBleService
import com.blemqttbridge.core.ConfigBackupManager
import com.blemqttbridge.core.MqttService
import com.blemqttbridge.core.PluginInstance
import com.blemqttbridge.core.PluginRegistry
import com.blemqttbridge.core.PollingPluginConfig
import com.blemqttbridge.core.ServiceStateManager
import com.blemqttbridge.core.interfaces.PluginConfig
import com.blemqttbridge.data.AppSettings
import com.blemqttbridge.util.ConfigValidator
import fi.iki.elonen.NanoHTTPD
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.runBlocking
import org.json.JSONArray
import org.json.JSONObject
import java.io.File
import java.security.MessageDigest

/**
 * Embedded web server for configuration and monitoring.
 * Provides REST API for viewing configuration, plugin status, and logs.
 * Can run independently of BLE service.
 */
class WebServerManager(
    private val context: Context,
    private val port: Int = 8088
) : NanoHTTPD(port) {

    // Get service dynamically instead of storing reference
    private fun getService(): BaseBleService? = BaseBleService.getInstance()
    
    // Get MQTT service dynamically
    private fun getMqttService(): MqttService? = MqttService.getInstance()

    companion object {
        private const val TAG = "WebServerManager"
        private const val SERVICE_RESTART_DELAY_MS = 500L  // Delay for service restart
    }
    
    private val appSettings = AppSettings(context)

    init {
        Log.i(TAG, "Web server initialized on port $port")
    }
    
    /**
     * Check HTTP Basic Authentication.
     * Returns true if authentication is disabled or credentials are valid.
     */
    private fun requireAuth(session: IHTTPSession): Boolean {
        val authEnabled = runBlocking { appSettings.webAuthEnabled.first() }
        if (!authEnabled) return true
        
        val authHeader = session.headers["authorization"]
        if (authHeader == null || !authHeader.startsWith("Basic ")) {
            return false
        }
        
        try {
            val base64Credentials = authHeader.substring(6)
            val credentials = String(Base64.decode(base64Credentials, Base64.DEFAULT))
            val parts = credentials.split(":", limit = 2)
            if (parts.size != 2) return false
            
            val (username, password) = parts
            val storedUsername = runBlocking { appSettings.webAuthUsername.first() }
            val storedPasswordHash = runBlocking { appSettings.webAuthPassword.first() }
            
            // Simple hash comparison (in production, use BCrypt or similar)
            val providedPasswordHash = hashPassword(password)
            
            return username == storedUsername && providedPasswordHash == storedPasswordHash
        } catch (e: Exception) {
            Log.e(TAG, "Auth error", e)
            return false
        }
    }
    
    /**
     * Send 401 Unauthorized response
     */
    private fun sendUnauthorized(): Response {
        val response = newFixedLengthResponse(
            Response.Status.UNAUTHORIZED,
            "application/json",
            """{"error":"Authentication required"}"""
        )
        response.addHeader("WWW-Authenticate", "Basic realm=\"BLE MQTT Bridge\"")
        return response
    }
    
    /**
     * Hash password using SHA-256 (basic implementation)
     * In production, use BCrypt or similar
     */
    private fun hashPassword(password: String): String {
        val bytes = MessageDigest.getInstance("SHA-256").digest(password.toByteArray())
        return bytes.joinToString("") { "%02x".format(it) }
    }

    override fun serve(session: IHTTPSession): Response {
        val uri = session.uri
        val method = session.method

        Log.d(TAG, "$method $uri from ${session.remoteIpAddress}")
        
        // Check authentication for all endpoints
        if (!requireAuth(session)) {
            return sendUnauthorized()
        }

        return try {
            when {
                uri == "/" || uri == "/index.html" -> serveIndexPage()
                uri == "/web_ui.html" -> serveAssetHtml("web_ui")
                uri == "/web_ui.css" -> serveAssetCss("web_ui")
                uri == "/web_ui.js" -> serveAssetJs("web_ui")
                uri == "/api/status" -> serveStatus()
                uri == "/api/config" -> serveConfig()
                uri == "/api/plugins" -> servePlugins()
                uri == "/api/instances" && method == Method.GET -> serveInstances()
                uri == "/api/instances/add" && method == Method.POST -> handleInstanceAdd(session)
                uri == "/api/instances/remove" && method == Method.POST -> handleInstanceRemove(session)
                uri == "/api/instances/update" && method == Method.POST -> handleInstanceUpdate(session)
                uri == "/api/logs/debug" -> serveDebugLog()
                uri == "/api/logs/ble" -> serveBleTrace()
                uri == "/api/control/service" && method == Method.POST -> handleServiceControl(session)
                uri == "/api/control/mqtt" && method == Method.POST -> handleMqttControl(session)
                uri == "/api/config/plugin" && method == Method.POST -> handlePluginConfig(session)
                uri == "/api/config/mqtt" && method == Method.POST -> handleMqttConfig(session)
                uri == "/api/plugins/add" && method == Method.POST -> handlePluginAdd(session)
                uri == "/api/plugins/remove" && method == Method.POST -> handlePluginRemove(session)
                uri == "/api/config/export" && method == Method.GET -> handleConfigExport()
                uri == "/api/config/import" && method == Method.POST -> handleConfigImport(session)
                uri == "/api/polling/instances" && method == Method.GET -> servePollingInstances()
                uri == "/api/polling/instances/add" && method == Method.POST -> handlePollingInstanceAdd(session)
                uri == "/api/polling/instances/remove" && method == Method.POST -> handlePollingInstanceRemove(session)
                uri == "/api/polling/instances/start" && method == Method.POST -> handlePollingInstanceStart(session)
                uri == "/api/polling/instances/stop" && method == Method.POST -> handlePollingInstanceStop(session)
                uri == "/api/polling/status" && method == Method.GET -> servePollingStatus()
                uri == "/api/polling/control/start" && method == Method.POST -> handlePollingControlStart()
                uri == "/api/polling/control/stop" && method == Method.POST -> handlePollingControlStop()
                uri.startsWith("/api/") -> newFixedLengthResponse(
                    Response.Status.NOT_FOUND,
                    "application/json",
                    """{"error":"Endpoint not found"}"""
                )
                else -> newFixedLengthResponse(
                    Response.Status.NOT_FOUND,
                    "text/html",
                    "<html><body><h1>404 Not Found</h1></body></html>"
                )
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error serving request: $uri", e)
            newFixedLengthResponse(
                Response.Status.INTERNAL_ERROR,
                "application/json",
                """{"error":"${e.message}"}"""
            )
        }
    }

    /**
     * Load a raw resource file from res/raw directory
     */
    private fun loadRawResource(resourceId: Int): String {
        return try {
            context.resources.openRawResource(resourceId).bufferedReader().use { it.readText() }
        } catch (e: Exception) {
            Log.e(TAG, "Failed to load raw resource", e)
            ""
        }
    }

    /**
     * Serve index page with version information injected
     */
    private fun serveIndexPage(): Response {
        try {
            val resourceId = context.resources.getIdentifier(
                "web_ui_html",
                "raw",
                context.packageName
            )
            
            if (resourceId == 0) {
                Log.e(TAG, "web_ui_html resource not found")
                return newFixedLengthResponse(
                    Response.Status.INTERNAL_ERROR,
                    "text/html",
                    "<html><body><h1>Error: UI resources not found</h1></body></html>"
                )
            }
            
            var html = loadRawResource(resourceId)
            
            // Inject version information into the span contents
            html = html.replace(
                "<span id=\"app-version\">Loading...</span>",
                "<span id=\"app-version\">v${BuildConfig.VERSION_NAME}</span>"
            )
            html = html.replace(
                "<span id=\"app-code\">Loading...</span>",
                "<span id=\"app-code\">${BuildConfig.VERSION_CODE}</span>"
            )
            
            return newFixedLengthResponse(Response.Status.OK, "text/html", html)
        } catch (e: Exception) {
            Log.e(TAG, "Error serving index page", e)
            return newFixedLengthResponse(
                Response.Status.INTERNAL_ERROR,
                "text/html",
                "<html><body><h1>Error loading UI</h1><p>${e.message}</p></body></html>"
            )
        }
    }

    /**
     * Serve HTML asset file
     */
    private fun serveAssetHtml(assetName: String): Response {
        return try {
            val resourceId = context.resources.getIdentifier(
                "${assetName}_html",
                "raw",
                context.packageName
            )
            
            if (resourceId == 0) {
                return newFixedLengthResponse(Response.Status.NOT_FOUND, "text/html", "")
            }
            
            val content = loadRawResource(resourceId)
            newFixedLengthResponse(Response.Status.OK, "text/html", content)
        } catch (e: Exception) {
            Log.e(TAG, "Error serving HTML asset: $assetName", e)
            newFixedLengthResponse(Response.Status.INTERNAL_ERROR, "text/html", "")
        }
    }

    /**
     * Serve CSS asset file
     */
    private fun serveAssetCss(assetName: String): Response {
        return try {
            val resourceId = context.resources.getIdentifier(
                "${assetName}_css",
                "raw",
                context.packageName
            )
            
            if (resourceId == 0) {
                return newFixedLengthResponse(Response.Status.NOT_FOUND, "text/css", "")
            }
            
            val content = loadRawResource(resourceId)
            newFixedLengthResponse(Response.Status.OK, "text/css", content)
        } catch (e: Exception) {
            Log.e(TAG, "Error serving CSS asset: $assetName", e)
            newFixedLengthResponse(Response.Status.INTERNAL_ERROR, "text/css", "")
        }
    }

    /**
     * Serve JavaScript asset file
     */
    private fun serveAssetJs(assetName: String): Response {
        return try {
            val resourceId = context.resources.getIdentifier(
                "${assetName}_js",
                "raw",
                context.packageName
            )
            
            if (resourceId == 0) {
                return newFixedLengthResponse(Response.Status.NOT_FOUND, "application/javascript", "")
            }
            
            val content = loadRawResource(resourceId)
            newFixedLengthResponse(Response.Status.OK, "application/javascript", content)
        } catch (e: Exception) {
            Log.e(TAG, "Error serving JS asset: $assetName", e)
            newFixedLengthResponse(Response.Status.INTERNAL_ERROR, "application/javascript", "")
        }
    }



    private fun serveStatus(): Response {
        val settings = AppSettings(context)
        val mqttEnabled = runBlocking { settings.mqttEnabled.first() }
        val running = BaseBleService.serviceRunning.value
        val json = JSONObject().apply {
            // Running reflects actual BLE service state
            put("running", running)
            // Expose BLE enabled reflecting actual service state for UI toggle consistency
            put("bleEnabled", running)
            // MQTT enabled is the setting; connection status from MqttService
            put("mqttEnabled", mqttEnabled)
            put("mqttConnected", com.blemqttbridge.core.MqttService.connectionStatus.value)
            put("bleTraceActive", getService()?.isBleTraceActive() ?: false)
            // BLE health indicators
            put("bluetoothAvailable", BaseBleService.bluetoothAvailable.value)
            put("bleScanningActive", BaseBleService.bleScanningActive.value)
            
            // Get BLE plugin statuses
            val bleStatuses = BaseBleService.pluginStatuses.value
            val bleStatusesJson = JSONObject()
            for ((pluginId, status) in bleStatuses) {
                bleStatusesJson.put(pluginId, JSONObject().apply {
                    put("connected", status.connected)
                    put("authenticated", status.authenticated)
                    put("dataHealthy", status.dataHealthy)
                })
            }
            
            // Get polling (HTTP) plugin statuses separately
            val pollingStatuses = BaseBleService.pollingPluginStatuses.value
            val pollingStatusesJson = JSONObject()
            for ((pluginId, status) in pollingStatuses) {
                pollingStatusesJson.put(pluginId, JSONObject().apply {
                    put("connected", status.connected)
                    put("authenticated", status.authenticated)
                    put("dataHealthy", status.dataHealthy)
                })
            }
            
            put("pluginStatuses", bleStatusesJson)
            put("pollingPluginStatuses", pollingStatusesJson)
        }
        return newFixedLengthResponse(Response.Status.OK, "application/json", json.toString())
    }

    private fun serveConfig(): Response = runBlocking {
        val settings = AppSettings(context)
        val json = JSONObject().apply {
            put("mqttBroker", settings.mqttBrokerHost.first())
            put("mqttPort", settings.mqttBrokerPort.first())
            put("mqttUsername", settings.mqttUsername.first())
            put("mqttPassword", settings.mqttPassword.first())
            put("mqttTopicPrefix", settings.mqttTopicPrefix.first())
            
            val enabledPlugins = JSONArray()
            if (settings.oneControlEnabled.first()) enabledPlugins.put("onecontrol")
            if (settings.easyTouchEnabled.first()) enabledPlugins.put("easytouch")
            if (settings.goPowerEnabled.first()) enabledPlugins.put("gopower")
            put("enabledPlugins", enabledPlugins)
            
            // Add configured MAC addresses
            val oneControlMacs = JSONArray()
            val oneControlMac = settings.oneControlGatewayMac.first()
            if (oneControlMac.isNotBlank()) oneControlMacs.put(oneControlMac)
            put("oneControlMacs", oneControlMacs)
            
            val easyTouchMacs = JSONArray()
            val easyTouchMac = settings.easyTouchThermostatMac.first()
            if (easyTouchMac.isNotBlank()) easyTouchMacs.put(easyTouchMac)
            put("easyTouchMacs", easyTouchMacs)
            
            val goPowerMacs = JSONArray()
            val goPowerMac = settings.goPowerControllerMac.first()
            if (goPowerMac.isNotBlank()) goPowerMacs.put(goPowerMac)
            put("goPowerMacs", goPowerMacs)
        }
        newFixedLengthResponse(Response.Status.OK, "application/json", json.toString())
    }

    private fun servePlugins(): Response = runBlocking {
        val settings = AppSettings(context)
        val statuses = BaseBleService.pluginStatuses.value
        val json = JSONObject()
        
        // OneControl
        if (statuses.containsKey("onecontrol") || settings.oneControlEnabled.first()) {
            val status = statuses["onecontrol"] ?: BaseBleService.Companion.PluginStatus("onecontrol")
            val oneControlEnabled = settings.oneControlEnabled.first()
            json.put("onecontrol", JSONObject().apply {
                put("enabled", oneControlEnabled as Any)
                put("macAddresses", JSONArray().apply {
                    val mac = settings.oneControlGatewayMac.first()
                    if (mac.isNotBlank()) put(mac)
                })
                put("gatewayPin", settings.oneControlGatewayPin.first())
                put("bluetoothPin", settings.oneControlBluetoothPin.first())
                put("connected", status.connected)
                put("authenticated", status.authenticated)
                put("dataHealthy", status.dataHealthy)
            })
        }
        
        // EasyTouch
        if (statuses.containsKey("easytouch") || settings.easyTouchEnabled.first()) {
            val status = statuses["easytouch"] ?: BaseBleService.Companion.PluginStatus("easytouch")
            val easyTouchEnabled = settings.easyTouchEnabled.first()
            json.put("easytouch", JSONObject().apply {
                put("enabled", easyTouchEnabled as Any)
                put("macAddresses", JSONArray().apply {
                    val mac = settings.easyTouchThermostatMac.first()
                    if (mac.isNotBlank()) put(mac)
                })
                put("password", settings.easyTouchThermostatPassword.first())
                put("connected", status.connected)
                put("authenticated", status.authenticated)
                put("dataHealthy", status.dataHealthy)
            })
        }
        
        // GoPower
        if (statuses.containsKey("gopower") || settings.goPowerEnabled.first()) {
            val status = statuses["gopower"] ?: BaseBleService.Companion.PluginStatus("gopower")
            val goPowerEnabled = settings.goPowerEnabled.first()
            json.put("gopower", JSONObject().apply {
                put("enabled", goPowerEnabled as Any)
                put("macAddresses", JSONArray().apply {
                    val mac = settings.goPowerControllerMac.first()
                    if (mac.isNotBlank()) put(mac)
                })
                put("connected", status.connected)
                put("authenticated", status.authenticated)
                put("dataHealthy", status.dataHealthy)
            })
        }
        
        // Mopeka (multi-instance support)
        val mopekaInstances = ServiceStateManager.getInstancesOfType(context, "mopeka")
        for (instance in mopekaInstances) {
            val instanceStatus = statuses[instance.instanceId] ?: BaseBleService.Companion.PluginStatus(instance.instanceId)
            val mac = instance.deviceMac
            json.put(instance.instanceId, JSONObject().apply {
                put("enabled", true)  // Instance exists = enabled
                put("displayName", instance.displayName)
                put("macAddresses", JSONArray().apply {
                    if (mac.isNotBlank()) put(mac)
                })
                put("mediumType", instance.config["mediumType"] ?: "propane")
                put("tankType", instance.config["tankType"] ?: "20lb_v")
                put("connected", instanceStatus.connected)
                put("authenticated", instanceStatus.authenticated)
                put("dataHealthy", instanceStatus.dataHealthy)
            })
        }
        
        // BLE Scanner
        if (statuses.containsKey("blescanner") || settings.bleScannerEnabled.first()) {
            val status = statuses["blescanner"] ?: BaseBleService.Companion.PluginStatus("blescanner")
            val bleScannerEnabled = settings.bleScannerEnabled.first()
            json.put("blescanner", JSONObject().apply {
                put("enabled", bleScannerEnabled as Any)
                put("macAddresses", JSONArray())  // BLE Scanner doesn't have a configured MAC
                put("connected", status.connected)
                put("authenticated", status.authenticated)
                put("dataHealthy", status.dataHealthy)
            })
        }
        
        newFixedLengthResponse(Response.Status.OK, "application/json", json.toString())
    }

    private fun serveDebugLog(): Response {
        val logText = getService()?.exportDebugLogToString() ?: "Service not running"
        return newFixedLengthResponse(
            Response.Status.OK,
            "text/plain; charset=utf-8",
            logText
        )
    }

    private fun serveBleTrace(): Response {
        val traceText = getService()?.exportBleTraceToString() ?: "Service not running"
        return newFixedLengthResponse(
            Response.Status.OK,
            "text/plain; charset=utf-8",
            traceText
        )
    }

    private fun handleServiceControl(session: IHTTPSession): Response {
        return try {
            // Parse request body
            val files = HashMap<String, String>()
            session.parseBody(files)
            val body = files["postData"] ?: return newFixedLengthResponse(
                Response.Status.BAD_REQUEST,
                "application/json",
                """{"success":false,"error":"No request body"}"""
            )
            
            val json = JSONObject(body)
            val enable = json.getBoolean("enable")
            
            // Update AppSettings for BLE service
            runBlocking {
                val settings = AppSettings(context)
                settings.setBleEnabled(enable)
            }
            
            if (enable) {
                // Start service
                val intent = android.content.Intent(context, BaseBleService::class.java).apply {
                    action = BaseBleService.ACTION_START_SCAN
                }
                context.startForegroundService(intent)
                Log.i(TAG, "Service start requested via web interface")
            } else {
                // Stop service - schedule on a background thread to avoid blocking
                Thread {
                    Thread.sleep(100) // Small delay to send response first
                    val intent = android.content.Intent(context, BaseBleService::class.java).apply {
                        action = BaseBleService.ACTION_STOP_SERVICE
                    }
                    context.startService(intent)
                    Log.i(TAG, "Service stop requested via web interface")
                }.start()
            }
            
            newFixedLengthResponse(
                Response.Status.OK,
                "application/json",
                """{"success":true}"""
            )
        } catch (e: Exception) {
            Log.e(TAG, "Error handling service control", e)
            newFixedLengthResponse(
                Response.Status.INTERNAL_ERROR,
                "application/json",
                """{"success":false,"error":"${e.message}"}"""
            )
        }
    }

    private fun handleMqttControl(session: IHTTPSession): Response {
        return try {
            // Parse request body
            val files = HashMap<String, String>()
            session.parseBody(files)
            val body = files["postData"] ?: return newFixedLengthResponse(
                Response.Status.BAD_REQUEST,
                "application/json",
                """{"success":false,"error":"No request body"}"""
            )
            
            val json = JSONObject(body)
            val enable = json.getBoolean("enable")
            
            // Update AppSettings for MQTT service
            runBlocking {
                val settings = AppSettings(context)
                settings.setMqttEnabled(enable)
            }
            
            if (enable) {
                // Start MqttService directly (no BLE service restart needed)
                val intent = android.content.Intent(context, MqttService::class.java)
                context.startForegroundService(intent)
                Log.i(TAG, "MQTT service start requested via web interface")
            } else {
                // Stop MqttService directly
                Thread {
                    Thread.sleep(100) // Small delay to send response first
                    val intent = android.content.Intent(context, MqttService::class.java)
                    context.stopService(intent)
                    Log.i(TAG, "MQTT service stop requested via web interface")
                }.start()
            }
            
            newFixedLengthResponse(
                Response.Status.OK,
                "application/json",
                """{"success":true}"""
            )
        } catch (e: Exception) {
            Log.e(TAG, "Error handling MQTT control", e)
            newFixedLengthResponse(
                Response.Status.INTERNAL_ERROR,
                "application/json",
                """{"success":false,"error":"${e.message}"}"""
            )
        }
    }

    /**
     * Check if a plugin can be edited based on service state.
     * BLE plugins can only be edited when BLE service is OFF (bleEnabled=false)
     * HTTP Polling plugins can only be edited when Polling is OFF (no running instances)
     * MQTT can only be edited when MQTT service is OFF (mqttEnabled=false)
     */
    private fun canEditPlugin(pluginId: String): Pair<Boolean, String> {
        return when {
            // BLE plugins - check bleEnabled setting only (BLE service independent from overall service)
            pluginId in listOf("onecontrol", "easytouch", "gopower", "blescanner") -> {
                val bleEnabled = runBlocking { appSettings.bleEnabled.first() }
                if (bleEnabled) {
                    Pair(false, "BLE service must be stopped before editing $pluginId configuration")
                } else {
                    Pair(true, "")
                }
            }
            // HTTP Polling plugins - check if any polling is running
            pluginId in listOf("mopeka", "peplink") -> {
                val registry = PluginRegistry.getInstance()
                val runningInstances = registry.getAllPollingPluginInstances()
                if (runningInstances.isNotEmpty()) {
                    Pair(false, "HTTP Polling service must be stopped before editing $pluginId configuration")
                } else {
                    Pair(true, "")
                }
            }
            // MQTT plugin - check if MQTT is enabled
            pluginId == "mqtt" -> {
                runBlocking {
                    val mqttEnabled = appSettings.mqttEnabled.first()
                    if (mqttEnabled) {
                        Pair(false, "MQTT service must be stopped before editing configuration")
                    } else {
                        Pair(true, "")
                    }
                }
            }
            else -> Pair(false, "Unknown plugin: $pluginId")
        }
    }

    private fun handleMqttConfig(session: IHTTPSession): Response {
        return try {
            // Parse request body
            val files = HashMap<String, String>()
            session.parseBody(files)
            val body = files["postData"] ?: return newFixedLengthResponse(
                Response.Status.BAD_REQUEST,
                "application/json",
                """{"success":false,"error":"No request body"}"""
            )

            val json = JSONObject(body)
            val field = json.getString("field")
            val value = json.getString("value")

            // Verify MQTT service is stopped using the new helper
            val (canEdit, errorMsg) = canEditPlugin("mqtt")
            if (!canEdit) {
                return newFixedLengthResponse(
                    Response.Status.FORBIDDEN,
                    "application/json",
                    """{"success":false,"error":"$errorMsg"}"""
                )
            }

            // Validate input based on field type
            val (isValid, errorMessage) = when (field) {
                "broker" -> ConfigValidator.validateBrokerHost(value)
                "port" -> ConfigValidator.validatePort(value)
                "topicPrefix" -> ConfigValidator.validateTopicPrefix(value)
                "username" -> ConfigValidator.validateUsername(value)
                "password" -> ConfigValidator.validatePassword(value)
                else -> Pair(false, "Unknown field: $field")
            }
            
            if (!isValid) {
                Log.w(TAG, "MQTT config validation failed for $field: $errorMessage")
                return newFixedLengthResponse(
                    Response.Status.BAD_REQUEST,
                    "application/json",
                    """{"success":false,"error":"$errorMessage"}"""
                )
            }

            // Update the appropriate MQTT setting
            val settings = AppSettings(context)
            runBlocking {
                when (field) {
                    "broker" -> settings.setMqttBrokerHost(value)
                    "port" -> settings.setMqttBrokerPort(value.toIntOrNull() ?: 1883)
                    "topicPrefix" -> settings.setMqttTopicPrefix(value)
                    "username" -> settings.setMqttUsername(value)
                    "password" -> settings.setMqttPassword(value)
                    else -> Unit  // Add else for exhaustive when
                }
            }

            Log.i(TAG, "MQTT config updated via web UI: $field")
            newFixedLengthResponse(
                Response.Status.OK,
                "application/json",
                """{"success":true}"""
            )
        } catch (e: Exception) {
            Log.e(TAG, "Error updating MQTT config", e)
            newFixedLengthResponse(
                Response.Status.INTERNAL_ERROR,
                "application/json",
                """{"success":false,"error":"${e.message}"}"""
            )
        }
    }

    private fun handlePluginConfig(session: IHTTPSession): Response {
        return try {
            // Parse request body
            val files = HashMap<String, String>()
            session.parseBody(files)
            val body = files["postData"] ?: return newFixedLengthResponse(
                Response.Status.BAD_REQUEST,
                "application/json",
                """{"success":false,"error":"No request body"}"""
            )
            
            val json = JSONObject(body)
            val pluginId = json.getString("pluginId")
            val field = json.getString("field")
            val value = json.getString("value")
            
            // Verify the appropriate service is stopped using the helper
            val (canEdit, errorMsg) = canEditPlugin(pluginId)
            if (!canEdit) {
                return newFixedLengthResponse(
                    Response.Status.BAD_REQUEST,
                    "application/json",
                    """{"success":false,"error":"$errorMsg"}"""
                )
            }
            
            // Update settings based on plugin and field
            runBlocking {
                val settings = AppSettings(context)
                when (pluginId) {
                    "onecontrol" -> when (field) {
                        "macAddress" -> settings.setOneControlGatewayMac(value)
                        "gatewayPin" -> settings.setOneControlGatewayPin(value)
                        else -> return@runBlocking
                    }
                    "easytouch" -> when (field) {
                        "macAddress" -> settings.setEasyTouchThermostatMac(value)
                        "password" -> settings.setEasyTouchThermostatPassword(value)
                        else -> return@runBlocking
                    }
                    "gopower" -> when (field) {
                        "macAddress" -> settings.setGoPowerControllerMac(value)
                        else -> return@runBlocking
                    }
                }
            }
            
            Log.i(TAG, "Updated $pluginId config: $field")
            
            newFixedLengthResponse(
                Response.Status.OK,
                "application/json",
                """{"success":true}"""
            )
        } catch (e: Exception) {
            Log.e(TAG, "Error handling plugin config", e)
            newFixedLengthResponse(
                Response.Status.INTERNAL_ERROR,
                "application/json",
                """{"success":false,"error":"${e.message}"}"""
            )
        }
    }

    private fun handlePluginAdd(session: IHTTPSession): Response {
        return try {
            // Parse request body
            val files = mutableMapOf<String, String>()
            session.parseBody(files)
            val body = files["postData"] ?: return newFixedLengthResponse(
                Response.Status.BAD_REQUEST,
                "application/json",
                """{"success":false,"error":"No request body"}"""
            )

            val jsonObject = JSONObject(body)
            val plugin = jsonObject.getString("plugin")

            // Verify the appropriate service is stopped using the helper
            val (canEdit, errorMsg) = canEditPlugin(plugin)
            if (!canEdit) {
                return newFixedLengthResponse(
                    Response.Status.BAD_REQUEST,
                    "application/json",
                    """{"success":false,"error":"$errorMsg"}"""
                )
            }

            // Add plugin by setting enabled=true in both AppSettings and ServiceStateManager
            val settings = AppSettings(context)
            runBlocking {
                when (plugin) {
                    "onecontrol" -> {
                        settings.setOneControlEnabled(true)
                        ServiceStateManager.enableBlePlugin(context, "onecontrol")
                    }
                    "easytouch" -> {
                        settings.setEasyTouchEnabled(true)
                        ServiceStateManager.enableBlePlugin(context, "easytouch")
                    }
                    "gopower" -> {
                        settings.setGoPowerEnabled(true)
                        ServiceStateManager.enableBlePlugin(context, "gopower")
                    }
                    "blescanner" -> {
                        settings.setBleScannerEnabled(true)
                        ServiceStateManager.enableBlePlugin(context, "blescanner")
                    }
                    else -> return@runBlocking newFixedLengthResponse(
                        Response.Status.BAD_REQUEST,
                        "application/json",
                        """{"success":false,"error":"Unknown plugin: $plugin"}"""
                    )
                }
            }

            Log.i(TAG, "Plugin added via web UI: $plugin")
            newFixedLengthResponse(
                Response.Status.OK,
                "application/json",
                """{"success":true}"""
            )
        } catch (e: Exception) {
            Log.e(TAG, "Error adding plugin", e)
            newFixedLengthResponse(
                Response.Status.INTERNAL_ERROR,
                "application/json",
                """{"success":false,"error":"${e.message}"}"""
            )
        }
    }

    private fun handlePluginRemove(session: IHTTPSession): Response {
        return try {
            // Parse request body
            val files = mutableMapOf<String, String>()
            session.parseBody(files)
            val body = files["postData"] ?: return newFixedLengthResponse(
                Response.Status.BAD_REQUEST,
                "application/json",
                """{"success":false,"error":"No request body"}"""
            )

            val jsonObject = JSONObject(body)
            val plugin = jsonObject.getString("plugin")

            // Verify the appropriate service is stopped using the helper
            val (canEdit, errorMsg) = canEditPlugin(plugin)
            if (!canEdit) {
                return newFixedLengthResponse(
                    Response.Status.BAD_REQUEST,
                    "application/json",
                    """{"success":false,"error":"$errorMsg"}"""
                )
            }

            // Remove plugin by setting enabled=false in both AppSettings and ServiceStateManager
            val settings = AppSettings(context)
            runBlocking {
                when (plugin) {
                    "onecontrol" -> {
                        settings.setOneControlEnabled(false)
                        ServiceStateManager.disableBlePlugin(context, "onecontrol")
                    }
                    "easytouch" -> {
                        settings.setEasyTouchEnabled(false)
                        ServiceStateManager.disableBlePlugin(context, "easytouch")
                    }
                    "gopower" -> {
                        settings.setGoPowerEnabled(false)
                        ServiceStateManager.disableBlePlugin(context, "gopower")
                    }
                    "blescanner" -> {
                        settings.setBleScannerEnabled(false)
                        ServiceStateManager.disableBlePlugin(context, "blescanner")
                    }
                    else -> return@runBlocking newFixedLengthResponse(
                        Response.Status.BAD_REQUEST,
                        "application/json",
                        """{"success":false,"error":"Unknown plugin: $plugin"}"""
                    )
                }
            }

            Log.i(TAG, "Plugin removed via web UI: $plugin")
            newFixedLengthResponse(
                Response.Status.OK,
                "application/json",
                """{"success":true}"""
            )
        } catch (e: Exception) {
            Log.e(TAG, "Error removing plugin", e)
            newFixedLengthResponse(
                Response.Status.INTERNAL_ERROR,
                "application/json",
                """{"success":false,"error":"${e.message}"}"""
            )
        }
    }

    /**
     * Phase 5: Serve all plugin instances as JSON.
     * Groups instances by plugin type for grouped UI display.
     */
    private fun serveInstances(): Response = runBlocking {
        return@runBlocking try {
            val allInstances = ServiceStateManager.getAllInstances(context)
            val jsonArray = JSONArray()
            
            // Build flat array of all instances
            for ((instanceId, instance) in allInstances) {
                val status = BaseBleService.pluginStatuses.value[instanceId] 
                    ?: BaseBleService.Companion.PluginStatus(instanceId, false, false, false)
                
                val instanceJson = JSONObject().apply {
                    put("instanceId", instanceId)
                    put("pluginType", instance.pluginType)
                    put("deviceMac", instance.deviceMac)
                    put("displayName", instance.displayName ?: instanceId)
                    put("connected", status.connected)
                    put("authenticated", status.authenticated)
                    put("dataHealthy", status.dataHealthy)
                    // Add config fields as JSON object
                    put("config", JSONObject(instance.config))
                }
                
                jsonArray.put(instanceJson)
            }
            
            newFixedLengthResponse(Response.Status.OK, "application/json", jsonArray.toString())
        } catch (e: Exception) {
            Log.e(TAG, "Error serving instances", e)
            newFixedLengthResponse(
                Response.Status.INTERNAL_ERROR,
                "application/json",
                """{"error":"${e.message}"}"""
            )
        }
    }

    /**
     * Phase 5: Add a new plugin instance.
     * Creates a new PluginInstance and saves it to ServiceStateManager.
     */
    private fun handleInstanceAdd(session: IHTTPSession): Response {
        return try {
            // Parse request body
            val files = mutableMapOf<String, String>()
            session.parseBody(files)
            val body = files["postData"] ?: return newFixedLengthResponse(
                Response.Status.BAD_REQUEST,
                "application/json",
                """{"success":false,"error":"No request body"}"""
            )

            val jsonObject = JSONObject(body)
            val pluginType = jsonObject.getString("pluginType")
            val deviceMac = jsonObject.getString("deviceMac")
            val displayName = jsonObject.optString("displayName", null)
            val config = jsonObject.optJSONObject("config")?.let { 
                val map = mutableMapOf<String, String>()
                it.keys().forEach { key -> map[key] = it.getString(key) }
                map
            } ?: mutableMapOf()

            // Validate service is stopped
            val service = BaseBleService.getInstance()
            if (service != null && runBlocking { BaseBleService.serviceRunning.first() }) {
                return newFixedLengthResponse(
                    Response.Status.BAD_REQUEST,
                    "application/json",
                    """{"success":false,"error":"Service must be stopped to add instances"}"""
                )
            }

            // Validate plugin type
            val validTypes = setOf("onecontrol", "easytouch", "gopower", "mopeka", "hughes_watchdog", "blescanner")
            if (!validTypes.contains(pluginType)) {
                return newFixedLengthResponse(
                    Response.Status.BAD_REQUEST,
                    "application/json",
                    """{"success":false,"error":"Invalid plugin type: $pluginType"}"""
                )
            }

            // Create instance
            val instanceId = PluginInstance.createInstanceId(pluginType, deviceMac)
            val instance = PluginInstance(
                instanceId = instanceId,
                pluginType = pluginType,
                deviceMac = deviceMac,
                displayName = displayName,
                config = config
            )

            // Save to ServiceStateManager
            ServiceStateManager.saveInstance(context, instance)
            // NOTE: Do NOT call enableBlePlugin() here - instances are loaded via getAllInstances(),
            // not the legacy enabled_ble_plugins set. Calling enableBlePlugin would add the instanceId
            // to the legacy set, causing the service to try loading it as both an instance AND a legacy plugin.
            Log.i(TAG, "Instance added via web UI: $instanceId (${instance.pluginType})")
            
            newFixedLengthResponse(
                Response.Status.OK,
                "application/json",
                """{"success":true,"instanceId":"$instanceId"}"""
            )
        } catch (e: Exception) {
            Log.e(TAG, "Error adding instance", e)
            newFixedLengthResponse(
                Response.Status.INTERNAL_ERROR,
                "application/json",
                """{"success":false,"error":"${e.message}"}"""
            )
        }
    }

    /**
     * Phase 5: Remove a plugin instance.
     * Deletes the instance from ServiceStateManager.
     */
    private fun handleInstanceRemove(session: IHTTPSession): Response {
        return try {
            // Parse request body
            val files = mutableMapOf<String, String>()
            session.parseBody(files)
            val body = files["postData"] ?: return newFixedLengthResponse(
                Response.Status.BAD_REQUEST,
                "application/json",
                """{"success":false,"error":"No request body"}"""
            )

            val jsonObject = JSONObject(body)
            val instanceId = jsonObject.getString("instanceId")

            // Validate BLE service is stopped (use running flag, not null instance)
            val service = BaseBleService.getInstance()
            val isRunning = if (service != null) runBlocking { BaseBleService.serviceRunning.first() } else false
            if (isRunning) {
                return newFixedLengthResponse(
                    Response.Status.BAD_REQUEST,
                    "application/json",
                    """{"success":false,"error":"Service must be stopped to remove instances"}"""
                )
            }

            // Remove from ServiceStateManager
            ServiceStateManager.removeInstance(context, instanceId)
            Log.i(TAG, "Instance removed via web UI: $instanceId")
            
            newFixedLengthResponse(
                Response.Status.OK,
                "application/json",
                """{"success":true}"""
            )
        } catch (e: Exception) {
            Log.e(TAG, "Error removing instance", e)
            newFixedLengthResponse(
                Response.Status.INTERNAL_ERROR,
                "application/json",
                """{"success":false,"error":"${e.message}"}"""
            )
        }
    }

    /**
     * Phase 5: Update a plugin instance.
     * Updates the config or other fields of an existing instance.
     */
    private fun handleInstanceUpdate(session: IHTTPSession): Response {
        return try {
            // Parse request body
            val files = mutableMapOf<String, String>()
            session.parseBody(files)
            val body = files["postData"] ?: return newFixedLengthResponse(
                Response.Status.BAD_REQUEST,
                "application/json",
                """{"success":false,"error":"No request body"}"""
            )

            val jsonObject = JSONObject(body)
            val instanceId = jsonObject.getString("instanceId")
            val displayName = jsonObject.optString("displayName", null)
            val deviceMac = jsonObject.optString("deviceMac", null)
            val config = jsonObject.optJSONObject("config")?.let { 
                val map = mutableMapOf<String, String>()
                it.keys().forEach { key -> map[key] = it.getString(key) }
                map
            }

            // Validate service is stopped
            val service = BaseBleService.getInstance()
            if (service != null && runBlocking { BaseBleService.serviceRunning.first() }) {
                return newFixedLengthResponse(
                    Response.Status.BAD_REQUEST,
                    "application/json",
                    """{"success":false,"error":"Service must be stopped to update instances"}"""
                )
            }

            // Load existing instance
            val allInstances = ServiceStateManager.getAllInstances(context)
            val existingInstance = allInstances[instanceId] ?: return newFixedLengthResponse(
                Response.Status.NOT_FOUND,
                "application/json",
                """{"success":false,"error":"Instance not found: $instanceId"}"""
            )

            // Check if MAC address changed
            val newMac = deviceMac ?: existingInstance.deviceMac
            val macChanged = newMac != existingInstance.deviceMac
            
            // If MAC changed, generate new instanceId and remove old instance
            val newInstanceId = if (macChanged) {
                ServiceStateManager.removeInstance(context, instanceId)
                PluginInstance.createInstanceId(existingInstance.pluginType, newMac)
            } else {
                instanceId
            }

            // Create updated instance
            val updatedInstance = existingInstance.copy(
                instanceId = newInstanceId,
                displayName = displayName ?: existingInstance.displayName,
                deviceMac = newMac,
                config = config ?: existingInstance.config
            )

            // Save updated instance
            ServiceStateManager.saveInstance(context, updatedInstance)
            Log.i(TAG, "Instance updated via web UI: $instanceId -> $newInstanceId")
            
            newFixedLengthResponse(
                Response.Status.OK,
                "application/json",
                """{"success":true,"instanceId":"$newInstanceId"}"""
            )
        } catch (e: Exception) {
            Log.e(TAG, "Error updating instance", e)
            newFixedLengthResponse(
                Response.Status.INTERNAL_ERROR,
                "application/json",
                """{"success":false,"error":"${e.message}"}"""
            )
        }
    }

    /**
     * Export all configuration as a JSON file download.
     * GET /api/config/export
     */
    private fun handleConfigExport(): Response {
        return try {
            Log.i(TAG, "Exporting configuration...")
            
            val backupJson = ConfigBackupManager.createBackup(context)
            
            // Return as downloadable file
            val response = newFixedLengthResponse(Response.Status.OK, "application/json", backupJson)
            val timestamp = java.time.LocalDateTime.now().format(
                java.time.format.DateTimeFormatter.ofPattern("yyyy-MM-dd_HH-mm-ss")
            )
            response.addHeader("Content-Disposition", "attachment; filename=\"ble-bridge-backup-$timestamp.json\"")
            
            Log.i(TAG, "✅ Configuration exported")
            response
        } catch (e: Exception) {
            Log.e(TAG, "Error exporting configuration", e)
            newFixedLengthResponse(
                Response.Status.INTERNAL_ERROR,
                "application/json",
                """{"success":false,"error":"${e.message}"}"""
            )
        }
    }

    /**
     * Import and restore configuration from an uploaded JSON file.
     * POST /api/config/import
     * 
     * Expects multipart form data with "backup" file field.
     * Optional "replace" parameter (true/false) to replace vs merge config.
     */
    private fun handleConfigImport(session: IHTTPSession): Response {
        return try {
            Log.i(TAG, "Importing configuration...")
            
            val files = mutableMapOf<String, String>()
            session.parseBody(files)
            
            // Get the backup JSON from multipart body
            val backupContent = files["postData"] ?: return newFixedLengthResponse(
                Response.Status.BAD_REQUEST,
                "application/json",
                """{"success":false,"error":"No backup file provided"}"""
            )
            
            // Try to parse as JSON
            val backupJson = try {
                JSONObject(backupContent).toString()
            } catch (e: Exception) {
                // If not JSON object at root, try to extract from request
                backupContent
            }
            
            // Check for replace parameter
            val queryParams = session.parms
            val replaceExisting = queryParams["replace"]?.equals("true", ignoreCase = true) ?: false
            
            Log.i(TAG, "Restoring backup (replace=$replaceExisting)...")
            
            val result = runBlocking {
                ConfigBackupManager.restoreBackup(backupJson, context, replaceExisting)
            }
            
            val response = JSONObject()
            response.put("success", result.success)
            response.put("message", result.message)
            
            val status = if (result.success) Response.Status.OK else Response.Status.BAD_REQUEST
            newFixedLengthResponse(
                status,
                "application/json",
                response.toString()
            )
        } catch (e: Exception) {
            Log.e(TAG, "Error importing configuration", e)
            newFixedLengthResponse(
                Response.Status.INTERNAL_ERROR,
                "application/json",
                """{"success":false,"error":"${e.message}"}"""
            )
        }
    }

    fun startServer() {
        try {
            start(NanoHTTPD.SOCKET_READ_TIMEOUT, false)
            Log.i(TAG, "✅ Web server started on port $port")
        } catch (e: Exception) {
            Log.e(TAG, "Failed to start web server", e)
            throw e
        }
    }

    fun stopServer() {
        try {
            stop()
            Log.i(TAG, "Web server stopped")
        } catch (e: Exception) {
            Log.e(TAG, "Error stopping web server", e)
        }
    }

    // ============================================================================
    // Polling Plugin Management (Peplink, etc.)
    // ============================================================================

    /**
     * Serve all polling plugin instances as JSON.
     * Loads from persistence (SharedPreferences) to include instances even when service is stopped.
     */
    private fun servePollingInstances(): Response {
        return try {
            val persistedInstances = ServiceStateManager.getAllPollingInstances(context)
            val registry = PluginRegistry.getInstance()
            val jsonArray = JSONArray()
            val allStatuses = BaseBleService.pluginStatuses.value

            for ((instanceId, config) in persistedInstances) {
                // Check if instance is running in memory
                val runningPlugin = registry.getPollingPluginInstance(instanceId)

                val instanceJson = JSONObject().apply {
                    put("instanceId", instanceId)
                    put("pluginId", config.pluginType)
                    put("displayName", config.displayName)

                    // If running, get live data; otherwise use config values
                    if (runningPlugin != null) {
                        put("baseTopic", runningPlugin.getMqttBaseTopic())
                        put("pollingInterval", runningPlugin.getPollingInterval())
                        put("running", true)
                    } else {
                        put("baseTopic", "homeassistant")  // Default
                        put("pollingInterval", config.config["polling_interval"]?.toLongOrNull()?.times(1000) ?: 30000)
                        put("running", false)
                    }
                    
                    // Include config data for display in web UI
                    val configJson = JSONObject()
                    for ((key, value) in config.config) {
                        configJson.put(key, value)
                    }
                    put("config", configJson)
                    
                    // Include health status if available
                    val status = allStatuses[instanceId]
                    if (status != null) {
                        put("connected", status.connected)
                        put("authenticated", status.authenticated)
                        put("dataHealthy", status.dataHealthy)
                    }
                }
                jsonArray.put(instanceJson)
            }

            newFixedLengthResponse(Response.Status.OK, "application/json", jsonArray.toString())
        } catch (e: Exception) {
            Log.e(TAG, "Error serving polling instances", e)
            newFixedLengthResponse(
                Response.Status.INTERNAL_ERROR,
                "application/json",
                """{"error":"${e.message}"}"""
            )
        }
    }

    /**
     * Add a new polling plugin instance (e.g., Peplink router).
     */
    private fun handlePollingInstanceAdd(session: IHTTPSession): Response {
        return try {
            // Parse request body
            val files = mutableMapOf<String, String>()
            session.parseBody(files)
            val body = files["postData"] ?: return newFixedLengthResponse(
                Response.Status.BAD_REQUEST,
                "application/json",
                """{"success":false,"error":"No request body"}"""
            )

            val jsonObject = JSONObject(body)
            val pluginType = jsonObject.getString("pluginType")
            
            // Verify polling is not running before allowing instance configuration
            val (canEdit, errorMsg) = canEditPlugin(pluginType)
            if (!canEdit) {
                return newFixedLengthResponse(
                    Response.Status.BAD_REQUEST,
                    "application/json",
                    """{"success":false,"error":"$errorMsg"}"""
                )
            }
            
            val instanceName = jsonObject.optString("instanceName", "main")
            val displayName = jsonObject.optString("displayName", instanceName)
            val config = jsonObject.optJSONObject("config")?.let {
                val map = mutableMapOf<String, String>()
                it.keys().forEach { key -> map[key] = it.getString(key) }
                map
            } ?: mutableMapOf()

            // Add instance_name to config
            config["instance_name"] = instanceName

            // Create plugin instance
            val registry = PluginRegistry.getInstance()
            val instanceId = "${pluginType}_$instanceName"

            val pluginInstance = registry.createPollingPluginInstance(
                pluginType = pluginType,
                instanceId = instanceId,
                context = context,
                config = PluginConfig(config)
            )

            if (pluginInstance == null) {
                return newFixedLengthResponse(
                    Response.Status.INTERNAL_ERROR,
                    "application/json",
                    """{"success":false,"error":"Failed to create plugin instance"}"""
                )
            }

            // Persist the instance configuration
            val pollingConfig = PollingPluginConfig(
                instanceId = instanceId,
                pluginType = pluginType,
                displayName = displayName,
                config = config
            )
            ServiceStateManager.savePollingInstance(context, pollingConfig)

            // Start polling if MQTT is available via standalone MqttService
            val mqttPublisher = getMqttService()?.getMqttPublisher()
            if (mqttPublisher != null) {
                runBlocking {
                    pluginInstance.startPolling(mqttPublisher)
                }
            } else {
                Log.w(TAG, "MQTT service not available; instance added but polling not started: $instanceId")
            }

            Log.i(TAG, "Polling plugin instance added and persisted: $instanceId")
            newFixedLengthResponse(
                Response.Status.OK,
                "application/json",
                """{"success":true,"instanceId":"$instanceId"}"""
            )
        } catch (e: Exception) {
            Log.e(TAG, "Error adding polling plugin instance", e)
            newFixedLengthResponse(
                Response.Status.INTERNAL_ERROR,
                "application/json",
                """{"success":false,"error":"${e.message}"}"""
            )
        }
    }

    /**
     * Remove a polling plugin instance.
     */
    private fun handlePollingInstanceRemove(session: IHTTPSession): Response {
        return try {
            // Parse request body
            val files = mutableMapOf<String, String>()
            session.parseBody(files)
            val body = files["postData"] ?: return newFixedLengthResponse(
                Response.Status.BAD_REQUEST,
                "application/json",
                """{"success":false,"error":"No request body"}"""
            )

            val jsonObject = JSONObject(body)
            val instanceId = jsonObject.getString("instanceId")

            // Verify polling is not running before allowing instance removal
            val (canEdit, errorMsg) = canEditPlugin("mopeka")
            if (!canEdit) {
                return newFixedLengthResponse(
                    Response.Status.BAD_REQUEST,
                    "application/json",
                    """{"success":false,"error":"$errorMsg"}"""
                )
            }

            // Stop and remove instance
            val registry = PluginRegistry.getInstance()
            val plugin = registry.getPollingPluginInstance(instanceId)

            if (plugin != null) {
                runBlocking {
                    plugin.stopPolling()
                }
            }

            runBlocking {
                registry.removePollingPluginInstance(instanceId)
            }

            // Remove from persistence
            ServiceStateManager.removePollingInstance(context, instanceId)

            Log.i(TAG, "Polling plugin instance removed and deleted from persistence: $instanceId")
            newFixedLengthResponse(
                Response.Status.OK,
                "application/json",
                """{"success":true}"""
            )
        } catch (e: Exception) {
            Log.e(TAG, "Error removing polling plugin instance", e)
            newFixedLengthResponse(
                Response.Status.INTERNAL_ERROR,
                "application/json",
                """{"success":false,"error":"${e.message}"}"""
            )
        }
    }

    /**
     * Start polling for an instance.
     */
    private fun handlePollingInstanceStart(session: IHTTPSession): Response {
        return try {
            val files = mutableMapOf<String, String>()
            session.parseBody(files)
            val body = files["postData"] ?: return newFixedLengthResponse(
                Response.Status.BAD_REQUEST,
                "application/json",
                """{"success":false,"error":"No request body"}"""
            )

            val jsonObject = JSONObject(body)
            val instanceId = jsonObject.getString("instanceId")

            val registry = PluginRegistry.getInstance()
            val plugin = registry.getPollingPluginInstance(instanceId)

            if (plugin == null) {
                return newFixedLengthResponse(
                    Response.Status.NOT_FOUND,
                    "application/json",
                    """{"success":false,"error":"Instance not found"}"""
                )
            }

            // Start polling using MQTT from standalone MqttService
            val mqttPublisher = getMqttService()?.getMqttPublisher() ?: return newFixedLengthResponse(
                Response.Status.BAD_REQUEST,
                "application/json",
                """{"success":false,"error":"MQTT service not available"}"""
            )
            runBlocking {
                plugin.startPolling(mqttPublisher)
            }

            Log.i(TAG, "Polling started for instance: $instanceId")
            newFixedLengthResponse(
                Response.Status.OK,
                "application/json",
                """{"success":true}"""
            )
        } catch (e: Exception) {
            Log.e(TAG, "Error starting polling instance", e)
            newFixedLengthResponse(
                Response.Status.INTERNAL_ERROR,
                "application/json",
                """{"success":false,"error":"${e.message}"}"""
            )
        }
    }

    /**
     * Stop polling for an instance.
     */
    private fun handlePollingInstanceStop(session: IHTTPSession): Response {
        return try {
            val files = mutableMapOf<String, String>()
            session.parseBody(files)
            val body = files["postData"] ?: return newFixedLengthResponse(
                Response.Status.BAD_REQUEST,
                "application/json",
                """{"success":false,"error":"No request body"}"""
            )

            val jsonObject = JSONObject(body)
            val instanceId = jsonObject.getString("instanceId")

            val registry = PluginRegistry.getInstance()
            val plugin = registry.getPollingPluginInstance(instanceId)

            if (plugin == null) {
                return newFixedLengthResponse(
                    Response.Status.NOT_FOUND,
                    "application/json",
                    """{"success":false,"error":"Instance not found"}"""
                )
            }

            // Stop polling
            runBlocking {
                plugin.stopPolling()
            }

            Log.i(TAG, "Polling stopped for instance: $instanceId")
            newFixedLengthResponse(
                Response.Status.OK,
                "application/json",
                """{"success":true}"""
            )
        } catch (e: Exception) {
            Log.e(TAG, "Error stopping polling instance", e)
            newFixedLengthResponse(
                Response.Status.INTERNAL_ERROR,
                "application/json",
                """{"success":false,"error":"${e.message}"}"""
            )
        }
    }

    // ============================================================================
    // Polling Plugin Control (Independent from BLE Service)
    // ============================================================================

    /**
     * Serve polling plugin status (running/stopped, counts).
     */
    private fun servePollingStatus(): Response {
        return try {
            val registry = PluginRegistry.getInstance()
            val allInstances = ServiceStateManager.getAllPollingInstances(context)
            val runningInstances = registry.getAllPollingPluginInstances()
            val isEnabled = runBlocking { appSettings.pollingEnabled.first() }

            val json = JSONObject().apply {
                put("running", runningInstances.isNotEmpty() || isEnabled)
                put("runningCount", runningInstances.size)
                put("totalCount", allInstances.size)
            }

            newFixedLengthResponse(Response.Status.OK, "application/json", json.toString())
        } catch (e: Exception) {
            Log.e(TAG, "Error serving polling status", e)
            newFixedLengthResponse(
                Response.Status.INTERNAL_ERROR,
                "application/json",
                """{"error":"${e.message}"}"""
            )
        }
    }

    /**
     * Start all polling plugin instances.
     * Loads instances from SharedPreferences, creates them, and starts polling.
     */
    private fun handlePollingControlStart(): Response {
        return try {
            val registry = PluginRegistry.getInstance()
            val allPollingInstances = ServiceStateManager.getAllPollingInstances(context)

            if (allPollingInstances.isEmpty()) {
                return newFixedLengthResponse(
                    Response.Status.OK,
                    "application/json",
                    """{"success":true,"message":"No polling instances configured","started":0}"""
                )
            }

            // Get MQTT publisher - polling plugins require MQTT to publish data
            val mqttPublisher = getMqttService()?.getMqttPublisher()
            if (mqttPublisher == null) {
                // MQTT not available - polling can't publish, so we'll wait
                Log.w(TAG, "MQTT service not available - polling plugins will start when MQTT connects")
                return newFixedLengthResponse(
                    Response.Status.OK,
                    "application/json",
                    """{"success":true,"message":"Polling service started but waiting for MQTT connection to begin polling","started":0,"warning":"MQTT service not yet connected"}"""
                )
            }

            var successCount = 0
            val errors = mutableListOf<String>()

            for ((instanceId, pollingConfig) in allPollingInstances) {
                try {
                    // Create plugin instance if not already created
                    var plugin = registry.getPollingPluginInstance(instanceId)
                    if (plugin == null) {
                        plugin = registry.createPollingPluginInstance(
                            pluginType = pollingConfig.pluginType,
                            instanceId = instanceId,
                            context = context,
                            config = PluginConfig(pollingConfig.config)
                        )
                    }

                    if (plugin != null) {
                        // Start polling
                        val result = runBlocking {
                            plugin.startPolling(mqttPublisher)
                        }

                        if (result.isSuccess) {
                            successCount++
                            Log.i(TAG, "Started polling for: $instanceId")
                        } else {
                            val errorMsg = "$instanceId: ${result.exceptionOrNull()?.message}"
                            errors.add(errorMsg)
                            Log.w(TAG, "Failed to start polling for $instanceId: ${result.exceptionOrNull()?.message}")
                        }
                    } else {
                        errors.add("$instanceId: Failed to create plugin instance")
                    }
                } catch (e: Exception) {
                    errors.add("$instanceId: ${e.message}")
                    Log.e(TAG, "Exception starting polling for $instanceId", e)
                }
            }

            // Save polling state if any plugins started successfully
            if (successCount > 0) {
                runBlocking {
                    appSettings.setPollingEnabled(true)
                }
            }

            val response = JSONObject().apply {
                put("success", successCount > 0)
                put("started", successCount)
                put("total", allPollingInstances.size)
                if (errors.isNotEmpty()) {
                    put("errors", JSONArray(errors))
                }
            }

            newFixedLengthResponse(Response.Status.OK, "application/json", response.toString())
        } catch (e: Exception) {
            Log.e(TAG, "Error starting polling control", e)
            newFixedLengthResponse(
                Response.Status.INTERNAL_ERROR,
                "application/json",
                """{"success":false,"error":"${e.message}"}"""
            )
        }
    }

    /**
     * Auto-start polling on app startup if it was previously enabled.
     * Called from WebServerService after server starts.
     * Retries up to 3 times if MQTT service is not yet available.
     */
    fun autoStartPolling(retryCount: Int = 0) {
        try {
            val registry = PluginRegistry.getInstance()
            val allPollingInstances = ServiceStateManager.getAllPollingInstances(context)

            if (allPollingInstances.isEmpty()) {
                Log.i(TAG, "No polling instances configured for auto-start")
                return
            }

            // Check if MQTT service is available
            val mqttPublisher = getMqttService()?.getMqttPublisher()
            if (mqttPublisher == null) {
                if (retryCount < 3) {
                    Log.w(TAG, "MQTT service not available for polling auto-start (attempt ${retryCount + 1}/3), will retry in 3 seconds...")
                    // Retry after delay
                    android.os.Handler(android.os.Looper.getMainLooper()).postDelayed({
                        autoStartPolling(retryCount + 1)
                    }, 3000)
                } else {
                    Log.e(TAG, "MQTT service not available after 3 attempts, polling auto-start failed")
                }
                return
            }

            Log.i(TAG, "Auto-starting ${allPollingInstances.size} polling instances...")

            for ((instanceId, pollingConfig) in allPollingInstances) {
                try {
                    // Create plugin instance if not already created
                    var plugin = registry.getPollingPluginInstance(instanceId)
                    if (plugin == null) {
                        plugin = registry.createPollingPluginInstance(
                            pluginType = pollingConfig.pluginType,
                            instanceId = instanceId,
                            context = context,
                            config = PluginConfig(pollingConfig.config)
                        )
                    }

                    if (plugin != null) {
                        // Start polling
                        runBlocking {
                            val result = plugin.startPolling(mqttPublisher)
                            if (result.isSuccess) {
                                Log.i(TAG, "Auto-started polling for: $instanceId")
                            } else {
                                Log.w(TAG, "Failed to auto-start polling for $instanceId: ${result.exceptionOrNull()?.message}")
                            }
                        }
                    } else {
                        Log.w(TAG, "Failed to create plugin instance for auto-start: $instanceId")
                    }
                } catch (e: Exception) {
                    Log.e(TAG, "Exception auto-starting polling for $instanceId", e)
                }
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error in autoStartPolling", e)
        }
    }

    /**
     * Stop all polling plugin instances.
     */
    private fun handlePollingControlStop(): Response {
        return try {
            val registry = PluginRegistry.getInstance()
            val runningInstances = registry.getAllPollingPluginInstances()

            if (runningInstances.isEmpty()) {
                return newFixedLengthResponse(
                    Response.Status.OK,
                    "application/json",
                    """{"success":true,"message":"No polling instances running","stopped":0}"""
                )
            }

            var successCount = 0
            for ((instanceId, plugin) in runningInstances) {
                try {
                    runBlocking {
                        plugin.stopPolling()
                        // Remove from registry
                        registry.removePollingPluginInstance(instanceId)
                    }
                    successCount++
                    Log.i(TAG, "Stopped polling for: $instanceId")
                } catch (e: Exception) {
                    Log.e(TAG, "Error stopping polling for $instanceId", e)
                }
            }

            // Save polling state
            runBlocking {
                appSettings.setPollingEnabled(false)
            }

            val response = JSONObject().apply {
                put("success", true)
                put("stopped", successCount)
            }

            newFixedLengthResponse(Response.Status.OK, "application/json", response.toString())
        } catch (e: Exception) {
            Log.e(TAG, "Error stopping polling control", e)
            newFixedLengthResponse(
                Response.Status.INTERNAL_ERROR,
                "application/json",
                """{"success":false,"error":"${e.message}"}"""
            )
        }
    }

    fun getUrl(ipAddress: String): String {
        return "http://$ipAddress:$port"
    }
}
