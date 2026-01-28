package com.blemqttbridge.core

import android.content.Context
import android.content.SharedPreferences
import android.util.Log
import com.blemqttbridge.BuildConfig
import com.blemqttbridge.data.AppSettings
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.runBlocking
import org.json.JSONArray
import org.json.JSONException
import org.json.JSONObject
import java.time.ZonedDateTime
import java.time.format.DateTimeFormatter

/**
 * Manages configuration backup and restore operations.
 * 
 * Exports all app configuration (DataStore, SharedPreferences) to JSON format.
 * Imports and validates configuration from backup JSON files.
 * Includes versioning for future compatibility.
 */
object ConfigBackupManager {
    
    private const val TAG = "ConfigBackupManager"
    
    // Backup version for future compatibility
    private const val BACKUP_FORMAT_VERSION = 1
    
    /**
     * Create a complete backup of all app configuration as JSON.
     * 
     * Includes:
     * - AppSettings from DataStore (MQTT, service, plugin settings)
     * - PluginInstance configurations from SharedPreferences
     * - App and system metadata
     */
    fun createBackup(context: Context): String {
        Log.i(TAG, "Creating configuration backup...")
        
        val backup = JSONObject()
        
        try {
            // Add metadata
            backup.put("version", BACKUP_FORMAT_VERSION)
            backup.put("appVersion", BuildConfig.VERSION_NAME)
            backup.put("versionCode", BuildConfig.VERSION_CODE)
            backup.put("exportedAt", ZonedDateTime.now().format(DateTimeFormatter.ISO_DATE_TIME))
            
            // Export AppSettings (DataStore preferences)
            val appSettingsJson = exportAppSettings(context)
            backup.put("appSettings", appSettingsJson)
            
            // Export MQTT configuration
            val mqttConfigJson = exportMqttConfig(context)
            backup.put("mqttConfig", mqttConfigJson)
            
            // Export plugin instances from SharedPreferences
            val pluginInstancesJson = exportPluginInstances(context)
            backup.put("pluginInstances", pluginInstancesJson)
            
            // Export polling plugin instances (Peplink, etc.)
            val pollingInstancesJson = exportPollingInstances(context)
            backup.put("pollingInstances", pollingInstancesJson)
            
            Log.i(TAG, "✅ Backup created successfully (version=$BACKUP_FORMAT_VERSION, appVersion=${BuildConfig.VERSION_NAME})")
            return backup.toString(2) // Pretty print with 2-space indent
            
        } catch (e: Exception) {
            Log.e(TAG, "Error creating backup", e)
            throw e
        }
    }
    
