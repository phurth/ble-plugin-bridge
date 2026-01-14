package com.blemqttbridge.data

import android.content.Context
import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.*
import androidx.datastore.preferences.preferencesDataStore
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.map

private val Context.dataStore: DataStore<Preferences> by preferencesDataStore(name = "app_settings")

/**
 * Settings repository using DataStore for persistent storage
 */
class AppSettings(private val context: Context) {
    
    companion object {
        // MQTT Settings
        val MQTT_ENABLED = booleanPreferencesKey("mqtt_enabled")
        val MQTT_BROKER_HOST = stringPreferencesKey("mqtt_broker_host")
        val MQTT_BROKER_PORT = intPreferencesKey("mqtt_broker_port")
        val MQTT_USERNAME = stringPreferencesKey("mqtt_username")
        val MQTT_PASSWORD = stringPreferencesKey("mqtt_password")
        val MQTT_TOPIC_PREFIX = stringPreferencesKey("mqtt_topic_prefix")
        
        // Service Settings
        val SERVICE_ENABLED = booleanPreferencesKey("service_enabled")
        
        // OneControl Plugin Settings
        val ONECONTROL_ENABLED = booleanPreferencesKey("onecontrol_enabled")
        val ONECONTROL_GATEWAY_MAC = stringPreferencesKey("onecontrol_gateway_mac")
        val ONECONTROL_GATEWAY_PIN = stringPreferencesKey("onecontrol_gateway_pin")
        val ONECONTROL_BLUETOOTH_PIN = stringPreferencesKey("onecontrol_bluetooth_pin")
        
        // EasyTouch Plugin Settings
        val EASYTOUCH_ENABLED = booleanPreferencesKey("easytouch_enabled")
        val EASYTOUCH_THERMOSTAT_MAC = stringPreferencesKey("easytouch_thermostat_mac")
        val EASYTOUCH_THERMOSTAT_PASSWORD = stringPreferencesKey("easytouch_thermostat_password")
        
        // GoPower Plugin Settings
        val GOPOWER_ENABLED = booleanPreferencesKey("gopower_enabled")
        val GOPOWER_CONTROLLER_MAC = stringPreferencesKey("gopower_controller_mac")
        
        // Mopeka Plugin Settings
        val MOPEKA_ENABLED = booleanPreferencesKey("mopeka_enabled")
        val MOPEKA_SENSOR_MAC = stringPreferencesKey("mopeka_sensor_mac")
        val MOPEKA_MEDIUM_TYPE = stringPreferencesKey("mopeka_medium_type")
        val MOPEKA_MINIMUM_QUALITY = intPreferencesKey("mopeka_minimum_quality")
        
        // BLE Scanner Plugin Settings
        val BLE_SCANNER_ENABLED = booleanPreferencesKey("ble_scanner_enabled")
        
        // Web Server Settings
        val WEB_SERVER_ENABLED = booleanPreferencesKey("web_server_enabled")
        val WEB_SERVER_PORT = intPreferencesKey("web_server_port")
        val WEB_AUTH_ENABLED = booleanPreferencesKey("web_auth_enabled")
        val WEB_AUTH_USERNAME = stringPreferencesKey("web_auth_username")
        val WEB_AUTH_PASSWORD = stringPreferencesKey("web_auth_password")
        
        // Default values
        const val DEFAULT_MQTT_HOST = ""
        const val DEFAULT_MQTT_PORT = 1883
        const val DEFAULT_MQTT_USERNAME = "mqtt"
        const val DEFAULT_MQTT_PASSWORD = "mqtt"
        const val DEFAULT_TOPIC_PREFIX = "homeassistant"
        const val DEFAULT_GATEWAY_MAC = ""
        const val DEFAULT_GATEWAY_PIN = ""
        const val DEFAULT_WEB_SERVER_PORT = 8088
    }
    
