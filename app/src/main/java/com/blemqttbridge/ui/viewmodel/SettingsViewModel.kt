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
    
    val bleScannerEnabled = settings.bleScannerEnabled.stateIn(viewModelScope, SharingStarted.Eagerly, false)
    
    // Expandable section states (collapsed by default)
    private val _mqttExpanded = MutableStateFlow(false)
    val mqttExpanded: StateFlow<Boolean> = _mqttExpanded
    
    private val _oneControlExpanded = MutableStateFlow(false)
    val oneControlExpanded: StateFlow<Boolean> = _oneControlExpanded
    
    private val _easyTouchExpanded = MutableStateFlow(false)
    val easyTouchExpanded: StateFlow<Boolean> = _easyTouchExpanded
    
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
    
    // Status indicators should only be true if service is running AND the condition is met
    private val _bleConnectedStatus = MutableStateFlow(BaseBleService.serviceRunning.value && BaseBleService.bleConnected.value)
    val bleConnectedStatus: StateFlow<Boolean> = _bleConnectedStatus
    
    private val _dataHealthyStatus = MutableStateFlow(BaseBleService.serviceRunning.value && BaseBleService.dataHealthy.value)
    val dataHealthyStatus: StateFlow<Boolean> = _dataHealthyStatus
    
    private val _devicePairedStatus = MutableStateFlow(BaseBleService.serviceRunning.value && BaseBleService.devicePaired.value)
    val devicePairedStatus: StateFlow<Boolean> = _devicePairedStatus
    
    private val _mqttConnectedStatus = MutableStateFlow(BaseBleService.serviceRunning.value && BaseBleService.mqttConnected.value)
    val mqttConnectedStatus: StateFlow<Boolean> = _mqttConnectedStatus
    
    // Trace status flows
    private val _traceActive = MutableStateFlow(BaseBleService.traceActive.value)
    val traceActive: StateFlow<Boolean> = _traceActive
    
    private val _traceFilePath = MutableStateFlow(BaseBleService.traceFilePath.value)
    val traceFilePath: StateFlow<String?> = _traceFilePath
    
    init {
        // Collect status updates from the service companion object
        // All status indicators are gated by serviceRunning
        viewModelScope.launch {
            BaseBleService.serviceRunning.collect { running ->
                _serviceRunningStatus.value = running
                // Reset all status indicators to false when service stops
                if (!running) {
                    _bleConnectedStatus.value = false
                    _dataHealthyStatus.value = false
                    _devicePairedStatus.value = false
                    _mqttConnectedStatus.value = false
                }
            }
        }
        viewModelScope.launch {
            BaseBleService.bleConnected.collect { 
                _bleConnectedStatus.value = _serviceRunningStatus.value && it 
            }
        }
        viewModelScope.launch {
            BaseBleService.dataHealthy.collect { 
                _dataHealthyStatus.value = _serviceRunningStatus.value && it 
            }
        }
        viewModelScope.launch {
            BaseBleService.devicePaired.collect { 
                _devicePairedStatus.value = _serviceRunningStatus.value && it 
            }
        }
        viewModelScope.launch {
            BaseBleService.mqttConnected.collect { 
                android.util.Log.i("SettingsViewModel", "MQTT status updated: $it (service running: ${_serviceRunningStatus.value})")
                _mqttConnectedStatus.value = _serviceRunningStatus.value && it 
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
            restartService()
        }
    }
    
    fun setEasyTouchThermostatMac(mac: String) {
        viewModelScope.launch { settings.setEasyTouchThermostatMac(mac) }
    }
    
    fun setEasyTouchThermostatPassword(password: String) {
        viewModelScope.launch { settings.setEasyTouchThermostatPassword(password) }
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
    
    fun showPluginPicker() {
        _showPluginPicker.value = true
    }
    
    fun hidePluginPicker() {
        _showPluginPicker.value = false
    }
    
    fun addPlugin(pluginId: String) {
        when (pluginId) {
            "ble_scanner" -> setBleScannerEnabled(true)
            "easytouch" -> {
                setEasyTouchEnabled(true)
                ServiceStateManager.enableBlePlugin(context, "easytouch")
            }
        }
        hidePluginPicker()
    }

    fun removePlugin(pluginId: String) {
        when (pluginId) {
            "ble_scanner" -> setBleScannerEnabled(false)
            "easytouch" -> {
                setEasyTouchEnabled(false)
                ServiceStateManager.disableBlePlugin(context, "easytouch")
            }
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
    
    private fun restartService() {
        stopService()
        // Small delay to allow service to fully stop
        viewModelScope.launch {
            kotlinx.coroutines.delay(500)
            startService()
        }
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
}
