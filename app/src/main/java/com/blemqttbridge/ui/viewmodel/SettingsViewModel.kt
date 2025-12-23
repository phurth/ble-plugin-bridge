package com.blemqttbridge.ui.viewmodel

import android.app.Application
import android.content.Context
import android.content.Intent
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.blemqttbridge.core.BaseBleService
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
    
    val bleScannerEnabled = settings.bleScannerEnabled.stateIn(viewModelScope, SharingStarted.Eagerly, false)
    
    // Expandable section states (collapsed by default)
    private val _mqttExpanded = MutableStateFlow(false)
    val mqttExpanded: StateFlow<Boolean> = _mqttExpanded
    
    private val _oneControlExpanded = MutableStateFlow(false)
    val oneControlExpanded: StateFlow<Boolean> = _oneControlExpanded
    
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
            restartService()
        }
    }
    
    fun setOneControlGatewayMac(mac: String) {
        viewModelScope.launch { settings.setOneControlGatewayMac(mac) }
    }
    
    fun setOneControlGatewayPin(pin: String) {
        viewModelScope.launch { settings.setOneControlGatewayPin(pin) }
    }
    
    fun toggleMqttExpanded() {
        _mqttExpanded.value = !_mqttExpanded.value
    }
    
    fun toggleOneControlExpanded() {
        _oneControlExpanded.value = !_oneControlExpanded.value
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
        }
        hidePluginPicker()
    }
    
    fun removePlugin(pluginId: String) {
        when (pluginId) {
            "ble_scanner" -> setBleScannerEnabled(false)
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
}
