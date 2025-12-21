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
     * Defaults to "onecontrol" if no plugins have been explicitly configured.
     */
    fun getEnabledBlePlugins(context: Context): Set<String> {
        val prefs = getPrefs(context)
        // Check if plugins have ever been explicitly set
        if (!prefs.contains(KEY_ENABLED_BLE_PLUGINS)) {
            // First run - default to onecontrol enabled
            return setOf("onecontrol")
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
    // Utility
    // ============================================================================
    
    /**
     * Clear all state.
     * Useful for testing or resetting app to default state.
     */
    fun clearAll(context: Context) {
        getPrefs(context).edit().clear().apply()
    }
    
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
