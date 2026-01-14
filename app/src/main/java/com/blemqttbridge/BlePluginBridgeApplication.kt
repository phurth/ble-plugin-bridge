package com.blemqttbridge

import android.app.Application
import android.content.Intent
import android.os.Build
import android.util.Log
import androidx.work.ExistingPeriodicWorkPolicy
import androidx.work.PeriodicWorkRequestBuilder
import androidx.work.WorkManager
import com.blemqttbridge.core.BaseBleService
import com.blemqttbridge.core.PluginRegistry
import com.blemqttbridge.plugins.device.MockBatteryPlugin
import com.blemqttbridge.plugins.onecontrol.OneControlDevicePlugin
import com.blemqttbridge.plugins.easytouch.EasyTouchDevicePlugin
import com.blemqttbridge.plugins.gopower.GoPowerDevicePlugin
import com.blemqttbridge.plugins.mopeka.MopekaDevicePlugin
import com.blemqttbridge.plugins.output.MqttOutputPlugin
import com.blemqttbridge.workers.ServiceWatchdogWorker
import java.util.concurrent.TimeUnit

/**
 * Application class for BLE Plugin Bridge.
 * 
 * IMPORTANT: This class only REGISTERS plugin factories.
 * Plugins are NOT loaded until user enables them through UI.
 * Service state is persisted and restored on app launch.
 */
class BlePluginBridgeApplication : Application() {
    
    companion object {
        private const val TAG = "BlePluginBridgeApp"
    }
    
    override fun onCreate() {
        super.onCreate()
        Log.i(TAG, "Application starting - registering plugin factories")
        
        val registry = PluginRegistry.getInstance()
        
        // Register BLE device plugin FACTORIES (not instances)
        // Plugins are instantiated only when user enables them
        
        // NEW ARCHITECTURE: OneControlDevicePlugin implements BleDevicePlugin
        // This plugin OWNS its BluetoothGattCallback - no forwarding layer
        registry.registerBlePlugin("onecontrol_v2") {
            OneControlDevicePlugin()
        }
        
        // COMPATIBILITY: Also register as "onecontrol" for backwards compatibility with old instances
        registry.registerBlePlugin("onecontrol") {
            OneControlDevicePlugin()
        }
        
        // EasyTouch thermostat plugin
        registry.registerBlePlugin("easytouch") {
            EasyTouchDevicePlugin()
        }
        
        // GoPower solar charge controller plugin
        registry.registerBlePlugin("gopower") {
            GoPowerDevicePlugin()
        }
        
        // Mopeka tank sensor plugin (passive BLE scanning)
        registry.registerBlePlugin("mopeka") {
            MopekaDevicePlugin()
        }
        
        registry.registerBlePlugin("mock_battery") {
            MockBatteryPlugin()
        }
        
        // Register output plugin FACTORIES
        registry.registerOutputPlugin("mqtt") {
            MqttOutputPlugin(this@BlePluginBridgeApplication)
        }
        
        // NOTE: Plugins are only active when user adds them via UI with a configured MAC address.
        // No plugins are auto-enabled - this prevents connecting to neighbors' devices in RV parks.
        
        Log.i(TAG, "Plugin factory registration complete")
        Log.i(TAG, "  Available BLE plugins: ${registry.getRegisteredBlePlugins().joinToString(", ")}")
        Log.i(TAG, "  Available output plugins: ${registry.getRegisteredOutputPlugins().joinToString(", ")}")
        
        // Schedule service watchdog (runs every 15 minutes to ensure service stays alive)
        scheduleServiceWatchdog()
        
        // Auto-start web service
        startWebService()
    }
    
    /**
     * Auto-start web service when app launches.
     * The web service runs independently and provides the management interface.
     */
    private fun startWebService() {
        try {
            val intent = Intent(this, BaseBleService::class.java).apply {
                action = "START_WEB_SERVER"
            }
            
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                startForegroundService(intent)
            } else {
                startService(intent)
            }
            
            Log.i(TAG, "Web service auto-started")
        } catch (e: Exception) {
            Log.e(TAG, "Failed to auto-start web service", e)
        }
    }
    
    /**
     * Schedule periodic watchdog to monitor service health.
     * Runs every 15 minutes (minimum allowed by WorkManager).
     * Survives app restart and device reboot.
     */
    private fun scheduleServiceWatchdog() {
        val watchdogRequest = PeriodicWorkRequestBuilder<ServiceWatchdogWorker>(
            15, TimeUnit.MINUTES
        ).build()
        
        WorkManager.getInstance(this).enqueueUniquePeriodicWork(
            ServiceWatchdogWorker.WORK_NAME,
            ExistingPeriodicWorkPolicy.KEEP, // Don't reschedule if already running
            watchdogRequest
        )
        
        Log.i(TAG, "Service watchdog scheduled (15min interval)")
    }
}
