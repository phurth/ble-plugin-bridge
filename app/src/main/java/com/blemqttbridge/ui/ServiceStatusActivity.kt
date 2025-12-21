package com.blemqttbridge.ui

import android.Manifest
import android.bluetooth.BluetoothAdapter
import android.bluetooth.BluetoothManager
import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
import android.os.Build
import android.os.Bundle
import android.widget.Button
import android.widget.ScrollView
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import androidx.appcompat.widget.SwitchCompat
import androidx.core.app.ActivityCompat
import androidx.core.content.ContextCompat
import com.blemqttbridge.R
import com.blemqttbridge.core.BaseBleService
import com.blemqttbridge.core.PluginRegistry
import com.blemqttbridge.core.ServiceStateManager

/**
 * Diagnostic UI for testing and debugging BLE service.
 * Shows:
 * - BLE adapter status
 * - Permissions status
 * - Service status
 * - Discovered devices
 * - Connected devices
 * - Loaded plugins
 * - MQTT connection status
 */
class ServiceStatusActivity : AppCompatActivity() {
    
    private lateinit var statusText: TextView
    private lateinit var scrollView: ScrollView
    private lateinit var startServiceButton: Button
    private lateinit var stopServiceButton: Button
    private lateinit var refreshButton: Button
    private lateinit var enableOneControlButton: Button
    private lateinit var disablePluginsButton: Button
    private lateinit var autoStartSwitch: SwitchCompat
    
    companion object {
        private const val PREFS_NAME = "service_ui_prefs"
        private const val KEY_AUTO_START = "auto_start_on_app_launch"
    }
    
