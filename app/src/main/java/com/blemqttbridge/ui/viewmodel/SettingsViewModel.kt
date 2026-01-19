package com.blemqttbridge.ui.viewmodel

import android.app.Application
import android.content.Context
import android.content.Intent
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.blemqttbridge.core.BaseBleService
import com.blemqttbridge.core.ServiceStateManager
import com.blemqttbridge.data.AppSettings
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch

/**
 * ViewModel for settings screen
 */
class SettingsViewModel(application: Application) : AndroidViewModel(application) {
    
    private val context: Context = application.applicationContext
    private val settings = AppSettings(context)
    
    // UI State flows
    val mqttEnabled = settings.mqttEnabled.stateIn(viewModelScope, SharingStarted.Eagerly, false)
    val mqttBrokerHost = settings.mqttBrokerHost.stateIn(viewModelScope, SharingStarted.Eagerly, AppSettings.DEFAULT_MQTT_HOST)
    val mqttBrokerPort = settings.mqttBrokerPort.stateIn(viewModelScope, SharingStarted.Eagerly, AppSettings.DEFAULT_MQTT_PORT)
    val mqttUsername = settings.mqttUsername.stateIn(viewModelScope, SharingStarted.Eagerly, AppSettings.DEFAULT_MQTT_USERNAME)
    val mqttPassword = settings.mqttPassword.stateIn(viewModelScope, SharingStarted.Eagerly, AppSettings.DEFAULT_MQTT_PASSWORD)
    val mqttTopicPrefix = settings.mqttTopicPrefix.stateIn(viewModelScope, SharingStarted.Eagerly, AppSettings.DEFAULT_TOPIC_PREFIX)
    
    val serviceEnabled = settings.serviceEnabled.stateIn(viewModelScope, SharingStarted.Eagerly, false)
    
    val oneControlEnabled = settings.oneControlEnabled.stateIn(viewModelScope, SharingStarted.Eagerly, false)
    val oneControlGatewayMac = settings.oneControlGatewayMac.stateIn(viewModelScope, SharingStarted.Eagerly, AppSettings.DEFAULT_GATEWAY_MAC)
    val oneControlGatewayPin = settings.oneControlGatewayPin.stateIn(viewModelScope, SharingStarted.Eagerly, AppSettings.DEFAULT_GATEWAY_PIN)
    
    val easyTouchEnabled = settings.easyTouchEnabled.stateIn(viewModelScope, SharingStarted.Eagerly, false)
    val easyTouchThermostatMac = settings.easyTouchThermostatMac.stateIn(viewModelScope, SharingStarted.Eagerly, "")
    val easyTouchThermostatPassword = settings.easyTouchThermostatPassword.stateIn(viewModelScope, SharingStarted.Eagerly, "")
    
    val goPowerEnabled = settings.goPowerEnabled.stateIn(viewModelScope, SharingStarted.Eagerly, false)
    val goPowerControllerMac = settings.goPowerControllerMac.stateIn(viewModelScope, SharingStarted.Eagerly, "")
    
    val mopekaEnabled = settings.mopekaEnabled.stateIn(viewModelScope, SharingStarted.Eagerly, false)
    val mopekaSensorMac = settings.mopekaSensorMac.stateIn(viewModelScope, SharingStarted.Eagerly, "")
    val mopekaMediumType = settings.mopekaMediumType.stateIn(viewModelScope, SharingStarted.Eagerly, "propane")
    val mopekaMinimumQuality = settings.mopekaMinimumQuality.stateIn(viewModelScope, SharingStarted.Eagerly, 0)
    
    val bleScannerEnabled = settings.bleScannerEnabled.stateIn(viewModelScope, SharingStarted.Eagerly, false)
    
    val webServerEnabled = settings.webServerEnabled.stateIn(viewModelScope, SharingStarted.Eagerly, false)
    val webServerPort = settings.webServerPort.stateIn(viewModelScope, SharingStarted.Eagerly, AppSettings.DEFAULT_WEB_SERVER_PORT)
    val webAuthEnabled = settings.webAuthEnabled.stateIn(viewModelScope, SharingStarted.Eagerly, false)
    val webAuthUsername = settings.webAuthUsername.stateIn(viewModelScope, SharingStarted.Eagerly, "")
    val webAuthPassword = settings.webAuthPassword.stateIn(viewModelScope, SharingStarted.Eagerly, "")
    
