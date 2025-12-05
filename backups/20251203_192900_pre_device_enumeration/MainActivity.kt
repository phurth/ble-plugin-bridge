package com.onecontrol.blebridge

import android.Manifest
import android.content.BroadcastReceiver
import android.content.ComponentName
import android.content.Context
import android.content.Intent
import android.content.ServiceConnection
import android.os.IBinder
import android.content.IntentFilter
import android.content.pm.PackageManager
import android.net.Uri
import android.os.Build
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.os.PowerManager
import android.provider.Settings
import android.widget.Button
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.core.app.ActivityCompat
import androidx.core.content.ContextCompat
import androidx.localbroadcastmanager.content.LocalBroadcastManager

class MainActivity : AppCompatActivity() {
    
    private lateinit var statusText: TextView
    private lateinit var logText: TextView
    private lateinit var startButton: Button
    private lateinit var stopButton: Button
    
    private val PERMISSIONS_REQUEST_CODE = 100
    
    private var bleService: OneControlBleService? = null
    private var serviceBinder: OneControlBleService.LocalBinder? = null
    private val handler = Handler(Looper.getMainLooper())
    private val serviceConnection = object : ServiceConnection {
        override fun onServiceConnected(name: ComponentName?, service: IBinder?) {
            serviceBinder = service as? OneControlBleService.LocalBinder
            bleService = serviceBinder?.getService()
            log("✅ Service connected - device control available")
            updateDeviceStatuses()
        }
        
        override fun onServiceDisconnected(name: ComponentName?) {
            serviceBinder = null
            bleService = null
            log("❌ Service disconnected")
        }
    }
    
    // Broadcast receiver for service logs and state
    private val serviceReceiver = object : BroadcastReceiver() {
        override fun onReceive(context: Context, intent: Intent) {
            when (intent.action) {
                OneControlBleService.ACTION_LOG -> {
                    val message = intent.getStringExtra(OneControlBleService.EXTRA_LOG_MESSAGE)
                    message?.let { log(it) }
                }
                OneControlBleService.ACTION_SERVICE_STATE -> {
                    val isRunning = intent.getBooleanExtra(OneControlBleService.EXTRA_SERVICE_RUNNING, false)
                    updateServiceState(isRunning)
                }
            }
        }
    }
    