    private val permissionsRequestCode = 1001
    private val requiredPermissions = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
        arrayOf(
            Manifest.permission.BLUETOOTH_SCAN,
            Manifest.permission.BLUETOOTH_CONNECT,
            Manifest.permission.ACCESS_FINE_LOCATION,
            Manifest.permission.POST_NOTIFICATIONS
        )
    } else {
        arrayOf(
            Manifest.permission.BLUETOOTH,
            Manifest.permission.BLUETOOTH_ADMIN,
            Manifest.permission.ACCESS_FINE_LOCATION
        )
    }
    
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_service_status)
        
        statusText = findViewById(R.id.statusText)
        autoStartSwitch = findViewById(R.id.autoStartSwitch)
        scrollView = findViewById(R.id.scrollView)
        startServiceButton = findViewById(R.id.startServiceButton)
        stopServiceButton = findViewById(R.id.stopServiceButton)
        refreshButton = findViewById(R.id.refreshButton)
        enableOneControlButton = findViewById(R.id.enableOneControlButton)
        disablePluginsButton = findViewById(R.id.disablePluginsButton)
        
        startServiceButton.setOnClickListener {
            if (checkPermissions()) {
                startBleService()
            } else {
                requestPermissions()
            }
        }
        
        stopServiceButton.setOnClickListener {
            stopBleService()
        }
        
        refreshButton.setOnClickListener {
            updateStatus()
        }
        
        enableOneControlButton.setOnClickListener {
            ServiceStateManager.enableBlePlugin(this, "onecontrol")
            statusText.append("\n✅ Enabled OneControl plugin\n")
            scrollView.post { scrollView.fullScroll(ScrollView.FOCUS_DOWN) }
            updateStatus()
        }
        
        disablePluginsButton.setOnClickListener {
            ServiceStateManager.setEnabledBlePlugins(this, emptySet())
            statusText.append("\n❌ Disabled all plugins\n")
            scrollView.post { scrollView.fullScroll(ScrollView.FOCUS_DOWN) }
            updateStatus()
        }
        // Setup auto-start toggle
        val prefs = getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)
        autoStartSwitch.isChecked = prefs.getBoolean(KEY_AUTO_START, true) // Default: enabled
        autoStartSwitch.setOnCheckedChangeListener { _, isChecked ->
            prefs.edit().putBoolean(KEY_AUTO_START, isChecked).apply()
            statusText.append("\n${if (isChecked) "✅" else "❌"} Auto-start ${if (isChecked) "enabled" else "disabled"}\n")
            scrollView.post { scrollView.fullScroll(ScrollView.FOCUS_DOWN) }
        }
        
        // Auto-start service if enabled (regardless of previous running state)
        // The intent is: if auto-start is ON, always start the service when the app opens
        val autoStartEnabled = prefs.getBoolean(KEY_AUTO_START, true)
        if (autoStartEnabled) {
            statusText.append("🚀 Auto-starting service...\n")
            scrollView.post { scrollView.fullScroll(ScrollView.FOCUS_DOWN) }
            if (checkPermissions()) {
                startBleService()
            } else {
                statusText.append("⚠️ Need permissions - tap Start Service to grant\n")
                scrollView.post { scrollView.fullScroll(ScrollView.FOCUS_DOWN) }
            }
        }
        
        
        updateStatus()
    }
    
    override fun onResume() {
        super.onResume()
        updateStatus()
    }
    
    private fun checkPermissions(): Boolean {
        return requiredPermissions.all { permission ->
            ContextCompat.checkSelfPermission(this, permission) == PackageManager.PERMISSION_GRANTED
        }
    }
    
    private fun requestPermissions() {
        ActivityCompat.requestPermissions(this, requiredPermissions, permissionsRequestCode)
    }
    
    override fun onRequestPermissionsResult(
        requestCode: Int,
        permissions: Array<out String>,
        grantResults: IntArray
    ) {
        super.onRequestPermissionsResult(requestCode, permissions, grantResults)
        if (requestCode == permissionsRequestCode) {
            updateStatus()
        }
    }
    
    private fun startBleService() {
        // Get enabled plugins from ServiceStateManager
        val enabledPlugins = ServiceStateManager.getEnabledBlePlugins(this)
        
        if (enabledPlugins.isEmpty()) {
            statusText.append("\n⚠️ No plugins enabled! Enable a plugin first.\n")
            statusText.append("Run: adb shell am broadcast -a com.blemqttbridge.ENABLE_PLUGIN --es plugin_id onecontrol\n")
            scrollView.post { scrollView.fullScroll(ScrollView.FOCUS_DOWN) }
            return
        }
        
        // Use first enabled plugin (for now - future: support multiple)
        val blePluginId = enabledPlugins.first()
        val outputPluginId = ServiceStateManager.getEnabledOutputPlugin(this) ?: "mqtt"
        
        val intent = Intent(this, BaseBleService::class.java).apply {
            action = BaseBleService.ACTION_START_SCAN
            putExtra(BaseBleService.EXTRA_BLE_PLUGIN_ID, blePluginId)
            putExtra(BaseBleService.EXTRA_OUTPUT_PLUGIN_ID, outputPluginId)
        }
        
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            startForegroundService(intent)
        } else {
            startService(intent)
        }
        
        // Update UI after short delay to let service start
        scrollView.postDelayed({ updateStatus() }, 500)
        
        updateStatus()
    }
    
    private fun stopBleService() {
        // Stop the service completely (not just scanning)
        val intent = Intent(this, BaseBleService::class.java).apply {
            action = BaseBleService.ACTION_STOP_SERVICE
        }
        startService(intent)
        
        // Update UI after short delay to let service stop
        scrollView.postDelayed({ updateStatus() }, 500)
    }
    
    private fun updateStatus() {
        // Update button states based on service status
        val serviceRunning = ServiceStateManager.wasServiceRunning(this)
        startServiceButton.isEnabled = !serviceRunning
        stopServiceButton.isEnabled = serviceRunning
        startServiceButton.alpha = if (serviceRunning) 0.5f else 1.0f
        stopServiceButton.alpha = if (serviceRunning) 1.0f else 0.5f
        
        val status = buildString {
            appendLine("=== BLE BRIDGE SERVICE STATUS ===")
            appendLine()
            
            // BLE Adapter
            val bluetoothManager = getSystemService(Context.BLUETOOTH_SERVICE) as BluetoothManager
            val bluetoothAdapter = bluetoothManager.adapter
            
            appendLine("📡 Bluetooth Adapter:")
            if (bluetoothAdapter == null) {
                appendLine("  ❌ Not available")
            } else {
                appendLine("  ✅ Available")
                appendLine("  State: ${getBluetoothState(bluetoothAdapter.state)}")
                // Don't try to get address without BLUETOOTH_CONNECT permission
                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S &&
                    ContextCompat.checkSelfPermission(this@ServiceStatusActivity, 
                        Manifest.permission.BLUETOOTH_CONNECT) != PackageManager.PERMISSION_GRANTED) {
                    appendLine("  Address: (need BLUETOOTH_CONNECT permission)")
                } else {
                    try {
                        appendLine("  Address: ${bluetoothAdapter.address ?: "Unknown"}")
                    } catch (e: SecurityException) {
                        appendLine("  Address: (permission denied)")
                    }
                }
            }
            appendLine()
            
            // Permissions
            appendLine("🔐 Permissions:")
            requiredPermissions.forEach { permission ->
                val granted = ContextCompat.checkSelfPermission(this@ServiceStatusActivity, permission) == PackageManager.PERMISSION_GRANTED
                val permName = permission.substringAfterLast('.')
                appendLine("  ${if (granted) "✅" else "❌"} $permName")
            }
            appendLine()
            
            // Location Services (required for BLE scanning)
            val locationManager = getSystemService(Context.LOCATION_SERVICE) as android.location.LocationManager
            val locationEnabled = locationManager.isProviderEnabled(android.location.LocationManager.GPS_PROVIDER) ||
                                 locationManager.isProviderEnabled(android.location.LocationManager.NETWORK_PROVIDER)
            appendLine("📍 Location Services:")
            appendLine("  ${if (locationEnabled) "✅ Enabled" else "❌ Disabled (required for BLE)"}")
            appendLine()
            
            // Service State
            appendLine("⚙️ Service State:")
            val serviceRunning = ServiceStateManager.wasServiceRunning(this@ServiceStatusActivity)
            val autoStart = ServiceStateManager.isAutoStartEnabled(this@ServiceStatusActivity)
            appendLine("  Running: ${if (serviceRunning) "✅ Yes" else "❌ No"}")
            appendLine("  Auto-start: ${if (autoStart) "✅ Enabled" else "❌ Disabled"}")
            appendLine()
            
            // Plugin Configuration
            appendLine("🔌 Plugin Configuration:")
            val enabledBlePlugins = ServiceStateManager.getEnabledBlePlugins(this@ServiceStatusActivity)
            val enabledOutputPlugin = ServiceStateManager.getEnabledOutputPlugin(this@ServiceStatusActivity)
            
            appendLine("  Enabled BLE Plugins:")
            if (enabledBlePlugins.isEmpty()) {
                appendLine("    (none - configure in settings)")
            } else {
                for (pluginId in enabledBlePlugins) {
                    appendLine("    ✅ $pluginId")
                }
            }
            appendLine()
            
            appendLine("  Enabled Output Plugin:")
            appendLine("    ${enabledOutputPlugin ?: "(none)"}")
            appendLine()
            
            // Plugin Registry
            appendLine("📦 Available Plugins:")
            val registry = PluginRegistry.getInstance()
            appendLine("  - onecontrol (OneControl Gateway)")
            appendLine("  - mock_battery (Test Plugin)")
            appendLine()
            
            appendLine("  Currently Loaded:")
            val loadedPlugins = registry.getLoadedBlePlugins()
            if (loadedPlugins.isEmpty()) {
                appendLine("    (none)")
            } else {
                loadedPlugins.forEach { (id, plugin) ->
                    appendLine("    ✅ $id: ${plugin.getPluginName()} v${plugin.getPluginVersion()}")
                }
            }
            appendLine()
            
            // Memory Status
            val runtime = Runtime.getRuntime()
            val usedMemoryMB = (runtime.totalMemory() - runtime.freeMemory()) / (1024 * 1024)
            val maxMemoryMB = runtime.maxMemory() / (1024 * 1024)
            val percentUsed = (usedMemoryMB * 100) / maxMemoryMB
            
            appendLine("💾 Memory:")
            appendLine("  Used: ${usedMemoryMB}MB / ${maxMemoryMB}MB ($percentUsed%)")
            appendLine()
            
            // Service Instructions
            appendLine("📋 Quick Start:")
            appendLine("  1. Ensure all permissions granted ✅")
            appendLine("  2. Enable Location Services ✅")
            appendLine("  3. Configure plugins (see below)")
            appendLine("  4. Tap 'Start Service'")
            appendLine()
            
            appendLine("🔧 Plugin Configuration:")
            appendLine("  To enable OneControl plugin, run:")
            appendLine("    adb shell 'am broadcast -a com.blemqttbridge.ENABLE_PLUGIN --es plugin_id onecontrol'")
            appendLine()
            appendLine("  To disable plugin, run:")
            appendLine("    adb shell 'am broadcast -a com.blemqttbridge.DISABLE_PLUGIN --es plugin_id onecontrol'")
            appendLine()
            
            appendLine("🔍 Expected Behavior:")
            appendLine("  - Service persists state across restarts")
            appendLine("  - If running when app closed, auto-starts on launch")
            appendLine("  - Only enabled plugins are loaded")
            appendLine("  - BLE scanning for configured device types")
            appendLine()
            
            appendLine("⚠️ Note:")
            appendLine("  - Plugins NOT auto-loaded on app startup")
            appendLine("  - User must enable plugins first")
            appendLine("  - Service remembers last on/off state")
            appendLine("  - For RV use, enable 'onecontrol' plugin")
        }
        
        statusText.text = status
        scrollView.post {
            scrollView.fullScroll(ScrollView.FOCUS_UP)
        }
    }
    
    private fun getBluetoothState(state: Int): String {
        return when (state) {
            BluetoothAdapter.STATE_OFF -> "OFF"
            BluetoothAdapter.STATE_TURNING_ON -> "TURNING ON"
            BluetoothAdapter.STATE_ON -> "ON"
            BluetoothAdapter.STATE_TURNING_OFF -> "TURNING OFF"
            else -> "UNKNOWN ($state)"
        }
    }
}
