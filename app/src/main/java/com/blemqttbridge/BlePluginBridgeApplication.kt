package com.blemqttbridge

import android.app.Application
import android.util.Log
import com.blemqttbridge.core.PluginRegistry
import com.blemqttbridge.plugins.device.MockBatteryPlugin
import com.blemqttbridge.plugins.onecontrol.OneControlDevicePlugin
import com.blemqttbridge.plugins.output.MqttOutputPlugin

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
        
        registry.registerBlePlugin("mock_battery") {
            MockBatteryPlugin()
        }
        
        // Register output plugin FACTORIES
        registry.registerOutputPlugin("mqtt") {
            MqttOutputPlugin(this@BlePluginBridgeApplication)
        }
        
        Log.i(TAG, "Plugin factory registration complete")
        Log.i(TAG, "  Available BLE plugins: ${registry.getRegisteredBlePlugins().joinToString(", ")}")
        Log.i(TAG, "  Available output plugins: ${registry.getRegisteredOutputPlugins().joinToString(", ")}")
        Log.i(TAG, "  No plugins loaded yet - waiting for user configuration")
    }
}
