package com.blemqttbridge.web

import android.content.Context
import android.util.Log
import com.blemqttbridge.BuildConfig
import com.blemqttbridge.core.BaseBleService
import com.blemqttbridge.data.AppSettings
import fi.iki.elonen.NanoHTTPD
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.runBlocking
import org.json.JSONArray
import org.json.JSONObject
import java.io.File

/**
 * Embedded web server for configuration and monitoring.
 * Provides REST API for viewing configuration, plugin status, and logs.
 * Can run independently of BLE service.
 */
class WebServerManager(
    private val context: Context,
    private val service: BaseBleService?,
    private val port: Int = 8088
) : NanoHTTPD(port) {

    companion object {
        private const val TAG = "WebServerManager"
    }

    init {
        Log.i(TAG, "Web server initialized on port $port")
    }

    override fun serve(session: IHTTPSession): Response {
        val uri = session.uri
        val method = session.method

        Log.d(TAG, "$method $uri from ${session.remoteIpAddress}")

        return try {
            when {
                uri == "/" || uri == "/index.html" -> serveIndexPage()
                uri == "/api/status" -> serveStatus()
                uri == "/api/config" -> serveConfig()
                uri == "/api/plugins" -> servePlugins()
                uri == "/api/logs/debug" -> serveDebugLog()
                uri == "/api/logs/ble" -> serveBleTrace()
                uri == "/api/control/service" && method == Method.POST -> handleServiceControl(session)
                uri == "/api/control/mqtt" && method == Method.POST -> handleMqttControl(session)
                uri == "/api/config/plugin" && method == Method.POST -> handlePluginConfig(session)
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

    private fun serveIndexPage(): Response {
        val html = """
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>BLE-MQTT Plugin Bridge</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            background: #f5f5f5;
            padding: 20px;
        }
        .container { max-width: 1200px; margin: 0 auto; }
        .header {
            background: #1976d2;
            color: white;
            padding: 20px;
            position: relative;
            z-index: 1;
            border-radius: 8px;
            margin-bottom: 20px;
        }
        .header h1 { font-size: 24px; margin-bottom: 5px; }
        .header .version { opacity: 0.8; font-size: 14px; }
        .card {
            background: white;
            border-radius: 8px;
            padding: 20px;
            margin-bottom: 20px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }
        .card h2 {
            font-size: 18px;
            margin-bottom: 15px;
            color: #333;
            border-bottom: 2px solid #1976d2;
            padding-bottom: 8px;
        }
        .status-row {
            display: flex;
            justify-content: space-between;
            padding: 10px 0;
            border-bottom: 1px solid #eee;
        }
        .status-row:last-child { border-bottom: none; }
        .status-label { font-weight: 500; color: #666; }
        .status-value { color: #333; }
        .status-online { color: #4caf50; font-weight: bold; }
        .status-offline { color: #f44336; font-weight: bold; }
        .plugin-item {
            padding: 12px;
            margin-bottom: 10px;
            background: #f9f9f9;
            border-radius: 4px;
            border-left: 4px solid #1976d2;
        }
        .plugin-name { font-weight: 600; color: #333; margin-bottom: 5px; text-align: left; }
        .plugin-status { font-size: 14px; color: #666; text-align: left; }
        .plugin-status-line { margin-bottom: 8px; text-align: left; }
        .plugin-config-field { margin: 4px 0; padding-left: 0; text-align: left; }
        .plugin-healthy { color: #4caf50; }
        .plugin-unhealthy { color: #f44336; }
        .toggle-switch {
            position: relative;
            display: inline-block;
            width: 50px;
            height: 24px;
        }
        .toggle-switch input {
            opacity: 0;
            width: 0;
            height: 0;
        }
        .toggle-slider {
            position: absolute;
            cursor: pointer;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            background-color: #ccc;
            transition: .4s;
            border-radius: 24px;
        }
        .toggle-slider:before {
            position: absolute;
            content: "";
            height: 18px;
            width: 18px;
            left: 3px;
            bottom: 3px;
            background-color: white;
            transition: .4s;
            border-radius: 50%;
        }
        input:checked + .toggle-slider {
            background-color: #4caf50;
        }
        input:checked + .toggle-slider:before {
            transform: translateX(26px);
        }
        input:disabled + .toggle-slider {
            cursor: not-allowed;
            opacity: 0.6;
        }
        button {
            background: #1976d2;
            color: white;
            border: none;
            padding: 10px 20px;
            border-radius: 4px;
            cursor: pointer;
            font-size: 14px;
            margin-right: 10px;
        }
        button:hover { background: #1565c0; }
        .log-container {
            background: #263238;
            color: #aed581;
            padding: 15px;
            border-radius: 4px;
            font-family: 'Courier New', monospace;
            font-size: 12px;
            max-height: 400px;
            overflow-y: auto;
            white-space: pre-wrap;
            word-break: break-all;
        }
        .loading { text-align: center; padding: 20px; color: #666; }
        .edit-btn {
            background: #1976d2;
            color: white;
            border: none;
            padding: 4px 12px;
            border-radius: 4px;
            cursor: pointer;
            font-size: 12px;
            margin-left: 8px;
        }
        .edit-btn:hover { background: #1565c0; }
        .edit-btn:disabled {
            background: #ccc;
            cursor: not-allowed;
        }
        .save-btn {
            background: #4caf50;
        }
        .save-btn:hover { background: #45a049; }
        .config-input {
            font-family: monospace;
            padding: 4px 8px;
            border: 1px solid #ddd;
            border-radius: 4px;
            font-size: 14px;
            width: 200px;
        }
        .helper-text {
            color: #ff9800;
            font-size: 12px;
            margin-left: 8px;
            font-style: italic;
        }
        .section-helper {
            color: #666;
            font-size: 13px;
            font-style: italic;
            margin-top: -8px;
            margin-bottom: 12px;
        }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>BLE-MQTT Plugin Bridge</h1>
            <div class="version">Version ${BuildConfig.VERSION_NAME} (${BuildConfig.VERSION_CODE})</div>
        </div>

        <div class="card">
            <h2>Service Status</h2>
            <div id="service-status" class="loading">Loading...</div>
        </div>

        <div class="card">
            <h2>Configuration</h2>
            <div id="config-info" class="loading">Loading...</div>
        </div>

        <div class="card">
            <h2>Plugin Status</h2>
            <div class="section-helper">Note: Service must be stopped to edit plugin configurations</div>
            <div id="plugin-status" class="loading">Loading...</div>
        </div>

        <div class="card">
            <h2>Debug Log</h2>
            <button onclick="loadDebugLog()">Load/Refresh Debug Log</button>
            <button onclick="downloadDebugLog()">Download Debug Log</button>
            <div id="debug-log" class="log-container" style="display:none; margin-top: 15px;"></div>
        </div>

        <div class="card">
            <h2>BLE Trace</h2>
            <button onclick="loadBleTrace()">Load/Refresh BLE Trace</button>
            <button onclick="downloadBleTrace()">Download BLE Trace</button>
            <div id="ble-trace" class="log-container" style="display:none; margin-top: 15px;"></div>
        </div>
    </div>

    <script>
        // Global state
        let serviceRunning = false;
        let configChanged = {}; // Track which configs have changed
        let editingFields = {}; // Track which fields are currently being edited
        
        // Load status on page load
        window.addEventListener('load', () => {
            loadStatus();
            loadConfig();
            loadPlugins();
            // Auto-refresh status every 5 seconds
            setInterval(() => {
                loadStatus();
                // Only refresh plugins if not currently editing
                if (Object.keys(editingFields).length === 0) {
                    loadPlugins();
                }
            }, 5000);
        });

        async function loadStatus() {
            try {
                const response = await fetch('/api/status');
                const data = await response.json();
                serviceRunning = data.running;
                const mqttStatusColor = data.mqttConnected ? '#4CAF50' : '#f44336';
                const mqttStatusText = data.mqttConnected ? '●' : '●';
                const html = ${'`'}
                    <div class="status-row">
                        <span class="status-label">Service Running:</span>
                        <label class="toggle-switch">
                            <input type="checkbox" ${'$'}{data.running ? 'checked' : ''} onchange="toggleService(this.checked)">
                            <span class="toggle-slider"></span>
                        </label>
                    </div>
                    <div class="status-row">
                        <span class="status-label">MQTT Enabled: <span style="color: ${'$'}{mqttStatusColor}">${'$'}{mqttStatusText}</span></span>
                        <label class="toggle-switch">
                            <input type="checkbox" ${'$'}{data.mqttEnabled ? 'checked' : ''} onchange="toggleMqtt(this.checked)">
                            <span class="toggle-slider"></span>
                        </label>
                    </div>
                ${'`'};
                document.getElementById('service-status').innerHTML = html;
            } catch (error) {
                document.getElementById('service-status').innerHTML = 
                    '<div style="color: #f44336;">Failed to load status</div>';
            }
        }

        async function loadConfig() {
            try {
                const response = await fetch('/api/config');
                const data = await response.json();
                const html = ${'`'}
                    <div class="status-row">
                        <span class="status-label">MQTT Broker:</span>
                        <span class="status-value">${'$'}{data.mqttBroker || 'Not configured'}</span>
                    </div>
                    <div class="status-row">
                        <span class="status-label">MQTT Port:</span>
                        <span class="status-value">${'$'}{data.mqttPort || 'Not configured'}</span>
                    </div>
                    <div class="status-row">
                        <span class="status-label">MQTT Username:</span>
                        <span class="status-value">${'$'}{data.mqttUsername || 'None'}</span>
                    </div>
                    <div class="status-row">
                        <span class="status-label">MQTT Password:</span>
                        <span class="status-value">${'$'}{data.mqttPassword ? '•'.repeat(data.mqttPassword.length) : 'None'}</span>
                    </div>
                    <div class="status-row">
                        <span class="status-label">Enabled Plugins:</span>
                        <span class="status-value">${'$'}{data.enabledPlugins.join(', ') || 'None'}</span>
                    </div>
                ${'`'};
                document.getElementById('config-info').innerHTML = html;
            } catch (error) {
                document.getElementById('config-info').innerHTML = 
                    '<div style="color: #f44336;">Failed to load configuration</div>';
            }
        }

        async function loadPlugins() {
            try {
                const response = await fetch('/api/plugins');
                const data = await response.json();
                let html = '';
                for (const [pluginId, status] of Object.entries(data)) {
                    const healthy = status.connected && status.authenticated && status.dataHealthy;
                    const macAddresses = status.macAddresses && status.macAddresses.length > 0 
                        ? status.macAddresses.join(', ') 
                        : '';
                    const enabled = status.enabled ? 'Yes' : 'No';
                    const showHelper = configChanged[pluginId] && !serviceRunning;
                    
                    // Build configuration field lines with edit buttons
                    let configLines = [];
                    const editDisabled = serviceRunning ? 'disabled' : '';
                    
                    // MAC Address field (always editable for all plugins)
                    configLines.push(buildEditableField(pluginId, 'macAddress', 'MAC Address', macAddresses, editDisabled, false));
                    
                    // Build status line - show authenticated only for plugins that actually authenticate
                    const showAuth = pluginId !== 'gopower'; // GoPower doesn't have separate auth
                    
                    // Add plugin-specific fields
                    if (pluginId === 'onecontrol') {
                        configLines.push(buildEditableField(pluginId, 'gatewayPin', 'Gateway PIN', status.gatewayPin || '', editDisabled, true));
                    } else if (pluginId === 'easytouch') {
                        configLines.push(buildEditableField(pluginId, 'password', 'Password', status.password || '', editDisabled, true));
                    }
                    
                    html += ${'`'}
                        <div class="plugin-item">
                            <div class="plugin-name">${'$'}{pluginId}${'$'}{showHelper ? '<span class="helper-text">Changes will take effect upon restarting service</span>' : ''}</div>
                            <div class="plugin-status">
                                <div class="plugin-status-line">
                                    Enabled: <span class="${'$'}{status.enabled ? 'plugin-healthy' : 'plugin-unhealthy'}">${'$'}{enabled}</span> | 
                                    Connected: <span class="${'$'}{status.connected ? 'plugin-healthy' : 'plugin-unhealthy'}">${'$'}{status.connected ? 'Yes' : 'No'}</span>${'$'}{showAuth ? ' | Authenticated: <span class="' + (status.authenticated ? 'plugin-healthy' : 'plugin-unhealthy') + '">' + (status.authenticated ? 'Yes' : 'No') + '</span>' : ''} | 
                                    Data Healthy: <span class="${'$'}{status.dataHealthy ? 'plugin-healthy' : 'plugin-unhealthy'}">${'$'}{status.dataHealthy ? 'Yes' : 'No'}</span>
                                </div>
                                ${'$'}{configLines.join('')}
                            </div>
                        </div>
                    ${'`'};
                }
                document.getElementById('plugin-status').innerHTML = html || '<div>No plugins loaded</div>';
            } catch (error) {
                document.getElementById('plugin-status').innerHTML = 
                    '<div style="color: #f44336;">Failed to load plugin status</div>';
            }
        }

        function buildEditableField(pluginId, fieldName, label, value, editDisabled, isSecret) {
            const fieldId = `${'$'}{pluginId}_${'$'}{fieldName}`;
            const displayValue = value || 'None';
            const maskedValue = isSecret && value ? '•'.repeat(value.length) : displayValue;
            
            return ${'`'}
                <div class="plugin-config-field">
                    ${'$'}{label}: 
                    <span id="${'$'}{fieldId}_display">${'$'}{maskedValue}</span>
                    <input type="text" id="${'$'}{fieldId}_input" class="config-input" value="${'$'}{value}" style="display:none;">
                    <button id="${'$'}{fieldId}_edit" class="edit-btn" ${'$'}{editDisabled} onclick="editField('${'$'}{pluginId}', '${'$'}{fieldName}', ${'$'}{isSecret})">Edit</button>
                    <button id="${'$'}{fieldId}_save" class="edit-btn save-btn" style="display:none;" onclick="saveField('${'$'}{pluginId}', '${'$'}{fieldName}')">Save</button>
                </div>
            ${'`'};
        }

        function editField(pluginId, fieldName, isSecret) {
            const fieldId = `${'$'}{pluginId}_${'$'}{fieldName}`;
            editingFields[fieldId] = true; // Track that this field is being edited
            document.getElementById(`${'$'}{fieldId}_display`).style.display = 'none';
            document.getElementById(`${'$'}{fieldId}_input`).style.display = 'inline-block';
            document.getElementById(`${'$'}{fieldId}_edit`).style.display = 'none';
            document.getElementById(`${'$'}{fieldId}_save`).style.display = 'inline-block';
        }

        async function saveField(pluginId, fieldName) {
            const fieldId = `${'$'}{pluginId}_${'$'}{fieldName}`;
            const value = document.getElementById(`${'$'}{fieldId}_input`).value;
            
            try {
                const response = await fetch('/api/config/plugin', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ 
                        pluginId: pluginId,
                        field: fieldName,
                        value: value
                    })
                });
                const result = await response.json();
                if (result.success) {
                    configChanged[pluginId] = true;
                    delete editingFields[fieldId]; // Clear edit state
                    loadPlugins(); // Reload to show saved value
                } else {
                    alert('Failed to save: ' + (result.error || 'Unknown error'));
                }
            } catch (error) {
                alert('Error saving configuration: ' + error.message);
            }
        }

        async function loadDebugLog() {
            const container = document.getElementById('debug-log');
            container.style.display = 'block';
            container.textContent = 'Loading...';
            try {
                const response = await fetch('/api/logs/debug');
                const text = await response.text();
                container.textContent = text;
            } catch (error) {
                container.textContent = 'Failed to load debug log: ' + error.message;
            }
        }

        async function loadBleTrace() {
            const container = document.getElementById('ble-trace');
            container.style.display = 'block';
            container.textContent = 'Loading...';
            try {
                const response = await fetch('/api/logs/ble');
                const text = await response.text();
                container.textContent = text;
            } catch (error) {
                container.textContent = 'Failed to load BLE trace: ' + error.message;
            }
        }

        function downloadDebugLog() {
            window.open('/api/logs/debug', '_blank');
        }

        function downloadBleTrace() {
            window.open('/api/logs/ble', '_blank');
        }

        async function toggleService(enable) {
            try {
                const response = await fetch('/api/control/service', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ enable: enable })
                });
                const result = await response.json();
                if (!result.success) {
                    alert('Failed to ' + (enable ? 'start' : 'stop') + ' service: ' + (result.error || 'Unknown error'));
                    loadStatus(); // Refresh to show actual state
                } else if (enable) {
                    // Clear config changed flags when service starts
                    configChanged = {};
                }
            } catch (error) {
                alert('Error controlling service: ' + error.message);
                loadStatus(); // Refresh to show actual state
            }
        }

        async function toggleMqtt(enable) {
            try {
                const response = await fetch('/api/control/mqtt', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ enable: enable })
                });
                const result = await response.json();
                if (!result.success) {
                    alert('Failed to ' + (enable ? 'connect' : 'disconnect') + ' MQTT: ' + (result.error || 'Unknown error'));
                    loadStatus(); // Refresh to show actual state
                }
            } catch (error) {
                alert('Error controlling MQTT: ' + error.message);
                loadStatus(); // Refresh to show actual state
            }
        }
    </script>
</body>
</html>
        """.trimIndent()

        return newFixedLengthResponse(Response.Status.OK, "text/html", html)
    }

    private fun serveStatus(): Response {
        val settings = AppSettings(context)
        val mqttEnabled = runBlocking { settings.mqttEnabled.first() }
        val json = JSONObject().apply {
            put("running", BaseBleService.serviceRunning.value)
            put("mqttEnabled", mqttEnabled) // Setting, not connection status
            put("mqttConnected", BaseBleService.mqttConnected.value) // Actual connection status
            put("bleTraceActive", service?.isBleTraceActive() ?: false)
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
        val logText = service?.exportDebugLogToString() ?: "Service not running"
        return newFixedLengthResponse(
            Response.Status.OK,
            "text/plain; charset=utf-8",
            logText
        )
    }

    private fun serveBleTrace(): Response {
        val traceText = service?.exportBleTraceToString() ?: "Service not running"
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
            
            // Update AppSettings first (like Android app does) so both UIs stay in sync
            runBlocking {
                val settings = AppSettings(context)
                settings.setServiceEnabled(enable)
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
            
            if (service == null) {
                return newFixedLengthResponse(
                    Response.Status.SERVICE_UNAVAILABLE,
                    "application/json",
                    """{"success":false,"error":"BLE service not running"}"""
                )
            }
            
            // Update AppSettings first (like Android app does) so both UIs stay in sync
            runBlocking {
                val settings = AppSettings(context)
                settings.setMqttEnabled(enable)
            }
            
            if (enable) {
                // Restart service to properly initialize MQTT (like Android app does)
                Thread {
                    Thread.sleep(100) // Small delay to send response first
                    
                    // Stop service
                    val stopIntent = android.content.Intent(context, BaseBleService::class.java).apply {
                        action = BaseBleService.ACTION_STOP_SERVICE
                    }
                    context.startService(stopIntent)
                    
                    // Wait for service to stop
                    Thread.sleep(500)
                    
                    // Start service with MQTT enabled
                    val startIntent = android.content.Intent(context, BaseBleService::class.java).apply {
                        action = BaseBleService.ACTION_START_SCAN
                    }
                    context.startForegroundService(startIntent)
                    Log.i(TAG, "Service restart requested via web interface for MQTT enable")
                }.start()
            } else {
                // Just disconnect MQTT without restarting service
                runBlocking {
                    service.disconnectMqtt()
                    Log.i(TAG, "MQTT disconnect requested via web interface")
                }
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
            
            // Verify service is not running
            if (BaseBleService.serviceRunning.value) {
                return newFixedLengthResponse(
                    Response.Status.BAD_REQUEST,
                    "application/json",
                    """{"success":false,"error":"Service must be stopped before editing configuration"}"""
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

    fun getUrl(ipAddress: String): String {
        return "http://$ipAddress:$port"
    }
}