    // Expandable section states (collapsed by default)
    private val _mqttExpanded = MutableStateFlow(false)
    val mqttExpanded: StateFlow<Boolean> = _mqttExpanded
    
    private val _oneControlExpanded = MutableStateFlow(false)
    val oneControlExpanded: StateFlow<Boolean> = _oneControlExpanded
    
    private val _easyTouchExpanded = MutableStateFlow(false)
    val easyTouchExpanded: StateFlow<Boolean> = _easyTouchExpanded
    
    private val _goPowerExpanded = MutableStateFlow(false)
    val goPowerExpanded: StateFlow<Boolean> = _goPowerExpanded
    
    private val _mopekaExpanded = MutableStateFlow(false)
    val mopekaExpanded: StateFlow<Boolean> = _mopekaExpanded
    
    private val _bleScannerExpanded = MutableStateFlow(false)
    val bleScannerExpanded: StateFlow<Boolean> = _bleScannerExpanded
    
    // Plugin picker dialog state
    private val _showPluginPicker = MutableStateFlow(false)
    val showPluginPicker: StateFlow<Boolean> = _showPluginPicker
    
    // Service status flows - use MutableStateFlows that we actively update
    // by collecting from the companion object flows
    // Initialize with current values from the service (in case service is already running)
    private val _serviceRunningStatus = MutableStateFlow(BaseBleService.serviceRunning.value)
    val serviceRunningStatus: StateFlow<Boolean> = _serviceRunningStatus
    
// Per-plugin status indicators - each plugin has its own status
    // Key is plugin ID (e.g., "onecontrol", "easytouch", "gopower")
    private val _pluginStatuses = MutableStateFlow<Map<String, BaseBleService.Companion.PluginStatus>>(emptyMap())
    val pluginStatuses: StateFlow<Map<String, BaseBleService.Companion.PluginStatus>> = _pluginStatuses
    
    private val _mqttConnectedStatus = MutableStateFlow(BaseBleService.serviceRunning.value && BaseBleService.mqttConnected.value)
    val mqttConnectedStatus: StateFlow<Boolean> = _mqttConnectedStatus
    
    // BLE scanning status
    private val _bleScanningActive = MutableStateFlow(BaseBleService.bleScanningActive.value)
    val bleScanningActive: StateFlow<Boolean> = _bleScanningActive
    
    // Bluetooth availability status
    private val _bluetoothAvailable = MutableStateFlow(BaseBleService.bluetoothAvailable.value)
    val bluetoothAvailable: StateFlow<Boolean> = _bluetoothAvailable
    
    // Trace status flows
    private val _traceActive = MutableStateFlow(BaseBleService.traceActive.value)
    val traceActive: StateFlow<Boolean> = _traceActive
    
    private val _traceFilePath = MutableStateFlow(BaseBleService.traceFilePath.value)
    val traceFilePath: StateFlow<String?> = _traceFilePath
    
    init {
        // Collect status updates from the service companion object
        // All status indicators are gated by serviceRunning using combine() to prevent race conditions
        
        // Combine serviceRunning with pluginStatuses to ensure atomic updates
        viewModelScope.launch {
            combine(
                BaseBleService.serviceRunning,
                BaseBleService.pluginStatuses
            ) { running, statuses ->
                Pair(running, statuses)
            }.collect { (running, statuses) ->
                _serviceRunningStatus.value = running
                
                if (!running) {
                    // Service stopped - clear ALL plugin statuses
                    _pluginStatuses.value = emptyMap()
                } else {
                    // Service running - pass through per-plugin statuses directly
                    _pluginStatuses.value = statuses
                }
            }
        }
        
        // Combine serviceRunning with mqttConnected
        viewModelScope.launch {
            combine(
                BaseBleService.serviceRunning,
                BaseBleService.mqttConnected
            ) { running, connected ->
                Pair(running, connected)
            }.collect { (running, connected) ->
                android.util.Log.i("SettingsViewModel", "MQTT status updated: $connected (service running: $running)")
                _mqttConnectedStatus.value = running && connected
            }
        }
        
        // Collect trace status updates from the service
        viewModelScope.launch {
            BaseBleService.traceActive.collect { 
                _traceActive.value = it 
            }
        }
        viewModelScope.launch {
            BaseBleService.traceFilePath.collect { 
                _traceFilePath.value = it 
            }
        }
        
        // Collect BLE scanning status updates
        viewModelScope.launch {
            BaseBleService.bleScanningActive.collect {
                _bleScanningActive.value = it
            }
        }
        
        // Collect Bluetooth availability status updates
        viewModelScope.launch {
            BaseBleService.bluetoothAvailable.collect {
                _bluetoothAvailable.value = it
            }
        }
        
        // Auto-start service on app launch if it should be running
        viewModelScope.launch {
            val shouldRun = settings.serviceEnabled.first()
            val isRunning = BaseBleService.serviceRunning.value
            android.util.Log.i("SettingsViewModel", "Init: serviceEnabled=$shouldRun, isRunning=$isRunning")
            if (shouldRun && !isRunning) {
                android.util.Log.i("SettingsViewModel", "Auto-starting service on app launch")
                startService()
            }
        }
        
        // Auto-start web server on app launch if it should be running
        viewModelScope.launch {
            val shouldRunWebServer = settings.webServerEnabled.first()
            val isWebServerRunning = com.blemqttbridge.web.WebServerService.serviceRunning.value
            android.util.Log.i("SettingsViewModel", "Init: webServerEnabled=$shouldRunWebServer, isRunning=$isWebServerRunning")
            if (shouldRunWebServer && !isWebServerRunning) {
                android.util.Log.i("SettingsViewModel", "Auto-starting web server on app launch")
                startWebServer()
            }
        }
    }
    
