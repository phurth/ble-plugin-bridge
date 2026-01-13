package com.blemqttbridge.core

import android.content.Context
import android.content.SharedPreferences

/**
 * Manages service and plugin state persistence.
 * 
 * State includes:
 * - Service running state (on/off)
 * - Enabled plugins
 * - Auto-start preference
 * 
 * Usage:
 * - On app launch: Check if service should auto-start
 * - On service start: Save running state
 * - On service stop: Clear running state
 * - On plugin enable/disable: Update enabled plugins list
 */
object ServiceStateManager {
    
    private const val PREFS_NAME = "service_state"
    
    // Service state keys
    private const val KEY_SERVICE_RUNNING = "service_running"
    private const val KEY_AUTO_START = "auto_start"
    
    // Plugin state keys
    private const val KEY_ENABLED_BLE_PLUGINS = "enabled_ble_plugins"
    private const val KEY_ENABLED_OUTPUT_PLUGIN = "enabled_output_plugin"
    
    // Plugin instance keys (v2.6.0+)
    private const val KEY_PLUGIN_INSTANCES = "plugin_instances"
    private const val KEY_MIGRATION_COMPLETE = "migration_complete_v2_6_0"
    
    private fun getPrefs(context: Context): SharedPreferences {
        return context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)
    }
    
    // ============================================================================
    // Service State
    // ============================================================================
    
    /**
     * Check if service was running when app last closed.
     * Used on app launch to determine if service should auto-start.
     */
    fun wasServiceRunning(context: Context): Boolean {
        return getPrefs(context).getBoolean(KEY_SERVICE_RUNNING, false)
    }
    
    /**
     * Mark service as running.
     * Call this when service starts successfully.
     */
    fun setServiceRunning(context: Context, running: Boolean) {
        getPrefs(context).edit()
            .putBoolean(KEY_SERVICE_RUNNING, running)
            .apply()
    }
    
    /**
     * Check if auto-start is enabled.
     * If true and wasServiceRunning() is true, service should start on app launch.
     */
    fun isAutoStartEnabled(context: Context): Boolean {
        // Default to true - if service was running, we should restore it
        return getPrefs(context).getBoolean(KEY_AUTO_START, true)
    }
    
    /**
     * Set auto-start preference.
     * User can disable this to prevent service from auto-starting.
     */
    fun setAutoStart(context: Context, enabled: Boolean) {
        getPrefs(context).edit()
            .putBoolean(KEY_AUTO_START, enabled)
            .apply()
    }
    
    /**
     * Check if service should start on app launch.
     * Returns true if:
     * - Auto-start is enabled AND
     * - Service was running when app last closed
     */
    fun shouldAutoStart(context: Context): Boolean {
        return isAutoStartEnabled(context) && wasServiceRunning(context)
    }
    
    // ============================================================================
    // Plugin State
    // ============================================================================
    
    /**
     * Get list of enabled BLE plugin IDs.
     * These plugins should be loaded when service starts.
     * Returns empty set if no plugins have been configured.
     */
    fun getEnabledBlePlugins(context: Context): Set<String> {
        val prefs = getPrefs(context)
        // Check if plugins have ever been explicitly set
        if (!prefs.contains(KEY_ENABLED_BLE_PLUGINS)) {
            // First run - no plugins enabled by default
            // User must explicitly add plugins via UI
            return emptySet()
        }
        val csv = prefs.getString(KEY_ENABLED_BLE_PLUGINS, "") ?: ""
        return if (csv.isEmpty()) {
            emptySet()
        } else {
            csv.split(",").toSet()
        }
    }
    
    /**
     * Set enabled BLE plugins.
     * Call this when user enables/disables plugins in UI.
     */
    fun setEnabledBlePlugins(context: Context, pluginIds: Set<String>) {
        val csv = pluginIds.joinToString(",")
        getPrefs(context).edit()
            .putString(KEY_ENABLED_BLE_PLUGINS, csv)
            .apply()
    }
    
    /**
     * Enable a BLE plugin.
     */
    fun enableBlePlugin(context: Context, pluginId: String) {
        val current = getEnabledBlePlugins(context).toMutableSet()
        current.add(pluginId)
        setEnabledBlePlugins(context, current)
    }
    
    /**
     * Disable a BLE plugin.
     */
    fun disableBlePlugin(context: Context, pluginId: String) {
        val current = getEnabledBlePlugins(context).toMutableSet()
        current.remove(pluginId)
        setEnabledBlePlugins(context, current)
    }
    
    /**
     * Check if a BLE plugin is enabled.
     */
    fun isBlePluginEnabled(context: Context, pluginId: String): Boolean {
        return getEnabledBlePlugins(context).contains(pluginId)
    }
    
    /**
     * Get the enabled output plugin ID.
     * Currently only one output plugin is supported.
     */
    fun getEnabledOutputPlugin(context: Context): String? {
        return getPrefs(context).getString(KEY_ENABLED_OUTPUT_PLUGIN, "mqtt")
    }
    
    /**
     * Set the enabled output plugin.
     */
    fun setEnabledOutputPlugin(context: Context, pluginId: String?) {
        getPrefs(context).edit()
            .putString(KEY_ENABLED_OUTPUT_PLUGIN, pluginId)
            .apply()
    }
    
    // ============================================================================
    // Plugin Instances (v2.6.0+)
    // ============================================================================

    /**
     * Get all plugin instances (both enabled and disabled).
     * Returns a map of instanceId -> PluginInstance
     */
    fun getAllInstances(context: Context): Map<String, PluginInstance> {
        val prefs = getPrefs(context)
        val jsonString = prefs.getString(KEY_PLUGIN_INSTANCES, "") ?: ""
        
        if (jsonString.isEmpty()) {
            return emptyMap()
        }
        
        return try {
            val json = org.json.JSONObject(jsonString)
            val instances = mutableMapOf<String, PluginInstance>()
            
            json.keys().forEach { instanceId ->
                val instanceJson = json.getString(instanceId)
                PluginInstance.fromJson(instanceJson)?.let {
                    instances[instanceId] = it
                }
            }
            
            instances
        } catch (e: Exception) {
            emptyMap()
        }
    }

    /**
     * Get plugin instances by type.
     * Example: getInstancesOfType(context, "easytouch") returns all EasyTouch instances
     */
    fun getInstancesOfType(context: Context, pluginType: String): List<PluginInstance> {
        return getAllInstances(context).values
            .filter { it.pluginType == pluginType }
    }

    /**
     * Save a single plugin instance.
     * If instance with same ID exists, it will be updated.
     */
    fun saveInstance(context: Context, instance: PluginInstance) {
        val prefs = getPrefs(context)
        val jsonString = prefs.getString(KEY_PLUGIN_INSTANCES, "") ?: ""
        
        val json = if (jsonString.isEmpty()) {
            org.json.JSONObject()
        } else {
            org.json.JSONObject(jsonString)
        }
        
        json.put(instance.instanceId, PluginInstance.toJson(instance))
        
        prefs.edit()
            .putString(KEY_PLUGIN_INSTANCES, json.toString())
            .apply()
    }

    /**
     * Remove a plugin instance by ID.
     */
    fun removeInstance(context: Context, instanceId: String) {
        val prefs = getPrefs(context)
        val jsonString = prefs.getString(KEY_PLUGIN_INSTANCES, "") ?: ""
        
        if (jsonString.isEmpty()) {
            return
        }
        
        try {
            val json = org.json.JSONObject(jsonString)
            json.remove(instanceId)
            
            prefs.edit()
                .putString(KEY_PLUGIN_INSTANCES, json.toString())
                .apply()
        } catch (e: Exception) {
            // Invalid JSON, ignore
        }
    }

    /**
     * Check if migration from old format to instance format is needed.
     */
    fun needsMigration(context: Context): Boolean {
        val prefs = getPrefs(context)
        
        // Already migrated
        if (prefs.getBoolean(KEY_MIGRATION_COMPLETE, false)) {
            return false
        }
        
        // Has old format
        val hasOldFormat = prefs.contains(KEY_ENABLED_BLE_PLUGINS)
        
        // Has new format
        val hasNewFormat = prefs.contains(KEY_PLUGIN_INSTANCES)
        
        // Need migration if old format exists and new format doesn't
        return hasOldFormat && !hasNewFormat
    }

    /**
     * Mark migration as complete.
     * Called after successful migration to prevent re-migration.
     */
    fun setMigrationComplete(context: Context) {
        getPrefs(context).edit()
            .putBoolean(KEY_MIGRATION_COMPLETE, true)
            .apply()
    }

    /**
     * Migrate from old format (enabled_ble_plugins string + AppConfig) to new format (PluginInstance).
     * 
     * This is called once during app startup if needsMigration() returns true.
     * It reads the old enabled_ble_plugins string and plugin configs, converts them to PluginInstance objects,
     * and saves them in the new format.
     * 
     * The old format is preserved for rollback purposes.
     */
    fun migrateToInstances(context: Context) {
        if (!needsMigration(context)) {
            return
        }
        
        try {
            val enabledPlugins = getEnabledBlePlugins(context)
            
            enabledPlugins.forEach { pluginType ->
                // Read existing config from AppConfig
                val config = AppConfig.getBlePluginConfig(context, pluginType)
                
                // Extract MAC address based on plugin type
                val mac = extractMacForPluginType(pluginType, config)
                
                if (mac != null && mac.isNotEmpty()) {
                    // Create PluginInstance with the extracted config
                    val instanceId = PluginInstance.createInstanceId(pluginType, mac)
                    val instance = PluginInstance(
                        instanceId = instanceId,
                        pluginType = pluginType,
                        deviceMac = mac,
                        displayName = null,  // No friendly names in old format
                        enabled = true,
                        config = config
                    )
                    saveInstance(context, instance)
                }
            }
            
            // Mark migration complete to prevent re-running
            setMigrationComplete(context)
            
            android.util.Log.i(
                "ServiceStateManager",
                "Migration complete: ${enabledPlugins.size} plugins converted to instances"
            )
        } catch (e: Exception) {
            android.util.Log.e("ServiceStateManager", "Migration failed", e)
            // Rethrow so caller can handle gracefully
            throw e
        }
    }

    /**
     * Extract MAC address from plugin config based on plugin type.
     */
    private fun extractMacForPluginType(pluginType: String, config: Map<String, String>): String? {
        return when (pluginType) {
            "easytouch" -> config["thermostat_mac"]
            "onecontrol_v2" -> config["gateway_mac"]
            "gopower" -> config["controller_mac"]
            "mopeka" -> config["sensor_mac"]
            else -> null
        }
    }

    // ============================================================================
    // Utility
    // ============================================================================
    
    /**
     * Get all state as a debug string.
     */
    fun getDebugInfo(context: Context): String {
        val prefs = getPrefs(context)
        return buildString {
            appendLine("Service State:")
            appendLine("  Running: ${wasServiceRunning(context)}")
            appendLine("  Auto-start: ${isAutoStartEnabled(context)}")
            appendLine("  Should auto-start: ${shouldAutoStart(context)}")
            appendLine()
            appendLine("Plugins:")
            appendLine("  Enabled BLE: ${getEnabledBlePlugins(context).joinToString(", ")}")
            appendLine("  Enabled Output: ${getEnabledOutputPlugin(context)}")
        }
    }
}
