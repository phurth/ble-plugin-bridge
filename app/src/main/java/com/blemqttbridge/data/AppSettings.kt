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
        
        // BLE Scanner Plugin Settings
        val BLE_SCANNER_ENABLED = booleanPreferencesKey("ble_scanner_enabled")
        
        // Default values
        const val DEFAULT_MQTT_HOST = "10.115.19.131"
        const val DEFAULT_MQTT_PORT = 1883
        const val DEFAULT_MQTT_USERNAME = "mqtt"
        const val DEFAULT_MQTT_PASSWORD = "mqtt"
        const val DEFAULT_TOPIC_PREFIX = "homeassistant"
        const val DEFAULT_GATEWAY_MAC = "24:DC:C3:ED:1E:0A"
        const val DEFAULT_GATEWAY_PIN = "090336"
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
    
    // BLE Scanner Plugin Flows
    val bleScannerEnabled: Flow<Boolean> = context.dataStore.data.map { it[BLE_SCANNER_ENABLED] ?: false }
    
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
    
    suspend fun setBleScannerEnabled(enabled: Boolean) {
        context.dataStore.edit { it[BLE_SCANNER_ENABLED] = enabled }
    }
}