    // Update functions
    fun setMqttEnabled(enabled: Boolean) {
        viewModelScope.launch {
            settings.setMqttEnabled(enabled)
            if (enabled) {
                restartService()
            }
        }
    }
    
    fun setMqttBrokerHost(host: String) {
        viewModelScope.launch { settings.setMqttBrokerHost(host) }
    }
    
    fun setMqttBrokerPort(port: Int) {
        viewModelScope.launch { settings.setMqttBrokerPort(port) }
    }
    
    fun setMqttUsername(username: String) {
        viewModelScope.launch { settings.setMqttUsername(username) }
    }
    
    fun setMqttPassword(password: String) {
        viewModelScope.launch { settings.setMqttPassword(password) }
    }
    
    fun setMqttTopicPrefix(prefix: String) {
        viewModelScope.launch { settings.setMqttTopicPrefix(prefix) }
    }
    
    fun setServiceEnabled(enabled: Boolean) {
        viewModelScope.launch {
            settings.setServiceEnabled(enabled)
            if (enabled) {
                startService()
            } else {
                stopService()
            }
        }
    }
    
    fun setOneControlEnabled(enabled: Boolean) {
        viewModelScope.launch {
            settings.setOneControlEnabled(enabled)
            // Sync with ServiceStateManager so service knows which plugins to load
            if (enabled) {
                ServiceStateManager.enableBlePlugin(context, "onecontrol_v2")
            } else {
                ServiceStateManager.disableBlePlugin(context, "onecontrol_v2")
            }
            restartService()
        }
    }
    
    fun setOneControlGatewayMac(mac: String) {
        viewModelScope.launch { settings.setOneControlGatewayMac(mac) }
    }
    
    fun setOneControlGatewayPin(pin: String) {
        viewModelScope.launch { settings.setOneControlGatewayPin(pin) }
    }
    
    fun setEasyTouchEnabled(enabled: Boolean) {
        viewModelScope.launch {
            settings.setEasyTouchEnabled(enabled)
            // Sync with ServiceStateManager so service knows which plugins to load
            if (enabled) {
                ServiceStateManager.enableBlePlugin(context, "easytouch")
            } else {
                ServiceStateManager.disableBlePlugin(context, "easytouch")
            }
            restartService()
        }
    }
    
    fun setEasyTouchThermostatMac(mac: String) {
        viewModelScope.launch { settings.setEasyTouchThermostatMac(mac) }
    }
    
    fun setEasyTouchThermostatPassword(password: String) {
        viewModelScope.launch { settings.setEasyTouchThermostatPassword(password) }
    }
    
    fun setGoPowerEnabled(enabled: Boolean) {
        viewModelScope.launch {
            settings.setGoPowerEnabled(enabled)
            if (enabled) {
                ServiceStateManager.enableBlePlugin(context, "gopower")
            } else {
                ServiceStateManager.disableBlePlugin(context, "gopower")
            }
            restartService()
        }
    }
    
    fun setGoPowerControllerMac(mac: String) {
        viewModelScope.launch { settings.setGoPowerControllerMac(mac) }
    }
    
    fun setMopekaEnabled(enabled: Boolean) {
        viewModelScope.launch {
            settings.setMopekaEnabled(enabled)
            if (enabled) {
                ServiceStateManager.enableBlePlugin(context, "mopeka")
            } else {
                ServiceStateManager.disableBlePlugin(context, "mopeka")
            }
            restartService()
        }
    }
    
