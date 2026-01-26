package com.blemqttbridge.core

import android.app.*
import android.bluetooth.*
import android.bluetooth.le.*
import android.content.BroadcastReceiver
import android.content.ComponentCallbacks2
import android.content.Context
import android.content.Intent
import android.content.IntentFilter
import android.os.Build
import android.os.IBinder
import android.util.Log
import androidx.core.app.NotificationCompat
import com.blemqttbridge.BuildConfig
import com.blemqttbridge.R
import com.blemqttbridge.core.interfaces.BleDevicePlugin
import com.blemqttbridge.core.interfaces.MqttPublisher
import com.blemqttbridge.core.interfaces.PluginConfig
import com.blemqttbridge.core.interfaces.OutputPluginInterface
import com.blemqttbridge.data.AppSettings
import com.blemqttbridge.plugins.blescanner.BleScannerPlugin
import com.blemqttbridge.plugins.mopeka.MopekaDevicePlugin
import com.blemqttbridge.plugins.onecontrol.OneControlDevicePlugin
import com.blemqttbridge.plugins.output.MqttOutputPlugin
import com.blemqttbridge.utils.AndroidTvHelper
import kotlinx.coroutines.*
import kotlinx.coroutines.flow.first
import java.util.concurrent.ConcurrentLinkedDeque

/**
 * Base BLE service with plugin hooks.
 * Manages BLE scanning, connections, and delegates protocol handling to plugins.
 */
class BaseBleService : Service() {
    
    companion object {
        private const val TAG = "BaseBleService"
        private const val NOTIFICATION_ID = 1
        private const val CHANNEL_ID = "ble_bridge_service"
        
        // Service instance for web server access
        @Volatile
        private var instance: BaseBleService? = null
        
        fun getInstance(): BaseBleService? = instance
        
        const val ACTION_START_SCAN = "com.blemqttbridge.START_SCAN"
        const val ACTION_STOP_SCAN = "com.blemqttbridge.STOP_SCAN"
        const val ACTION_STOP_SERVICE = "com.blemqttbridge.STOP_SERVICE"
        const val ACTION_DISCONNECT = "com.blemqttbridge.DISCONNECT"
        const val ACTION_ADD_PLUGIN = "com.blemqttbridge.ADD_PLUGIN"
        const val ACTION_REMOVE_PLUGIN = "com.blemqttbridge.REMOVE_PLUGIN"
        const val ACTION_CLEAR_PLUGIN_DISCOVERY = "com.blemqttbridge.CLEAR_PLUGIN_DISCOVERY"
        const val ACTION_EXPORT_DEBUG_LOG = "com.blemqttbridge.EXPORT_DEBUG_LOG"
        const val ACTION_START_TRACE = "com.blemqttbridge.START_TRACE"
        const val ACTION_STOP_TRACE = "com.blemqttbridge.STOP_TRACE"
        const val ACTION_KEEPALIVE_PING = "com.blemqttbridge.KEEPALIVE_PING"
        
        // Keepalive interval for Doze mode prevention (30 minutes)
        private const val KEEPALIVE_INTERVAL_MS = 30 * 60 * 1000L // 30 minutes
        
        // BLE timing constants
        private const val BLE_RECONNECT_DELAY_MS = 2000L
        private const val GATT_SETTLE_DELAY_MS = 500L
        
        const val EXTRA_PLUGIN_ID = "plugin_id"
        
        const val EXTRA_BLE_PLUGIN_ID = "ble_plugin_id"
        const val EXTRA_OUTPUT_PLUGIN_ID = "output_plugin_id"
        const val EXTRA_BLE_CONFIG = "ble_config"
        const val EXTRA_OUTPUT_CONFIG = "output_config"
        
        // Debug and trace limits
        private const val MAX_DEBUG_LOG_LINES = 2000
        private const val MAX_BLE_TRACE_LINES = 1000
        private const val TRACE_MAX_BYTES = 10 * 1024 * 1024  // 10 MB
        private const val TRACE_MAX_DURATION_MS = 10 * 60 * 1000L  // 10 minutes
        
        // Service status StateFlows for UI observation
        private val _serviceRunning = kotlinx.coroutines.flow.MutableStateFlow(false)
        val serviceRunning: kotlinx.coroutines.flow.StateFlow<Boolean> = _serviceRunning
        
        // BLE scanning state - tracks if BLE scanner is actively running
        private val _bleScanningActive = kotlinx.coroutines.flow.MutableStateFlow(false)
        val bleScanningActive: kotlinx.coroutines.flow.StateFlow<Boolean> = _bleScanningActive
        
        // Bluetooth availability state - tracks if BT adapter is enabled
        private val _bluetoothAvailable = kotlinx.coroutines.flow.MutableStateFlow(true)
        val bluetoothAvailable: kotlinx.coroutines.flow.StateFlow<Boolean> = _bluetoothAvailable
        
        // Per-plugin status tracking
        data class PluginStatus(
            val pluginId: String,
            val connected: Boolean = false,
            val authenticated: Boolean = false,
            val dataHealthy: Boolean = false
        )
        
        private val _pluginStatuses = kotlinx.coroutines.flow.MutableStateFlow<Map<String, PluginStatus>>(emptyMap())
        val pluginStatuses: kotlinx.coroutines.flow.StateFlow<Map<String, PluginStatus>> = _pluginStatuses
        
        private val _mqttConnected = kotlinx.coroutines.flow.MutableStateFlow(false)
        val mqttConnected: kotlinx.coroutines.flow.StateFlow<Boolean> = _mqttConnected
        
        // Trace status StateFlows for UI observation
        private val _traceActive = kotlinx.coroutines.flow.MutableStateFlow(false)
        val traceActive: kotlinx.coroutines.flow.StateFlow<Boolean> = _traceActive
        
        private val _traceFilePath = kotlinx.coroutines.flow.MutableStateFlow<String?>(null)
        val traceFilePath: kotlinx.coroutines.flow.StateFlow<String?> = _traceFilePath
    }
    
    private val serviceScope = CoroutineScope(Dispatchers.Default + SupervisorJob())
    
    private lateinit var bluetoothAdapter: BluetoothAdapter
    private var bluetoothLeScanner: BluetoothLeScanner? = null
    private var bleNotAvailable: Boolean = false
    private lateinit var pluginRegistry: PluginRegistry
    private lateinit var memoryManager: MemoryManager
    private var alarmManager: AlarmManager? = null
    private var keepAlivePendingIntent: PendingIntent? = null
    
    private var outputPlugin: OutputPluginInterface? = null
    private var bleScannerPlugin: BleScannerPlugin? = null
    
    // Connected devices map: device address -> (BluetoothGatt, pluginId)
    private val connectedDevices = mutableMapOf<String, Pair<BluetoothGatt, String>>()
    
    // Instance to plugin type mapping: instanceId -> pluginType (e.g., "easytouch_b1241e" -> "easytouch")
    private val instancePluginTypes = mutableMapOf<String, String>()
    
    // Devices currently undergoing bonding process
    private val pendingBondDevices = mutableSetOf<String>()
    
    // Pending bond PINs for legacy gateway pairing: device address -> PIN
    private val pendingBondPins = mutableMapOf<String, String>()
    
    // Device names from scan results: device address -> name (prevents 'null' in pairing dialog)
    private val deviceNames = mutableMapOf<String, String>()
    
    private var isScanning = false
    private var bluetoothEnabled = true
    
    // Guard against double initialization during startup race conditions
    @Volatile
    private var isInitializing = false
    private val initializationLock = Any()
    
    // Service debug logging (separate from BLE trace)
    private val serviceLogBuffer = ArrayDeque<String>()
    private val MAX_SERVICE_LOG_LINES = 1000
    
    // BLE trace logging (for BLE events only)
    // Thread-safe: uses ConcurrentLinkedDeque to protect against concurrent access
    // from BLE callbacks and web server threads
    private val bleTraceBuffer = ConcurrentLinkedDeque<String>()
    private val MAX_BLE_TRACE_LINES = 1000
    private var traceEnabled = false
    private var traceWriter: java.io.BufferedWriter? = null
    private var traceFile: java.io.File? = null
    private var traceBytes: Long = 0
    private var traceStartedAt: Long = 0
    private var traceTimeout: Runnable? = null
    private val handler = android.os.Handler(android.os.Looper.getMainLooper())
    
    // Bluetooth state receiver
    private val bluetoothStateReceiver = object : android.content.BroadcastReceiver() {
        override fun onReceive(context: android.content.Context?, intent: android.content.Intent?) {
            val action = intent?.action ?: return
            if (action == BluetoothAdapter.ACTION_STATE_CHANGED) {
                val state = intent.getIntExtra(BluetoothAdapter.EXTRA_STATE, BluetoothAdapter.ERROR)
                handleBluetoothStateChange(state)
            }
        }
    }
    
    /**
     * MQTT Publisher implementation for plugins.
     * Wraps the output plugin to provide a clean interface for BLE plugins.
     */
    private val mqttPublisher = object : MqttPublisher {
        override val topicPrefix: String
            get() = outputPlugin?.getTopicPrefix() ?: "homeassistant"
        
        override fun publishState(topic: String, payload: String, retained: Boolean) {
            Log.v(TAG, "📤 publishState called: topic=$topic, outputPlugin=${outputPlugin != null}")
            serviceScope.launch {
                if (outputPlugin == null) {
                    Log.w(TAG, "❌ outputPlugin is null, cannot publish state to: $topic")
                } else {
                    outputPlugin?.publishState(topic, payload, retained)
                }
            }
        }
        
        override fun publishDiscovery(topic: String, payload: String) {
            Log.d(TAG, "📤 publishDiscovery called: topic=$topic, outputPlugin=${outputPlugin != null}")
            serviceScope.launch {
                if (outputPlugin == null) {
                    Log.w(TAG, "❌ outputPlugin is null, cannot publish discovery to: $topic")
                } else {
                    Log.d(TAG, "✅ Calling outputPlugin.publishDiscovery for: $topic")
                    outputPlugin?.publishDiscovery(topic, payload)
                }
            }
        }
        
        override fun publishAvailability(topic: String, online: Boolean) {
            serviceScope.launch {
                (outputPlugin as? MqttOutputPlugin)?.publishAvailability(topic, online)
                    ?: outputPlugin?.publishAvailability(online)
            }
        }
        
        override fun isConnected(): Boolean {
            return outputPlugin?.isConnected() ?: false
        }
        
        @Suppress("OVERRIDE_DEPRECATION")
        override fun updateDiagnosticStatus(dataHealthy: Boolean) {
            // Deprecated - plugins should use updatePluginStatus instead
            Log.w(TAG, "⚠️ updateDiagnosticStatus called (deprecated) - use updatePluginStatus instead")
        }
        
        @Suppress("OVERRIDE_DEPRECATION")
        override fun updateBleStatus(connected: Boolean, paired: Boolean) {
            // Deprecated - plugins should use updatePluginStatus instead
            Log.w(TAG, "⚠️ updateBleStatus called (deprecated) - use updatePluginStatus instead")
        }
        
        override fun updatePluginStatus(pluginId: String, connected: Boolean, authenticated: Boolean, dataHealthy: Boolean) {
            val status = PluginStatus(pluginId, connected, authenticated, dataHealthy)
            val newStatuses = _pluginStatuses.value.toMutableMap()
            newStatuses[pluginId] = status
            
            // BACKWARD COMPAT: Also store under plugin type for single-instance plugins
            // UI still looks up by plugin type (e.g., "onecontrol" not "onecontrol_ed1e0a")
            val pluginType = instancePluginTypes[pluginId]
            if (pluginType != null && pluginType != pluginId) {
                // This is an instance ID, also store under plugin type
                newStatuses[pluginType] = status
                Log.d(TAG, "📊 Updated plugin status: $pluginId (also as $pluginType) - connected=$connected, authenticated=$authenticated, dataHealthy=$dataHealthy")
            } else {
                Log.d(TAG, "📊 Updated plugin status: $pluginId - connected=$connected, authenticated=$authenticated, dataHealthy=$dataHealthy")
            }
            
            _pluginStatuses.value = newStatuses
            if (connected && authenticated) {
                appendServiceLog("Plugin ready: $pluginId (connected & authenticated)")
            }
        }
        
        override fun updateMqttStatus(connected: Boolean) {
            Log.i(TAG, "📊 updateMqttStatus: connected=$connected (was ${_mqttConnected.value})")
            appendServiceLog("MQTT connection status: ${if (connected) "connected" else "disconnected"}")
            _mqttConnected.value = connected
        }
        
        override fun subscribeToCommands(topicPattern: String, callback: (topic: String, payload: String) -> Unit) {
            serviceScope.launch {
                try {
                    outputPlugin?.subscribeToCommands(topicPattern, callback)
                } catch (e: Exception) {
                    Log.e(TAG, "Failed to subscribe to commands: $topicPattern", e)
                }
            }
        }
        
        override fun logBleEvent(message: String) {
            appendBleTrace(message)
        }
    }