    // MQTT Settings Flows
    val mqttEnabled: Flow<Boolean> = context.dataStore.data.map { it[MQTT_ENABLED] ?: false }
    val mqttBrokerHost: Flow<String> = context.dataStore.data.map { it[MQTT_BROKER_HOST] ?: DEFAULT_MQTT_HOST }
    val mqttBrokerPort: Flow<Int> = context.dataStore.data.map { it[MQTT_BROKER_PORT] ?: DEFAULT_MQTT_PORT }
    val mqttUsername: Flow<String> = context.dataStore.data.map { it[MQTT_USERNAME] ?: DEFAULT_MQTT_USERNAME }
    val mqttPassword: Flow<String> = context.dataStore.data.map { it[MQTT_PASSWORD] ?: DEFAULT_MQTT_PASSWORD }
    val mqttTopicPrefix: Flow<String> = context.dataStore.data.map { it[MQTT_TOPIC_PREFIX] ?: DEFAULT_TOPIC_PREFIX }
    
    // Service Settings Flows
    val serviceEnabled: Flow<Boolean> = context.dataStore.data.map { it[SERVICE_ENABLED] ?: false }
    
    // OneControl Plugin Flows
    val oneControlEnabled: Flow<Boolean> = context.dataStore.data.map { it[ONECONTROL_ENABLED] ?: false }
    val oneControlGatewayMac: Flow<String> = context.dataStore.data.map { it[ONECONTROL_GATEWAY_MAC] ?: DEFAULT_GATEWAY_MAC }
    val oneControlGatewayPin: Flow<String> = context.dataStore.data.map { it[ONECONTROL_GATEWAY_PIN] ?: DEFAULT_GATEWAY_PIN }
    val oneControlBluetoothPin: Flow<String> = context.dataStore.data.map { it[ONECONTROL_BLUETOOTH_PIN] ?: "" }
    
    // EasyTouch Plugin Flows
    val easyTouchEnabled: Flow<Boolean> = context.dataStore.data.map { it[EASYTOUCH_ENABLED] ?: false }
    val easyTouchThermostatMac: Flow<String> = context.dataStore.data.map { it[EASYTOUCH_THERMOSTAT_MAC] ?: "" }
    val easyTouchThermostatPassword: Flow<String> = context.dataStore.data.map { it[EASYTOUCH_THERMOSTAT_PASSWORD] ?: "" }
    
    // GoPower Plugin Flows
    val goPowerEnabled: Flow<Boolean> = context.dataStore.data.map { it[GOPOWER_ENABLED] ?: false }
    val goPowerControllerMac: Flow<String> = context.dataStore.data.map { it[GOPOWER_CONTROLLER_MAC] ?: "" }
    
    // Mopeka Plugin Flows
    val mopekaEnabled: Flow<Boolean> = context.dataStore.data.map { it[MOPEKA_ENABLED] ?: false }
    val mopekaSensorMac: Flow<String> = context.dataStore.data.map { it[MOPEKA_SENSOR_MAC] ?: "" }
    val mopekaMediumType: Flow<String> = context.dataStore.data.map { it[MOPEKA_MEDIUM_TYPE] ?: "propane" }
    val mopekaMinimumQuality: Flow<Int> = context.dataStore.data.map { it[MOPEKA_MINIMUM_QUALITY] ?: 0 }
    
    // BLE Scanner Plugin Flows
    val bleScannerEnabled: Flow<Boolean> = context.dataStore.data.map { it[BLE_SCANNER_ENABLED] ?: false }
    
    // Web Server Flows
    val webServerEnabled: Flow<Boolean> = context.dataStore.data.map { it[WEB_SERVER_ENABLED] ?: false }
    val webServerPort: Flow<Int> = context.dataStore.data.map { it[WEB_SERVER_PORT] ?: DEFAULT_WEB_SERVER_PORT }
    val webAuthEnabled: Flow<Boolean> = context.dataStore.data.map { it[WEB_AUTH_ENABLED] ?: false }
    val webAuthUsername: Flow<String> = context.dataStore.data.map { it[WEB_AUTH_USERNAME] ?: "" }
    val webAuthPassword: Flow<String> = context.dataStore.data.map { it[WEB_AUTH_PASSWORD] ?: "" }
    
    // Update functions
    suspend fun setMqttEnabled(enabled: Boolean) {
        context.dataStore.edit { it[MQTT_ENABLED] = enabled }
    }
    
