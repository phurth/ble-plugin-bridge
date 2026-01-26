package com.blemqttbridge.core

import org.json.JSONObject

/**
 * Configuration for a polling plugin instance (e.g., Peplink router).
 *
 * Unlike BLE plugins, polling plugins:
 * - Don't have BLE MAC addresses
 * - Use REST APIs or other network protocols
 * - Are identified by plugin type + instance name
 *
 * Examples:
 * - "peplink_main" -> Main RV router
 * - "peplink_towed" -> Towed vehicle router
 */
data class PollingPluginConfig(
    val instanceId: String,          // Unique ID: "{pluginType}_{instanceName}" e.g., "peplink_main"
    val pluginType: String,          // Plugin type: "peplink", "victronvrm", etc.
    val displayName: String,         // User-friendly name: "Main Router", "Towed Router", etc.
    val config: Map<String, String>  // Plugin-specific config (URL, credentials, polling interval, etc.)
) {
    companion object {
        /**
         * Serialize PollingPluginConfig to JSON string.
         */
        fun toJson(config: PollingPluginConfig): String {
            val json = JSONObject()
            json.put("instanceId", config.instanceId)
            json.put("pluginType", config.pluginType)
            json.put("displayName", config.displayName)

            // Serialize config map
            val configJson = JSONObject()
            config.config.forEach { (key, value) ->
                configJson.put(key, value)
            }
            json.put("config", configJson)

            return json.toString()
        }

        /**
         * Deserialize PollingPluginConfig from JSON string.
         * Returns null if JSON is malformed.
         */
        fun fromJson(jsonString: String): PollingPluginConfig? {
            return try {
                val json = JSONObject(jsonString)

                // Parse config map
                val configJson = json.optJSONObject("config") ?: JSONObject()
                val configMap = mutableMapOf<String, String>()
                configJson.keys().forEach { key ->
                    val value = configJson.optString(key, "")
                    if (value.isNotEmpty()) {
                        configMap[key] = value
                    }
                }

                PollingPluginConfig(
                    instanceId = json.getString("instanceId"),
                    pluginType = json.getString("pluginType"),
                    displayName = json.getString("displayName"),
                    config = configMap
                )
            } catch (e: Exception) {
                null
            }
        }
    }
}
