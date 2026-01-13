package com.blemqttbridge.core

import org.json.JSONObject

/**
 * Represents a single instance of a plugin managing a specific BLE device.
 * 
 * Multiple instances of the same plugin type can exist, each with:
 * - Unique instanceId (pluginType_macSuffix)
 * - Own BLE device connection (MAC address)
 * - Own configuration (password, PIN, etc.)
 * - Own status tracking (connected, dataHealthy)
 * 
 * Examples:
 * - "easytouch_b1241e" -> EasyTouch for master bedroom thermostat
 * - "easytouch_c4f892" -> EasyTouch for guest room thermostat
 * - "onecontrol_v2_1e0a" -> OneControl gateway
 */
data class PluginInstance(
    val instanceId: String,       // Unique ID: "{pluginType}_{macSuffix}" e.g., "easytouch_b1241e"
    val pluginType: String,       // Plugin type: "easytouch", "onecontrol_v2", "gopower", "mopeka", etc.
    val deviceMac: String,        // BLE MAC address (uppercase with colons) e.g., "EC:C9:FF:B1:24:1E"
    val displayName: String?,     // User-friendly name: "Master Bedroom", "Fresh Water Tank", null for default
    val enabled: Boolean,         // Whether this instance is active/enabled
    val config: Map<String, String>  // Plugin-specific config (password, PIN, sensor type, etc.)
) {
    companion object {
        /**
         * Generate a unique instance ID from plugin type and MAC address.
         * 
         * Takes the last 6 characters of the MAC (without colons) to create a short suffix:
         * - "EC:C9:FF:B1:24:1E" -> "b1241e" (last 6 chars lowercase)
         * - Result: "easytouch_b1241e"
         */
        fun createInstanceId(pluginType: String, mac: String): String {
            val macSuffix = mac.replace(":", "").takeLast(6).lowercase()
            return "${pluginType}_$macSuffix"
        }

        /**
         * Serialize PluginInstance to JSON string.
         */
        fun toJson(instance: PluginInstance): String {
            val json = JSONObject()
            json.put("instanceId", instance.instanceId)
            json.put("pluginType", instance.pluginType)
            json.put("deviceMac", instance.deviceMac)
            json.put("displayName", instance.displayName)
            json.put("enabled", instance.enabled)
            
            // Serialize config map
            val configJson = JSONObject()
            instance.config.forEach { (key, value) ->
                configJson.put(key, value)
            }
            json.put("config", configJson)
            
            return json.toString()
        }

        /**
         * Deserialize PluginInstance from JSON string.
         * Returns null if JSON is malformed.
         */
        fun fromJson(jsonString: String): PluginInstance? {
            return try {
                val json = JSONObject(jsonString)
                
                // Parse config map
                val configJson = json.optJSONObject("config") ?: JSONObject()
                val config = mutableMapOf<String, String>()
                configJson.keys().forEach { key ->
                    val value = configJson.optString(key)
                    if (value != null) {
                        config[key] = value
                    }
                }
                
                PluginInstance(
                    instanceId = json.getString("instanceId"),
                    pluginType = json.getString("pluginType"),
                    deviceMac = json.getString("deviceMac"),
                    displayName = json.optString("displayName", null).takeIf { it.isNotEmpty() },
                    enabled = json.getBoolean("enabled"),
                    config = config
                )
            } catch (e: Exception) {
                null
            }
        }
    }
}
