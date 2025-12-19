package com.blemqttbridge

import android.app.Application
import android.util.Log
import com.blemqttbridge.core.PluginRegistry
import com.blemqttbridge.plugins.device.MockBatteryPlugin
import com.blemqttbridge.plugins.device.onecontrol.OneControlPlugin
import com.blemqttbridge.plugins.output.MqttOutputPlugin

/**
 * Application class for BLE Plugin Bridge.
 * Registers all available plugins on startup.
 */
class BlePluginBridgeApplication : Application() {
    
    companion object {
        private const val TAG = "BlePluginBridgeApp"
    }
    
    override fun onCreate() {
        super.onCreate()
        Log.i(TAG, "Application starting - registering plugins")
        
        val registry = PluginRegistry.getInstance()
        
        // Register BLE device plugins
        registry.registerBlePlugin("onecontrol") {
            OneControlPlugin()
        }
        
        registry.registerBlePlugin("mock_battery") {
            MockBatteryPlugin()
        }
        
        // Register output plugins  
        registry.registerOutputPlugin("mqtt") {
            MqttOutputPlugin(this@BlePluginBridgeApplication)
        }
        
        Log.i(TAG, "Plugin registration complete")
        Log.i(TAG, "  BLE plugins: onecontrol, mock_battery")
        Log.i(TAG, "  Output plugins: mqtt")
    }
}
