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
import android.os.Build
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.os.PowerManager
import android.provider.Settings
import android.content.SharedPreferences
import android.net.Uri
import androidx.core.content.FileProvider
import android.widget.Button
import android.widget.CompoundButton
import android.widget.EditText
import android.widget.LinearLayout
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.appcompat.widget.SwitchCompat
import androidx.core.app.ActivityCompat
import androidx.core.content.ContextCompat
import androidx.localbroadcastmanager.content.LocalBroadcastManager
import java.io.File

class MainActivity : AppCompatActivity() {
    
    companion object {
        private const val PREFS_NAME = "oc_settings"
        private const val PREF_START_ON_BOOT = "pref_start_on_boot"
        private const val PREF_DISABLE_BATT_OPT = "pref_disable_batt_opt"
        private const val PREF_WATCHDOG = "pref_watchdog"
    }
    
    private lateinit var statusText: TextView
    private lateinit var appVersionText: TextView
    private lateinit var checkService: TextView
    private lateinit var checkPaired: TextView
    private lateinit var checkBle: TextView
    private lateinit var checkData: TextView
    private lateinit var checkMqtt: TextView
    private lateinit var startButton: Button
    private lateinit var stopButton: Button
    private lateinit var switchBatteryOpt: SwitchCompat
    private lateinit var switchStartOnBoot: SwitchCompat
    private lateinit var switchWatchdog: SwitchCompat
    private lateinit var inputGatewayMac: EditText
    private lateinit var inputGatewayPin: EditText
    private lateinit var inputMqttHost: EditText
    private lateinit var inputMqttPort: EditText
    private lateinit var inputMqttTopicPrefix: EditText
    private lateinit var inputMqttUser: EditText
    private lateinit var inputMqttPassword: EditText
    private lateinit var saveConfigButton: Button
    private lateinit var buttonExportDebugLog: Button
    private lateinit var buttonToggleTrace: Button
    private lateinit var textTraceStatus: TextView
    private lateinit var configFields: LinearLayout
    private var configExpanded = false
    
    private val PERMISSIONS_REQUEST_CODE = 100
    
    private var bleService: OneControlBleService? = null
    private var serviceBinder: OneControlBleService.LocalBinder? = null
    private val handler = Handler(Looper.getMainLooper())
    private lateinit var prefs: SharedPreferences
    private val serviceConnection = object : ServiceConnection {
        override fun onServiceConnected(name: ComponentName?, service: IBinder?) {
            serviceBinder = service as? OneControlBleService.LocalBinder
            bleService = serviceBinder?.getService()
            log("✅ Service connected - device control available")
            updateDeviceStatuses()
            loadConfigFieldsFromService()
            updateTraceUi(bleService?.isTraceActive() == true, null)
        }
        
        override fun onServiceDisconnected(name: ComponentName?) {
            serviceBinder = null
            bleService = null
            log("❌ Service disconnected")
        }
    }
    
