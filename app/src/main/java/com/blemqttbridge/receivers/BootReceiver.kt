package com.blemqttbridge.receivers

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.util.Log
import com.blemqttbridge.core.BaseBleService
import com.blemqttbridge.core.ServiceStateManager

/**
 * BroadcastReceiver to auto-start the BLE bridge service on device boot.
 * 
 * This enables completely hands-free operation:
 * 1. Configure MQTT credentials once via ADB or UI
 * 2. Enable auto-start (default: enabled)
 * 3. Device boots → Service starts → Connects to MQTT
 * 4. All control via MQTT from anywhere
 * 
 * Control auto-start via ADB:
 * 
 * # Enable auto-start
 * adb shell am broadcast --receiver-foreground \
 *   -a com.blemqttbridge.SET_AUTO_START \
 *   --ez enabled true
 * 
 * # Disable auto-start
 * adb shell am broadcast --receiver-foreground \
 *   -a com.blemqttbridge.SET_AUTO_START \
 *   --ez enabled false
 * 
 * # Check status
 * adb logcat | grep "BootReceiver:"
 */
class BootReceiver : BroadcastReceiver() {
    
    companion object {
        private const val TAG = "BootReceiver"
        const val ACTION_SET_AUTO_START = "com.blemqttbridge.SET_AUTO_START"
        const val EXTRA_ENABLED = "enabled"
        
        private const val PREFS_NAME = "ble_bridge_config"
        private const val KEY_AUTO_START = "auto_start_on_boot"
        private const val DEFAULT_AUTO_START = true
        
        private const val RESPONSE_PREFIX = "BootReceiver:"
        
        fun isAutoStartEnabled(context: Context): Boolean {
            val prefs = context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)
            return prefs.getBoolean(KEY_AUTO_START, DEFAULT_AUTO_START)
        }
        
        fun setAutoStartEnabled(context: Context, enabled: Boolean) {
            context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)
                .edit()
                .putBoolean(KEY_AUTO_START, enabled)
                .apply()
        }
    }
    
    override fun onReceive(context: Context, intent: Intent) {
        when (intent.action) {
            Intent.ACTION_BOOT_COMPLETED -> handleBootCompleted(context)
            ACTION_SET_AUTO_START -> handleSetAutoStart(context, intent)
            else -> {
                Log.w(TAG, "$RESPONSE_PREFIX Unknown action: ${intent.action}")
            }
        }
    }
    
    private fun handleBootCompleted(context: Context) {
        Log.i(TAG, "$RESPONSE_PREFIX Device boot completed")
        
        if (!isAutoStartEnabled(context)) {
            Log.i(TAG, "$RESPONSE_PREFIX Auto-start disabled, not starting service")
            return
        }
        
        try {
            Log.i(TAG, "$RESPONSE_PREFIX Auto-start enabled, starting BLE bridge service...")
            
            val serviceIntent = Intent(context, BaseBleService::class.java).apply {
                action = BaseBleService.ACTION_START_SCAN
                putExtra(BaseBleService.EXTRA_BLE_PLUGIN_ID, "onecontrol_v2")
                putExtra(BaseBleService.EXTRA_OUTPUT_PLUGIN_ID, "mqtt")
            }
            
            context.startForegroundService(serviceIntent)
            
            Log.i(TAG, "$RESPONSE_PREFIX ✅ Service start command sent")
            Log.i(TAG, "$RESPONSE_PREFIX Service will connect to MQTT and enable remote control")
            
        } catch (e: Exception) {
            Log.e(TAG, "$RESPONSE_PREFIX ❌ Failed to start service on boot", e)
        }
    }
    
    private fun handleSetAutoStart(context: Context, intent: Intent) {
        val enabled = intent.getBooleanExtra(EXTRA_ENABLED, DEFAULT_AUTO_START)
        
        setAutoStartEnabled(context, enabled)
        
        if (enabled) {
            Log.i(TAG, "$RESPONSE_PREFIX ✅ Auto-start ENABLED")
            Log.i(TAG, "$RESPONSE_PREFIX Service will start automatically on next boot")
        } else {
            Log.i(TAG, "$RESPONSE_PREFIX ⚠️  Auto-start DISABLED")
            Log.i(TAG, "$RESPONSE_PREFIX Service will NOT start on next boot")
        }
    }
}