    /**
     * Get the MQTT publisher for use by polling plugins.
     * @return The MqttPublisher instance
     */
    fun getMqttPublisher(): MqttPublisher = mqttPublisher

    /**
     * Clears the internal GATT cache for a device.
     * This is a hidden Android method that resolves status=133 errors caused by stale cached services.
     * Should be called before service discovery when reconnecting to a device.
     */
    private fun refreshGattCache(gatt: BluetoothGatt): Boolean {
        try {
            val refreshMethod = BluetoothGatt::class.java.getMethod("refresh")
            val result = refreshMethod.invoke(gatt) as Boolean
            Log.i(TAG, "🔄 GATT cache refresh: $result")
            return result
        } catch (e: Exception) {
            Log.w(TAG, "GATT cache refresh not available: ${e.message}")
            return false
        }
    }
    
    override fun onCreate() {
        super.onCreate()
        instance = this
        Log.i(TAG, "Service created")
        appendServiceLog("Service created")
        
        val bluetoothManager = getSystemService(Context.BLUETOOTH_SERVICE) as BluetoothManager
        bluetoothAdapter = bluetoothManager.adapter
        bluetoothLeScanner = bluetoothAdapter.bluetoothLeScanner
        
        if (bluetoothLeScanner == null) {
            Log.e(TAG, "BLE Scanner not available - device may not support Bluetooth Low Energy")
            appendServiceLog("ERROR: BLE Scanner not available - device may not support Bluetooth Low Energy")
            bleNotAvailable = true
        }
        
        pluginRegistry = PluginRegistry.getInstance()
        memoryManager = MemoryManager(application)
        memoryManager.setMemoryCallback(object : MemoryManager.MemoryCallback {
            override suspend fun onMemoryPressure(level: Int) {
                handleMemoryPressure(level)
            }
        })
        memoryManager.initialize()
        
        val bondFilter = IntentFilter(BluetoothDevice.ACTION_BOND_STATE_CHANGED)
        registerReceiver(bondStateReceiver, bondFilter)
        Log.d(TAG, "Bond state receiver registered")
        
        val pairingFilter = IntentFilter(BluetoothDevice.ACTION_PAIRING_REQUEST)
        pairingFilter.priority = IntentFilter.SYSTEM_HIGH_PRIORITY
        registerReceiver(pairingRequestReceiver, pairingFilter)
        Log.d(TAG, "Pairing request receiver registered with high priority")
        
        val btStateFilter = IntentFilter(BluetoothAdapter.ACTION_STATE_CHANGED)
        registerReceiver(bluetoothStateReceiver, btStateFilter)
        bluetoothEnabled = bluetoothAdapter.isEnabled
        Log.d(TAG, "Bluetooth state receiver registered (BT currently ${if (bluetoothEnabled) "ON" else "OFF"})")
        
        alarmManager = getSystemService(Context.ALARM_SERVICE) as AlarmManager
        
        if (AndroidTvHelper.applyRecommendedSettings(this)) {
            Log.i(TAG, "📺 Android TV: Applied HDMI-CEC fix for service reliability")
        }
        
        createNotificationChannel()
        startForeground(NOTIFICATION_ID, createNotification("Service starting..."))
        
        if (isKeepAliveEnabled()) {
            Log.i(TAG, "⏰ Scheduling keepalive from onCreate() (backup path)")
            scheduleKeepAlive()
        } else {
            Log.i(TAG, "⏰ Keepalive is disabled, skipping schedule")
        }
    }

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        Log.i(TAG, "⚙️ onStartCommand: action=${intent?.action ?: "null"}, startId=$startId")
        appendServiceLog("onStartCommand: action=${intent?.action ?: "null"}")
        
        when (intent?.action) {
            ACTION_START_SCAN -> {
                synchronized(initializationLock) {
                    if (isInitializing) {
                        Log.i(TAG, "⚙️ Skipping ACTION_START_SCAN - already initializing")
                        return START_STICKY
                    }
                    isInitializing = true
                }
                
                ServiceStateManager.setServiceRunning(applicationContext, true)
                _serviceRunning.value = true
                Log.i(TAG, "Service marked as running")
                
                val enabledBlePlugins = ServiceStateManager.getEnabledBlePlugins(applicationContext)
                val outputPluginId = intent.getStringExtra(EXTRA_OUTPUT_PLUGIN_ID) ?: "mqtt"
                val outputConfig = AppConfig.getMqttConfig(applicationContext)
                
                serviceScope.launch {
                    try {
                        initializeMultiplePlugins(enabledBlePlugins, outputPluginId, outputConfig)
                        if (isKeepAliveEnabled()) {
                            Log.i(TAG, "⏰ Scheduling keepalive from ACTION_START_SCAN (primary path)")
                            scheduleKeepAlive()
                        } else {
                            Log.i(TAG, "⏰ Keepalive is disabled")
                        }
                    } finally {
                        isInitializing = false
                    }
                }
            }
            null, "START_WEB_SERVER" -> {
                Log.i(TAG, "⚙️ Service started with action=${intent?.action ?: "null"} - checking if service should auto-start")
                
                serviceScope.launch {
                    synchronized(initializationLock) {
                        if (isInitializing) {
                            Log.i(TAG, "⚙️ Skipping auto-start - already initializing from another path")
                            return@launch
                        }
                    }
                    
                    val settings = AppSettings(applicationContext)
                    val serviceEnabled = settings.serviceEnabled.first()
                    
                    if (serviceEnabled) {
                        synchronized(initializationLock) {
                            if (isInitializing) {
                                Log.i(TAG, "⚙️ Skipping auto-start - initialization started by another path")
                                return@launch
                            }
                            isInitializing = true
                        }
                        
                        try {
                            Log.i(TAG, "⚙️ Service was enabled in settings - auto-starting BLE scanning")
                            appendServiceLog("Auto-starting BLE scanning (serviceEnabled=true in settings)")
                            
                            ServiceStateManager.setServiceRunning(applicationContext, true)
                            _serviceRunning.value = true
                            
                            val enabledBlePlugins = ServiceStateManager.getEnabledBlePlugins(applicationContext)
                            val outputPluginId = "mqtt"
                            val outputConfig = AppConfig.getMqttConfig(applicationContext)
                            
                            initializeMultiplePlugins(enabledBlePlugins, outputPluginId, outputConfig)
                            
                            if (isKeepAliveEnabled()) {
                                Log.i(TAG, "⏰ Scheduling keepalive from auto-start path")
                                scheduleKeepAlive()
                            }
                        } finally {
                            isInitializing = false
                        }
                    } else {
                        Log.i(TAG, "⚙️ Service not enabled in settings - web server only mode")
                        appendServiceLog("Service not enabled in settings - running web server only")
                        
                        if (isKeepAliveEnabled() && keepAlivePendingIntent == null) {
                            Log.i(TAG, "⏰ Keepalive not yet scheduled, scheduling now")
                            scheduleKeepAlive()
                        }
                    }
                }
            }
            ACTION_STOP_SCAN -> {
                stopScanning()
            }
            ACTION_STOP_SERVICE -> {
                Log.i(TAG, "Stopping service...")
                stopScanning()
                disconnectAll()
                @Suppress("DEPRECATION")
                stopForeground(true)
                stopSelf()
            }
            ACTION_DISCONNECT -> {
                disconnectAll()
            }
            ACTION_REMOVE_PLUGIN -> {
                val pluginId = intent.getStringExtra(EXTRA_PLUGIN_ID)
                if (pluginId != null) {
                    Log.i(TAG, "Removing plugin: $pluginId")
                    disablePluginKeepConnection(pluginId)
                } else {
                    Log.w(TAG, "REMOVE_PLUGIN action missing plugin_id extra")
                }
            }
            ACTION_ADD_PLUGIN -> {
                val pluginId = intent.getStringExtra(EXTRA_PLUGIN_ID)
                if (pluginId != null) {
                    Log.i(TAG, "Re-enabling plugin: $pluginId")
                    enablePluginResumeConnection(pluginId)
                } else {
                    Log.w(TAG, "ADD_PLUGIN action missing plugin_id extra")
                }
            }
            ACTION_CLEAR_PLUGIN_DISCOVERY -> {
                val pluginId = intent.getStringExtra(EXTRA_PLUGIN_ID)
                if (pluginId != null) {
                    Log.i(TAG, "Clearing discovery for plugin: $pluginId")
                    serviceScope.launch {
                        clearPluginDiscovery(pluginId)
                    }
                } else {
                    Log.w(TAG, "CLEAR_PLUGIN_DISCOVERY action missing plugin_id extra")
                }
            }
            ACTION_EXPORT_DEBUG_LOG -> {
                val file = exportDebugLog()
                if (file != null && file.exists()) {
                    shareFile(file, "text/plain")
                } else {
                    android.widget.Toast.makeText(this, "Could not create debug log", android.widget.Toast.LENGTH_SHORT).show()
                }
            }
            ACTION_START_TRACE -> {
                val file = startBleTrace()
                if (file != null) {
                    android.widget.Toast.makeText(this, "Trace started", android.widget.Toast.LENGTH_SHORT).show()
                } else {
                    android.widget.Toast.makeText(this, "Failed to start trace", android.widget.Toast.LENGTH_SHORT).show()
                }
            }
            ACTION_STOP_TRACE -> {
                val file = stopBleTrace("user stop")
                if (file != null && file.exists()) {
                    shareFile(file, "text/plain")
                } else {
                    android.widget.Toast.makeText(this, "Trace stopped (no file created)", android.widget.Toast.LENGTH_SHORT).show()
                }
            }
            ACTION_KEEPALIVE_PING -> {
                Log.d(TAG, "⏰ Keepalive ping received")
                serviceScope.launch {
                    performKeepAlivePing()
                }
            }
        }
        