    fun setMopekaSensorMac(mac: String) {
        viewModelScope.launch { settings.setMopekaSensorMac(mac) }
    }
    
    fun setMopekaMediumType(mediumType: String) {
        viewModelScope.launch { settings.setMopekaMediumType(mediumType) }
    }
    
    fun setMopekaMinimumQuality(quality: Int) {
        viewModelScope.launch { settings.setMopekaMinimumQuality(quality) }
    }
    
    fun toggleMqttExpanded() {
        _mqttExpanded.value = !_mqttExpanded.value
    }
    
    fun toggleOneControlExpanded() {
        _oneControlExpanded.value = !_oneControlExpanded.value
    }
    
    fun toggleEasyTouchExpanded() {
        _easyTouchExpanded.value = !_easyTouchExpanded.value
    }
    
    fun toggleGoPowerExpanded() {
        _goPowerExpanded.value = !_goPowerExpanded.value
    }
    
    fun toggleMopekaExpanded() {
        _mopekaExpanded.value = !_mopekaExpanded.value
    }
    
    fun toggleBleScannerExpanded() {
        _bleScannerExpanded.value = !_bleScannerExpanded.value
    }
    
    fun setBleScannerEnabled(enabled: Boolean) {
        viewModelScope.launch {
            android.util.Log.i("SettingsViewModel", "setBleScannerEnabled: $enabled")
            settings.setBleScannerEnabled(enabled)
            // Don't restart service for BLE Scanner - it can be dynamically loaded
            // The service will pick up the new setting on next start
        }
    }
    
    fun setWebServerPort(port: Int) {
        viewModelScope.launch { settings.setWebServerPort(port) }
    }
    
    fun setWebAuthEnabled(enabled: Boolean) {
        viewModelScope.launch { settings.setWebAuthEnabled(enabled) }
    }
    
    fun setWebAuthUsername(username: String) {
        viewModelScope.launch { settings.setWebAuthUsername(username) }
    }
    
    fun setWebAuthPassword(password: String) {
        viewModelScope.launch { settings.setWebAuthPassword(password) }
    }
    
    fun showPluginPicker() {
        _showPluginPicker.value = true
    }
    
    fun hidePluginPicker() {
        _showPluginPicker.value = false
    }
    
    fun addPlugin(pluginId: String) {
        viewModelScope.launch {
            android.util.Log.i("SettingsViewModel", "Adding plugin: $pluginId")
            
            // Map UI plugin ID to internal plugin ID
            val internalPluginId = when (pluginId) {
                "onecontrol" -> "onecontrol_v2"
                else -> pluginId
            }
            
            // 1. Update DataStore (for UI state)
            when (pluginId) {
                "ble_scanner" -> settings.setBleScannerEnabled(true)
                "onecontrol" -> settings.setOneControlEnabled(true)
                "easytouch" -> settings.setEasyTouchEnabled(true)
                "gopower" -> settings.setGoPowerEnabled(true)
                "mopeka" -> settings.setMopekaEnabled(true)
            }
            
            // 2. Update ServiceStateManager (for service to know which plugins to load)
            ServiceStateManager.enableBlePlugin(context, internalPluginId)
            
            // 3. Send intent to service to load the plugin dynamically (NO restart)
            val intent = Intent(context, BaseBleService::class.java).apply {
                action = BaseBleService.ACTION_ADD_PLUGIN
                putExtra(BaseBleService.EXTRA_PLUGIN_ID, pluginId)
            }
            context.startService(intent)
            
            android.util.Log.i("SettingsViewModel", "Plugin $pluginId added, service notified")
        }
        hidePluginPicker()
    }

