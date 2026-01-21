package com.blemqttbridge.web

import android.content.Context
import android.util.Base64
import android.util.Log
import com.blemqttbridge.BuildConfig
import com.blemqttbridge.core.BaseBleService
import com.blemqttbridge.core.ConfigBackupManager
import com.blemqttbridge.core.PluginInstance
import com.blemqttbridge.core.ServiceStateManager
import com.blemqttbridge.data.AppSettings
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
    private val service: BaseBleService?,
    private val port: Int = 8088
) : NanoHTTPD(port) {

    companion object {
        private const val TAG = "WebServerManager"
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
        :root {
            /* Light mode colors (default) */
            --bg-primary: #f5f5f5;
            --bg-secondary: #fafafa;
            --bg-card: white;
            --bg-header: #1976d2;
            --bg-input: white;
            --text-primary: #333;
            --text-secondary: #666;
            --text-on-primary: white;
            --border-color: #eee;
            --border-light: #ddd;
            --shadow: rgba(0,0,0,0.1);
            --shadow-strong: rgba(0,0,0,0.3);
            --code-bg: #263238;
            --code-text: #aed581;
            --modal-overlay: rgba(0,0,0,0.5);
        }

        [data-theme="dark"] {
            /* Dark mode colors */
            --bg-primary: #121212;
            --bg-secondary: #1a1a1a;
            --bg-card: #1e1e1e;
            --bg-header: #1565c0;
            --bg-input: #2a2a2a;
            --text-primary: #e0e0e0;
            --text-secondary: #b0b0b0;
            --text-on-primary: #e0e0e0;
            --border-color: #333;
            --border-light: #444;
            --shadow: rgba(0,0,0,0.3);
            --shadow-strong: rgba(0,0,0,0.6);
            --code-bg: #0d1117;
            --code-text: #58a6ff;
            --modal-overlay: rgba(0,0,0,0.7);
        }

        * { 
            margin: 0; 
            padding: 0; 
            box-sizing: border-box;
            transition: background-color 0.3s ease, color 0.3s ease, border-color 0.3s ease;
        }
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            background: var(--bg-primary);
            color: var(--text-primary);
            padding: 20px;
        }
        .container { max-width: 1200px; margin: 0 auto; }
        .header {
            background: var(--bg-header);
            color: var(--text-on-primary);
            padding: 20px;
            position: relative;
            z-index: 1;
            border-radius: 8px;
            margin-bottom: 20px;
            display: flex;
            justify-content: space-between;
            align-items: center;
        }
        .header h1 { font-size: 24px; margin-bottom: 5px; }
        .header .version { opacity: 0.8; font-size: 14px; }
        .header .header-content { flex: 1; }
        .theme-toggle {
            background: rgba(255,255,255,0.15);
            color: var(--text-on-primary);
            border: 1px solid rgba(255,255,255,0.3);
            padding: 8px 16px;
            border-radius: 4px;
            cursor: pointer;
            font-size: 14px;
            white-space: nowrap;
            transition: all 0.2s;
        }
        .theme-toggle:hover {
            background: rgba(255,255,255,0.25);
        }
        .card {
            background: var(--bg-card);
            border-radius: 8px;
            padding: 20px;
            margin-bottom: 20px;
            box-shadow: 0 2px 4px var(--shadow);
        }
        .card h2 {
            font-size: 18px;
            margin-bottom: 15px;
            color: var(--text-primary);
            border-bottom: 2px solid var(--bg-header);
            padding-bottom: 8px;
        }
        .status-row {
            display: flex;
            justify-content: space-between;
            padding: 10px 0;
            border-bottom: 1px solid var(--border-color);
        }
        .status-row:last-child { border-bottom: none; }
        .status-label { font-weight: 500; color: var(--text-secondary); }
        .status-value { color: var(--text-primary); }
        .status-online { color: #4caf50; font-weight: bold; }
        .status-offline { color: #f44336; font-weight: bold; }
        .plugin-item {
            padding: 12px;
            margin-bottom: 10px;
            background: var(--bg-secondary);
            border-radius: 4px;
            border-left: 4px solid var(--bg-header);
        }
        .mqtt-config-item {
            padding: 12px;
            margin-bottom: 10px;
            background: var(--bg-secondary);
            border-radius: 4px;
        }
        .mqtt-config-item.mqtt-connected {
            border-left: 4px solid #4caf50;
        }
        .mqtt-config-item.mqtt-disconnected {
            border-left: 4px solid #f44336;
        }
        .plugin-name { font-weight: 600; color: var(--text-primary); margin-bottom: 5px; text-align: left; }
        .plugin-status { font-size: 14px; color: var(--text-secondary); text-align: left; }
        .plugin-status-line { margin-bottom: 8px; text-align: left; }
        .plugin-config-field { margin: 4px 0; padding-left: 0; text-align: left; }
        .mqtt-config-field { margin: 4px 0; padding-left: 0; text-align: left; font-size: 14px; color: var(--text-secondary); }
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
            background: var(--code-bg);
            color: var(--code-text);
            padding: 15px;
            border-radius: 4px;
            font-family: 'Courier New', monospace;
            font-size: 12px;
            max-height: 400px;
            overflow-y: auto;
            white-space: pre-wrap;
            word-break: break-all;
        }
        .loading { text-align: center; padding: 20px; color: var(--text-secondary); }
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
        [data-theme="dark"] .edit-btn:disabled {
            background: #3a3a3a;
            color: #666;
        }
        .save-btn {
            background: #4caf50;
        }
        .save-btn:hover { background: #45a049; }
        .config-input {
            font-family: monospace;
            padding: 4px 8px;
            border: 1px solid var(--border-light);
            background: var(--bg-input);
            color: var(--text-primary);
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
            color: var(--text-secondary);
            font-size: 13px;
            font-style: italic;
            margin-top: -8px;
            margin-bottom: 12px;
        }
        .plugin-type-section {
            margin-bottom: 20px;
        }
        .plugin-type-section.dragging {
            opacity: 0.5;
        }
        .plugin-type-section.drag-over {
            transform: translateY(-5px);
        }
        .plugin-type-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 10px 15px;
            background: var(--bg-secondary);
            border-radius: 4px;
            margin-bottom: 10px;
            cursor: move;
            user-select: none;
            border: 1px solid var(--border-color);
        }
        .plugin-type-header:hover {
            background: var(--bg-primary);
        }
        .plugin-type-title {
            font-weight: 600;
            font-size: 16px;
            color: var(--text-primary);
        }
        .add-instance-btn {
            background: #4caf50;
            color: white;
            border: none;
            padding: 6px 12px;
            border-radius: 4px;
            cursor: pointer;
            font-size: 13px;
        }
        .add-instance-btn:hover { background: #45a049; }
        .add-instance-btn:disabled {
            background: #ccc;
            cursor: not-allowed;
        }
        [data-theme="dark"] .add-instance-btn:disabled {
            background: #3a3a3a;
            color: #666;
        }
        .instance-card {
            background: var(--bg-secondary);
            border: 1px solid var(--border-color);
            border-radius: 4px;
            padding: 12px;
            margin-bottom: 8px;
            margin-left: 20px;
            position: relative;
        }
        .instance-card.instance-healthy {
            border-left: 4px solid #4caf50;
        }
        .instance-card.instance-unhealthy {
            border-left: 4px solid #f44336;
        }
        .instance-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 8px;
        }
        .instance-name {
            font-weight: 600;
            font-size: 14px;
            color: var(--text-primary);
        }
        .instance-actions {
            display: flex;
            gap: 8px;
        }
        .instance-edit-btn, .instance-remove-btn {
            padding: 4px 8px;
            border: none;
            border-radius: 3px;
            cursor: pointer;
            font-size: 12px;
        }
        .instance-edit-btn {
            background: #2196f3;
            color: white;
        }
        .instance-edit-btn:hover { background: #1976d2; }
        .instance-remove-btn {
            background: #f44336;
            color: white;
        }
        .instance-remove-btn:hover { background: #d32f2f; }
        .instance-edit-btn:disabled, .instance-remove-btn:disabled {
            background: #ccc;
            cursor: not-allowed;
        }
        [data-theme="dark"] .instance-edit-btn:disabled,
        [data-theme="dark"] .instance-remove-btn:disabled {
            background: #3a3a3a;
            color: #666;
        }
        .instance-details {
            font-size: 13px;
            color: var(--text-secondary);
        }
        .instance-detail-line {
            margin: 4px 0;
        }

        .remove-btn {
            position: absolute;
            top: 8px;
            right: 8px;
            background: #f44336;
            color: white;
            border: none;
            padding: 4px 8px;
            border-radius: 4px;
            cursor: pointer;
            font-size: 16px;
            font-weight: bold;
            line-height: 1;
            z-index: 1;
            width: 24px;
            height: 24px;
            display: flex;
            align-items: center;
            justify-content: center;
        }
        .remove-btn:hover { background: #d32f2f; }
        .remove-btn:disabled {
            background: #ccc;
            cursor: not-allowed;
        }
        [data-theme="dark"] .remove-btn:disabled {
            background: #3a3a3a;
            color: #666;
        }
        .modal {
            display: none;
            position: fixed;
            z-index: 1000;
            left: 0;
            top: 0;
            width: 100%;
            height: 100%;
            background-color: var(--modal-overlay);
        }
        .modal-content {
            background-color: var(--bg-card);
            margin: 15% auto;
            padding: 20px;
            border-radius: 8px;
            width: 80%;
            max-width: 400px;
            box-shadow: 0 4px 6px var(--shadow-strong);
        }
        .modal-content h3 {
            margin-top: 0;
            color: var(--text-primary);
        }
        .modal-content label {
            color: var(--text-primary);
        }
        .modal-content input,
        .modal-content select {
            background: var(--bg-input);
            color: var(--text-primary);
            border: 1px solid var(--border-light);
        }
        .modal-buttons {
            margin-top: 20px;
            text-align: right;
        }
        .modal-btn {
            padding: 8px 16px;
            margin-left: 8px;
            border: none;
            border-radius: 4px;
            cursor: pointer;
            font-size: 14px;
        }
        .modal-btn-primary {
            background: #1976d2;
            color: white;
        }
        .modal-btn-primary:hover { background: #1565c0; }
        .modal-btn-secondary {
            background: #ccc;
            color: var(--text-primary);
        }
        .modal-btn-secondary:hover { background: #bbb; }
        .modal-btn-danger {
            background: #f44336;
            color: white;
        }
        .modal-btn-danger:hover { background: #d32f2f; }
        .plugin-list {
            margin: 15px 0;
        }
        .plugin-option {
            padding: 10px;
            margin: 5px 0;
            border: 1px solid var(--border-light);
            border-radius: 4px;
            cursor: pointer;
            background: var(--bg-card);
        }
        .plugin-option:hover { background: var(--bg-secondary); }
        .plugin-option.disabled {
            background: var(--bg-secondary);
            color: #999;
            cursor: not-allowed;
        }
        .plugin-option.disabled:hover { background: var(--bg-secondary); }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <div class="header-content">
                <h1>BLE-MQTT Plugin Bridge</h1>
                <div class="version">Version ${BuildConfig.VERSION_NAME} (${BuildConfig.VERSION_CODE})</div>
            </div>
            <button id="theme-toggle" class="theme-toggle" onclick="toggleTheme()">🌙 Dark Mode</button>
        </div>

        <div class="card">
            <h2>Service Status</h2>
            <div id="service-status" class="loading">Loading...</div>
        </div>

        <div class="card">
            <h2>MQTT Configuration</h2>
            <div class="section-helper">Note: MQTT service must be stopped to edit the MQTT configuration</div>
            <div id="config-info" class="loading">Loading...</div>
        </div>

        <div class="card">
            <h2>Plugin Instances</h2>
            <div class="section-helper">Note: Service must be stopped to add/remove/edit plugin instances</div>
            <button id="add-instance-btn" class="add-instance-btn" onclick="showAddInstanceDialog()" disabled style="margin-bottom: 15px;">+ Add Plugin Instance</button>
            <div id="plugin-status" class="loading">Loading...</div>
        </div>

        <!-- Add Instance Modal -->
        <div id="addInstanceModal" class="modal">
            <div class="modal-content">
                <h3>Add Plugin Instance</h3>
                <div style="margin-bottom: 15px;">
                    <label style="display: block; margin-bottom: 5px; font-weight: 500;">Plugin Type:</label>
                    <select id="new-plugin-type" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
                        <option value="">-- Select Plugin Type --</option>
                        <option value="onecontrol" data-multi="false">OneControl (LCI RV control system)</option>
                        <option value="easytouch" data-multi="true">EasyTouch (Micro-Air thermostat) - Supports multiple</option>
                        <option value="gopower" data-multi="false">GoPower (Solar controller)</option>
                        <option value="hughes_watchdog" data-multi="false">Hughes Power Watchdog (Surge protector)</option>
                        <option value="mopeka" data-multi="true">Mopeka (Tank level sensor) - Supports multiple</option>
                        <option value="blescanner" data-multi="false">BLE Scanner (Generic BLE device scanner)</option>
                    </select>
                    <div id="multi-instance-warning" style="display: none; margin-top: 8px; padding: 8px; background: #fff3cd; border: 1px solid #ffc107; border-radius: 4px; font-size: 13px; color: #856404;">
                        ⚠️ This plugin type already exists and does not support multiple instances.
                    </div>
                </div>
                <div style="margin-bottom: 15px;">
                    <label style="display: block; margin-bottom: 5px; font-weight: 500;">Display Name:</label>
                    <input type="text" id="new-display-name" placeholder="e.g., Living Room Thermostat" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
                </div>
                <div id="new-mac-container" style="margin-bottom: 15px;">
                    <label style="display: block; margin-bottom: 5px; font-weight: 500;">Device MAC Address:</label>
                    <input type="text" id="new-device-mac" placeholder="e.g., AA:BB:CC:DD:EE:FF" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
                </div>
                <div id="plugin-specific-fields"></div>
                <div class="modal-buttons">
                    <button class="modal-btn modal-btn-secondary" onclick="closeAddInstanceDialog()">Cancel</button>
                    <button class="modal-btn modal-btn-primary" onclick="confirmAddInstance()">Add Instance</button>
                </div>
            </div>
        </div>

        <!-- Edit Instance Modal -->
        <div id="editInstanceModal" class="modal">
            <div class="modal-content">
                <h3>Edit Instance</h3>
                <input type="hidden" id="edit-instance-id">
                <input type="hidden" id="edit-plugin-type">
                <div style="margin-bottom: 15px;">
                    <label style="display: block; margin-bottom: 5px; font-weight: 500;">Display Name:</label>
                    <input type="text" id="edit-display-name" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
                </div>
                <div id="edit-mac-container" style="margin-bottom: 15px;">
                    <label style="display: block; margin-bottom: 5px; font-weight: 500;">Device MAC Address:</label>
                    <input type="text" id="edit-device-mac" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
                </div>
                <div id="edit-plugin-specific-fields"></div>
                <div class="modal-buttons">
                    <button class="modal-btn modal-btn-secondary" onclick="closeEditInstanceDialog()">Cancel</button>
                    <button class="modal-btn modal-btn-primary" onclick="confirmEditInstance()">Save Changes</button>
                </div>
            </div>
        </div>

        <!-- Confirm Remove Modal -->
        <div id="confirmRemoveModal" class="modal">
            <div class="modal-content">
                <h3>Remove Instance</h3>
                <p id="remove-message">Are you sure you want to remove this instance?</p>
                <div class="modal-buttons">
                    <button class="modal-btn modal-btn-secondary" onclick="closeRemoveDialog()">Cancel</button>
                    <button class="modal-btn modal-btn-danger" onclick="confirmRemove()">Remove</button>
                </div>
            </div>
        </div>

        <!-- Configuration Backup/Restore -->
        <div class="card">
            <h2>Configuration Backup & Restore</h2>
            <p style="color: var(--text-secondary); margin-bottom: 15px; font-size: 14px;">
                Export all settings and plugin configurations to a JSON file, or restore from a previous backup.
            </p>
            
            <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 10px; margin-bottom: 15px;">
                <button onclick="exportConfig()" style="background: #4caf50; color: white; padding: 10px; border: none; border-radius: 4px; cursor: pointer; font-size: 14px; font-weight: 500;">
                    📥 Export Configuration
                </button>
                <button onclick="document.getElementById('importFile').click()" style="background: #2196f3; color: white; padding: 10px; border: none; border-radius: 4px; cursor: pointer; font-size: 14px; font-weight: 500;">
                    📤 Import Configuration
                </button>
            </div>
            
            <input type="file" id="importFile" accept=".json" style="display: none;" onchange="handleFileImport(event)">
            
            <div id="importStatus" style="display: none; margin-top: 15px; padding: 12px; border-radius: 4px; font-size: 14px;"></div>
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

        <!-- Android TV Power Fix Section -->
        <div class="card">
            <div style="cursor: pointer; display: flex; justify-content: space-between; align-items: center;" onclick="toggleTvFixSection()">
                <h2 style="margin: 0;">Android TV Power Fix</h2>
                <span id="tv-fix-toggle" style="font-size: 20px; transition: transform 0.3s;">▶</span>
            </div>
            <div id="tv-fix-content" style="display: none; margin-top: 20px;">
                <h3 style="margin-top: 0; margin-bottom: 10px;">HDMI-CEC Auto Device Off</h3>
                
                <p style="line-height: 1.6; margin-bottom: 15px;">
                    When enabled, the TV can put this device to sleep via HDMI-CEC, which kills the service. 
                    Disable this to keep the service running when the TV powers off.
                </p>
                
                <div style="background: #fff3cd; border-left: 4px solid #ffc107; padding: 12px; margin-bottom: 15px; border-radius: 4px;">
                    <div style="font-weight: 500; color: #856404; margin-bottom: 5px;">
                        ⚠️ CEC Auto-Off Enabled
                    </div>
                    <div style="color: #856404; font-size: 14px;">
                        Service will be killed when TV sleeps
                    </div>
                </div>
                
                <div style="background: #fff3cd; border-left: 4px solid #ffc107; padding: 12px; margin-bottom: 20px; border-radius: 4px;">
                    <div style="font-weight: 500; color: #856404; margin-bottom: 10px;">
                        ⚠️ Permission Required
                    </div>
                    <div style="color: #856404; font-size: 14px; line-height: 1.6;">
                        To enable automatic CEC control, grant WRITE_SECURE_SETTINGS via ADB (one-time setup):
                    </div>
                </div>
                
                <div style="background: #1e1e1e; color: #d4d4d4; padding: 15px; border-radius: 4px; font-family: 'Courier New', monospace; font-size: 13px; position: relative; margin-bottom: 15px; overflow-x: auto;">
                    <button onclick="copyToClipboard('adb shell pm grant com.blemqttbridge android.permission.WRITE_SECURE_SETTINGS')" 
                            style="position: absolute; right: 10px; top: 10px; background: #007acc; color: white; border: none; padding: 5px 10px; border-radius: 3px; cursor: pointer; font-size: 12px;">
                        Copy
                    </button>
                    <div style="padding-right: 70px; word-wrap: break-word;">adb shell pm grant com.blemqttbridge android.permission.WRITE_SECURE_SETTINGS</div>
                </div>
                
                <p style="line-height: 1.6; margin-bottom: 10px; font-size: 14px; color: #666;">
                    After granting permission, restart the app. The service will automatically disable CEC auto-off on startup.
                </p>
                
                <h4 style="margin-top: 25px; margin-bottom: 10px;">Alternative: Disable CEC directly via ADB:</h4>
                
                <div style="background: #1e1e1e; color: #d4d4d4; padding: 15px; border-radius: 4px; font-family: 'Courier New', monospace; font-size: 13px; position: relative; overflow-x: auto;">
                    <button onclick="copyToClipboard('adb shell settings put global hdmi_control_auto_device_off_enabled 0')" 
                            style="position: absolute; right: 10px; top: 10px; background: #007acc; color: white; border: none; padding: 5px 10px; border-radius: 3px; cursor: pointer; font-size: 12px;">
                        Copy
                    </button>
                    <div style="padding-right: 70px; word-wrap: break-word;">adb shell settings put global hdmi_control_auto_device_off_enabled 0</div>
                </div>
            </div>
        </div>
    </div>

    <script>
        // Theme management
        function initTheme() {
            const savedTheme = localStorage.getItem('theme') || 'light';
            document.documentElement.setAttribute('data-theme', savedTheme);
            updateThemeButton(savedTheme);
        }

        function toggleTheme() {
            const current = document.documentElement.getAttribute('data-theme') || 'light';
            const newTheme = current === 'light' ? 'dark' : 'light';
            document.documentElement.setAttribute('data-theme', newTheme);
            localStorage.setItem('theme', newTheme);
            updateThemeButton(newTheme);
        }

        function updateThemeButton(theme) {
            const btn = document.getElementById('theme-toggle');
            if (btn) {
                btn.textContent = theme === 'dark' ? '☀️ Light Mode' : '🌙 Dark Mode';
            }
        }

        // Initialize theme on page load
        initTheme();

        // Global state
        let serviceRunning = false;
        let configChanged = {}; // Track which configs have changed
        let editingFields = {}; // Track which fields are currently being edited
        let instanceToRemove = null;
        
        const PLUGIN_TYPE_NAMES = {
            'onecontrol': 'OneControl',
            'easytouch': 'EasyTouch',
            'gopower': 'GoPower',
            'hughes_watchdog': 'Hughes Watchdog',
            'mopeka': 'Mopeka',
            'blescanner': 'BLE Scanner'
        };
        
        const MULTI_INSTANCE_PLUGINS = ['easytouch', 'mopeka']; // Plugins supporting multiple instances
        
        // Load status on page load
        window.addEventListener('load', () => {
            loadStatus();
            loadConfig();
            loadInstances();
            // Auto-refresh status every 5 seconds
            setInterval(() => {
                loadStatus();
                // Only refresh instances if not currently editing
                if (Object.keys(editingFields).length === 0) {
                    loadInstances();
                }
            }, 5000);
        });

        async function loadStatus() {
            try {
                const response = await fetch('/api/status');
                const data = await response.json();
                serviceRunning = data.running;
                const html = ${'`'}
                    <div class="status-row">
                        <span class="status-label">BLE Bridge Service:</span>
                        <label class="toggle-switch">
                            <input type="checkbox" ${'$'}{data.running ? 'checked' : ''} onchange="toggleService(this.checked)">
                            <span class="toggle-slider"></span>
                        </label>
                    </div>
                    <div class="status-row">
                        <span class="status-label">MQTT Output Service:</span>
                        <label class="toggle-switch">
                            <input type="checkbox" ${'$'}{data.mqttEnabled ? 'checked' : ''} onchange="toggleMqtt(this.checked)">
                            <span class="toggle-slider"></span>
                        </label>
                    </div>
                ${'`'};
                document.getElementById('service-status').innerHTML = html;
                
                // Update add instance button state
                const addInstanceBtn = document.getElementById('add-instance-btn');
                if (addInstanceBtn) {
                    addInstanceBtn.disabled = serviceRunning;
                }
            } catch (error) {
                document.getElementById('service-status').innerHTML = 
                    '<div style="color: #f44336;">Failed to load status</div>';
            }
        }

        async function loadConfig() {
            try {
                const statusResponse = await fetch('/api/status');
                const statusData = await statusResponse.json();
                const mqttRunning = statusData.mqttEnabled; // Use MQTT enabled setting
                
                const response = await fetch('/api/config');
                const data = await response.json();
                const editDisabled = mqttRunning ? 'disabled' : '';
                
                // Fix: Check both mqttEnabled AND mqttConnected for health indicator
                const mqttConnected = statusData.mqttEnabled && statusData.mqttConnected;
                const mqttClass = mqttConnected ? 'mqtt-connected' : 'mqtt-disconnected';
                const html = ${'`'}
                    <div class="mqtt-config-item ${'$'}{mqttClass}">
                        <div class="mqtt-config-field">${'$'}{buildEditableField('mqtt', 'broker', 'MQTT Broker', data.mqttBroker, editDisabled, false)}</div>
                        <div class="mqtt-config-field">${'$'}{buildEditableField('mqtt', 'port', 'MQTT Port', data.mqttPort, editDisabled, false)}</div>
                        <div class="mqtt-config-field">${'$'}{buildEditableField('mqtt', 'topicPrefix', 'Topic Prefix', data.mqttTopicPrefix, editDisabled, false, 'Default: homeassistant (for Home Assistant discovery)')}</div>
                        <div class="mqtt-config-field">${'$'}{buildEditableField('mqtt', 'username', 'MQTT Username', data.mqttUsername, editDisabled, false)}</div>
                        <div class="mqtt-config-field">${'$'}{buildEditableField('mqtt', 'password', 'MQTT Password', data.mqttPassword, editDisabled, true)}</div>
                    </div>
                ${'`'};
                document.getElementById('config-info').innerHTML = html;
            } catch (error) {
                document.getElementById('config-info').innerHTML = 
                    '<div style="color: #f44336;">Failed to load configuration</div>';
            }
        }

        async function loadInstances() {
            try {
                const [instancesResponse, statusResponse] = await Promise.all([
                    fetch('/api/instances'),
                    fetch('/api/status')
                ]);
                const instances = await instancesResponse.json();
                const statusData = await statusResponse.json();
                
                // Check if service is running
                const serviceRunning = statusData.running || false;
                
                // Get plugin statuses
                const pluginStatuses = {};
                for (const instance of instances) {
                    const status = statusData.pluginStatuses?.[instance.instanceId] || 
                                   statusData.pluginStatuses?.[instance.pluginType] || {};
                    pluginStatuses[instance.instanceId] = status;
                }
                
                // Group instances by plugin type
                const grouped = {};
                for (const instance of instances) {
                    if (!grouped[instance.pluginType]) {
                        grouped[instance.pluginType] = [];
                    }
                    grouped[instance.pluginType].push(instance);
                }
                
                let html = '';
                
                // Get saved plugin order from localStorage (or use default alphabetical)
                const savedOrder = JSON.parse(localStorage.getItem('pluginOrder') || '[]');
                const pluginTypes = Object.keys(grouped);
                
                // Sort by saved order, then alphabetically for new plugins
                const sortedTypes = pluginTypes.sort((a, b) => {
                    const aIndex = savedOrder.indexOf(a);
                    const bIndex = savedOrder.indexOf(b);
                    if (aIndex !== -1 && bIndex !== -1) return aIndex - bIndex;
                    if (aIndex !== -1) return -1;
                    if (bIndex !== -1) return 1;
                    return a.localeCompare(b);
                });
                
                // Render each plugin type section
                for (const pluginType of sortedTypes) {
                    const typeInstances = grouped[pluginType];
                    const typeName = PLUGIN_TYPE_NAMES[pluginType] || pluginType;
                    
                    html += ${'`'}
                        <div class="plugin-type-section" draggable="true" data-plugin-type="${'$'}{pluginType}">
                            <div class="plugin-type-header">
                                <span class="plugin-type-title">⋮⋮ ${'$'}{typeName}</span>
                            </div>
                    ${'`'};
                    
                    // Render each instance
                    for (const instance of typeInstances) {
                        const status = pluginStatuses[instance.instanceId] || {};
                        const connected = status.connected || false;
                        const authenticated = status.authenticated || false;
                        const dataHealthy = status.dataHealthy || false;
                        
                        // Passive plugins (mopeka, gopower, blescanner) only need dataHealthy to be green
                        // Active plugins (onecontrol, easytouch) need full connection
                            const isPassive = pluginType === 'mopeka' || pluginType === 'gopower' || pluginType === 'blescanner' || pluginType === 'hughes_watchdog';
                        
                        // If service is stopped, all instances are unhealthy
                        const healthy = serviceRunning ? (isPassive ? dataHealthy : (connected && authenticated && dataHealthy)) : false;
                        
                        const healthClass = healthy ? 'instance-healthy' : 'instance-unhealthy';
                        const displayName = instance.displayName || instance.instanceId;
                        
                        // Get plugin-specific config
                        let configDetails = '';
                        if (pluginType === 'onecontrol') {
                            const pin = instance.config?.gateway_pin || 'Not set';
                            configDetails = ${'`'}<div class="instance-detail-line">PIN: ${'$'}{pin}</div>${'`'};
                        } else if (pluginType === 'easytouch') {
                            const hasPassword = instance.config?.password ? 'Set' : 'Not set';
                            configDetails = ${'`'}<div class="instance-detail-line">Password: ${'$'}{hasPassword}</div>${'`'};
                        } else if (pluginType === 'hughes_watchdog') {
                            const expectedName = instance.config?.expected_name || 'Any';
                            const forceVersion = instance.config?.force_version || 'Auto';
                            configDetails = ${'`'}<div class="instance-detail-line">Name: ${'$'}{expectedName} | Gen: ${'$'}{forceVersion}</div>${'`'};
                        } else if (pluginType === 'mopeka') {
                            const mediumType = instance.config?.medium_type || 'propane';
                            const tankType = instance.config?.tank_type || '20lb_v';
                            configDetails = ${'`'}<div class="instance-detail-line">Medium: ${'$'}{mediumType} | Tank: ${'$'}{tankType}</div>${'`'};
                        }
                        
                        html += ${'`'}
                            <div class="instance-card ${'$'}{healthClass}">
                                <div class="instance-header">
                                    <div>
                                        <span class="instance-name">${'$'}{displayName}</span>
                                    </div>
                                    <div class="instance-actions">
                                        <button class="instance-edit-btn" onclick="showEditInstanceDialog('${'$'}{instance.instanceId}')" ${'$'}{serviceRunning ? 'disabled' : ''}>
                                            Edit
                                        </button>
                                        <button class="instance-remove-btn" onclick="showRemoveInstanceDialog('${'$'}{instance.instanceId}', '${'$'}{displayName}')" ${'$'}{serviceRunning ? 'disabled' : ''}>
                                            Remove
                                        </button>
                                    </div>
                                </div>
                                <div class="instance-details">
                                    <div class="instance-detail-line">ID: ${'$'}{instance.instanceId}</div>
                                    ${'$'}{pluginType !== 'blescanner' ? `<div class="instance-detail-line">MAC: ${'$'}{instance.deviceMac}</div>` : ''}
                                    ${'$'}{configDetails}
                                    ${'$'}{!isPassive ? `<div class="instance-detail-line">
                                        Connected: <span class="${'$'}{connected ? 'plugin-healthy' : 'plugin-unhealthy'}">${'$'}{connected ? 'Yes' : 'No'}</span>
                                        | Authenticated: <span class="${'$'}{authenticated ? 'plugin-healthy' : 'plugin-unhealthy'}">${'$'}{authenticated ? 'Yes' : 'No'}</span>
                                        | Data: <span class="${'$'}{dataHealthy ? 'plugin-healthy' : 'plugin-unhealthy'}">${'$'}{dataHealthy ? 'Healthy' : 'Unhealthy'}</span>
                                    </div>` : ''}
                                </div>
                            </div>
                        ${'`'};
                    }
                    
                    html += '</div>';
                }
                
                if (html === '') {
                    html = '<div style="padding: 20px; text-align: center; color: #666;">No plugin instances configured.<br><br><button class="add-instance-btn" onclick="showAddInstanceDialog()" ' + (serviceRunning ? 'disabled' : '') + '>Add First Instance</button></div>';
                }
                
                document.getElementById('plugin-status').innerHTML = html;
                
                // Setup drag-and-drop for plugin sections
                setupDragAndDrop();
            } catch (error) {
                console.error('Failed to load instances:', error);
                document.getElementById('plugin-status').innerHTML = 
                    '<div style="color: #f44336;">Failed to load plugin instances. Ensure the app and web service are running.</div>';
            }
        }
        
        function setupDragAndDrop() {
            const sections = document.querySelectorAll('.plugin-type-section');
            let draggedElement = null;
            
            sections.forEach(section => {
                section.addEventListener('dragstart', (e) => {
                    draggedElement = section;
                    section.classList.add('dragging');
                    e.dataTransfer.effectAllowed = 'move';
                });
                
                section.addEventListener('dragend', (e) => {
                    section.classList.remove('dragging');
                    sections.forEach(s => s.classList.remove('drag-over'));
                    
                    // Save new order to localStorage
                    const newOrder = Array.from(document.querySelectorAll('.plugin-type-section'))
                        .map(s => s.dataset.pluginType);
                    localStorage.setItem('pluginOrder', JSON.stringify(newOrder));
                });
                
                section.addEventListener('dragover', (e) => {
                    e.preventDefault();
                    e.dataTransfer.dropEffect = 'move';
                    
                    if (draggedElement && draggedElement !== section) {
                        section.classList.add('drag-over');
                        
                        const container = section.parentNode;
                        const afterElement = getDragAfterElement(container, e.clientY);
                        
                        if (afterElement == null) {
                            container.appendChild(draggedElement);
                        } else {
                            container.insertBefore(draggedElement, afterElement);
                        }
                    }
                });
                
                section.addEventListener('dragleave', (e) => {
                    section.classList.remove('drag-over');
                });
                
                section.addEventListener('drop', (e) => {
                    e.preventDefault();
                    section.classList.remove('drag-over');
                });
            });
        }
        
        function getDragAfterElement(container, y) {
            const draggableElements = [...container.querySelectorAll('.plugin-type-section:not(.dragging)')];
            
            return draggableElements.reduce((closest, child) => {
                const box = child.getBoundingClientRect();
                const offset = y - box.top - box.height / 2;
                
                if (offset < 0 && offset > closest.offset) {
                    return { offset: offset, element: child };
                } else {
                    return closest;
                }
            }, { offset: Number.NEGATIVE_INFINITY }).element;
        }

        async function showAddInstanceDialog(pluginType = '') {
            // Reset form
            document.getElementById('new-plugin-type').value = pluginType;
            document.getElementById('new-display-name').value = '';
            document.getElementById('new-device-mac').value = '';
            document.getElementById('multi-instance-warning').style.display = 'none';
            updatePluginSpecificFields();
            
            // Fetch current instances to check which plugin types exist
            try {
                const response = await fetch('/api/instances');
                const instances = await response.json();
                const existingTypes = [...new Set(instances.map(i => i.pluginType))];
                
                // Update select options based on existing instances
                const select = document.getElementById('new-plugin-type');
                for (const option of select.options) {
                    if (option.value) {
                        const isMulti = option.getAttribute('data-multi') === 'true';
                        const alreadyExists = existingTypes.includes(option.value);
                        
                        // Disable if it already exists and doesn't support multiple instances
                        if (alreadyExists && !isMulti) {
                            option.disabled = true;
                            option.text = option.text.replace(' - Supports multiple', '') + ' (Already configured)';
                        } else {
                            option.disabled = false;
                        }
                    }
                }
            } catch (error) {
                console.error('Failed to load instances:', error);
            }
            
            document.getElementById('addInstanceModal').style.display = 'block';
        }

        function closeAddInstanceDialog() {
            document.getElementById('addInstanceModal').style.display = 'none';
        }

        async function confirmAddInstance() {
            const pluginType = document.getElementById('new-plugin-type').value;
            const displayName = document.getElementById('new-display-name').value.trim();
            const deviceMac = document.getElementById('new-device-mac').value.trim().toUpperCase();
            
            if (!pluginType) {
                alert('Please select a plugin type');
                return;
            }
            
            // Check if this plugin type already exists and doesn't support multiple instances
            const response = await fetch('/api/instances');
            const instances = await response.json();
            const existingTypes = instances.map(i => i.pluginType);
            const supportsMultiple = MULTI_INSTANCE_PLUGINS.includes(pluginType);
            
            if (existingTypes.includes(pluginType) && !supportsMultiple) {
                alert('This plugin type already exists and does not support multiple instances. Please edit the existing instance instead.');
                return;
            }
            
            if (!displayName) {
                alert('Please enter a display name');
                return;
            }
            if (!deviceMac && pluginType !== 'blescanner') {
                alert('Please enter a device MAC address');
                return;
            }
            
            // Collect plugin-specific config
            const config = {};
            if (pluginType === 'onecontrol') {
                const pin = document.getElementById('new-gateway-pin')?.value.trim();
                if (pin) config.gateway_pin = pin;
            } else if (pluginType === 'easytouch') {
                const password = document.getElementById('new-password')?.value.trim();
                if (password) config.password = password;
            } else if (pluginType === 'hughes_watchdog') {
                const expectedName = document.getElementById('new-expected-name')?.value.trim();
                const forceVersion = document.getElementById('new-force-version')?.value;
                if (expectedName) config.expected_name = expectedName;
                if (forceVersion && forceVersion !== 'auto') config.force_version = forceVersion;
            } else if (pluginType === 'mopeka') {
                const mediumType = document.getElementById('new-medium-type')?.value || 'propane';
                const tankType = document.getElementById('new-tank-type')?.value || '20lb_v';
                config.medium_type = mediumType;
                config.tank_type = tankType;
            }
            
            try {
                const response = await fetch('/api/instances/add', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        pluginType: pluginType,
                        displayName: displayName,
                        deviceMac: deviceMac || '',
                        config: config
                    })
                });
                const result = await response.json();
                if (result.success) {
                    closeAddInstanceDialog();
                    loadInstances();
                } else {
                    alert('Failed to add instance: ' + (result.error || 'Unknown error'));
                }
            } catch (error) {
                alert('Error adding instance: ' + error.message);
            }
        }

        async function showEditInstanceDialog(instanceId) {
            try {
                const response = await fetch('/api/instances');
                const instances = await response.json();
                const instance = instances.find(i => i.instanceId === instanceId);
                
                if (!instance) {
                    alert('Instance not found');
                    return;
                }
                
                document.getElementById('edit-instance-id').value = instanceId;
                document.getElementById('edit-plugin-type').value = instance.pluginType;
                document.getElementById('edit-display-name').value = instance.displayName || '';
                document.getElementById('edit-device-mac').value = instance.deviceMac || '';
                
                // Hide MAC field for BLE Scanner
                const macContainer = document.getElementById('edit-mac-container');
                if (macContainer) {
                    macContainer.style.display = (instance.pluginType === 'blescanner') ? 'none' : 'block';
                }
                
                // Populate plugin-specific fields
                updateEditPluginSpecificFields(instance.pluginType, instance.config);
                
                document.getElementById('editInstanceModal').style.display = 'block';
            } catch (error) {
                alert('Error loading instance: ' + error.message);
            }
        }

        function closeEditInstanceDialog() {
            document.getElementById('editInstanceModal').style.display = 'none';
        }

        async function confirmEditInstance() {
            const instanceId = document.getElementById('edit-instance-id').value;
            const pluginType = document.getElementById('edit-plugin-type').value;
            const displayName = document.getElementById('edit-display-name').value.trim();
            const deviceMac = document.getElementById('edit-device-mac').value.trim().toUpperCase();
            
            if (!displayName) {
                alert('Please enter a display name');
                return;
            }
            if (!deviceMac && pluginType !== 'blescanner') {
                alert('Please enter a device MAC address');
                return;
            }
            
            // Collect plugin-specific config
            const config = {};
            const pinField = document.getElementById('edit-gateway-pin');
            const passwordField = document.getElementById('edit-password');
            const expectedNameField = document.getElementById('edit-expected-name');
            const forceVersionField = document.getElementById('edit-force-version');
            const mediumTypeField = document.getElementById('edit-medium-type');
            const tankTypeField = document.getElementById('edit-tank-type');
            
            if (pinField) {
                const pin = pinField.value.trim();
                if (pin) config.gateway_pin = pin;
            }
            if (passwordField) {
                const password = passwordField.value.trim();
                if (password) config.password = password;
            }
            if (expectedNameField) {
                const expectedName = expectedNameField.value.trim();
                if (expectedName) config.expected_name = expectedName;
            }
            if (forceVersionField) {
                const forceVersion = forceVersionField.value;
                if (forceVersion && forceVersion !== 'auto') config.force_version = forceVersion;
            }
            if (mediumTypeField) {
                config.medium_type = mediumTypeField.value || 'propane';
            }
            if (tankTypeField) {
                config.tank_type = tankTypeField.value || '20lb_v';
            }
            
            try {
                const response = await fetch('/api/instances/update', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        instanceId: instanceId,
                        displayName: displayName,
                        deviceMac: deviceMac,
                        config: config
                    })
                });
                const result = await response.json();
                if (result.success) {
                    closeEditInstanceDialog();
                    loadInstances();
                } else {
                    alert('Failed to update instance: ' + (result.error || 'Unknown error'));
                }
            } catch (error) {
                alert('Error updating instance: ' + error.message);
            }
        }

        function showRemoveInstanceDialog(instanceId, displayName) {
            instanceToRemove = instanceId;
            document.getElementById('remove-message').textContent = 
                ${'`'}Are you sure you want to remove "${'$'}{displayName}"?${'`'};
            document.getElementById('confirmRemoveModal').style.display = 'block';
        }

        function closeRemoveDialog() {
            instanceToRemove = null;
            document.getElementById('confirmRemoveModal').style.display = 'none';
        }

        async function confirmRemove() {
            if (!instanceToRemove) return;
            
            try {
                const response = await fetch('/api/instances/remove', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ instanceId: instanceToRemove })
                });
                const result = await response.json();
                if (result.success) {
                    closeRemoveDialog();
                    loadInstances();
                } else {
                    alert('Failed to remove instance: ' + (result.error || 'Unknown error'));
                }
            } catch (error) {
                alert('Error removing instance: ' + error.message);
            }
        }

        // Update plugin-specific fields in add dialog
        document.getElementById('new-plugin-type')?.addEventListener('change', updatePluginSpecificFields);
        
        function updatePluginSpecificFields() {
            const pluginType = document.getElementById('new-plugin-type').value;
            const container = document.getElementById('plugin-specific-fields');
            const macContainer = document.getElementById('new-mac-container');
            
            if (!container) return;
            
            // Hide MAC address field for BLE Scanner
            if (macContainer) {
                macContainer.style.display = (pluginType === 'blescanner') ? 'none' : 'block';
            }
            
            if (pluginType === 'onecontrol') {
                container.innerHTML = ${'`'}
                    <div style="margin-bottom: 15px;">
                        <label style="display: block; margin-bottom: 5px; font-weight: 500;">Gateway PIN:</label>
                        <input type="text" id="new-gateway-pin" placeholder="e.g., 1234" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
                    </div>
                ${'`'};
            } else if (pluginType === 'easytouch') {
                container.innerHTML = ${'`'}
                    <div style="margin-bottom: 15px;">
                        <label style="display: block; margin-bottom: 5px; font-weight: 500;">Password:</label>
                        <input type="password" id="new-password" placeholder="Device password" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
                    </div>
                ${'`'};
            } else if (pluginType === 'hughes_watchdog') {
                container.innerHTML = ${'`'}
                    <div style="margin-bottom: 15px;">
                        <label style="display: block; margin-bottom: 5px; font-weight: 500;">Expected Device Name (optional):</label>
                        <input type="text" id="new-expected-name" placeholder="e.g., PWS123 (leave empty for any)" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
                    </div>
                    <div style="margin-bottom: 15px;">
                        <label style="display: block; margin-bottom: 5px; font-weight: 500;">Force Generation (optional):</label>
                        <select id="new-force-version" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
                            <option value="auto">Auto-detect</option>
                            <option value="gen1">Gen 1 (E2)</option>
                            <option value="gen2">Gen 2+ (E3/E4)</option>
                        </select>
                    </div>
                ${'`'};
            } else if (pluginType === 'mopeka') {
                container.innerHTML = ${'`'}
                    <div style="margin-bottom: 15px;">
                        <label style="display: block; margin-bottom: 5px; font-weight: 500;">Medium Type:</label>
                        <select id="new-medium-type" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
                            <option value="propane">Propane</option>
                            <option value="air">Air (Tank Ratio)</option>
                            <option value="fresh_water">Fresh Water</option>
                            <option value="waste_water">Waste Water</option>
                            <option value="black_water">Black Water</option>
                            <option value="live_well">Live Well</option>
                            <option value="gasoline">Gasoline</option>
                            <option value="diesel">Diesel</option>
                            <option value="lng">LNG</option>
                            <option value="oil">Oil</option>
                            <option value="hydraulic_oil">Hydraulic Oil</option>
                            <option value="custom">Custom</option>
                        </select>
                    </div>
                    <div style="margin-bottom: 15px;">
                        <label style="display: block; margin-bottom: 5px; font-weight: 500;">Tank Type:</label>
                        <select id="new-tank-type" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
                            <optgroup label="Vertical Propane">
                                <option value="20lb_v">20lb Vertical</option>
                                <option value="30lb_v">30lb Vertical</option>
                                <option value="40lb_v">40lb Vertical</option>
                            </optgroup>
                            <optgroup label="Horizontal Propane">
                                <option value="250gal_h">250 Gallon Horizontal</option>
                                <option value="500gal_h">500 Gallon Horizontal</option>
                                <option value="1000gal_h">1000 Gallon Horizontal</option>
                            </optgroup>
                            <optgroup label="European">
                                <option value="europe_6kg">6kg European Vertical</option>
                                <option value="europe_11kg">11kg European Vertical</option>
                                <option value="europe_14kg">14kg European Vertical</option>
                            </optgroup>
                            <option value="custom">Custom Tank</option>
                        </select>
                    </div>
                ${'`'};
            } else {
                container.innerHTML = '';
            }
        }

        function updateEditPluginSpecificFields(pluginType, config) {
            const container = document.getElementById('edit-plugin-specific-fields');
            
            if (!container) return;
            
            if (pluginType === 'onecontrol') {
                const pin = config?.gateway_pin || '';
                container.innerHTML = ${'`'}
                    <div style="margin-bottom: 15px;">
                        <label style="display: block; margin-bottom: 5px; font-weight: 500;">Gateway PIN:</label>
                        <input type="text" id="edit-gateway-pin" value="${'$'}{pin}" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
                    </div>
                ${'`'};
            } else if (pluginType === 'easytouch') {
                const password = config?.password || '';
                container.innerHTML = ${'`'}
                    <div style="margin-bottom: 15px;">
                        <label style="display: block; margin-bottom: 5px; font-weight: 500;">Password:</label>
                        <input type="password" id="edit-password" value="${'$'}{password}" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
                    </div>
                ${'`'};
            } else if (pluginType === 'hughes_watchdog') {
                const expectedName = config?.expected_name || '';
                const forceVersion = config?.force_version || 'auto';
                container.innerHTML = ${'`'}
                    <div style="margin-bottom: 15px;">
                        <label style="display: block; margin-bottom: 5px; font-weight: 500;">Expected Device Name (optional):</label>
                        <input type="text" id="edit-expected-name" value="${'$'}{expectedName}" placeholder="e.g., PWS123 (leave empty for any)" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
                    </div>
                    <div style="margin-bottom: 15px;">
                        <label style="display: block; margin-bottom: 5px; font-weight: 500;">Force Generation (optional):</label>
                        <select id="edit-force-version" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
                            <option value="auto" ${'$'}{forceVersion === 'auto' ? 'selected' : ''}>Auto-detect</option>
                            <option value="gen1" ${'$'}{forceVersion === 'gen1' ? 'selected' : ''}>Gen 1 (E2)</option>
                            <option value="gen2" ${'$'}{forceVersion === 'gen2' ? 'selected' : ''}>Gen 2+ (E3/E4)</option>
                        </select>
                    </div>
                ${'`'};
            } else if (pluginType === 'mopeka') {
                const mediumType = config?.medium_type || 'propane';
                const tankType = config?.tank_type || '20lb_v';
                container.innerHTML = ${'`'}
                    <div style="margin-bottom: 15px;">
                        <label style="display: block; margin-bottom: 5px; font-weight: 500;">Medium Type:</label>
                        <select id="edit-medium-type" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
                            <option value="propane" ${'$'}{mediumType === 'propane' ? 'selected' : ''}>Propane</option>
                            <option value="air" ${'$'}{mediumType === 'air' ? 'selected' : ''}>Air (Tank Ratio)</option>
                            <option value="fresh_water" ${'$'}{mediumType === 'fresh_water' ? 'selected' : ''}>Fresh Water</option>
                            <option value="waste_water" ${'$'}{mediumType === 'waste_water' ? 'selected' : ''}>Waste Water</option>
                            <option value="black_water" ${'$'}{mediumType === 'black_water' ? 'selected' : ''}>Black Water</option>
                            <option value="live_well" ${'$'}{mediumType === 'live_well' ? 'selected' : ''}>Live Well</option>
                            <option value="gasoline" ${'$'}{mediumType === 'gasoline' ? 'selected' : ''}>Gasoline</option>
                            <option value="diesel" ${'$'}{mediumType === 'diesel' ? 'selected' : ''}>Diesel</option>
                            <option value="lng" ${'$'}{mediumType === 'lng' ? 'selected' : ''}>LNG</option>
                            <option value="oil" ${'$'}{mediumType === 'oil' ? 'selected' : ''}>Oil</option>
                            <option value="hydraulic_oil" ${'$'}{mediumType === 'hydraulic_oil' ? 'selected' : ''}>Hydraulic Oil</option>
                            <option value="custom" ${'$'}{mediumType === 'custom' ? 'selected' : ''}>Custom</option>
                        </select>
                    </div>
                    <div style="margin-bottom: 15px;">
                        <label style="display: block; margin-bottom: 5px; font-weight: 500;">Tank Type:</label>
                        <select id="edit-tank-type" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
                            <optgroup label="Vertical Propane">
                                <option value="20lb_v" ${'$'}{tankType === '20lb_v' ? 'selected' : ''}>20lb Vertical</option>
                                <option value="30lb_v" ${'$'}{tankType === '30lb_v' ? 'selected' : ''}>30lb Vertical</option>
                                <option value="40lb_v" ${'$'}{tankType === '40lb_v' ? 'selected' : ''}>40lb Vertical</option>
                            </optgroup>
                            <optgroup label="Horizontal Propane">
                                <option value="250gal_h" ${'$'}{tankType === '250gal_h' ? 'selected' : ''}>250 Gallon Horizontal</option>
                                <option value="500gal_h" ${'$'}{tankType === '500gal_h' ? 'selected' : ''}>500 Gallon Horizontal</option>
                                <option value="1000gal_h" ${'$'}{tankType === '1000gal_h' ? 'selected' : ''}>1000 Gallon Horizontal</option>
                            </optgroup>
                            <optgroup label="European">
                                <option value="europe_6kg" ${'$'}{tankType === 'europe_6kg' ? 'selected' : ''}>6kg European Vertical</option>
                                <option value="europe_11kg" ${'$'}{tankType === 'europe_11kg' ? 'selected' : ''}>11kg European Vertical</option>
                                <option value="europe_14kg" ${'$'}{tankType === 'europe_14kg' ? 'selected' : ''}>14kg European Vertical</option>
                            </optgroup>
                            <option value="custom" ${'$'}{tankType === 'custom' ? 'selected' : ''}>Custom Tank</option>
                        </select>
                    </div>
                ${'`'};
            } else {
                container.innerHTML = '';
            }
        }

        function buildEditableField(pluginId, fieldName, label, value, editDisabled, isSecret, helperText) {
            const fieldId = ${'`'}${'$'}{pluginId}_${'$'}{fieldName}${'`'};
            const displayValue = value || 'None';
            const maskedValue = isSecret && value ? '•'.repeat(value.length) : displayValue;
            const helper = helperText ? ${'`'}<div style="font-size: 12px; color: #888; margin-top: 2px;">${'$'}{helperText}</div>${'`'} : '';
            
            return ${'`'}
                <div class="plugin-config-field">
                    ${'$'}{label}: 
                    <span id="${'$'}{fieldId}_display">${'$'}{maskedValue}</span>
                    <input type="text" id="${'$'}{fieldId}_input" class="config-input" value="${'$'}{value}" style="display:none;">
                    <button id="${'$'}{fieldId}_edit" class="edit-btn" ${'$'}{editDisabled} onclick="editField('${'$'}{pluginId}', '${'$'}{fieldName}', ${'$'}{isSecret})">Edit</button>
                    <button id="${'$'}{fieldId}_save" class="edit-btn save-btn" style="display:none;" onclick="saveField('${'$'}{pluginId}', '${'$'}{fieldName}')">Save</button>
                    ${'$'}{helper}
                </div>
            ${'`'};
        }

        function editField(pluginId, fieldName, isSecret) {
            const fieldId = ${'`'}${'$'}{pluginId}_${'$'}{fieldName}${'`'};
            editingFields[fieldId] = true;
            document.getElementById(${'`'}${'$'}{fieldId}_display${'`'}).style.display = 'none';
            document.getElementById(${'`'}${'$'}{fieldId}_input${'`'}).style.display = 'inline-block';
            document.getElementById(${'`'}${'$'}{fieldId}_edit${'`'}).style.display = 'none';
            document.getElementById(${'`'}${'$'}{fieldId}_save${'`'}).style.display = 'inline-block';
        }

        async function saveField(pluginId, fieldName) {
            const fieldId = ${'`'}${'$'}{pluginId}_${'$'}{fieldName}${'`'};
            const value = document.getElementById(${'`'}${'$'}{fieldId}_input${'`'}).value;
            
            try {
                const endpoint = pluginId === 'mqtt' ? '/api/config/mqtt' : '/api/config/plugin';
                const response = await fetch(endpoint, {
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
                    delete editingFields[fieldId];
                    if (pluginId === 'mqtt') {
                        loadConfig();
                    } else {
                        loadInstances();
                    }
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

        // Close modals when clicking outside
        window.onclick = function(event) {
            const addModal = document.getElementById('addInstanceModal');
            const editModal = document.getElementById('editInstanceModal');
            const removeModal = document.getElementById('confirmRemoveModal');
            if (event.target === addModal) {
                closeAddInstanceDialog();
            } else if (event.target === editModal) {
                closeEditInstanceDialog();
            } else if (event.target === removeModal) {
                closeRemoveDialog();
            }
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
                    loadStatus();
                } else if (enable) {
                    configChanged = {};
                }
            } catch (error) {
                alert('Error controlling service: ' + error.message);
                loadStatus();
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
                    loadStatus();
                    loadConfig();
                } else {
                    // When enabling MQTT, service restarts and needs time to reconnect
                    // Wait 2 seconds before refreshing UI to allow connection to establish
                    if (enable) {
                        await new Promise(resolve => setTimeout(resolve, 2000));
                    }
                    loadConfig();
                }
            } catch (error) {
                alert('Error controlling MQTT: ' + error.message);
                loadStatus();
                loadConfig();
            }
        }
        
        // Android TV Power Fix section toggle
        function toggleTvFixSection() {
            const content = document.getElementById('tv-fix-content');
            const toggle = document.getElementById('tv-fix-toggle');
            if (content.style.display === 'none') {
                content.style.display = 'block';
                toggle.textContent = '▼';
            } else {
                content.style.display = 'none';
                toggle.textContent = '▶';
            }
        }
        
        // Copy text to clipboard
        function copyToClipboard(text) {
            navigator.clipboard.writeText(text).then(() => {
                // Show brief success feedback
                const event = new Event('copied');
                event.text = text;
                window.dispatchEvent(event);
                
                // Simple visual feedback
                const allButtons = document.querySelectorAll('button');
                allButtons.forEach(btn => {
                    if (btn.textContent === 'Copy' && btn.onclick && btn.onclick.toString().includes(text.substring(0, 20))) {
                        const originalText = btn.textContent;
                        btn.textContent = 'Copied!';
                        btn.style.background = '#4CAF50';
                        setTimeout(() => {
                            btn.textContent = originalText;
                            btn.style.background = '#007acc';
                        }, 2000);
                    }
                });
            }).catch(err => {
                alert('Failed to copy: ' + err);
            });
        }

        // Configuration backup/restore functions
        async function exportConfig() {
            try {
                const response = await fetch('/api/config/export');
                if (!response.ok) {
                    throw new Error('Export failed: ' + response.statusText);
                }
                
                // Get the filename from Content-Disposition header if available
                const contentDisposition = response.headers.get('content-disposition');
                let filename = 'ble-bridge-backup.json';
                if (contentDisposition) {
                    const filenamePart = contentDisposition.split('filename=')[1];
                    if (filenamePart) {
                        filename = filenamePart.replace(/"/g, '');
                    }
                }
                
                // Create download link
                const blob = await response.blob();
                const url = window.URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = filename;
                document.body.appendChild(a);
                a.click();
                window.URL.revokeObjectURL(url);
                document.body.removeChild(a);
                
                showImportStatus('✅ Configuration exported successfully', 'success');
            } catch (err) {
                console.error('Export error:', err);
                showImportStatus('❌ Export failed: ' + err.message, 'error');
            }
        }

        function handleFileImport(event) {
            const file = event.target.files[0];
            if (!file) return;
            
            if (!file.name.endsWith('.json')) {
                showImportStatus('❌ Please select a valid JSON backup file', 'error');
                return;
            }
            
            const reader = new FileReader();
            reader.onload = async (e) => {
                try {
                    // Validate JSON
                    const backup = JSON.parse(e.target.result);
                    
                    // Show confirmation dialog
                    const confirmed = confirm(
                        'Import configuration from ' + file.name + '?\n\n' +
                        'Version: ' + (backup.appVersion || 'unknown') + '\n' +
                        'Exported: ' + (backup.exportedAt || 'unknown') + '\n\n' +
                        'This will overwrite your current configuration.\n\n' +
                        'Continue?'
                    );
                    
                    if (!confirmed) {
                        showImportStatus('Import cancelled', 'info');
                        return;
                    }
                    
                    // Send to server for import
                    const formData = new FormData();
                    formData.append('backup', new Blob([JSON.stringify(backup)], { type: 'application/json' }));
                    
                    showImportStatus('Importing...', 'info');
                    
                    const response = await fetch('/api/config/import?replace=true', {
                        method: 'POST',
                        body: JSON.stringify(backup)
                    });
                    
                    const result = await response.json();
                    
                    if (result.success) {
                        showImportStatus('✅ Configuration imported successfully.\n\nNote: Restart the service to apply changes.', 'success');
                        // Clear the file input
                        event.target.value = '';
                        
                        // Reload page after 2 seconds
                        setTimeout(() => {
                            location.reload();
                        }, 2000);
                    } else {
                        showImportStatus('❌ Import failed: ' + result.message, 'error');
                    }
                } catch (err) {
                    console.error('Import error:', err);
                    showImportStatus('❌ Invalid backup file or import error: ' + err.message, 'error');
                }
            };
            reader.readAsText(file);
        }

        function showImportStatus(message, type) {
            const statusDiv = document.getElementById('importStatus');
            statusDiv.style.display = 'block';
            statusDiv.textContent = message;
            
            // Set background color based on type
            if (type === 'success') {
                statusDiv.style.background = '#dff0d8';
                statusDiv.style.color = '#3c763d';
                statusDiv.style.border = '1px solid #d6e9c6';
            } else if (type === 'error') {
                statusDiv.style.background = '#f8d7da';
                statusDiv.style.color = '#721c24';
                statusDiv.style.border = '1px solid #f5c6cb';
            } else {
                statusDiv.style.background = '#d1ecf1';
                statusDiv.style.color = '#0c5460';
                statusDiv.style.border = '1px solid #bee5eb';
            }
            
            // Auto-hide success messages after 5 seconds
            if (type === 'success' || type === 'info') {
                setTimeout(() => {
                    statusDiv.style.display = 'none';
                }, 5000);
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
            
            // Include plugin statuses for the UI
            val statuses = BaseBleService.pluginStatuses.value
            val statusesJson = JSONObject()
            for ((pluginId, status) in statuses) {
                statusesJson.put(pluginId, JSONObject().apply {
                    put("connected", status.connected)
                    put("authenticated", status.authenticated)
                    put("dataHealthy", status.dataHealthy)
                })
            }
            put("pluginStatuses", statusesJson)
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

            // Verify MQTT service is stopped (check mqttEnabled setting)
            val settings = AppSettings(context)
            val mqttEnabled = runBlocking { settings.mqttEnabled.first() }
            if (mqttEnabled) {
                return newFixedLengthResponse(
                    Response.Status.FORBIDDEN,
                    "application/json",
                    """{"success":false,"error":"MQTT service must be stopped before editing configuration"}"""
                )
            }

            // Update the appropriate MQTT setting
            runBlocking {
                when (field) {
                    "broker" -> settings.setMqttBrokerHost(value)
                    "port" -> settings.setMqttBrokerPort(value.toIntOrNull() ?: 1883)
                    "topicPrefix" -> settings.setMqttTopicPrefix(value)
                    "username" -> settings.setMqttUsername(value)
                    "password" -> settings.setMqttPassword(value)
                    else -> return@runBlocking newFixedLengthResponse(
                        Response.Status.BAD_REQUEST,
                        "application/json",
                        """{"success":false,"error":"Unknown field: $field"}"""
                    )
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

            // Validate service is stopped
            val service = BaseBleService.getInstance()
            if (service != null) {
                return newFixedLengthResponse(
                    Response.Status.BAD_REQUEST,
                    "application/json",
                    """{"success":false,"error":"Service must be stopped to add plugins"}"""
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

            // Validate service is stopped
            val service = BaseBleService.getInstance()
            if (service != null && runBlocking { BaseBleService.serviceRunning.first() }) {
                return newFixedLengthResponse(
                    Response.Status.BAD_REQUEST,
                    "application/json",
                    """{"success":false,"error":"Service must be stopped to remove plugins"}"""
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

            // Validate service is stopped
            val service = BaseBleService.getInstance()
            if (service != null) {
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

    fun getUrl(ipAddress: String): String {
        return "http://$ipAddress:$port"
    }
}
