package com.blemqttbridge.utils

import android.content.Context
import android.content.Intent
import android.net.Uri
import android.os.PowerManager
import android.provider.Settings
import android.util.Log

/**
 * Utility for managing battery optimization exemptions.
 * 
 * Battery optimization restricts background execution and can kill the service:
 * - Doze mode: Network/CPU access restricted when device idle
 * - App standby: Limits background activity for unused apps
 * - Aggressive vendor restrictions: Samsung/TCL SmartManager
 * 
 * Requesting exemption allows:
 * - Continuous background execution
 * - Network access during Doze
 * - BLE scanning while screen off
 * - Service stays alive overnight
 */
object BatteryOptimizationHelper {
    
    private const val TAG = "BatteryOptimization"
    
    /**
     * Check if app is exempt from battery optimizations.
     */
    fun isIgnoringBatteryOptimizations(context: Context): Boolean {
        val powerManager = context.getSystemService(Context.POWER_SERVICE) as PowerManager
        val packageName = context.packageName
        val isIgnoring = powerManager.isIgnoringBatteryOptimizations(packageName)
        Log.d(TAG, "Battery optimization status: ${if (isIgnoring) "EXEMPT" else "RESTRICTED"}")
        return isIgnoring
    }
    
    /**
     * Create intent to request battery optimization exemption.
     * Shows system dialog asking user to exempt this app.
     * 
     * Returns null if ACTION_REQUEST_IGNORE_BATTERY_OPTIMIZATIONS not available.
     */
    fun createBatteryOptimizationIntent(context: Context): Intent? {
        return try {
            Intent(Settings.ACTION_REQUEST_IGNORE_BATTERY_OPTIMIZATIONS).apply {
                data = Uri.parse("package:${context.packageName}")
            }
        } catch (e: Exception) {
            Log.e(TAG, "Failed to create battery optimization intent", e)
            null
        }
    }
    
    /**
     * Create intent to open battery optimization settings for this app.
     * Fallback if REQUEST_IGNORE_BATTERY_OPTIMIZATIONS doesn't work.
     */
    fun createBatterySettingsIntent(context: Context): Intent {
        return Intent(Settings.ACTION_IGNORE_BATTERY_OPTIMIZATION_SETTINGS)
    }
    
    /**
     * Get user-friendly status message.
     */
    fun getStatusMessage(context: Context): String {
        return if (isIgnoringBatteryOptimizations(context)) {
            "✓ Battery optimization disabled\nService can run in background"
        } else {
            "⚠ Battery optimization enabled\nService may be killed overnight"
        }
    }
    
    /**
     * Get recommendation message for user.
     */
    fun getRecommendationMessage(isExempt: Boolean): String {
        return if (isExempt) {
            "Battery optimization is disabled. The service can run continuously in the background."
        } else {
            "Battery optimization may kill the service during sleep. " +
            "For reliable 24/7 operation, please disable battery optimization for this app."
        }
    }
}