    fun removePlugin(pluginId: String) {
        viewModelScope.launch {
            android.util.Log.i("SettingsViewModel", "Removing plugin: $pluginId")
            
            // Map UI plugin ID to internal plugin ID
            val internalPluginId = when (pluginId) {
                "onecontrol" -> "onecontrol_v2"
                else -> pluginId
            }
            
            // 1. Tell service to clear HA discovery for this plugin
            val clearIntent = Intent(context, BaseBleService::class.java).apply {
                action = BaseBleService.ACTION_CLEAR_PLUGIN_DISCOVERY
                putExtra(BaseBleService.EXTRA_PLUGIN_ID, pluginId)
            }
            context.startService(clearIntent)
            
            // 2. Update DataStore (for UI state)
            when (pluginId) {
                "ble_scanner" -> settings.setBleScannerEnabled(false)
                "onecontrol" -> settings.setOneControlEnabled(false)
                "easytouch" -> settings.setEasyTouchEnabled(false)
                "gopower" -> settings.setGoPowerEnabled(false)
            }
            
            // 3. Update ServiceStateManager
            ServiceStateManager.disableBlePlugin(context, internalPluginId)
            
            // Wait for discovery clearing and settings to persist
            kotlinx.coroutines.delay(1000)
            
            android.util.Log.i("SettingsViewModel", "Plugin $pluginId removed, killing app")
            
            // 4. Kill the app - this stops the service and all BLE connections
            android.os.Process.killProcess(android.os.Process.myPid())
        }
    }

    private fun startService() {
        val intent = Intent(context, BaseBleService::class.java).apply {
            action = BaseBleService.ACTION_START_SCAN
        }
        context.startForegroundService(intent)
    }
    
    private fun stopService() {
        val intent = Intent(context, BaseBleService::class.java)
        context.stopService(intent)
    }
    
    private fun startWebServer() {
        val intent = Intent(context, com.blemqttbridge.web.WebServerService::class.java)
        context.startForegroundService(intent)
        android.util.Log.i("SettingsViewModel", "Web server start initiated")
    }
    
    private fun stopWebServer() {
        val intent = Intent(context, com.blemqttbridge.web.WebServerService::class.java).apply {
            action = com.blemqttbridge.web.WebServerService.ACTION_STOP_SERVICE
        }
        context.startService(intent)
        android.util.Log.i("SettingsViewModel", "Web server stop initiated")
    }
    
    private suspend fun restartService() {
        android.util.Log.i("SettingsViewModel", "Restarting service...")
        stopService()
        // Delay to allow service to fully stop
        kotlinx.coroutines.delay(500)
        startService()
        android.util.Log.i("SettingsViewModel", "Service restart initiated")
    }
    
    /**
     * Export debug log and share via intent.
     * Creates a debug log file and opens a share sheet for the user to share it.
     */
    fun exportDebugLog() {
        viewModelScope.launch {
            try {
                // Get the service instance via broadcast intent
                val intent = Intent(context, BaseBleService::class.java).apply {
                    action = "com.blemqttbridge.EXPORT_DEBUG_LOG"
                }
                context.startService(intent)
                
                // Note: The actual file export and sharing needs to be done by the service
                // since we need access to the service instance
                android.util.Log.i("SettingsViewModel", "Debug log export requested")
            } catch (e: Exception) {
                android.util.Log.e("SettingsViewModel", "Failed to export debug log", e)
            }
        }
    }
    
    /**
     * Toggle BLE trace logging.
     * Starts or stops BLE trace based on current state.
     */
    fun toggleBleTrace() {
        viewModelScope.launch {
            try {
                val action = if (_traceActive.value) {
                    "com.blemqttbridge.STOP_TRACE"
                } else {
                    "com.blemqttbridge.START_TRACE"
                }
                val intent = Intent(context, BaseBleService::class.java).apply {
                    this.action = action
                }
                context.startService(intent)
                
                android.util.Log.i("SettingsViewModel", "BLE trace toggle requested: ${if (_traceActive.value) "stop" else "start"}")
            } catch (e: Exception) {
                android.util.Log.e("SettingsViewModel", "Failed to toggle BLE trace", e)
            }
        }
    }
    
    fun setWebServerEnabled(enabled: Boolean) {
        viewModelScope.launch {
            settings.setWebServerEnabled(enabled)
            // Start/stop web server service independently of BLE service
            if (enabled) {
                startWebServer()
            } else {
                stopWebServer()
            }
        }
    }
    
    fun getLocalIpAddress(): String {
        try {
            val networkInterface = java.net.NetworkInterface.getNetworkInterfaces()
            while (networkInterface.hasMoreElements()) {
                val ni = networkInterface.nextElement()
                val addresses = ni.inetAddresses
                while (addresses.hasMoreElements()) {
                    val addr = addresses.nextElement()
                    if (!addr.isLoopbackAddress && addr is java.net.Inet4Address) {
                        return addr.hostAddress ?: "0.0.0.0"
                    }
                }
            }
        } catch (e: Exception) {
            android.util.Log.e("SettingsViewModel", "Failed to get local IP", e)
        }
        return "0.0.0.0"
    }
}
