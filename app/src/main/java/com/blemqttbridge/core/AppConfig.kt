package com.blemqttbridge.core

import android.content.Context
import com.blemqttbridge.data.AppSettings
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.runBlocking

/**
 * Configuration manager using DataStore via AppSettings.
 * Provides MQTT broker settings and plugin configuration.
 */
object AppConfig {
    
    /**
     * Get MQTT configuration as a map for plugin initialization.
     * Reads from DataStore synchronously using runBlocking.
     */
    fun getMqttConfig(context: Context): Map<String, String> {
        val settings = AppSettings(context)
        
        return runBlocking {
            mapOf(
                "broker_url" to "tcp://${settings.mqttBrokerHost.first()}:${settings.mqttBrokerPort.first()}",
                "username" to settings.mqttUsername.first(),
                "password" to settings.mqttPassword.first(),
                "topic_prefix" to settings.mqttTopicPrefix.first(),
                "client_id" to "ble_bridge_${System.currentTimeMillis()}"
            )
        }
    }
    
    /**
     * Get BLE plugin configuration.
     * Returns plugin-specific settings like gateway MAC and PIN.
     */
    fun getBlePluginConfig(context: Context, pluginId: String): Map<String, String> {
        val settings = AppSettings(context)
        
        return when (pluginId) {
            "onecontrol_v2" -> runBlocking {
                mapOf(
                    "gateway_mac" to settings.oneControlGatewayMac.first(),
                    "gateway_pin" to settings.oneControlGatewayPin.first()
                )
            }
            "easytouch" -> runBlocking {
                mapOf(
                    "thermostat_mac" to settings.easyTouchThermostatMac.first(),
                    "thermostat_password" to settings.easyTouchThermostatPassword.first()
                )
            }
            "gopower" -> runBlocking {
                mapOf(
                    "controller_mac" to settings.goPowerControllerMac.first()
                )
            }
            "hughes_watchdog" -> mapOf()  // MAC is handled by PluginInstance
            else -> emptyMap()
        }
    }
}