    // Broadcast receiver for service state
    private val serviceReceiver = object : BroadcastReceiver() {
        override fun onReceive(context: Context, intent: Intent) {
            when (intent.action) {
                OneControlBleService.ACTION_SERVICE_STATE -> {
                    val isRunning = intent.getBooleanExtra(OneControlBleService.EXTRA_SERVICE_RUNNING, false)
                    updateServiceState(isRunning)
                    updateChecklist(intent)
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
        prefs = getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)
        
        statusText = findViewById(R.id.statusText)
        appVersionText = findViewById(R.id.appVersionText)
        startButton = findViewById(R.id.startServiceButton)
        stopButton = findViewById(R.id.stopServiceButton)
        checkService = findViewById(R.id.checkService)
        checkPaired = findViewById(R.id.checkPaired)
        checkBle = findViewById(R.id.checkBle)
        checkData = findViewById(R.id.checkData)
        checkMqtt = findViewById(R.id.checkMqtt)
        switchBatteryOpt = findViewById(R.id.switchBatteryOpt)
        switchStartOnBoot = findViewById(R.id.switchStartOnBoot)
        switchWatchdog = findViewById(R.id.switchWatchdog)
        inputGatewayMac = findViewById(R.id.inputGatewayMac)
        inputGatewayPin = findViewById(R.id.inputGatewayPin)
        inputMqttHost = findViewById(R.id.inputMqttHost)
        inputMqttPort = findViewById(R.id.inputMqttPort)
        inputMqttTopicPrefix = findViewById(R.id.inputMqttTopicPrefix)
        inputMqttUser = findViewById(R.id.inputMqttUser)
        inputMqttPassword = findViewById(R.id.inputMqttPassword)
        saveConfigButton = findViewById(R.id.saveConfigButton)
        configFields = findViewById(R.id.configFields)
        buttonExportDebugLog = findViewById(R.id.buttonExportDebugLog)
        buttonToggleTrace = findViewById(R.id.buttonToggleTrace)
        textTraceStatus = findViewById(R.id.textTraceStatus)
        
        // Register broadcast receiver for service logs and state
        val filter = IntentFilter().apply {
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
        updateChecklist(null)
        refreshSettingsToggles()
        attachToggleHandlers()
        attachConfigCollapseToggle()
        loadConfigFieldsFromPrefs()
        attachConfigSaveHandler()
        attachDiagnosticsHandlers()
        appVersionText.text = "Version: ${getAppVersionString()}"
        
        // Bind to service to access control methods (defer to avoid blocking onCreate)
        handler.postDelayed({
            val intent = Intent(this, OneControlBleService::class.java)
            bindService(intent, serviceConnection, Context.BIND_AUTO_CREATE)
        }, 100)
    }
    
    override fun onResume() {
        super.onResume()
        updateServiceState()
        updateChecklist(null)
        updateDeviceStatuses()
        refreshSettingsToggles()
        loadConfigFieldsFromService()
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
    
    private fun refreshSettingsToggles() {
        val batteryIgnored = isIgnoringBatteryOptimizations()
        switchBatteryOpt.isChecked = batteryIgnored
        switchStartOnBoot.isChecked = prefs.getBoolean(PREF_START_ON_BOOT, false)
        switchWatchdog.isChecked = prefs.getBoolean(PREF_WATCHDOG, false)
    }

    private fun loadConfigFieldsFromService() {
        val cfg = bleService?.getConfig()
        if (cfg != null) {
            inputGatewayMac.setText(cfg.gatewayMac)
            inputGatewayPin.setText(cfg.gatewayPin)
            inputMqttUser.setText(cfg.mqttUsername)
            inputMqttPassword.setText(cfg.mqttPassword)
            inputMqttTopicPrefix.setText(cfg.mqttTopicPrefix)
            // Split broker into host/port if possible
            parseBroker(cfg.mqttBroker)?.let { (host, port) ->
                inputMqttHost.setText(host)
                inputMqttPort.setText(port.toString())
            }
        } else {
            loadConfigFieldsFromPrefs()
        }
    }

    private fun getAppVersionString(): String {
        return try {
            val pkg = packageManager.getPackageInfo(packageName, 0)
            val code = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.P) pkg.longVersionCode else pkg.versionCode.toLong()
            "${pkg.versionName} ($code)"
        } catch (e: Exception) {
            "unknown"
        }
    }

    private fun loadConfigFieldsFromPrefs() {
        inputGatewayMac.setText(prefs.getString("cfg_gateway_mac", OneControlBleService.DEFAULT_GATEWAY_MAC))
        inputGatewayPin.setText(prefs.getString("cfg_gateway_pin", OneControlBleService.DEFAULT_GATEWAY_PIN))
        inputMqttUser.setText(prefs.getString("cfg_mqtt_username", OneControlBleService.DEFAULT_MQTT_USERNAME))
        inputMqttPassword.setText(prefs.getString("cfg_mqtt_password", OneControlBleService.DEFAULT_MQTT_PASSWORD))
        inputMqttTopicPrefix.setText(prefs.getString("cfg_mqtt_topic_prefix", OneControlBleService.DEFAULT_MQTT_TOPIC_PREFIX))
        val broker = prefs.getString("cfg_mqtt_broker", OneControlBleService.DEFAULT_MQTT_BROKER) ?: OneControlBleService.DEFAULT_MQTT_BROKER
        val parsed = parseBroker(broker)
        inputMqttHost.setText(parsed?.first ?: "10.115.19.131")
        inputMqttPort.setText(parsed?.second?.toString() ?: "1883")
    }

    private fun attachConfigSaveHandler() {
        saveConfigButton.setOnClickListener {
            val newConfig = buildConfigFromInputs() ?: return@setOnClickListener
            bleService?.updateConfig(newConfig)
            Toast.makeText(this, "Settings saved and applied", Toast.LENGTH_SHORT).show()
        }
    }

    private fun attachDiagnosticsHandlers() {
        buttonExportDebugLog.setOnClickListener {
            val file = bleService?.exportDebugLog()
            if (file != null && file.exists()) {
                shareFile(file, "text/plain")
            } else {
                Toast.makeText(this, "Could not create debug log", Toast.LENGTH_SHORT).show()
            }
        }

        buttonToggleTrace.setOnClickListener {
            val service = bleService
            if (service == null) {
                Toast.makeText(this, "Service not connected yet", Toast.LENGTH_SHORT).show()
                return@setOnClickListener
            }
            if (service.isTraceActive()) {
                val file = service.stopBleTrace("user stop")
                updateTraceUi(false, file?.absolutePath)
                val msg = file?.let { "Trace saved: ${it.absolutePath}" } ?: "Trace stopped"
                Toast.makeText(this, msg, Toast.LENGTH_SHORT).show()
            } else {
                val file = service.startBleTrace()
                if (file != null) {
                    updateTraceUi(true, file.absolutePath)
                    Toast.makeText(this, "Trace started", Toast.LENGTH_SHORT).show()
                } else {
                    updateTraceUi(false, null)
                    Toast.makeText(this, "Failed to start trace", Toast.LENGTH_SHORT).show()
                }
            }
        }
    }

    
    private fun attachToggleHandlers() {
        switchBatteryOpt.setOnCheckedChangeListener { _: CompoundButton, isChecked: Boolean ->
            if (isChecked) {
                if (isIgnoringBatteryOptimizations()) {
                    prefs.edit().putBoolean(PREF_DISABLE_BATT_OPT, true).apply()
                    Toast.makeText(this, "Battery optimization already disabled", Toast.LENGTH_SHORT).show()
                    switchBatteryOpt.isChecked = true
                } else {
                    prefs.edit().putBoolean(PREF_DISABLE_BATT_OPT, true).apply()
                    val opened = requestBatteryOptimizationExemption()
                    if (!opened) {
                        Toast.makeText(this, "Could not open battery optimization settings", Toast.LENGTH_LONG).show()
                        switchBatteryOpt.isChecked = false
                        prefs.edit().putBoolean(PREF_DISABLE_BATT_OPT, false).apply()
                    }
                }
            } else {
                prefs.edit().putBoolean(PREF_DISABLE_BATT_OPT, false).apply()
                Toast.makeText(this, "Battery optimization may throttle the bridge", Toast.LENGTH_SHORT).show()
            }
        }
        
        switchStartOnBoot.setOnCheckedChangeListener { _: CompoundButton, isChecked: Boolean ->
            prefs.edit().putBoolean(PREF_START_ON_BOOT, isChecked).apply()
        }
        
        switchWatchdog.setOnCheckedChangeListener { _: CompoundButton, isChecked: Boolean ->
            prefs.edit().putBoolean(PREF_WATCHDOG, isChecked).apply()
            bleService?.setWatchdogEnabled(isChecked)
        }
    }

    private fun attachConfigCollapseToggle() {
        val title = findViewById<TextView>(R.id.configTitle)
        configExpanded = false
        configFields.visibility = LinearLayout.GONE
        title.setOnClickListener {
            configExpanded = !configExpanded
            configFields.visibility = if (configExpanded) LinearLayout.VISIBLE else LinearLayout.GONE
            title.text = if (configExpanded) "Connection settings ▲" else "Connection settings ▼"
        }
        title.text = "Connection settings ▼"
    }

    private fun buildConfigFromInputs(): OneControlBleService.AppConfig? {
        val mac = inputGatewayMac.text.toString().trim()
        val pin = inputGatewayPin.text.toString().trim()
        val host = inputMqttHost.text.toString().trim()
        val portStr = inputMqttPort.text.toString().trim()
        val topic = inputMqttTopicPrefix.text.toString().trim().ifEmpty { OneControlBleService.DEFAULT_MQTT_TOPIC_PREFIX }
        val user = inputMqttUser.text.toString().trim()
        val pass = inputMqttPassword.text.toString()

        if (mac.isEmpty()) {
            Toast.makeText(this, "Gateway MAC is required", Toast.LENGTH_SHORT).show()
            return null
        }
        if (host.isEmpty()) {
            Toast.makeText(this, "MQTT host is required", Toast.LENGTH_SHORT).show()
            return null
        }
        val port = portStr.toIntOrNull() ?: 1883
        val broker = "tcp://$host:$port"
        return OneControlBleService.AppConfig(
            gatewayMac = mac,
            gatewayPin = pin.ifEmpty { OneControlBleService.DEFAULT_GATEWAY_PIN },
            gatewayCypher = OneControlBleService.DEFAULT_GATEWAY_CYPHER.toLong(),
            mqttBroker = broker,
            mqttClientId = OneControlBleService.DEFAULT_MQTT_CLIENT_ID,
            mqttTopicPrefix = topic,
            mqttUsername = user.ifEmpty { OneControlBleService.DEFAULT_MQTT_USERNAME },
            mqttPassword = pass.ifEmpty { OneControlBleService.DEFAULT_MQTT_PASSWORD }
        )
    }

    private fun parseBroker(broker: String): Pair<String, Int>? {
        return try {
            val clean = broker.removePrefix("tcp://").removePrefix("mqtt://")
            val parts = clean.split(":")
            if (parts.size == 2) {
                val host = parts[0]
                val port = parts[1].toIntOrNull() ?: return null
                host to port
            } else null
        } catch (_: Exception) {
            null
        }
    }
    
    private fun hasAllPermissions(): Boolean {
        return requiredPermissions.all {
            ContextCompat.checkSelfPermission(this, it) == PackageManager.PERMISSION_GRANTED
        }
    }
    
    private fun requestBatteryOptimizationExemption(): Boolean {
        val powerManager = getSystemService(Context.POWER_SERVICE) as PowerManager
        val packageName = packageName
        if (powerManager.isIgnoringBatteryOptimizations(packageName)) {
            log("✅ Battery optimization already disabled")
            return true
        }
        log("Requesting battery optimization exemption...")
        return try {
            val intent = Intent(Settings.ACTION_REQUEST_IGNORE_BATTERY_OPTIMIZATIONS).apply {
                data = Uri.parse("package:$packageName")
            }
            startActivity(intent)
            true
        } catch (e: Exception) {
            log("Could not request battery optimization exemption: ${e.message}")
            try {
                val fallback = Intent(Settings.ACTION_IGNORE_BATTERY_OPTIMIZATION_SETTINGS)
                startActivity(fallback)
                true
            } catch (e2: Exception) {
                log("Fallback battery optimization settings failed: ${e2.message}")
                false
            }
        }
    }
    
    private fun isIgnoringBatteryOptimizations(): Boolean {
        val powerManager = getSystemService(Context.POWER_SERVICE) as PowerManager
        return powerManager.isIgnoringBatteryOptimizations(packageName)
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
        val intent = Intent(this, OneControlBleService::class.java)
        val stopped = serviceBinder?.stopServiceAndDisconnect() ?: false
        if (!stopped) {
            stopService(intent)
        }
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
        updateTraceUi(bleService?.isTraceActive() == true, null)
    }
    
    private fun log(message: String) {
        // Keep console logging for debugging; UI stays clean for kiosk use
        android.util.Log.d("MainActivity", message)
    }

    private fun updateTraceUi(active: Boolean, lastPath: String?) {
        buttonToggleTrace.text = if (active) "Stop BLE trace" else "Start BLE trace"
        textTraceStatus.text = if (active) {
            "Trace: active"
        } else {
            lastPath?.let { "Trace: saved to $it" } ?: "Trace: inactive"
        }
    }

    private fun shareFile(file: File, mime: String) {
        val uri: Uri = FileProvider.getUriForFile(this, "$packageName.fileprovider", file)
        val intent = Intent(Intent.ACTION_SEND).apply {
            type = mime
            putExtra(Intent.EXTRA_STREAM, uri)
            addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
        }
        startActivity(Intent.createChooser(intent, "Share log"))
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
        // Temporarily comment out to fix compilation
        /*
        val statuses = service.getAllDeviceStatuses()

        if (statuses.isNotEmpty()) {
            val statusText = statuses.entries.joinToString("\n") { (key, status) ->
                when (status) {
                    is DeviceStatus.Relay -> "Relay ${status.deviceId}: ${if (status.isOn) "ON" else "OFF"}"
                    is DeviceStatus.DimmableLight -> "Light ${status.deviceId}: ${if (status.isOn) "ON" else "OFF"} @ ${status.brightness}%"
                    is DeviceStatus.RgbLight -> "RGB Light ${status.deviceId}: Status updated"
                    else -> "Unknown device type"
                }
            }
            log("📊 Device Statuses:\n$statusText")
        }
        */
        log("📊 Device statuses display temporarily disabled")
    }
    
    /**
     * Control a switch (relay)
     * Example: controlSwitch(1, true) to turn on device ID 1
     */
    /*
    fun controlSwitch(deviceId: Byte, turnOn: Boolean) {
        bleService?.controlSwitch(deviceId, turnOn) ?: run {
            log("❌ Service not available - cannot control switch")
            Toast.makeText(this, "Service not available", Toast.LENGTH_SHORT).show()
        }
    }
    */
    
    /**
     * Control a dimmable light
     * Example: controlDimmableLight(2, 75) to set device ID 2 to 75% brightness
     */
    /*
    fun controlDimmableLight(deviceId: Byte, brightness: Int) {
        bleService?.controlDimmableLight(deviceId, brightness.coerceIn(0, 100)) ?: run {
            log("❌ Service not available - cannot control light")
            Toast.makeText(this, "Service not available", Toast.LENGTH_SHORT).show()
        }
    }
    */
    
    /**
     * Get device status
     */
    /*
    fun getDeviceStatus(deviceId: Byte): DeviceStatus? {
        return bleService?.getDeviceStatus(deviceId)
    }
    */

    private fun updateChecklist(intent: Intent?) {
        // Try intent extras first; fall back to service snapshot to keep UI accurate
        val snapshotFromService = bleService?.getStatusSnapshot()

        fun Intent?.booleanExtraOrNull(key: String): Boolean? =
            if (this != null && hasExtra(key)) getBooleanExtra(key, false) else null

        val paired = intent.booleanExtraOrNull(OneControlBleService.EXTRA_STATUS_PAIRED)
            ?: snapshotFromService?.paired
        val connected = intent.booleanExtraOrNull(OneControlBleService.EXTRA_STATUS_BLE_CONNECTED)
            ?: snapshotFromService?.bleConnected
        val authenticated = intent.booleanExtraOrNull(OneControlBleService.EXTRA_STATUS_AUTHENTICATED)
            ?: snapshotFromService?.authenticated
        val dataHealthy = intent.booleanExtraOrNull(OneControlBleService.EXTRA_STATUS_DATA_HEALTHY)
            ?: snapshotFromService?.dataHealthy
        val mqtt = intent.booleanExtraOrNull(OneControlBleService.EXTRA_STATUS_MQTT_CONNECTED)
            ?: snapshotFromService?.mqttConnected
        val running = OneControlBleService.isServiceRunning || snapshotFromService?.serviceRunning == true

        fun format(status: Boolean?): String = when (status) {
            true -> "✅"
            false -> "❌"
            null -> "⏳"
        }

        checkService.text = "Service: ${format(if (running) true else snapshotFromService?.serviceRunning)}"
        checkPaired.text = "Paired: ${format(paired)}"
        checkBle.text = "BLE Connected: ${format(connected)}"
        checkData.text = "Data Healthy: ${format(if (dataHealthy == true && authenticated == true) true else dataHealthy)}"
        checkMqtt.text = "MQTT Connected: ${format(mqtt)}"
    }
}

