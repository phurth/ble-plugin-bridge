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
    val mqttEnabled = settings.mqttEnabled.stateIn(viewModelScope, SharingStarted.Eagerly, true)
    val mqttBrokerHost = settings.mqttBrokerHost.stateIn(viewModelScope, SharingStarted.Eagerly, AppSettings.DEFAULT_MQTT_HOST)
    val mqttBrokerPort = settings.mqttBrokerPort.stateIn(viewModelScope, SharingStarted.Eagerly, AppSettings.DEFAULT_MQTT_PORT)
    val mqttUsername = settings.mqttUsername.stateIn(viewModelScope, SharingStarted.Eagerly, AppSettings.DEFAULT_MQTT_USERNAME)
    val mqttPassword = settings.mqttPassword.stateIn(viewModelScope, SharingStarted.Eagerly, AppSettings.DEFAULT_MQTT_PASSWORD)
    val mqttTopicPrefix = settings.mqttTopicPrefix.stateIn(viewModelScope, SharingStarted.Eagerly, AppSettings.DEFAULT_TOPIC_PREFIX)
    
    val serviceEnabled = settings.serviceEnabled.stateIn(viewModelScope, SharingStarted.Eagerly, true)
    
    val oneControlEnabled = settings.oneControlEnabled.stateIn(viewModelScope, SharingStarted.Eagerly, true)
    val oneControlGatewayMac = settings.oneControlGatewayMac.stateIn(viewModelScope, SharingStarted.Eagerly, AppSettings.DEFAULT_GATEWAY_MAC)
    val oneControlGatewayPin = settings.oneControlGatewayPin.stateIn(viewModelScope, SharingStarted.Eagerly, AppSettings.DEFAULT_GATEWAY_PIN)
    
    // Expandable section states (collapsed by default)
    private val _mqttExpanded = MutableStateFlow(false)
    val mqttExpanded: StateFlow<Boolean> = _mqttExpanded
    
    private val _oneControlExpanded = MutableStateFlow(false)
    val oneControlExpanded: StateFlow<Boolean> = _oneControlExpanded
    
    // Service status flows (from BaseBleService companion object)
    val serviceRunningStatus: StateFlow<Boolean> = BaseBleService.serviceRunning
    val bleConnectedStatus: StateFlow<Boolean> = BaseBleService.bleConnected
    val dataHealthyStatus: StateFlow<Boolean> = BaseBleService.dataHealthy
    val devicePairedStatus: StateFlow<Boolean> = BaseBleService.devicePaired
    val mqttConnectedStatus: StateFlow<Boolean> = BaseBleService.mqttConnected
    
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