    // Required permissions for Android 12+ (API 31+)
    private val requiredPermissions = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
        arrayOf(
            Manifest.permission.BLUETOOTH_SCAN,
            Manifest.permission.BLUETOOTH_CONNECT,
            Manifest.permission.ACCESS_FINE_LOCATION,
            Manifest.permission.POST_NOTIFICATIONS
        )
    } else {
        // Android 11 and below
        arrayOf(
            Manifest.permission.BLUETOOTH,
            Manifest.permission.BLUETOOTH_ADMIN,
            Manifest.permission.ACCESS_FINE_LOCATION
        )
    }
    
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)
        
        statusText = findViewById(R.id.statusText)
        logText = findViewById(R.id.logText)
        startButton = findViewById(R.id.startServiceButton)
        stopButton = findViewById(R.id.stopServiceButton)
        
        // Register broadcast receiver for service logs and state
        val filter = IntentFilter().apply {
            addAction(OneControlBleService.ACTION_LOG)
            addAction(OneControlBleService.ACTION_SERVICE_STATE)
        }
        LocalBroadcastManager.getInstance(this).registerReceiver(serviceReceiver, filter)
        
        // Check and request permissions
        if (!hasAllPermissions()) {
            log("Requesting permissions...")
            ActivityCompat.requestPermissions(
                this,
                requiredPermissions,
                PERMISSIONS_REQUEST_CODE
            )
        } else {
            log("All permissions granted")
        }
        
        // Request battery optimization exemption
        requestBatteryOptimizationExemption()
        
        // Button click handlers
        startButton.setOnClickListener {
            android.util.Log.d("MainActivity", "🔵 Start button clicked!")
            log("🔵 Start button clicked!")
            if (hasAllPermissions()) {
                log("✅ Permissions granted, starting service...")
                startBleService()
            } else {
                log("❌ Permissions not granted")
                Toast.makeText(this, "Please grant all permissions first", Toast.LENGTH_SHORT).show()
                ActivityCompat.requestPermissions(this, requiredPermissions, PERMISSIONS_REQUEST_CODE)
            }
        }
        
        stopButton.setOnClickListener {
            stopBleService()
        }
        
        // Update UI based on service state
        updateServiceState()
        
        // Bind to service to access control methods (defer to avoid blocking onCreate)
        handler.postDelayed({
            val intent = Intent(this, OneControlBleService::class.java)
            bindService(intent, serviceConnection, Context.BIND_AUTO_CREATE)
        }, 100)
    }
    
    override fun onResume() {
        super.onResume()
        updateServiceState()
        updateDeviceStatuses()
    }
    
    override fun onPause() {
        super.onPause()
        // Don't unbind - keep connection for background control
    }
    
    override fun onRequestPermissionsResult(
        requestCode: Int,
        permissions: Array<out String>,
        grantResults: IntArray
    ) {
        super.onRequestPermissionsResult(requestCode, permissions, grantResults)
        if (requestCode == PERMISSIONS_REQUEST_CODE) {
            if (grantResults.all { it == PackageManager.PERMISSION_GRANTED }) {
                log("✅ All permissions granted")
                Toast.makeText(this, "Permissions granted", Toast.LENGTH_SHORT).show()
            } else {
                log("❌ Some permissions denied")
                Toast.makeText(
                    this,
                    "Some permissions were denied. The app may not work correctly.",
                    Toast.LENGTH_LONG
                ).show()
            }
        }
    }
    
    private fun hasAllPermissions(): Boolean {
        return requiredPermissions.all {
            ContextCompat.checkSelfPermission(this, it) == PackageManager.PERMISSION_GRANTED
        }
    }
    
    private fun requestBatteryOptimizationExemption() {
        val powerManager = getSystemService(Context.POWER_SERVICE) as PowerManager
        val packageName = packageName
        
        if (!powerManager.isIgnoringBatteryOptimizations(packageName)) {
            log("Requesting battery optimization exemption...")
            try {
                val intent = Intent(Settings.ACTION_REQUEST_IGNORE_BATTERY_OPTIMIZATIONS).apply {
                    data = Uri.parse("package:$packageName")
                }
                startActivity(intent)
            } catch (e: Exception) {
                log("Could not request battery optimization exemption: ${e.message}")
            }
        } else {
            log("✅ Battery optimization already disabled")
        }
    }
    
    private fun startBleService() {
        android.util.Log.d("MainActivity", "🚀 startBleService() called")
        log("Starting BLE Bridge service...")
        val intent = Intent(this, OneControlBleService::class.java)
        
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            android.util.Log.d("MainActivity", "🚀 Calling startForegroundService()")
            startForegroundService(intent)
        } else {
            android.util.Log.d("MainActivity", "🚀 Calling startService()")
            startService(intent)
        }
        
        // Wait a moment for service to bind, then start connection
        handler.postDelayed({
            android.util.Log.d("MainActivity", "🔍 Checking serviceBinder: ${serviceBinder != null}")
            if (serviceBinder != null) {
                android.util.Log.d("MainActivity", "✅ Calling startConnection()")
                log("✅ Calling startConnection()")
                serviceBinder?.startConnection()
            } else {
                android.util.Log.d("MainActivity", "⏳ Service not bound yet, retrying...")
                log("⏳ Service not bound yet, retrying...")
                // Service not bound yet, try again
                handler.postDelayed({
                    android.util.Log.d("MainActivity", "🔍 Retry: Checking serviceBinder: ${serviceBinder != null}")
                    if (serviceBinder != null) {
                        android.util.Log.d("MainActivity", "✅ Retry: Calling startConnection()")
                        log("✅ Retry: Calling startConnection()")
                        serviceBinder?.startConnection()
                    } else {
                        android.util.Log.e("MainActivity", "❌ Service still not bound after retry!")
                        log("❌ Service still not bound after retry!")
                    }
                }, 500)
            }
        }, 500)
        
        updateServiceState()
        Toast.makeText(this, "BLE Bridge service started", Toast.LENGTH_SHORT).show()
    }
    
    private fun stopBleService() {
        log("Stopping BLE Bridge service...")
        // Stop connection first
        serviceBinder?.stopConnection()
        // Then stop the service
        val intent = Intent(this, OneControlBleService::class.java)
        stopService(intent)
        updateServiceState()
        Toast.makeText(this, "BLE Bridge service stopped", Toast.LENGTH_SHORT).show()
    }
    
    private fun updateServiceState() {
        updateServiceState(OneControlBleService.isServiceRunning)
    }
    
    private fun updateServiceState(isRunning: Boolean) {
        statusText.text = if (isRunning) {
            "Status: Service Running"
        } else {
            "Status: Service Stopped"
        }
        startButton.isEnabled = !isRunning
        stopButton.isEnabled = isRunning
    }
    
    private fun log(message: String) {
        val timestamp = System.currentTimeMillis()
        val timeStr = android.text.format.DateFormat.format("HH:mm:ss", timestamp)
        if (::logText.isInitialized) {
            runOnUiThread {
                logText.append("[$timeStr] $message\n")
                // Auto-scroll to bottom
                val scrollView = logText.parent as? android.widget.ScrollView
                scrollView?.post {
                    scrollView.fullScroll(android.view.View.FOCUS_DOWN)
                }
            }
        }
    }
    
    override fun onDestroy() {
        super.onDestroy()
        try {
            LocalBroadcastManager.getInstance(this).unregisterReceiver(serviceReceiver)
            unbindService(serviceConnection)
        } catch (e: Exception) {
            // Receiver may not be registered or service not bound
        }
    }
    
    /**
     * Update device statuses display
     */
    private fun updateDeviceStatuses() {
        val service = bleService ?: return
        val statuses = service.getAllDeviceStatuses()
        
        if (statuses.isNotEmpty()) {
            val statusText = statuses.entries.joinToString("\n") { (key, status) ->
                when (status) {
                    is DeviceStatus.Relay -> "Relay ${status.deviceId}: ${if (status.isOn) "ON" else "OFF"}"
                    is DeviceStatus.DimmableLight -> "Light ${status.deviceId}: ${if (status.isOn) "ON" else "OFF"} @ ${status.brightness}%"
                    is DeviceStatus.RgbLight -> "RGB Light ${status.deviceId}: Status updated"
                }
            }
            log("📊 Device Statuses:\n$statusText")
        }
    }
    
    /**
     * Control a switch (relay)
     * Example: controlSwitch(1, true) to turn on device ID 1
     */
    fun controlSwitch(deviceId: Byte, turnOn: Boolean) {
        bleService?.controlSwitch(deviceId, turnOn) ?: run {
            log("❌ Service not available - cannot control switch")
            Toast.makeText(this, "Service not available", Toast.LENGTH_SHORT).show()
        }
    }
    
    /**
     * Control a dimmable light
     * Example: controlDimmableLight(2, 75) to set device ID 2 to 75% brightness
     */
    fun controlDimmableLight(deviceId: Byte, brightness: Int) {
        bleService?.controlDimmableLight(deviceId, brightness.coerceIn(0, 100)) ?: run {
            log("❌ Service not available - cannot control light")
            Toast.makeText(this, "Service not available", Toast.LENGTH_SHORT).show()
        }
    }
    
    /**
     * Get device status
     */
    fun getDeviceStatus(deviceId: Byte): DeviceStatus? {
        return bleService?.getDeviceStatus(deviceId)
    }
}