    suspend fun setMqttBrokerHost(host: String) {
        context.dataStore.edit { it[MQTT_BROKER_HOST] = host }
    }
    
    suspend fun setMqttBrokerPort(port: Int) {
        context.dataStore.edit { it[MQTT_BROKER_PORT] = port }
    }
    
    suspend fun setMqttUsername(username: String) {
        context.dataStore.edit { it[MQTT_USERNAME] = username }
    }
    
    suspend fun setMqttPassword(password: String) {
        context.dataStore.edit { it[MQTT_PASSWORD] = password }
    }
    
    suspend fun setMqttTopicPrefix(prefix: String) {
        context.dataStore.edit { it[MQTT_TOPIC_PREFIX] = prefix }
    }
    
    suspend fun setServiceEnabled(enabled: Boolean) {
        context.dataStore.edit { it[SERVICE_ENABLED] = enabled }
    }
    
    suspend fun setOneControlEnabled(enabled: Boolean) {
        context.dataStore.edit { it[ONECONTROL_ENABLED] = enabled }
    }
    
    suspend fun setOneControlGatewayMac(mac: String) {
        context.dataStore.edit { it[ONECONTROL_GATEWAY_MAC] = mac }
    }
    
    suspend fun setOneControlGatewayPin(pin: String) {
        context.dataStore.edit { it[ONECONTROL_GATEWAY_PIN] = pin }
    }
    
    suspend fun setOneControlBluetoothPin(pin: String) {
        context.dataStore.edit { it[ONECONTROL_BLUETOOTH_PIN] = pin }
    }
    
    suspend fun setEasyTouchEnabled(enabled: Boolean) {
        context.dataStore.edit { it[EASYTOUCH_ENABLED] = enabled }
    }
    
    suspend fun setEasyTouchThermostatMac(mac: String) {
        context.dataStore.edit { it[EASYTOUCH_THERMOSTAT_MAC] = mac }
    }
    
    suspend fun setEasyTouchThermostatPassword(password: String) {
        context.dataStore.edit { it[EASYTOUCH_THERMOSTAT_PASSWORD] = password }
    }
    
    suspend fun setGoPowerEnabled(enabled: Boolean) {
        context.dataStore.edit { it[GOPOWER_ENABLED] = enabled }
    }
    
    suspend fun setGoPowerControllerMac(mac: String) {
        context.dataStore.edit { it[GOPOWER_CONTROLLER_MAC] = mac }
    }
    
    suspend fun setMopekaEnabled(enabled: Boolean) {
        context.dataStore.edit { it[MOPEKA_ENABLED] = enabled }
    }
    
    suspend fun setMopekaSensorMac(mac: String) {
        context.dataStore.edit { it[MOPEKA_SENSOR_MAC] = mac }
    }
    
    suspend fun setMopekaMediumType(mediumType: String) {
        context.dataStore.edit { it[MOPEKA_MEDIUM_TYPE] = mediumType }
    }
    
    suspend fun setMopekaMinimumQuality(quality: Int) {
        context.dataStore.edit { it[MOPEKA_MINIMUM_QUALITY] = quality }
    }
    
    suspend fun setWebServerEnabled(enabled: Boolean) {
        context.dataStore.edit { it[WEB_SERVER_ENABLED] = enabled }
    }
    
    suspend fun setWebServerPort(port: Int) {
        context.dataStore.edit { it[WEB_SERVER_PORT] = port }
    }
    
    suspend fun setBleScannerEnabled(enabled: Boolean) {
        context.dataStore.edit { it[BLE_SCANNER_ENABLED] = enabled }
    }
    
    suspend fun setWebAuthEnabled(enabled: Boolean) {
        context.dataStore.edit { it[WEB_AUTH_ENABLED] = enabled }
    }
    
    suspend fun setWebAuthUsername(username: String) {
        context.dataStore.edit { it[WEB_AUTH_USERNAME] = username }
    }
    
    suspend fun setWebAuthPassword(password: String) {
        context.dataStore.edit { it[WEB_AUTH_PASSWORD] = password }
    }
}