    /**
     * Export AppSettings (DataStore preferences) as JSON.
     */
    private fun exportAppSettings(context: Context): JSONObject {
        val appSettings = AppSettings(context)
        val json = JSONObject()
        
        try {
            runBlocking {
                // MQTT settings
                json.put("mqttEnabled", appSettings.mqttEnabled.first())
                json.put("mqttBrokerHost", appSettings.mqttBrokerHost.first())
                json.put("mqttBrokerPort", appSettings.mqttBrokerPort.first())
                json.put("mqttUsername", appSettings.mqttUsername.first())
                json.put("mqttPassword", appSettings.mqttPassword.first())
                json.put("mqttTopicPrefix", appSettings.mqttTopicPrefix.first())
                
                // Service settings
                json.put("serviceEnabled", appSettings.serviceEnabled.first())
                
                // Plugin enable/disable flags
                json.put("oneControlEnabled", appSettings.oneControlEnabled.first())
                json.put("easyTouchEnabled", appSettings.easyTouchEnabled.first())
                json.put("goPowerEnabled", appSettings.goPowerEnabled.first())
                json.put("mopekaEnabled", appSettings.mopekaEnabled.first())
                json.put("bleScannerEnabled", appSettings.bleScannerEnabled.first())
                
                // Web server settings
                json.put("webServerEnabled", appSettings.webServerEnabled.first())
                json.put("webServerPort", appSettings.webServerPort.first())
                json.put("webAuthEnabled", appSettings.webAuthEnabled.first())
                json.put("webAuthUsername", appSettings.webAuthUsername.first())
                // Note: We intentionally skip webAuthPassword here for security
                // Users can reconfigure auth after import if needed
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error exporting app settings", e)
        }
        
        return json
    }
    
    /**
     * Export MQTT configuration as JSON.
     */
    private fun exportMqttConfig(context: Context): JSONObject {
        val appSettings = AppSettings(context)
        val json = JSONObject()
        
        try {
            runBlocking {
                val host = appSettings.mqttBrokerHost.first()
                val port = appSettings.mqttBrokerPort.first()
                
                json.put("broker", "tcp://$host:$port")
                json.put("username", appSettings.mqttUsername.first())
                json.put("password", appSettings.mqttPassword.first())
                json.put("clientId", "ble_bridge_${System.currentTimeMillis()}")
                json.put("topicPrefix", appSettings.mqttTopicPrefix.first())
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error exporting MQTT config", e)
        }
        
        return json
    }
    
    /**
     * Export all plugin instances from SharedPreferences as JSON.
     */
    private fun exportPluginInstances(context: Context): JSONObject {
        val instances = JSONObject()
        
        try {
            // Plugin instances are stored in ServiceStateManager's SharedPreferences
            // under the key "plugin_instances" as a nested JSON object
            val sharedPrefs = context.getSharedPreferences("service_state", Context.MODE_PRIVATE)
            
            val jsonString = sharedPrefs.getString("plugin_instances", "") ?: ""
            
            if (jsonString.isNotEmpty()) {
                try {
                    val json = JSONObject(jsonString)
                    
                    // json contains: { "instanceId1": "{json string}", "instanceId2": "{json string}" }
                    json.keys().forEach { instanceId ->
                        val instanceJsonString = json.getString(instanceId)
                        try {
                            // Parse and re-add to our export
                            val pluginInstance = PluginInstance.fromJson(instanceJsonString)
                            if (pluginInstance != null) {
                                instances.put(instanceId, JSONObject(instanceJsonString))
                                Log.d(TAG, "Exported plugin instance: $instanceId")
                            }
                        } catch (e: Exception) {
                            Log.w(TAG, "Skipping invalid plugin instance: $instanceId", e)
                        }
                    }
                } catch (e: Exception) {
                    Log.w(TAG, "Error parsing plugin instances JSON", e)
                }
            }
            
            Log.i(TAG, "Exported ${instances.length()} plugin instances")
        } catch (e: Exception) {
            Log.e(TAG, "Error exporting plugin instances", e)
        }
        
        return instances
    }
    
    /**
     * Export all polling plugin instances from SharedPreferences as JSON.
     * Polling instances (Peplink, etc.) are stored separately from BLE instances.
     */
    private fun exportPollingInstances(context: Context): JSONObject {
        val instances = JSONObject()
        
        try {
            val sharedPrefs = context.getSharedPreferences("service_state", Context.MODE_PRIVATE)
            val jsonString = sharedPrefs.getString("polling_instances", "") ?: ""
            
            if (jsonString.isNotEmpty()) {
                try {
                    val json = JSONObject(jsonString)
                    
                    // json contains: { "instanceId1": "{json string}", "instanceId2": "{json string}" }
                    json.keys().forEach { instanceId ->
                        val instanceJsonString = json.getString(instanceId)
                        try {
                            // Parse and re-add to our export
                            val pollingConfig = PollingPluginConfig.fromJson(instanceJsonString)
                            if (pollingConfig != null) {
                                instances.put(instanceId, JSONObject(instanceJsonString))
                                Log.d(TAG, "Exported polling instance: $instanceId")
                            }
                        } catch (e: Exception) {
                            Log.w(TAG, "Skipping invalid polling instance: $instanceId", e)
                        }
                    }
                } catch (e: Exception) {
                    Log.w(TAG, "Error parsing polling instances JSON", e)
                }
            }
            
            Log.i(TAG, "Exported ${instances.length()} polling instances")
        } catch (e: Exception) {
            Log.e(TAG, "Error exporting polling instances", e)
        }
        
        return instances
    }
    
    /**
     * Export current plugin connection statuses (for informational purposes).
     * Note: These won't be restored, just included for reference.
     */
    @Deprecated("Plugin statuses are runtime state and not exported")
    private fun exportPluginStatuses(service: BaseBleService): JSONObject {
        val statuses = JSONObject()
        // This is intentionally stubbed - we don't export runtime state
        return statuses
    }
    
    
    /**
     * Restore configuration from a backup JSON string.
     * 
     * Validates the JSON structure and version compatibility.
     * Can either replace or merge existing configuration.
     * 
     * @param backupJson The backup JSON string
     * @param context Android context for accessing preferences
     * @param replaceExisting If true, replaces all config. If false, merges with existing.
     * @return Result containing success/failure info
     */
    suspend fun restoreBackup(
        backupJson: String,
        context: Context,
        replaceExisting: Boolean = false
    ): RestoreResult {
        Log.i(TAG, "Restoring configuration from backup (replace=$replaceExisting)...")
        
        return try {
            val backup = JSONObject(backupJson)
            
            // Validate backup format version
            val version = backup.optInt("version", -1)
            if (version != BACKUP_FORMAT_VERSION) {
                return RestoreResult(
                    success = false,
                    message = "Unsupported backup version: $version (expected $BACKUP_FORMAT_VERSION)"
                )
            }
            
            val appSettings = AppSettings(context)
            val sharedPrefs = context.getSharedPreferences("service_state", Context.MODE_PRIVATE)
            
            val errors = mutableListOf<String>()
            
            // Restore AppSettings
            try {
                restoreAppSettings(backup.optJSONObject("appSettings"), appSettings, replaceExisting)
            } catch (e: Exception) {
                errors.add("AppSettings: ${e.message}")
                Log.e(TAG, "Error restoring app settings", e)
            }
            
            // Restore plugin instances
            try {
                restorePluginInstances(
                    backup.optJSONObject("pluginInstances"),
                    sharedPrefs,
                    replaceExisting
                )
            } catch (e: Exception) {
                errors.add("Plugin instances: ${e.message}")
                Log.e(TAG, "Error restoring plugin instances", e)
            }
            
            // Restore polling instances (Peplink, etc.)
            try {
                restorePollingInstances(
                    backup.optJSONObject("pollingInstances"),
                    sharedPrefs,
                    replaceExisting
                )
            } catch (e: Exception) {
                errors.add("Polling instances: ${e.message}")
                Log.e(TAG, "Error restoring polling instances", e)
            }
            
            val message = if (errors.isEmpty()) {
                "✅ Configuration restored successfully"
            } else {
                "⚠️ Configuration restored with errors:\n${errors.joinToString("\n")}"
            }
            
            Log.i(TAG, message)
            RestoreResult(success = errors.isEmpty(), message = message)
            
        } catch (e: JSONException) {
            Log.e(TAG, "Invalid backup JSON", e)
            RestoreResult(
                success = false,
                message = "Invalid backup file: ${e.message}"
            )
        } catch (e: Exception) {
            Log.e(TAG, "Error restoring backup", e)
            RestoreResult(
                success = false,
                message = "Restore failed: ${e.message}"
            )
        }
    }
    
    /**
     * Restore AppSettings from backup JSON.
     */
    private suspend fun restoreAppSettings(
        settingsJson: JSONObject?,
        appSettings: AppSettings,
        replaceExisting: Boolean
    ) {
        if (settingsJson == null) return
        
        Log.i(TAG, "Restoring app settings...")
        
        // MQTT settings
        if (settingsJson.has("mqttEnabled")) {
            appSettings.setMqttEnabled(settingsJson.getBoolean("mqttEnabled"))
        }
        if (settingsJson.has("mqttBrokerHost")) {
            appSettings.setMqttBrokerHost(settingsJson.getString("mqttBrokerHost"))
        }
        if (settingsJson.has("mqttBrokerPort")) {
            appSettings.setMqttBrokerPort(settingsJson.getInt("mqttBrokerPort"))
        }
        if (settingsJson.has("mqttUsername")) {
            appSettings.setMqttUsername(settingsJson.getString("mqttUsername"))
        }
        if (settingsJson.has("mqttPassword")) {
            appSettings.setMqttPassword(settingsJson.getString("mqttPassword"))
        }
        if (settingsJson.has("mqttTopicPrefix")) {
            appSettings.setMqttTopicPrefix(settingsJson.getString("mqttTopicPrefix"))
        }
        
        // Service settings
        if (settingsJson.has("serviceEnabled")) {
            appSettings.setServiceEnabled(settingsJson.getBoolean("serviceEnabled"))
        }
        
        // Plugin enable/disable flags
        if (settingsJson.has("oneControlEnabled")) {
            appSettings.setOneControlEnabled(settingsJson.getBoolean("oneControlEnabled"))
        }
        if (settingsJson.has("easyTouchEnabled")) {
            appSettings.setEasyTouchEnabled(settingsJson.getBoolean("easyTouchEnabled"))
        }
        if (settingsJson.has("goPowerEnabled")) {
            appSettings.setGoPowerEnabled(settingsJson.getBoolean("goPowerEnabled"))
        }
        if (settingsJson.has("mopekaEnabled")) {
            appSettings.setMopekaEnabled(settingsJson.getBoolean("mopekaEnabled"))
        }
        if (settingsJson.has("bleScannerEnabled")) {
            appSettings.setBleScannerEnabled(settingsJson.getBoolean("bleScannerEnabled"))
        }
        
        // Web server settings
        if (settingsJson.has("webServerEnabled")) {
            appSettings.setWebServerEnabled(settingsJson.getBoolean("webServerEnabled"))
        }
        if (settingsJson.has("webServerPort")) {
            appSettings.setWebServerPort(settingsJson.getInt("webServerPort"))
        }
        if (settingsJson.has("webAuthEnabled")) {
            appSettings.setWebAuthEnabled(settingsJson.getBoolean("webAuthEnabled"))
        }
        if (settingsJson.has("webAuthUsername")) {
            appSettings.setWebAuthUsername(settingsJson.getString("webAuthUsername"))
        }
        
        Log.i(TAG, "✅ App settings restored")
    }
    
    /**
     * Restore plugin instances from backup JSON.
     */
    private fun restorePluginInstances(
        instancesJson: JSONObject?,
        sharedPrefs: SharedPreferences,
        replaceExisting: Boolean
    ) {
        if (instancesJson == null || instancesJson.length() == 0) return
        
        Log.i(TAG, "Restoring plugin instances...")
        
        // Build the nested JSON structure that ServiceStateManager expects
        val nestJson = JSONObject()
        var restoredCount = 0
        
        for (instanceId in instancesJson.keys()) {
            try {
                val instanceJson = instancesJson.getJSONObject(instanceId)
                
                // Validate the instance structure
                val instance = PluginInstance.fromJson(instanceJson.toString())
                if (instance != null) {
                    // Store as nested JSON string (the format ServiceStateManager uses)
                    nestJson.put(instanceId, instanceJson.toString())
                    restoredCount++
                }
            } catch (e: Exception) {
                Log.w(TAG, "Failed to restore plugin instance: $instanceId", e)
            }
        }
        
        // Save the nested JSON to the default SharedPreferences under "plugin_instances" key
        if (restoredCount > 0) {
            sharedPrefs.edit()
                .putString("plugin_instances", nestJson.toString())
                .apply()
        } else if (replaceExisting) {
            // If replacing and no instances to restore, clear existing
            sharedPrefs.edit()
                .remove("plugin_instances")
                .apply()
        }
        
        Log.i(TAG, "✅ Restored $restoredCount plugin instances")
    }
    
    /**
     * Restore polling plugin instances from backup JSON.
     * Polling instances (Peplink, etc.) are stored separately from BLE instances.
     */
    private fun restorePollingInstances(
        instancesJson: JSONObject?,
        sharedPrefs: SharedPreferences,
        replaceExisting: Boolean
    ) {
        if (instancesJson == null || instancesJson.length() == 0) {
            Log.i(TAG, "No polling instances to restore")
            return
        }
        
        Log.i(TAG, "Restoring polling instances...")
        
        // Build the nested JSON structure that ServiceStateManager expects
        val nestJson = JSONObject()
        var restoredCount = 0
        
        for (instanceId in instancesJson.keys()) {
            try {
                val instanceJson = instancesJson.getJSONObject(instanceId)
                
                // Validate the instance structure
                val instance = PollingPluginConfig.fromJson(instanceJson.toString())
                if (instance != null) {
                    // Store as nested JSON string (the format ServiceStateManager uses)
                    nestJson.put(instanceId, instanceJson.toString())
                    restoredCount++
                }
            } catch (e: Exception) {
                Log.w(TAG, "Failed to restore polling instance: $instanceId", e)
            }
        }
        
        // Save the nested JSON to SharedPreferences under "polling_instances" key
        if (restoredCount > 0) {
            sharedPrefs.edit()
                .putString("polling_instances", nestJson.toString())
                .apply()
        } else if (replaceExisting) {
            // If replacing and no instances to restore, clear existing
            sharedPrefs.edit()
                .remove("polling_instances")
                .apply()
        }
        
        Log.i(TAG, "✅ Restored $restoredCount polling instances")
    }
    
    /**
     * Result of a restore operation.
     */
    data class RestoreResult(
        val success: Boolean,
        val message: String
    )
}