        return START_STICKY
    }
    
    override fun onBind(intent: Intent?): IBinder? = null
    
    override fun onDestroy() {
        super.onDestroy()
        Log.i(TAG, "Service destroyed")
        appendServiceLog("Service destroyed")
        
        ServiceStateManager.setServiceRunning(applicationContext, false)
        _serviceRunning.value = false
        _pluginStatuses.value = emptyMap()
        _mqttConnected.value = false
        Log.i(TAG, "Service marked as stopped")
        
        stopScanning()
        disconnectAll()
        
        try {
            unregisterReceiver(bondStateReceiver)
            Log.d(TAG, "Bond state receiver unregistered")
        } catch (e: IllegalArgumentException) {
            Log.w(TAG, "Bond state receiver not registered")
        }
        
        try {
            unregisterReceiver(pairingRequestReceiver)
            Log.d(TAG, "Pairing request receiver unregistered")
        } catch (e: IllegalArgumentException) {
            Log.w(TAG, "Pairing request receiver not registered")
        }
        
        try {
            unregisterReceiver(bluetoothStateReceiver)
            Log.d(TAG, "Bluetooth state receiver unregistered")
        } catch (e: IllegalArgumentException) {
            Log.w(TAG, "Bluetooth state receiver not registered")
        }
        
        cancelKeepAlive()
        instance = null
        
        serviceScope.launch {
            pluginRegistry.cleanup()
            outputPlugin?.disconnect()
            outputPlugin = null
            memoryManager.logMemoryUsage()
        }
    }
    
    /**
     * Initialize plugins.
     */
    private suspend fun initializePlugins(
        blePluginId: String,
        outputPluginId: String,
        _bleConfig: Map<String, String>,
        outputConfig: Map<String, String>
    ) {
        Log.i(TAG, "Initializing plugins: BLE=$blePluginId, Output=$outputPluginId")
        appendServiceLog("Initializing plugins: BLE=$blePluginId, Output=$outputPluginId")
        
        // Load output plugin first (needed for publishing)
        // Note: Output plugin is optional for testing (MQTT needs broker config)
        outputPlugin = pluginRegistry.getOutputPlugin(outputPluginId, applicationContext, outputConfig)
        if (outputPlugin == null) {
            Log.w(TAG, "Output plugin $outputPluginId not available (may need configuration)")
            Log.i(TAG, "Continuing without output plugin for BLE testing")
            _mqttConnected.value = false
        } else {
            // Set up connection status listener BEFORE initialize so we catch connection state changes
            outputPlugin?.setConnectionStatusListener(object : OutputPluginInterface.ConnectionStatusListener {
                override fun onConnectionStatusChanged(connected: Boolean) {
                    Log.i(TAG, "✅ MQTT connection status changed: $connected")
                    _mqttConnected.value = connected
                }
            })
        }
        
        // Load BLE device plugin (new architecture)
        val devicePlugin = pluginRegistry.getDevicePlugin(blePluginId, applicationContext)
        if (devicePlugin == null) {
            Log.e(TAG, "Failed to load BLE plugin: $blePluginId (not found in registry)")
            appendServiceLog("ERROR: Failed to load BLE plugin: $blePluginId (not found in registry)")
            updateNotification("Error: BLE plugin failed to load")
            return
        } else {
            Log.i(TAG, "Loaded device plugin: $blePluginId (plugin-owned GATT callback)")
            appendServiceLog("Loaded BLE plugin: $blePluginId (device plugin)")
        }
        
        Log.i(TAG, "Plugins initialized successfully")
        memoryManager.logMemoryUsage()
        
        // CRITICAL: Try to reconnect to bonded devices first!
        // Many BLE devices don't actively advertise when bonded - they wait for reconnection
        reconnectToBondedDevices()
    }
    
    /**
     * Initialize multiple BLE plugins from instances.
     * Phase 4: Loads plugin instances from ServiceStateManager instead of old enabled_ble_plugins format.
     * Performs migration if needed, then creates instance-based plugins.
     */
    private suspend fun initializeMultiplePlugins(
        _enabledBlePlugins: Set<String>,
        outputPluginId: String,
        outputConfig: Map<String, String>
    ) {
        Log.i(TAG, "🔄 Phase 4: Initializing plugins from instances...")
        
        // CRITICAL: Clear any previously loaded plugins to ensure fresh state
        Log.i(TAG, "Clearing previously loaded plugins before initialization...")
        pluginRegistry.cleanup()
        
        // Phase 4: Check if migration is needed and perform it
        if (ServiceStateManager.needsMigration(applicationContext)) {
            Log.i(TAG, "🔄 Migration needed: Converting old format to PluginInstance format")
            ServiceStateManager.migrateToInstances(applicationContext)
            appendServiceLog("Migrated legacy plugins to instance format")
        } else {
            Log.i(TAG, "✓ No migration needed (already using instance format)")
        }
        
        // Load output plugin first (needed for publishing)
        outputPlugin = pluginRegistry.getOutputPlugin(outputPluginId, applicationContext, outputConfig)
        if (outputPlugin == null) {
            Log.w(TAG, "Output plugin $outputPluginId not available (may need configuration)")
            Log.i(TAG, "Continuing without output plugin for BLE testing")
            appendServiceLog("WARNING: Output plugin $outputPluginId not available")
            _mqttConnected.value = false
        } else {
            appendServiceLog("Loaded output plugin: $outputPluginId")
            // Set up connection status listener BEFORE initialize so we catch connection state changes
            outputPlugin?.setConnectionStatusListener(object : OutputPluginInterface.ConnectionStatusListener {
                override fun onConnectionStatusChanged(connected: Boolean) {
                    Log.i(TAG, "✅ MQTT connection status changed: $connected")
                    _mqttConnected.value = connected
                }
            })
            
        }
        
        // Phase 4: Load all plugin instances from ServiceStateManager
        val allInstances = ServiceStateManager.getAllInstances(applicationContext)
        val allPollingInstances = ServiceStateManager.getAllPollingInstances(applicationContext)
        Log.i(TAG, "📦 Found ${allInstances.size} BLE plugin instance(s) and ${allPollingInstances.size} polling plugin instance(s)")

        if (allInstances.isEmpty() && allPollingInstances.isEmpty()) {
            Log.e(TAG, "No plugin instances configured - cannot start service")
            appendServiceLog("ERROR: No plugin instances configured")
            updateNotification("Error: No plugins loaded")
            return
        } else if (allInstances.isEmpty()) {
            // No BLE plugins but polling plugins exist - keep service running for MQTT support
            Log.i(TAG, "No BLE plugins configured, but polling plugins need MQTT - keeping service running")
            appendServiceLog("INFO: Service running for polling plugin MQTT support")
            updateNotification("Running (MQTT only)")
            return
        } else {
            // Load all instances via PluginRegistry
            var loadedCount = 0
            for ((instanceId, instance) in allInstances) {
                try {
                    Log.i(TAG, "Loading instance: $instanceId (type: ${instance.pluginType}, device: ${instance.deviceMac})")
                    
                    // Special handling for BLE Scanner (not a BleDevicePlugin)
                    if (instance.pluginType == BleScannerPlugin.PLUGIN_ID) {
                        bleScannerPlugin = BleScannerPlugin(applicationContext, mqttPublisher)
                        if (bleScannerPlugin?.initialize() == true) {
                            Log.i(TAG, "✓ BLE Scanner plugin initialized")
                            // Add to plugin statuses (BLE Scanner is always "healthy" when running)
                            _pluginStatuses.value = _pluginStatuses.value + (instanceId to PluginStatus(instanceId, true, true, true))
                            appendServiceLog("✓ Loaded BLE Scanner")
                            loadedCount++
                        } else {
                            Log.w(TAG, "✗ BLE Scanner plugin failed to initialize")
                            bleScannerPlugin = null
                        }
                        continue
                    }
                    
                    val plugin = pluginRegistry.createPluginInstance(instance, applicationContext)
                    if (plugin != null) {
                        // Track instance -> plugin type mapping for connectToDevice lookup
                        instancePluginTypes[instanceId] = instance.pluginType
                        // Update status for this instance (store under both instance ID and plugin type for backward compat)
                        val initialStatus = PluginStatus(instanceId, false, false, false)
                        val newStatuses = _pluginStatuses.value.toMutableMap()
                        newStatuses[instanceId] = initialStatus
                        newStatuses[instance.pluginType] = initialStatus  // Also store under plugin type
                        _pluginStatuses.value = newStatuses
                        appendServiceLog("✓ Loaded instance: $instanceId (${instance.pluginType})")
                        loadedCount++
                    } else {
                        Log.w(TAG, "✗ Failed to create instance: $instanceId")
                        appendServiceLog("✗ Failed to load instance: $instanceId")
                    }
                } catch (e: Exception) {
                    Log.e(TAG, "Exception loading instance $instanceId", e)
                    appendServiceLog("ERROR loading instance $instanceId: ${e.message}")
                }
            }
            
            if (loadedCount == 0) {
                Log.e(TAG, "No instances were loaded!")
                appendServiceLog("ERROR: No plugin instances were loaded!")
                updateNotification("Error: No plugins loaded")
                return
            }
            
            Log.i(TAG, "✓ Loaded $loadedCount/${allInstances.size} plugin instance(s) successfully")
        }
        
        memoryManager.logMemoryUsage()
        
        // Cleanup: Remove plugin types from enabled_ble_plugins that should only be instances
        // The enabled set should ONLY contain instanceIds, not plugin types
        val enabledPlugins = ServiceStateManager.getEnabledBlePlugins(applicationContext).toMutableSet()
        val pluginTypes = setOf("onecontrol", "onecontrol_v2", "easytouch", "gopower", "mopeka", "hughes_watchdog", "blescanner")
        val removedPluginTypes = enabledPlugins.filter { it in pluginTypes }
        if (removedPluginTypes.isNotEmpty()) {
            Log.i(TAG, "🔄 Cleanup: Removing legacy plugin types from enabled set: $removedPluginTypes")
            enabledPlugins.removeAll(pluginTypes)
            ServiceStateManager.setEnabledBlePlugins(applicationContext, enabledPlugins)
        }
        
        // Migration: Ensure all loaded instances are in the enabled plugins list
        // (for instances added before multi-instance support was finalized)
        val currentEnabled = ServiceStateManager.getEnabledBlePlugins(applicationContext)
        val needsUpdate = allInstances.keys.any { instanceId ->
            !currentEnabled.contains(instanceId)
        }
        if (needsUpdate) {
            Log.i(TAG, "🔄 Migration: Auto-enabling discovered plugin instances")
            for (instanceId in allInstances.keys) {
                if (!currentEnabled.contains(instanceId)) {
                    ServiceStateManager.enableBlePlugin(applicationContext, instanceId)
                    Log.i(TAG, "✓ Auto-enabled instance: $instanceId")
                }
            }
        }

        // Note: Polling plugins (HTTP/REST-based) are managed independently via web UI
        // They are not auto-started with the BLE service

        // Try to reconnect to bonded devices or scan for new ones
        reconnectToBondedDevices()
    }
    
    /**
     * Load a single plugin dynamically without restarting the service.
     * Used when user adds a plugin from the UI.
     */
    private suspend fun loadPluginDynamically(pluginId: String) {
        // Map UI plugin IDs to internal plugin IDs
        val internalPluginId = when (pluginId) {
            "onecontrol" -> "onecontrol_v2"
            else -> pluginId
        }
        
        Log.i(TAG, "Loading plugin dynamically: $pluginId (internal: $internalPluginId)")
        
        // CRITICAL: Unload any existing instance first to prevent duplicate heartbeats/resources
        pluginRegistry.unloadDevicePlugin(internalPluginId)
        
        // Give BLE stack time to fully release any previous connections
        // Without this, writeCharacteristicNoResponse may fail with result=false
        delay(GATT_SETTLE_DELAY_MS)
        
        val bleConfig = AppConfig.getBlePluginConfig(applicationContext, internalPluginId)
        Log.i(TAG, "Plugin config: $bleConfig")
        
        val devicePlugin = pluginRegistry.getDevicePlugin(internalPluginId, applicationContext)
        if (devicePlugin != null) {
            val config = PluginConfig(parameters = bleConfig)
            devicePlugin.initialize(applicationContext, config)
            Log.i(TAG, "✓ Dynamically loaded device plugin: $internalPluginId (target MACs: ${devicePlugin.getConfiguredDevices()})")
            serviceScope.launch {
                delay(BLE_RECONNECT_DELAY_MS)
                Log.i(TAG, "🔄 Attempting reconnection for newly added plugin: $internalPluginId")
                connectToPluginDevices(internalPluginId)
            }
        } else {
            Log.w(TAG, "✗ Failed to load plugin dynamically: $internalPluginId (not found in registry)")
        }
    }
    
    /**
     * Connect to devices for a specific plugin.
     * Used when dynamically adding a plugin.
     */
    private suspend fun connectToPluginDevices(pluginId: String) {
        val devicePlugin = pluginRegistry.getDevicePlugin(pluginId, applicationContext)
        if (devicePlugin == null) {
            Log.w(TAG, "No device plugin found for: $pluginId")
            return
        }
        
        // Check if any bonded device matches this plugin
        try {
            val bondedDevices = bluetoothAdapter.bondedDevices
            for (device in bondedDevices) {
                if (devicePlugin.matchesDevice(device, null)) {
                    Log.i(TAG, "🔗 Found bonded device for newly added plugin: ${device.address} -> $pluginId")
                    // Check if already connected
                    if (!connectedDevices.containsKey(device.address)) {
                        appendServiceLog("Reconnecting to bonded device: ${device.address} (plugin: $pluginId)")
                        connectToDevice(device, pluginId)
                    } else {
                        Log.d(TAG, "Device ${device.address} already connected")
                    }
                }
            }
        } catch (e: SecurityException) {
            Log.e(TAG, "Permission denied for accessing bonded devices", e)
        }
        
        // Also check configured MAC addresses
        val configuredMacs = devicePlugin.getConfiguredDevices()
        for (mac in configuredMacs) {
            if (!connectedDevices.containsKey(mac)) {
                try {
                    val device = bluetoothAdapter.getRemoteDevice(mac)
                    Log.i(TAG, "🔗 Connecting to configured device for newly added plugin: $mac -> $pluginId")
                    connectToDevice(device, pluginId)
                } catch (e: Exception) {
                    Log.e(TAG, "Failed to get remote device $mac", e)
                }
            }
        }
    }

    /**
     * Connect to known devices - both bonded and configured.
     * Phase 4: Updated to work with plugin instances instead of legacy format.
     * 
     * This replaces continuous scanning with direct connections:
     * 1. Bonded devices: Connect directly (they may not advertise when bonded)
     * 2. Configured devices: Connect directly using MAC addresses from instances
     * 
     * Scanning is only needed for device discovery, not for connecting to known devices.
     */
    private fun reconnectToBondedDevices() {
        val devicesToConnect = mutableMapOf<String, String>() // MAC -> instanceId
        
        // Phase 4: Get all loaded instances and their configured MACs
        val allInstances = ServiceStateManager.getAllInstances(applicationContext)
        Log.i(TAG, "🔄 Phase 4: Reconnecting ${allInstances.size} instances to their devices...")
        
        // 1. For each instance, connect to its configured device MAC (if it requires GATT connection)
        for ((instanceId, instance) in allInstances) {
            // Check if this plugin requires GATT connection
            val devicePlugin = pluginRegistry.getPluginInstance(instanceId)
            val configuredDevices = devicePlugin?.getConfiguredDevices() ?: emptyList()
            
            if (configuredDevices.isEmpty()) {
                // Plugin uses passive scanning (e.g., Mopeka) - skip GATT connection
                Log.i(TAG, "⏭️ Skipping GATT connection for passive scan plugin: $instanceId (${instance.pluginType})")
                continue
            }
            
            val deviceMac = instance.deviceMac.uppercase()
            if (devicesToConnect.containsKey(deviceMac)) {
                Log.w(TAG, "Device $deviceMac already assigned to another instance, skipping $instanceId")
            } else {
                Log.i(TAG, "🔗 Found instance device: $deviceMac -> $instanceId (${instance.pluginType})")
                devicesToConnect[deviceMac] = instanceId
            }
        }
        
        // 2. If no instances, fall back to legacy bonded device matching (backward compat)
        if (devicesToConnect.isEmpty()) {
            Log.w(TAG, "No instances configured, falling back to legacy bonded device matching...")
            try {
                val bondedDevices = bluetoothAdapter.bondedDevices
                Log.i(TAG, "Checking ${bondedDevices.size} bonded device(s) for plugin matches...")
                
                for (device in bondedDevices) {
                    val pluginId = pluginRegistry.findPluginForDevice(device, null, applicationContext)
                    if (pluginId != null) {
                        Log.i(TAG, "🔗 Found bonded device matching plugin: ${device.address} -> $pluginId")
                        devicesToConnect[device.address.uppercase()] = pluginId
                    }
                }
            } catch (e: SecurityException) {
                Log.e(TAG, "Permission denied for accessing bonded devices", e)
            }
        }
        
        // 3. Connect to all known devices
        if (devicesToConnect.isEmpty()) {
            Log.w(TAG, "No known devices to connect to")
            updateNotification("No devices configured")
            return
        }
        
        Log.i(TAG, "✓ Found ${devicesToConnect.size} device(s) to connect")
        
        for ((mac, instanceId) in devicesToConnect) {
            serviceScope.launch {
                try {
                    val device = bluetoothAdapter.getRemoteDevice(mac)
                    Log.i(TAG, "🔗 Connecting directly to $mac (instance: $instanceId)")
                    // For instances, connect using instanceId instead of pluginId
                    connectToDevice(device, instanceId)
                } catch (e: Exception) {
                    Log.e(TAG, "Failed to get remote device $mac: ${e.message}")
                }
            }
        }
        
        // 4. Check if any passive scan plugins are enabled (e.g., Mopeka)
        // If so, start BLE scanning to receive advertisements
        val hasPassivePlugins = allInstances.values.any { instance ->
            val devicePlugin = pluginRegistry.getPluginInstance(instance.instanceId)
            devicePlugin?.getConfiguredDevices()?.isEmpty() == true
        }
        
        if (hasPassivePlugins) {
            Log.i(TAG, "📊 Passive scan plugins detected - starting BLE scanning for advertisements")
            serviceScope.launch {
                // Give active plugins time to establish GATT connections first
                delay(BLE_RECONNECT_DELAY_MS)
                startScanning()
            }
        }
    }
    
    /**
     * Start BLE scanning for devices.
     * Uses scan filters to allow scanning when screen is off (Android 8.1+ requirement).
     */
    private fun startScanning() {
        if (isScanning) {
            Log.d(TAG, "Already scanning")
            return
        }
        
        // Build scan filters from ALL plugin types
        // CRITICAL: Android 8.1+ blocks unfiltered scans when screen is off
        // Using filters allows scanning to continue with screen locked
        val scanFilters = mutableListOf<ScanFilter>()
        val addedMacs = mutableSetOf<String>() // Track MACs to avoid duplicates
        val enabledPlugins = ServiceStateManager.getEnabledBlePlugins(applicationContext)
        
        // Get all plugin instances (for multi-instance support)
        val allInstances = ServiceStateManager.getAllInstances(applicationContext)
        val enabledInstanceIds = allInstances.keys.filter { instanceId ->
            // Instance is enabled if:
            // 1. Full instance ID is explicitly in enabled list (new style: "easytouch_b1241e")
            // 2. OR the plugin type is in enabled list (legacy style: "easytouch")
            val pluginType = instanceId.substringBefore("_")
            enabledPlugins.contains(instanceId) || enabledPlugins.contains(pluginType)
        }.toSet()
        
        // For multi-instance plugins, process each enabled instance
        var hasMopekaInstances = false
        for ((instanceId, instance) in allInstances) {
            if (!enabledInstanceIds.contains(instanceId)) {
                Log.d(TAG, "Skipping disabled instance for scan filters: $instanceId")
                continue
            }
            
            // Track if we have any Mopeka instances
            if (instance.pluginType == "mopeka") {
                hasMopekaInstances = true
            }
            
            // Get the already-initialized plugin instance from the registry
            val instancePlugin = pluginRegistry.getPluginInstance(instanceId)
            if (instancePlugin != null) {
                try {
                    val configuredMacs = instancePlugin.getConfiguredDevices()
                    
                    // For active GATT plugins, add MACs from getConfiguredDevices()
                    for (mac in configuredMacs) {
                        if (addedMacs.add(mac.uppercase())) {
                            Log.d(TAG, "Adding scan filter for MAC: $mac (instance: $instanceId)")
                            scanFilters.add(
                                ScanFilter.Builder()
                                    .setDeviceAddress(mac.uppercase())
                                    .build()
                            )
                        }
                    }
                    
                    // For passive scan plugins (getConfiguredDevices() is empty), add instance MAC
                    if (configuredMacs.isEmpty()) {
                        val mac = instance.deviceMac
                        if (mac.isNotEmpty() && addedMacs.add(mac.uppercase())) {
                            Log.d(TAG, "Adding scan filter for MAC: $mac (passive instance: $instanceId)")
                            scanFilters.add(
                                ScanFilter.Builder()
                                    .setDeviceAddress(mac.uppercase())
                                    .build()
                            )
                        }
                    }
                } catch (e: Exception) {
                    Log.w(TAG, "Could not get configured devices for instance $instanceId: ${e.message}")
                }
            } else {
                Log.d(TAG, "Instance not yet loaded: $instanceId (will be loaded when device is discovered)")
            }
        }
        
        // Add manufacturer data filter for Mopeka sensors (0x0059)
        // This allows detecting ANY Mopeka sensor, enabling background scanning
        // The individual MAC matching happens in matchesDevice() after detection
        if (hasMopekaInstances) {
            Log.d(TAG, "Adding manufacturer data filter for Mopeka (0x0059)")
            scanFilters.add(
                ScanFilter.Builder()
                    .setManufacturerData(0x0059, byteArrayOf())  // Match any Mopeka device
                    .build()
            )
        }
        
        // If no specific targets, add a permissive filter (allows screen-off scanning)
        if (scanFilters.isEmpty()) {
            Log.w(TAG, "No target MACs configured - using unfiltered scan (may not work with screen off)")
            appendServiceLog("WARNING: Starting BLE scan with no device filters")
        }
        
        // Check if Bluetooth is enabled before scanning
        if (!bluetoothAdapter.isEnabled) {
            Log.w(TAG, "⚠️ Cannot start scan: Bluetooth is disabled")
            _bluetoothAvailable.value = false
            updateNotification("Bluetooth is disabled")
            return
        }
        _bluetoothAvailable.value = true
        
        // Check if BLE scanner is available
        val scanner = bluetoothLeScanner
        if (scanner == null) {
            Log.e(TAG, "Cannot start scan: BLE not available on this device")
            updateNotification("BLE not available")
            return
        }
        
        val scanSettings = ScanSettings.Builder()
            .setScanMode(ScanSettings.SCAN_MODE_LOW_LATENCY)
            .build()
        
        appendServiceLog("Starting BLE scan with ${scanFilters.size} device filters")
        try {
            // Use scan filters for known devices - required for background scanning on Android 8.1+
            // Filters allow scanning to continue when screen is off (Doze mode)
            if (scanFilters.isNotEmpty()) {
                Log.i(TAG, "📡 Starting filtered BLE scan with ${scanFilters.size} filter(s)")
                scanner.startScan(scanFilters, scanSettings, scanCallback)
            } else {
                Log.w(TAG, "⚠️ No scan filters configured - using unfiltered scan (may stop when screen off)")
                scanner.startScan(null, scanSettings, scanCallback)
            }
            isScanning = true
            _bleScanningActive.value = true
            updateNotification("Scanning for devices...")
            Log.i(TAG, "BLE scan started")
        } catch (e: SecurityException) {
            Log.e(TAG, "Permission denied for BLE scan", e)
            updateNotification("Error: BLE permission denied")
        } catch (e: IllegalStateException) {
            Log.e(TAG, "Bluetooth adapter state error", e)
            updateNotification("Bluetooth error")
        }
    }
    
    /**
     * Stop BLE scanning.
     */
    private fun stopScanning() {
        if (!isScanning) return
        
        val scanner = bluetoothLeScanner ?: return
        
        try {
            scanner.stopScan(scanCallback)
            isScanning = false
            _bleScanningActive.value = false
            Log.i(TAG, "BLE scan stopped")
        } catch (e: SecurityException) {
            Log.e(TAG, "Permission denied for stopping scan", e)
        }
    }
    
    /**
     * Check if any passive scan plugins (like Mopeka) are enabled.
     * Passive plugins don't use GATT connections - they read BLE advertisements directly.
     */
    private fun hasPassivePluginsEnabled(): Boolean {
        val allInstances = ServiceStateManager.getAllInstances(applicationContext)
        return allInstances.values.any { instance ->
            val devicePlugin = pluginRegistry.getPluginInstance(instance.instanceId)
            devicePlugin?.getConfiguredDevices()?.isEmpty() == true
        }
    }
    
    /**
     * Resume scanning if passive plugins need it.
     * Called after GATT connections are established to ensure passive plugins
     * (like Mopeka) continue receiving BLE advertisements.
     */
    private fun resumeScanningForPassivePlugins() {
        if (hasPassivePluginsEnabled() && !isScanning) {
            serviceScope.launch {
                delay(GATT_SETTLE_DELAY_MS)  // Brief delay to avoid BLE stack contention
                Log.i(TAG, "📊 Resuming scan for passive plugins (Mopeka, etc.)")
                startScanning()
            }
        }
    }
    
    /**
     * Handle Bluetooth adapter state changes.
     */
    private fun handleBluetoothStateChange(state: Int) {
        when (state) {
            BluetoothAdapter.STATE_OFF -> {
                Log.w(TAG, "⚠️ Bluetooth turned OFF")
                bluetoothEnabled = false
                _bluetoothAvailable.value = false
                
                // Stop scanning if active
                if (isScanning) {
                    isScanning = false  // Set directly without calling stopScan (BT is off)
                    _bleScanningActive.value = false
                    Log.i(TAG, "Scanning stopped due to Bluetooth OFF")
                }
                
                updateNotification("Bluetooth is OFF - waiting...")
                
                // Disconnect all devices gracefully
                serviceScope.launch {
                    Log.i(TAG, "Disconnecting ${connectedDevices.size} device(s) due to BT OFF")
                    disconnectAll()
                }
            }
            
            BluetoothAdapter.STATE_ON -> {
                Log.i(TAG, "✅ Bluetooth turned ON")
                bluetoothEnabled = true
                _bluetoothAvailable.value = true
                
                updateNotification("Bluetooth restored - reconnecting...")
                
                // Wait a bit for BT stack to stabilize, then reconnect
                serviceScope.launch {
                    delay(BLE_RECONNECT_DELAY_MS)
                    Log.i(TAG, "Attempting to reconnect devices after BT restore")
                    reconnectToBondedDevices()
                }
            }
            
            BluetoothAdapter.STATE_TURNING_OFF -> {
                Log.w(TAG, "⚠️ Bluetooth turning OFF...")
                updateNotification("Bluetooth turning off...")
            }
            
            BluetoothAdapter.STATE_TURNING_ON -> {
                Log.i(TAG, "Bluetooth turning ON...")
                updateNotification("Bluetooth turning on...")
            }
        }
    }
    
    /**
     * BLE scan callback.
     */
    private val scanCallback = object : ScanCallback() {
        private var scanResultCount = 0
        
        override fun onScanResult(callbackType: Int, result: ScanResult) {
            val device = result.device
            val scanRecordBytes = result.scanRecord?.bytes
            
            // Store device name from scan result (prevents 'null' in pairing dialog)
            val deviceName = device.name ?: result.scanRecord?.deviceName
            if (deviceName != null) {
                deviceNames[device.address] = deviceName
            }
            
            scanResultCount++
            // Log every 10th device to avoid spam, but always log our target devices
            if (device.address.contains("24:DC:C3", ignoreCase = true) || 
                device.address.contains("1E:0A", ignoreCase = true) ||
                device.address.contains("C4:39:95", ignoreCase = true) ||
                device.address.contains("DD:69:F4", ignoreCase = true) ||
                scanResultCount % 10 == 1) {
                Log.d(TAG, "📡 Scan result #$scanResultCount: ${device.address} (name: ${deviceName ?: "?"})")
            }
            
            // Check if we already have this device
            if (connectedDevices.containsKey(device.address)) {
                return
            }
            
            // Check if any plugin can handle this device
            val pluginId = pluginRegistry.findPluginForDevice(device, scanRecordBytes, applicationContext)
            if (pluginId != null) {
                Log.i(TAG, "✅ Found matching device: ${device.address} -> plugin: $pluginId")
                appendServiceLog("Found device: ${device.address} (plugin: $pluginId)")
                
                // Find the specific instance for this device MAC (for multi-instance support)
                val matchingInstanceId = ServiceStateManager.getAllInstances(applicationContext)
                    .entries
                    .firstOrNull { (_, instance) -> 
                        instance.deviceMac.equals(device.address, ignoreCase = true)
                    }?.key
                
                // Get the plugin instance (either specific instance or generic plugin)
                val instanceId = matchingInstanceId ?: pluginId
                val devicePlugin = pluginRegistry.getPluginInstance(instanceId)
                val requiresConnection = devicePlugin?.getConfiguredDevices()?.isNotEmpty() ?: true
                
                if (!requiresConnection) {
                    // Passive scan plugin - just pass the scan result, don't connect
                    Log.i(TAG, "📊 Passive scan plugin detected - processing advertisement from ${device.address} (instance: $instanceId)")
                    result.scanRecord?.let { record ->
                        Log.d(TAG, "🎯 Calling handleScanResult on MopekaDevicePlugin (plugin=$devicePlugin, isMopeka=${devicePlugin is MopekaDevicePlugin})")
                        // Set MQTT publisher for passive plugins before processing
                        (devicePlugin as? MopekaDevicePlugin)?.apply {
                            setMqttPublisher(mqttPublisher)
                            handleScanResult(device, record)
                        }
                        // Update plugin status to show data is being received
                        mqttPublisher.updatePluginStatus(instanceId, connected = false, authenticated = false, dataHealthy = true)
                    } ?: Log.w(TAG, "⚠️ No scan record in result!")
                    return  // Continue scanning for more advertisements
                }
                
                // Stop scanning (we found a device to connect to)
                stopScanning()
                
                // Load plugin and connect to device
                serviceScope.launch {
                    if (devicePlugin != null) {
                        connectToDevice(device, instanceId)  // Use instanceId for connection
                        // After kicking off a GATT connection, restart scanning so passive plugins (e.g., Mopeka) keep receiving advertisements
                        resumeScanningForPassivePlugins()
                    } else {
                        Log.e(TAG, "Failed to load plugin $pluginId for device ${device.address}")
                        appendServiceLog("ERROR: Failed to load plugin $pluginId for device ${device.address}")
                        updateNotification("Error: Failed to load plugin $pluginId")
                        // Resume scanning
                        startScanning()
                    }
                }
            }
        }
        
        override fun onScanFailed(errorCode: Int) {
            Log.e(TAG, "BLE scan failed: $errorCode")
            updateNotification("Scan failed: $errorCode")
            isScanning = false
            _bleScanningActive.value = false
        }
    }
    
    /**
     * Connect to a BLE device using plugin-owned GATT callback.
     * 
     * NEW ARCHITECTURE: Plugin provides the BluetoothGattCallback.
     * This allows device-specific protocols to be fully isolated without forwarding layers.
     */
    private suspend fun connectToDevice(device: BluetoothDevice, pluginId: String) {
        Log.i(TAG, "Connecting to ${device.address} (plugin: $pluginId)")
        
        // CRITICAL: Close any existing GATT connection first to prevent resource leaks
        val existingGatt = connectedDevices[device.address]?.first
        if (existingGatt != null) {
            Log.w(TAG, "⚠️ Closing existing GATT connection before reconnect")
            try {
                existingGatt.disconnect()
                existingGatt.close()
            } catch (e: Exception) {
                Log.e(TAG, "Error closing existing GATT", e)
            }
            connectedDevices.remove(device.address)
            delay(GATT_SETTLE_DELAY_MS) // Brief delay after closing
        }
        
        updateNotification("Connecting to ${device.address}...")

        try {
            // Check if this is a new-style BleDevicePlugin
            // pluginId might be an instanceId (e.g., "easytouch_b1241e"), so look up the actual plugin instance
            val devicePlugin = pluginRegistry.getPluginInstance(pluginId)
            if (devicePlugin == null) {
                Log.e(TAG, "No plugin instance found for $pluginId; cannot connect to ${device.address}")
                appendServiceLog("ERROR: No plugin instance for $pluginId (device ${device.address})")
                updateNotification("Error: Plugin $pluginId not initialized")
                startScanning()
                return
            }
            
            // NEW: Plugin instance provides its own callback
            Log.i(TAG, "Using plugin-owned GATT callback for ${device.address}")
            
            // Plugin was already initialized by createPluginInstance - no need to reinitialize
            
            val onDisconnect: (BluetoothDevice, Int) -> Unit = { dev, status ->
                handleDeviceDisconnect(dev, status, pluginId)
            }
            
            val callback = devicePlugin.createGattCallback(device, applicationContext, mqttPublisher, onDisconnect)
            
            // Use 'this' (Service context) like the legacy app, not applicationContext
            val gatt = device.connectGatt(
                this,
                false, // autoConnect = false for faster connection
                callback,
                BluetoothDevice.TRANSPORT_LE
            )
            
            connectedDevices[device.address] = Pair(gatt, pluginId)
            
            // Notify plugin of GATT connection
            devicePlugin.onGattConnected(device, gatt)
            
            // Subscribe to command topics for this device
            subscribeToDeviceCommands(device, pluginId, devicePlugin)
            
            // Log current bond state
            val bondStateStr = when(device.bondState) {
                BluetoothDevice.BOND_BONDED -> "BONDED"
                BluetoothDevice.BOND_BONDING -> "BONDING"
                BluetoothDevice.BOND_NONE -> "NONE"
                else -> "UNKNOWN"
            }
            Log.i(TAG, "Connected GATT - bond state: ${device.bondState} ($bondStateStr)")
            
            // Check if this is a CONFIGURED device that requires bonding
            // SECURITY: Only call createBond() for explicitly configured devices,
            // not auto-discovered devices. This prevents unwanted pairing in
            // crowded environments like RV parks.
            val isConfiguredDevice = devicePlugin.getConfiguredDevices()
                .any { it.equals(device.address, ignoreCase = true) }
            val requiresBonding = devicePlugin.requiresBonding()
            
            if (isConfiguredDevice && requiresBonding && device.bondState == BluetoothDevice.BOND_NONE) {
                Log.i(TAG, "🔐 Device ${device.address} requires bonding - initiating createBond()")
                Log.i(TAG, "   (This is a CONFIGURED device - safe to bond)")
                appendServiceLog("Initiating bonding for device: ${device.address}")
                
                // Check if plugin provides a bonding PIN (legacy gateways)
                val bondingPin = (devicePlugin as? OneControlDevicePlugin)?.getBondingPin()
                
                if (bondingPin != null) {
                    Log.i(TAG, "   Legacy gateway - will use PIN for bonding")
                    // Store PIN temporarily for the pairing request broadcast
                    pendingBondPins[device.address] = bondingPin
                } else {
                    Log.i(TAG, "   Modern gateway - using push-to-pair")
                }
                
                pendingBondDevices.add(device.address)
                
                // Store device name if available (prevents 'null' in pairing dialog)
                val displayName = device.name ?: deviceNames[device.address] ?: "OneControl Gateway"
                deviceNames[device.address] = displayName
                
                updateNotification("Pairing with $displayName...")
                val bondResult = device.createBond()
                Log.i(TAG, "🔐 createBond() returned: $bondResult")
            } else if (!isConfiguredDevice && requiresBonding) {
                Log.w(TAG, "⚠️ Device ${device.address} requires bonding but is NOT configured - skipping createBond()")
                Log.w(TAG, "   (This prevents accidental pairing with neighbors' devices)")
            }
        } catch (e: SecurityException) {
            Log.e(TAG, "Permission denied for BLE connect", e)
            updateNotification("Error: BLE permission denied")
        }
    }
    
    /**
     * Subscribe to command topics for a connected device.
     * Routes MQTT commands to the plugin's handleCommand method.
     */
    private fun subscribeToDeviceCommands(device: BluetoothDevice, pluginId: String, plugin: BleDevicePlugin?) {
        Log.i(TAG, "📡 subscribeToDeviceCommands called for $pluginId, plugin=${plugin != null}")
        val output = outputPlugin
        if (output == null || plugin == null) {
            Log.w(TAG, "❌ Cannot subscribe to commands - output=${output != null}, plugin=${plugin != null}")
            return
        }
        
        // Get command topic pattern from plugin (allows zone wildcards, etc.)
        val commandTopicPattern = plugin.getCommandTopicPattern(device)
        
        Log.i(TAG, "📡 Subscribing to command topic: $commandTopicPattern")
        
        serviceScope.launch {
            try {
                output.subscribeToCommands(commandTopicPattern) { topic, payload ->
                    Log.i(TAG, "📥 MQTT command received: $topic = $payload")
                    
                    // Route command to plugin
                    serviceScope.launch {
                        try {
                            val result = plugin.handleCommand(device, topic, payload)
                            if (result.isFailure) {
                                Log.w(TAG, "❌ Plugin command failed: ${result.exceptionOrNull()?.message}")
                            } else {
                                Log.i(TAG, "✅ Plugin command succeeded")
                            }
                        } catch (e: Exception) {
                            Log.e(TAG, "❌ Command handling error", e)
                        }
                    }
                }
                Log.i(TAG, "📡 Successfully subscribed to: $commandTopicPattern")
            } catch (e: Exception) {
                Log.e(TAG, "❌ Failed to subscribe to command topics", e)
            }
        }
    }
    
    /**
     * Handle device disconnect (called by plugin-owned callbacks).
     */
    private fun handleDeviceDisconnect(device: BluetoothDevice, status: Int, pluginId: String) {
        Log.i(TAG, "🔌 Device disconnected: ${device.address}, status=$status")
        
        connectedDevices.remove(device.address)
        
        // Notify plugin
        val devicePlugin = pluginRegistry.getDevicePlugin(pluginId, applicationContext)
        devicePlugin?.onDeviceDisconnected(device)
        
        // Publish availability offline
        mqttPublisher.publishAvailability("${pluginId}/${device.address}/availability", false)
        
        // Always resume scanning to reconnect any missing configured devices
        // This ensures devices like EasyTouch (not bonded) can reconnect automatically
        serviceScope.launch {
            delay(BLE_RECONNECT_DELAY_MS)  // Brief delay to avoid immediate churn
            Log.i(TAG, "🔍 Resuming scan to find and reconnect ${device.address}")
            startScanning()
        }
    }
    
    /**
     * Pairing request receiver for providing PIN programmatically.
     * Intercepts the pairing request before the system dialog appears.
     */
    private val pairingRequestReceiver = object : BroadcastReceiver() {
        override fun onReceive(context: Context, intent: Intent) {
            if (BluetoothDevice.ACTION_PAIRING_REQUEST == intent.action) {
                val device = intent.getParcelableExtra<BluetoothDevice>(BluetoothDevice.EXTRA_DEVICE)
                device?.let {
                    val pin = pendingBondPins[it.address]
                    if (pin != null) {
                        Log.i(TAG, "Providing PIN for legacy gateway pairing: ${it.address}")
                        try {
                            // Set device alias to show friendly name in pairing dialog
                            val deviceName = deviceNames[it.address]
                            if (deviceName != null && it.name == null) {
                                try {
                                    it.setAlias(deviceName)
                                    Log.d(TAG, "Set device alias to: $deviceName")
                                } catch (e: Exception) {
                                    Log.w(TAG, "Could not set device alias (Android API level may not support it)", e)
                                }
                            }
                            
                            // Convert PIN string to bytes
                            val pinBytes = pin.toByteArray()
                            // Set the PIN and abort the default pairing dialog
                            it.setPin(pinBytes)
                            abortBroadcast()
                            Log.d(TAG, "PIN set successfully for ${it.address}")
                        } catch (e: Exception) {
                            Log.e(TAG, "Failed to set PIN for ${it.address}", e)
                        }
                    }
                }
            }
        }
    }
    
    /**
     * Bond state receiver for pairing events.
     * Matches working OneControlBleService implementation.
     */
    private val bondStateReceiver = object : BroadcastReceiver() {
        override fun onReceive(context: Context, intent: Intent) {
            val action = intent.action
            if (BluetoothDevice.ACTION_BOND_STATE_CHANGED == action) {
                val device = intent.getParcelableExtra<BluetoothDevice>(BluetoothDevice.EXTRA_DEVICE)
                val bondState = intent.getIntExtra(BluetoothDevice.EXTRA_BOND_STATE, BluetoothDevice.ERROR)
                val previousBondState = intent.getIntExtra(BluetoothDevice.EXTRA_PREVIOUS_BOND_STATE, BluetoothDevice.ERROR)
                
                device?.let {
                    // Check if this device is one we're managing (connected or pending bond)
                    val isOurDevice = connectedDevices.containsKey(it.address) || pendingBondDevices.contains(it.address)
                    if (isOurDevice) {
                        Log.i(TAG, "🔗 Bond state changed for ${it.address}: $previousBondState -> $bondState")
                        
                        when (bondState) {
                            BluetoothDevice.BOND_BONDED -> {
                                Log.i(TAG, "✅✅✅ Device ${it.address} bonded successfully!")
                                pendingBondDevices.remove(it.address)
                                updateNotification("Bonded - Discovering services...")
                                
                                // Proceed with service discovery if connected
                                val gatt = connectedDevices[it.address]?.first
                                if (gatt != null) {
                                    serviceScope.launch {
                                        delay(GATT_SETTLE_DELAY_MS) // Brief settle delay (matches working app)
                                        try {
                                            Log.i(TAG, "Starting service discovery after bonding")
                                            gatt.discoverServices()
                                        } catch (e: SecurityException) {
                                            Log.e(TAG, "Permission denied for service discovery after bonding", e)
                                        }
                                    }
                                } else {
                                    Log.e(TAG, "GATT connection not found after bonding")
                                }
                            }
                            BluetoothDevice.BOND_BONDING -> {
                                Log.i(TAG, "⏳ Bonding in progress for ${it.address}...")
                                updateNotification("Pairing in progress...")
                            }
                            BluetoothDevice.BOND_NONE -> {
                                pendingBondDevices.remove(it.address)
                                if (previousBondState == BluetoothDevice.BOND_BONDING) {
                                    // Bonding failed - matches working app: just log and notify
                                    Log.e(TAG, "❌ Bonding failed for ${it.address}!")
                                    updateNotification("Pairing failed - try again")
                                    // Working app does NOT proceed with service discovery here
                                    // User needs to retry pairing
                                } else {
                                    Log.w(TAG, "Bond removed for ${it.address}")
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    
    /**
     * Disconnect all devices.
     */
    private fun disconnectAll() {
        Log.i(TAG, "Disconnecting all devices (${connectedDevices.size} connected)")
        
        // Create a copy to avoid ConcurrentModificationException
        for ((_, deviceInfo) in connectedDevices.toList()) {
            val (gatt, _) = deviceInfo
            try {
                gatt.disconnect()
                gatt.close()  // Fully release GATT object to clear stack state
            } catch (e: SecurityException) {
                Log.e(TAG, "Permission denied for disconnect", e)
            }
        }
        
        connectedDevices.clear()
    }
    
    /**
     * Disable a plugin without disconnecting BLE.
     * Publishes offline messages but keeps GATT connection and heartbeat running.
     * Used when a plugin is removed by the user.
     */
    private fun disablePluginKeepConnection(pluginId: String) {
        // Map UI plugin IDs to internal plugin IDs
        val internalPluginId = when (pluginId) {
            "onecontrol" -> "onecontrol_v2"
            else -> pluginId
        }
        
        Log.i(TAG, "Disabling plugin (keeping BLE connection): $pluginId (internal: $internalPluginId)")
        
        // Find all devices belonging to this plugin
        val pluginDevices = connectedDevices.filter { (_, deviceInfo) ->
            deviceInfo.second == internalPluginId
        }
        
        Log.i(TAG, "Found ${pluginDevices.size} device(s) for plugin $pluginId - keeping connections open")
        
        for ((address, _) in pluginDevices) {
            // Publish MQTT offline/unavailable message
            val availabilityTopic = "$internalPluginId/$address/availability"
            Log.i(TAG, "Publishing offline to $availabilityTopic")
            mqttPublisher.publishAvailability(availabilityTopic, false)
            
            // DON'T cancel heartbeat jobs - let the connection stay fully active
            // This way re-enabling is instant
        }
        
        // Update plugin status to show disabled in UI
        val currentStatuses = _pluginStatuses.value.toMutableMap()
        currentStatuses.remove(internalPluginId)
        _pluginStatuses.value = currentStatuses
        
        Log.i(TAG, "Plugin $pluginId disabled. BLE connections and heartbeats still running.")
    }
    
    /**
     * Clear all Home Assistant discovery configs for a plugin.
     * This removes the entities from HA when a plugin is removed.
     */
    private suspend fun clearPluginDiscovery(pluginId: String) {
        val mqtt = outputPlugin as? MqttOutputPlugin
        if (mqtt == null) {
            Log.w(TAG, "Cannot clear discovery - MQTT plugin not available")
            return
        }
        
        // Get device MAC for this plugin from settings
        val settings = AppSettings(this)
        val deviceMac = when (pluginId) {
            "onecontrol" -> settings.oneControlGatewayMac.first()
            "easytouch" -> settings.easyTouchThermostatMac.first()
            "gopower" -> settings.goPowerControllerMac.first()
            else -> null
        }
        
        if (deviceMac.isNullOrBlank()) {
            Log.w(TAG, "Cannot clear discovery - no MAC address for $pluginId")
            return
        }
        
        Log.i(TAG, "🧹 Clearing HA discovery for $pluginId ($deviceMac)")
        
        // Clear discovery using the tracked topics
        val macPattern = deviceMac.replace(":", "").lowercase()
        val cleared = mqtt.clearPluginDiscovery(macPattern)
        
        Log.i(TAG, "🧹 Cleared $cleared discovery topics for $pluginId")
    }
    
    /**
     * Re-enable a plugin that was disabled (but kept its BLE connection).
     * Publishes online messages - heartbeat was never stopped.
     */
    private fun enablePluginResumeConnection(pluginId: String) {
        // Map UI plugin IDs to internal plugin IDs
        val internalPluginId = when (pluginId) {
            "onecontrol" -> "onecontrol_v2"
            else -> pluginId
        }
        
        Log.i(TAG, "Re-enabling plugin: $pluginId (internal: $internalPluginId)")
        
        // Find existing devices for this plugin
        val pluginDevices = connectedDevices.filter { (_, deviceInfo) ->
            deviceInfo.second == internalPluginId
        }
        
        if (pluginDevices.isNotEmpty()) {
            Log.i(TAG, "Found ${pluginDevices.size} existing connection(s) for plugin $pluginId - resuming")
            
            for ((address, _) in pluginDevices) {
                // Publish MQTT online/available message
                val availabilityTopic = "$internalPluginId/$address/availability"
                Log.i(TAG, "Publishing online to $availabilityTopic")
                mqttPublisher.publishAvailability(availabilityTopic, true)
            }
            
            Log.i(TAG, "Plugin $pluginId re-enabled. Connection was never interrupted.")
        } else {
            Log.i(TAG, "No existing connections for plugin $pluginId - will connect fresh")
            // No existing connection, need to connect from scratch
            serviceScope.launch {
                loadPluginDynamically(pluginId)
            }
        }
    }
    
    /**
     * Handle memory pressure.
     */
    private suspend fun handleMemoryPressure(level: Int) {
        Log.w(TAG, "Handling memory pressure: $level")
        
        when (level) {
            ComponentCallbacks2.TRIM_MEMORY_RUNNING_CRITICAL,
            ComponentCallbacks2.TRIM_MEMORY_COMPLETE -> {
                // Critical - disconnect devices and unload plugins to free memory
                Log.w(TAG, "Critical memory - disconnecting devices and unloading plugins")
                disconnectAll()
                pluginRegistry.cleanup()
            }
        }
        
        memoryManager.logMemoryUsage()
    }
    
    /**
     * Create notification channel (Android O+).
     */
    private fun createNotificationChannel() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val channel = NotificationChannel(
                CHANNEL_ID,
                "BLE Bridge Service",
                NotificationManager.IMPORTANCE_LOW
            ).apply {
                description = "BLE to MQTT bridge service"
            }
            
            val notificationManager = getSystemService(NotificationManager::class.java)
            notificationManager.createNotificationChannel(channel)
        }
    }
    
    /**
     * Create foreground service notification.
     */
    private fun createNotification(text: String): Notification {
        return NotificationCompat.Builder(this, CHANNEL_ID)
            .setContentTitle("BLE MQTT Bridge")
            .setContentText(text)
            .setSmallIcon(android.R.drawable.ic_dialog_info)
            .setPriority(NotificationCompat.PRIORITY_LOW)
            .build()
    }
    
    /**
     * Update notification text.
     */
    private fun updateNotification(text: String) {
        val notificationManager = getSystemService(NotificationManager::class.java)
        notificationManager.notify(NOTIFICATION_ID, createNotification(text))
    }
    
    // =====================================================================
    // Debug Logging and Trace Functions
    // =====================================================================
    
    /**
     * Append a service event to the service log buffer (limited to MAX_SERVICE_LOG_LINES).
     * This is for general service events, not BLE-specific events.
     */
    private fun appendServiceLog(message: String) {
        val ts = System.currentTimeMillis()
        val formatted = "${java.text.SimpleDateFormat("yyyy-MM-dd HH:mm:ss.SSS", java.util.Locale.US).format(java.util.Date(ts))} - $message"
        serviceLogBuffer.addLast(formatted)
        while (serviceLogBuffer.size > MAX_SERVICE_LOG_LINES) {
            serviceLogBuffer.removeFirst()
        }
    }
    
    /**
     * Append a BLE event to the BLE trace buffer and trace file if active.
     * This is specifically for BLE GATT events and notifications.
     */
    private fun appendBleTrace(message: String) {
        val ts = System.currentTimeMillis()
        val formatted = "${java.text.SimpleDateFormat("yyyy-MM-dd HH:mm:ss.SSS", java.util.Locale.US).format(java.util.Date(ts))} - $message"
        bleTraceBuffer.addLast(formatted)
        while (bleTraceBuffer.size > MAX_BLE_TRACE_LINES) {
            bleTraceBuffer.removeFirst()
        }
        logTrace(message) // mirror into trace file if enabled
    }
    
    /**
     * Write a message to the trace file if tracing is enabled.
     * Automatically stops trace if size limit is reached.
     */
    private fun logTrace(message: String) {
        if (!traceEnabled) return
        try {
            val writer = traceWriter ?: return
            val line = "${java.text.SimpleDateFormat("HH:mm:ss.SSS", java.util.Locale.US).format(java.util.Date())} $message\n"
            writer.write(line)
            writer.flush()
            traceBytes += line.toByteArray().size
            if (traceBytes >= TRACE_MAX_BYTES) {
                stopBleTrace("size limit reached")
            }
        } catch (_: Exception) {
            stopBleTrace("trace write error")
        }
    }
    
    /**
     * Start BLE trace logging to a file.
     * Creates a trace file with timestamp and starts recording all BLE events.
     * Returns the trace file if successful, null otherwise.
     */
    fun startBleTrace(): java.io.File? {
        try {
            val dir = java.io.File(getExternalFilesDir(null), "traces")
            if (!dir.exists()) dir.mkdirs()
            val ts = java.text.SimpleDateFormat("yyyyMMdd_HHmmss", java.util.Locale.US).format(java.util.Date())
            val file = java.io.File(dir, "trace_${ts}.log")
            traceWriter = file.bufferedWriter()
            traceFile = file
            traceBytes = 0
            traceStartedAt = System.currentTimeMillis()
            traceEnabled = true
            traceTimeout?.let { handler.removeCallbacks(it) }
            traceTimeout = Runnable {
                stopBleTrace("time limit reached")
            }.also { handler.postDelayed(it, TRACE_MAX_DURATION_MS) }
            logTrace("TRACE START ts=$ts")
            _traceActive.value = true
            _traceFilePath.value = file.absolutePath
            Log.i(TAG, "🔍 BLE trace started: ${file.absolutePath}")
            return file
        } catch (e: Exception) {
            Log.e(TAG, "Failed to start trace: ${e.message}")
            stopBleTrace("error")
            return null
        }
    }
    
    /**
     * Stop BLE trace logging.
     * Flushes and closes the trace file.
     * Returns the trace file if it exists, null otherwise.
     */
    fun stopBleTrace(reason: String = "stopped"): java.io.File? {
        _traceActive.value = false  // Update UI state first (fixes stuck button)
        if (!traceEnabled) return traceFile
        logTrace("TRACE STOP reason=$reason")
        traceTimeout?.let { handler.removeCallbacks(it) }
        traceTimeout = null
        traceEnabled = false
        try {
            traceWriter?.flush()
            traceWriter?.close()
        } catch (_: Exception) {}
        traceWriter = null
        if (traceFile != null) {
            _traceFilePath.value = traceFile!!.absolutePath
            Log.i(TAG, "🔍 BLE trace stopped: ${traceFile!!.absolutePath}")
        }
        return traceFile
    }
    
    /**
     * Check if BLE trace is currently active.
     */
    fun isTraceActive(): Boolean = traceEnabled
    
    /**
     * Export debug log to a file.
     * Creates a debug log file with all buffered log entries and system information.
     * Returns the debug log file if successful, null otherwise.
     */
    fun exportDebugLog(): java.io.File? {
        return try {
            val dir = java.io.File(getExternalFilesDir(null), "logs")
            if (!dir.exists()) {
                val created = dir.mkdirs()
                if (!created) {
                    Log.e(TAG, "Failed to create logs directory: ${dir.absolutePath}")
                    return null
                }
            }
            
            val ts = java.text.SimpleDateFormat("yyyyMMdd_HHmmss", java.util.Locale.US).format(java.util.Date())
            val file = java.io.File(dir, "debug_${ts}.txt")
            
            file.bufferedWriter().use { out ->
                out.appendLine("BLE-MQTT Plugin Bridge Debug Log")
                out.appendLine("================================")
                out.appendLine("Timestamp: $ts")
                out.appendLine("App Version: ${BuildConfig.VERSION_NAME} (${BuildConfig.VERSION_CODE})")
                out.appendLine("")
                
                out.appendLine("Service Status:")
                out.appendLine("  Running: ${_serviceRunning.value}")
                out.appendLine("  MQTT Connected: ${_mqttConnected.value}")
                out.appendLine("  BLE Trace Active: $traceEnabled")
                traceFile?.let { out.appendLine("  Trace File: ${it.absolutePath}") }
                out.appendLine("")
                
                out.appendLine("Plugin Statuses:")
                // Deduplicate aliases: if an instance is present, skip the legacy plugin-type alias
                val instanceTypesWithInstances = instancePluginTypes.values.toSet()
                _pluginStatuses.value
                    .filterNot { (pluginId, _) -> instanceTypesWithInstances.contains(pluginId) }
                    .forEach { (pluginId, status) ->
                        out.appendLine("  $pluginId:")
                        out.appendLine("    Connected: ${status.connected}")
                        out.appendLine("    Authenticated: ${status.authenticated}")
                        out.appendLine("    Data Healthy: ${status.dataHealthy}")
                    }
                out.appendLine("")
                
                out.appendLine("Active Plugins:")
                outputPlugin?.let { out.appendLine("  Output: ${it.javaClass.simpleName}") }
                out.appendLine("")
                
                out.appendLine("Recent Service Events (last $MAX_SERVICE_LOG_LINES):")
                out.appendLine("=".repeat(50))
                serviceLogBuffer.forEach { line -> out.appendLine(line) }
                
                if (serviceLogBuffer.isEmpty()) {
                    out.appendLine("(No service events logged yet)")
                }
            }
            
            Log.i(TAG, "📝 Debug log exported: ${file.absolutePath}")
            appendServiceLog("Debug log exported: ${file.name}")
            file
        } catch (e: Exception) {
            Log.e(TAG, "Failed to write debug log: ${e.message}", e)
            appendServiceLog("ERROR: Failed to export debug log: ${e.message}")
            null
        }
    }
    
    /**
     * Export debug log as string (for web interface).
     */
    fun exportDebugLogToString(): String {
        val ts = java.text.SimpleDateFormat("yyyyMMdd_HHmmss", java.util.Locale.US).format(java.util.Date())
        return buildString {
            appendLine("BLE-MQTT Plugin Bridge Debug Log")
            appendLine("================================")
            appendLine("Timestamp: $ts")
            appendLine("App Version: ${BuildConfig.VERSION_NAME} (${BuildConfig.VERSION_CODE})")
            appendLine("")
            
            appendLine("Service Status:")
            appendLine("  Running: ${_serviceRunning.value}")
            appendLine("  MQTT Connected: ${_mqttConnected.value}")
            appendLine("  BLE Trace Active: $traceEnabled")
            traceFile?.let { appendLine("  Trace File: ${it.absolutePath}") }
            appendLine("")
            
            appendLine("Plugin Statuses:")
            val instanceTypesWithInstances = instancePluginTypes.values.toSet()
            _pluginStatuses.value
                .filterNot { (pluginId, _) -> instanceTypesWithInstances.contains(pluginId) }
                .forEach { (pluginId, status) ->
                    appendLine("  $pluginId:")
                    appendLine("    Connected: ${status.connected}")
                    appendLine("    Authenticated: ${status.authenticated}")
                    appendLine("    Data Healthy: ${status.dataHealthy}")
                }
            appendLine("")
            
            appendLine("Active Plugins:")
            outputPlugin?.let { appendLine("  Output: ${it.javaClass.simpleName}") }
            appendLine("")
            
            appendLine("Recent Service Events (last $MAX_SERVICE_LOG_LINES):")
            appendLine("=".repeat(50))
            serviceLogBuffer.forEach { line -> appendLine(line) }
            
            if (serviceLogBuffer.isEmpty()) {
                appendLine("(No service events logged yet)")
            }
        }
    }
    
    /**
     * Export BLE trace as string (for web interface).
     */
    fun exportBleTraceToString(): String {
        return buildString {
            appendLine("BLE-MQTT Plugin Bridge BLE Trace")
            appendLine("================================")
            appendLine("Timestamp: ${java.text.SimpleDateFormat("yyyyMMdd_HHmmss", java.util.Locale.US).format(java.util.Date())}")
            appendLine("")
            appendLine("Recent BLE Events (last $MAX_BLE_TRACE_LINES):")
            appendLine("=".repeat(50))
            bleTraceBuffer.forEach { line: String -> appendLine(line) }
            
            if (bleTraceBuffer.isEmpty()) {
                appendLine("(No BLE events logged yet)")
            }
        }
    }
    
    /**
     * Check if BLE trace is active.
     */
    fun isBleTraceActive(): Boolean = traceEnabled
    
    /**
     * Disconnect MQTT (for web interface control).
     */
    suspend fun disconnectMqtt() {
        Log.i(TAG, "MQTT disconnect requested")
        // Force UI state to reflect disconnect immediately, even if the plugin
        // never notifies (observed when toggling MQTT via web UI)
        _mqttConnected.value = false
        appendServiceLog("MQTT connection status: disconnected (manual disconnect)")
        outputPlugin?.disconnect()
    }
    
    /**
     * Reconnect MQTT (for web interface control).
     */
    suspend fun reconnectMqtt() {
        Log.i(TAG, "MQTT reconnect requested")
        val currentPlugin = outputPlugin
        if (currentPlugin != null) {
            // Disconnect first
            currentPlugin.disconnect()
            // Small delay
            kotlinx.coroutines.delay(GATT_SETTLE_DELAY_MS)
            // Re-initialize with same config
            val settings = AppSettings(applicationContext)
            val config = mapOf(
                "broker_url" to "tcp://${settings.mqttBrokerHost.first()}:${settings.mqttBrokerPort.first()}",
                "username" to settings.mqttUsername.first(),
                "password" to settings.mqttPassword.first(),
                "topic_prefix" to settings.mqttTopicPrefix.first()
            )
            currentPlugin.initialize(config)
        }
    }
    
    /**
     * Share a file via Android share intent.
     * Uses FileProvider to grant temporary read access to the file.
     */
    private fun shareFile(file: java.io.File, mimeType: String) {
        try {
            val uri: android.net.Uri = androidx.core.content.FileProvider.getUriForFile(
                this, 
                "${packageName}.fileprovider", 
                file
            )
            val intent = Intent(Intent.ACTION_SEND).apply {
                type = mimeType
                putExtra(Intent.EXTRA_STREAM, uri)
                addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
                addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
            }
            startActivity(Intent.createChooser(intent, "Share log").addFlags(Intent.FLAG_ACTIVITY_NEW_TASK))
            Log.i(TAG, "📤 File share intent launched: ${file.name}")
        } catch (e: Exception) {
            Log.e(TAG, "Failed to share file: ${e.message}", e)
            android.widget.Toast.makeText(this, "Failed to share file: ${e.message}", android.widget.Toast.LENGTH_LONG).show()
        }
    }
    
    // ============================================================================
    // Keepalive Management (Doze Mode Prevention)
    // ============================================================================
    
    /**
     * Check if keepalive feature is enabled in preferences.
     */
    private fun isKeepAliveEnabled(): Boolean {
        val prefs = getSharedPreferences("app_settings", Context.MODE_PRIVATE)
        return prefs.getBoolean("keepalive_enabled", true) // Default ON
    }
    
    /**
     * Schedule periodic keepalive alarm using AlarmManager.
     * Uses setExactAndAllowWhileIdle() to wake device during Doze mode.
     */
    private fun scheduleKeepAlive() {
        val intent = Intent(this, com.blemqttbridge.receivers.KeepAliveReceiver::class.java).apply {
            action = com.blemqttbridge.receivers.KeepAliveReceiver.ACTION_KEEPALIVE
        }
        
        val pendingIntent = PendingIntent.getBroadcast(
            this,
            0,
            intent,
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
                PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE
            } else {
                PendingIntent.FLAG_UPDATE_CURRENT
            }
        )
        
        keepAlivePendingIntent = pendingIntent
        
        val triggerAtMillis = System.currentTimeMillis() + KEEPALIVE_INTERVAL_MS
        
        alarmManager?.setExactAndAllowWhileIdle(
            AlarmManager.RTC_WAKEUP,
            triggerAtMillis,
            pendingIntent
        )
        
        Log.i(TAG, "⏰ Keepalive scheduled: next ping in ${KEEPALIVE_INTERVAL_MS / 60000} minutes")
    }
    
    /**
     * Cancel the keepalive alarm.
     */
    private fun cancelKeepAlive() {
        val pendingIntent = keepAlivePendingIntent
        if (pendingIntent != null) {
            alarmManager?.cancel(pendingIntent)
            pendingIntent.cancel()
            Log.i(TAG, "⏰ Keepalive cancelled")
        }
        keepAlivePendingIntent = null
    }
    
    /**
     * Perform a lightweight keepalive operation on all connected devices.
     * This is called periodically to prevent BLE connections from going stale during Doze.
     */
    private suspend fun performKeepAlivePing() {
        withContext(Dispatchers.IO) {
            try {
                Log.d(TAG, "⏰ Performing keepalive ping on ${connectedDevices.size} devices")
                
                var pingsSuccessful = 0
                var pingsFailed = 0
                
                connectedDevices.forEach { (address, deviceInfo) ->
                    try {
                        val gatt = deviceInfo.first
                        // Attempt to read RSSI as a lightweight keepalive operation
                        // The actual result comes via onReadRemoteRssi callback, but the
                        // initiation itself is enough to keep the connection alive
                        val initiated = gatt.readRemoteRssi()
                        when {
                            initiated -> {
                                pingsSuccessful++
                                Log.d(TAG, "⏰ Keepalive ping OK: $address")
                            }
                            else -> {
                                pingsFailed++
                                Log.d(TAG, "⏰ Keepalive ping failed: $address")
                            }
                        }
                    } catch (e: Exception) {
                        pingsFailed++
                        Log.w(TAG, "⚠️ Keepalive ping exception: $address - ${e.message}")
                    }
                    
                    // Small delay between pings to avoid overwhelming BLE stack
                    delay(100)
                }
                
                Log.i(TAG, "⏰ Keepalive complete: $pingsSuccessful OK, $pingsFailed failed")
                
                // Reschedule next keepalive if still enabled
                when (isKeepAliveEnabled()) {
                    true -> scheduleKeepAlive()
                    false -> {
                        // Keepalive disabled, don't reschedule
                    }
                }
            } catch (e: Exception) {
                Log.e(TAG, "❌ Keepalive ping error: ${e.message}", e)
            }
        }
    }
}

