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
import com.blemqttbridge.core.interfaces.BlePluginInterface
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
import java.util.UUID
import java.util.Collections
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
        
        const val EXTRA_PLUGIN_ID = "plugin_id"
        
        const val EXTRA_BLE_PLUGIN_ID = "ble_plugin_id"
        const val EXTRA_OUTPUT_PLUGIN_ID = "output_plugin_id"
        const val EXTRA_BLE_CONFIG = "ble_config"
        const val EXTRA_OUTPUT_CONFIG = "output_config"
        
        // Debug and trace limits
        private const val MAX_DEBUG_LOG_LINES = 2000
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
    
    private var blePlugin: BlePluginInterface? = null
    private var outputPlugin: OutputPluginInterface? = null
    private var remoteControlManager: RemoteControlManager? = null
    private var bleScannerPlugin: BleScannerPlugin? = null
    
    // Connected devices map: device address -> (BluetoothGatt, pluginId)
    private val connectedDevices = mutableMapOf<String, Pair<BluetoothGatt, String>>()
    
    // Instance to plugin type mapping: instanceId -> pluginType (e.g., "easytouch_b1241e" -> "easytouch")
    private val instancePluginTypes = mutableMapOf<String, String>()
    
    // Polling jobs for devices that need periodic updates
    private val pollingJobs = mutableMapOf<String, Job>()
    
    // Pending GATT operations: characteristic UUID -> result deferred
    private val pendingReads = mutableMapOf<String, CompletableDeferred<Result<ByteArray>>>()
    private val pendingWrites = mutableMapOf<String, CompletableDeferred<Result<Unit>>>()
    private val pendingDescriptorWrites = mutableMapOf<String, CompletableDeferred<Result<Unit>>>()
    
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
            serviceScope.launch {
                outputPlugin?.publishState(topic, payload, retained)
            }
        }
        
        override fun publishDiscovery(topic: String, payload: String) {
            serviceScope.launch {
                outputPlugin?.publishDiscovery(topic, payload)
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
        
        override fun updateDiagnosticStatus(dataHealthy: Boolean) {
            // Deprecated - plugins should use updatePluginStatus instead
            Log.w(TAG, "⚠️ updateDiagnosticStatus called (deprecated) - use updatePluginStatus instead")
        }
        
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
        
        // Check if BLE is available
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
        
        // Register bond state receiver
        val bondFilter = IntentFilter(BluetoothDevice.ACTION_BOND_STATE_CHANGED)
        registerReceiver(bondStateReceiver, bondFilter)
        Log.d(TAG, "Bond state receiver registered")
        
        // Register pairing request receiver with high priority to intercept before system dialog
        val pairingFilter = IntentFilter(BluetoothDevice.ACTION_PAIRING_REQUEST)
        pairingFilter.priority = IntentFilter.SYSTEM_HIGH_PRIORITY
        registerReceiver(pairingRequestReceiver, pairingFilter)
        Log.d(TAG, "Pairing request receiver registered with high priority")
        
        // Register Bluetooth state receiver
        val btStateFilter = IntentFilter(BluetoothAdapter.ACTION_STATE_CHANGED)
        registerReceiver(bluetoothStateReceiver, btStateFilter)
        bluetoothEnabled = bluetoothAdapter.isEnabled
        Log.d(TAG, "Bluetooth state receiver registered (BT currently ${if (bluetoothEnabled) "ON" else "OFF"})")
        
        // Initialize AlarmManager for keepalive
        alarmManager = getSystemService(Context.ALARM_SERVICE) as AlarmManager
        
        // Apply Android TV power fix if applicable
        // This disables HDMI-CEC auto device off to prevent service from being killed
        // when TV enters standby mode
        if (AndroidTvHelper.applyRecommendedSettings(this)) {
            Log.i(TAG, "📺 Android TV: Applied HDMI-CEC fix for service reliability")
        }
        
        createNotificationChannel()
        startForeground(NOTIFICATION_ID, createNotification("Service starting..."))
        
        // Schedule keepalive as backup (in case onStartCommand doesn't get ACTION_START_SCAN)
        // This ensures keepalive is always active regardless of startup path
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
                // Check if already initializing to prevent race condition
                synchronized(initializationLock) {
                    if (isInitializing) {
                        Log.i(TAG, "⚙️ Skipping ACTION_START_SCAN - already initializing")
                        return START_STICKY
                    }
                    isInitializing = true
                }
                
                // Mark service as running
                ServiceStateManager.setServiceRunning(applicationContext, true)
                _serviceRunning.value = true
                Log.i(TAG, "Service marked as running")
                
                // Get ALL enabled BLE plugins from ServiceStateManager
                val enabledBlePlugins = ServiceStateManager.getEnabledBlePlugins(applicationContext)
                val outputPluginId = intent.getStringExtra(EXTRA_OUTPUT_PLUGIN_ID) ?: "mqtt"
                
                Log.i(TAG, "Enabled BLE plugins: $enabledBlePlugins")
                
                // Load output config
                val outputConfig = AppConfig.getMqttConfig(applicationContext)
                
                serviceScope.launch {
                    try {
                        initializeMultiplePlugins(enabledBlePlugins, outputPluginId, outputConfig)
                        // Note: initializeMultiplePlugins now calls reconnectToBondedDevices() which 
                        // either connects to bonded devices or falls back to scanning
                        
                        // Schedule keepalive if enabled (redundant with onCreate but ensures it's set)
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
                // Service started without explicit scan action (e.g., from MainActivity, web server, or system restart)
                Log.i(TAG, "⚙️ Service started with action=${intent?.action ?: "null"} - checking if service should auto-start")
                
                // Check if service was previously enabled - if so, auto-start scanning
                serviceScope.launch {
                    // Check if already initializing to prevent race condition
                    synchronized(initializationLock) {
                        if (isInitializing) {
                            Log.i(TAG, "⚙️ Skipping auto-start - already initializing from another path")
                            return@launch
                        }
                    }
                    
                    val settings = AppSettings(applicationContext)
                    val serviceEnabled = settings.serviceEnabled.first()
                    
                    if (serviceEnabled) {
                        // Double-check we're not racing with ACTION_START_SCAN
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
                            
                            // Mark service as running
                            ServiceStateManager.setServiceRunning(applicationContext, true)
                            _serviceRunning.value = true
                            
                            // Get ALL enabled BLE plugins from ServiceStateManager
                            val enabledBlePlugins = ServiceStateManager.getEnabledBlePlugins(applicationContext)
                            val outputPluginId = "mqtt"
                            
                            Log.i(TAG, "Enabled BLE plugins: $enabledBlePlugins")
                            
                            // Load output config
                            val outputConfig = AppConfig.getMqttConfig(applicationContext)
                            
                            initializeMultiplePlugins(enabledBlePlugins, outputPluginId, outputConfig)
                            
                            // Schedule keepalive if enabled
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
                        
                        // Ensure keepalive is scheduled (onCreate already did this, but be explicit)
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
        
        // Mark service as stopped
        ServiceStateManager.setServiceRunning(applicationContext, false)
        _serviceRunning.value = false
        // Clear plugin statuses
        _pluginStatuses.value = emptyMap()
        _mqttConnected.value = false
        Log.i(TAG, "Service marked as stopped")
        
        stopScanning()
        disconnectAll()
        
        // Unregister bond state receiver
        try {
            unregisterReceiver(bondStateReceiver)
            Log.d(TAG, "Bond state receiver unregistered")
        } catch (e: IllegalArgumentException) {
            Log.w(TAG, "Bond state receiver not registered")
        }
        
        // Unregister pairing request receiver
        try {
            unregisterReceiver(pairingRequestReceiver)
            Log.d(TAG, "Pairing request receiver unregistered")
        } catch (e: IllegalArgumentException) {
            Log.w(TAG, "Pairing request receiver not registered")
        }
        
        // Unregister Bluetooth state receiver
        try {
            unregisterReceiver(bluetoothStateReceiver)
            Log.d(TAG, "Bluetooth state receiver unregistered")
        } catch (e: IllegalArgumentException) {
            Log.w(TAG, "Bluetooth state receiver not registered")
        }
        
        // Cancel keepalive alarm
        cancelKeepAlive()
        
        // Clear instance reference
        instance = null
        
        // Cleanup remote control manager
        remoteControlManager?.cleanup()
        remoteControlManager = null
        
        // Cleanup BLE Scanner plugin
        bleScannerPlugin?.cleanup()
        bleScannerPlugin = null
        
        serviceScope.launch {
            pluginRegistry.cleanup()
            serviceScope.cancel()
        }
    }
    
    /**
     * Initialize plugins.
     */
    private suspend fun initializePlugins(
        blePluginId: String,
        outputPluginId: String,
        bleConfig: Map<String, String>,
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
            // Set up connection status listener for real-time UI updates
            outputPlugin?.setConnectionStatusListener(object : OutputPluginInterface.ConnectionStatusListener {
                override fun onConnectionStatusChanged(connected: Boolean) {
                    Log.i(TAG, "MQTT connection status changed: $connected")
                    _mqttConnected.value = connected
                }
            })
            
            // Initialize remote control manager for MQTT commands
            remoteControlManager = RemoteControlManager(applicationContext, serviceScope, pluginRegistry)
            remoteControlManager?.initialize(outputPlugin!!)
            Log.i(TAG, "Remote control manager initialized")
        }
        
        // Load BLE plugin - support both legacy (BlePluginInterface) and new (BleDevicePlugin) architecture
        blePlugin = pluginRegistry.getBlePlugin(blePluginId, applicationContext, bleConfig)
        if (blePlugin == null) {
            // Plugin might be a BleDevicePlugin (new architecture) - check if it exists
            val devicePlugin = pluginRegistry.getDevicePlugin(blePluginId, applicationContext)
            if (devicePlugin == null) {
                Log.e(TAG, "Failed to load BLE plugin: $blePluginId (not found in registry)")
                appendServiceLog("ERROR: Failed to load BLE plugin: $blePluginId (not found in registry)")
                updateNotification("Error: BLE plugin failed to load")
                return
            } else {
                Log.i(TAG, "Loaded device plugin: $blePluginId (new architecture - plugin-owned GATT callback)")
                appendServiceLog("Loaded BLE plugin: $blePluginId (device plugin)")
            }
        } else {
            Log.i(TAG, "Loaded legacy plugin: $blePluginId (BlePluginInterface - forwarding callback)")
            appendServiceLog("Loaded BLE plugin: $blePluginId (legacy plugin)")
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
        enabledBlePlugins: Set<String>,
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
            // Set up connection status listener for real-time UI updates
            outputPlugin?.setConnectionStatusListener(object : OutputPluginInterface.ConnectionStatusListener {
                override fun onConnectionStatusChanged(connected: Boolean) {
                    Log.i(TAG, "MQTT connection status changed: $connected")
                    _mqttConnected.value = connected
                }
            })
            
            // Initialize remote control manager for MQTT commands
            remoteControlManager = RemoteControlManager(applicationContext, serviceScope, pluginRegistry)
            remoteControlManager?.initialize(outputPlugin!!)
            Log.i(TAG, "Remote control manager initialized")
        }
        
        // Phase 4: Load all plugin instances from ServiceStateManager
        val allInstances = ServiceStateManager.getAllInstances(applicationContext)
        Log.i(TAG, "📦 Found ${allInstances.size} plugin instance(s) to load")
        
        if (allInstances.isEmpty()) {
            Log.w(TAG, "No plugin instances configured - checking for legacy enabled plugins...")
            // Fallback: If no instances exist, try loading from old enabled_ble_plugins format
            if (enabledBlePlugins.isNotEmpty()) {
                Log.i(TAG, "Loading ${enabledBlePlugins.size} legacy enabled plugins...")
                loadLegacyPlugins(enabledBlePlugins)
            } else {
                Log.e(TAG, "No plugins were loaded!")
                appendServiceLog("ERROR: No plugins were loaded!")
                updateNotification("Error: No plugins loaded")
                return
            }
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
                        // Update status for this instance
                        _pluginStatuses.value = _pluginStatuses.value + (instanceId to PluginStatus(instanceId, false, false, false))
                        appendServiceLog("✓ Loaded instance: $instanceId (${instance.pluginType})")
                        loadedCount++
                        // Keep reference to last one for backward compat
                        blePlugin = plugin as? BlePluginInterface
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
        
        // Migration: Ensure all loaded instances are in the enabled plugins list
        // (for instances added before multi-instance support was finalized)
        val enabledPlugins = ServiceStateManager.getEnabledBlePlugins(applicationContext)
        val needsUpdate = allInstances.keys.any { instanceId ->
            !enabledPlugins.contains(instanceId)
        }
        if (needsUpdate) {
            Log.i(TAG, "🔄 Migration: Auto-enabling discovered plugin instances")
            for (instanceId in allInstances.keys) {
                if (!enabledPlugins.contains(instanceId)) {
                    ServiceStateManager.enableBlePlugin(applicationContext, instanceId)
                    Log.i(TAG, "✓ Auto-enabled instance: $instanceId")
                }
            }
        }
        
        // Try to reconnect to bonded devices or scan for new ones
        reconnectToBondedDevices()
    }
    
    /**
     * Fallback: Load legacy plugins from enabled_ble_plugins set.
     * Used when no instances exist (pre-migration or legacy setup).
     */
    private suspend fun loadLegacyPlugins(enabledBlePlugins: Set<String>) {
        Log.i(TAG, "⚠️ Loading legacy plugins (no instances found)")
        var loadedCount = 0
        
        for (pluginId in enabledBlePlugins) {
            val bleConfig = AppConfig.getBlePluginConfig(applicationContext, pluginId)
            Log.i(TAG, "Loading legacy plugin: $pluginId with config: $bleConfig")
            
            // Try legacy interface first
            val legacyPlugin = pluginRegistry.getBlePlugin(pluginId, applicationContext, bleConfig)
            if (legacyPlugin != null) {
                // For legacy plugins, we keep the last one in blePlugin (for backward compat)
                blePlugin = legacyPlugin
                Log.i(TAG, "✓ Loaded legacy plugin: $pluginId")
                loadedCount++
                continue
            }
            
            // Try new BleDevicePlugin architecture
            val devicePlugin = pluginRegistry.getDevicePlugin(pluginId, applicationContext)
            if (devicePlugin != null) {
                // Initialize with config using PluginConfig data class
                val config = PluginConfig(parameters = bleConfig)
                devicePlugin.initialize(applicationContext, config)
                Log.i(TAG, "✓ Loaded device plugin: $pluginId (target MACs: ${devicePlugin.getConfiguredDevices()})")
                loadedCount++
            } else {
                Log.w(TAG, "✗ Failed to load plugin: $pluginId (not found in registry)")
            }
        }
        
        if (loadedCount == 0) {
            Log.e(TAG, "No legacy plugins were loaded!")
            appendServiceLog("ERROR: No legacy plugins were loaded!")
            updateNotification("Error: No plugins loaded")
            return
        }
        
        Log.i(TAG, "✓ Loaded $loadedCount/${enabledBlePlugins.size} legacy plugins successfully")
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
        delay(500)
        
        val bleConfig = AppConfig.getBlePluginConfig(applicationContext, internalPluginId)
        Log.i(TAG, "Plugin config: $bleConfig")
        
        // Try legacy interface first
        val legacyPlugin = pluginRegistry.getBlePlugin(internalPluginId, applicationContext, bleConfig)
        if (legacyPlugin != null) {
            blePlugin = legacyPlugin
            Log.i(TAG, "✓ Dynamically loaded legacy plugin: $internalPluginId")
            // Schedule reconnection after delay to let BLE stack fully reset
            serviceScope.launch {
                delay(2000) // Give BLE stack time to fully release previous connection
                Log.i(TAG, "🔄 Attempting reconnection for newly added plugin: $internalPluginId")
                connectToPluginDevices(internalPluginId)
            }
            return
        }
        
        // Try new BleDevicePlugin architecture
        val devicePlugin = pluginRegistry.getDevicePlugin(internalPluginId, applicationContext)
        if (devicePlugin != null) {
            val config = PluginConfig(parameters = bleConfig)
            devicePlugin.initialize(applicationContext, config)
            Log.i(TAG, "✓ Dynamically loaded device plugin: $internalPluginId (target MACs: ${devicePlugin.getConfiguredDevices()})")
            // Schedule reconnection after delay to let BLE stack fully reset
            serviceScope.launch {
                delay(2000) // Give BLE stack time to fully release previous connection
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
                delay(2000)
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
        
        // Get target MAC addresses from loaded BlePluginInterface plugins (legacy)
        for ((pluginId, plugin) in pluginRegistry.getLoadedBlePlugins()) {
            // Skip disabled plugins
            if (!enabledPlugins.contains(pluginId)) continue
            
            val targetMacs = plugin.getTargetDeviceAddresses()
            for (mac in targetMacs) {
                if (addedMacs.add(mac.uppercase())) {
                    Log.d(TAG, "Adding scan filter for MAC: $mac (plugin: $pluginId)")
                    scanFilters.add(
                        ScanFilter.Builder()
                            .setDeviceAddress(mac)
                            .build()
                    )
                }
            }
        }
        
        // REMOVED: Old loop that called initialize() on every registered plugin
        // This was creating temporary plugin instances and logging "No MAC configured!" warnings
        // Multi-instance architecture handles scan filters via the instance loop below
        
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
                delay(500)  // Brief delay to avoid BLE stack contention
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
                    delay(2000)
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
                    // Check if plugin can be loaded (either legacy BlePluginInterface or new BleDevicePlugin)
                    val legacyPlugin = pluginRegistry.getBlePlugin(pluginId, applicationContext, emptyMap())
                    
                    if (legacyPlugin != null || devicePlugin != null) {
                        connectToDevice(device, pluginId)
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
            delay(500) // Brief delay after closing
        }
        
        updateNotification("Connecting to ${device.address}...")

        try {
            // Check if this is a new-style BleDevicePlugin
            // pluginId might be an instanceId (e.g., "easytouch_b1241e"), so look up the actual plugin instance
            val devicePlugin = pluginRegistry.getPluginInstance(pluginId)
            
            val callback = if (devicePlugin != null) {
                // NEW: Plugin instance provides its own callback
                Log.i(TAG, "Using plugin-owned GATT callback for ${device.address}")
                
                // Plugin was already initialized by createPluginInstance - no need to reinitialize
                
                val onDisconnect: (BluetoothDevice, Int) -> Unit = { dev, status ->
                    handleDeviceDisconnect(dev, status, pluginId)
                }
                
                devicePlugin.createGattCallback(device, applicationContext, mqttPublisher, onDisconnect)
            } else {
                // LEGACY: Use old forwarding callback
                Log.i(TAG, "Using legacy forwarding GATT callback for ${device.address}")
                gattCallback
            }
            
            // Use 'this' (Service context) like the legacy app, not applicationContext
            val gatt = device.connectGatt(
                this,
                false, // autoConnect = false for faster connection
                callback,
                BluetoothDevice.TRANSPORT_LE
            )
            
            connectedDevices[device.address] = Pair(gatt, pluginId)
            
            // Notify plugin of GATT connection
            devicePlugin?.onGattConnected(device, gatt)
            
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
            val isConfiguredDevice = devicePlugin?.getConfiguredDevices()
                ?.any { it.equals(device.address, ignoreCase = true) } == true
            val requiresBonding = devicePlugin?.requiresBonding() == true
            
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
            delay(2000)  // Brief delay to avoid immediate churn
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
                                        delay(500) // Brief settle delay (matches working app)
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
     * GATT callback for BLE connections.
     */
    private val gattCallback = object : BluetoothGattCallback() {
        override fun onConnectionStateChange(gatt: BluetoothGatt, status: Int, newState: Int) {
            val device = gatt.device
            val deviceInfo = connectedDevices[device.address]
            val pluginId = deviceInfo?.second
            
            // Log the full status for debugging bond/connection issues
            val statusName = when (status) {
                BluetoothGatt.GATT_SUCCESS -> "GATT_SUCCESS"
                133 -> "GATT_ERROR(133) - likely stale bond/link key mismatch"
                8 -> "GATT_CONN_TIMEOUT"
                19 -> "GATT_CONN_TERMINATE_PEER_USER"
                22 -> "GATT_CONN_TERMINATE_LOCAL_HOST"
                34 -> "GATT_CONN_LMP_TIMEOUT"
                62 -> "GATT_CONN_FAIL_ESTABLISH"
                else -> "UNKNOWN($status)"
            }
            val stateName = when (newState) {
                BluetoothProfile.STATE_CONNECTED -> "CONNECTED"
                BluetoothProfile.STATE_DISCONNECTED -> "DISCONNECTED"
                BluetoothProfile.STATE_CONNECTING -> "CONNECTING"
                BluetoothProfile.STATE_DISCONNECTING -> "DISCONNECTING"
                else -> "UNKNOWN($newState)"
            }
            Log.i(TAG, "🔗 onConnectionStateChange: ${device.address} status=$statusName, newState=$stateName")
            
            // CRITICAL: Handle stale bond detection (status 133 with BOND_BONDED)
            // This happens when Android thinks it's bonded but the gateway has forgotten the bond
            if (status == 133 && device.bondState == BluetoothDevice.BOND_BONDED) {
                Log.e(TAG, "⚠️ STALE BOND DETECTED for ${device.address}!")
                Log.e(TAG, "   Android reports BOND_BONDED but connection failed with status 133")
                Log.e(TAG, "   This means the gateway has forgotten the bond - user must re-pair manually")
                Log.e(TAG, "   Go to Android Settings > Bluetooth > Forget device, then re-pair")
                updateNotification("Bond invalid - please re-pair device")
                
                // Clean up this connection attempt
                connectedDevices.remove(device.address)
                gatt.close()
                return
            }
            
            when (newState) {
                BluetoothProfile.STATE_CONNECTED -> {
                    // Only proceed if status is success
                    if (status != BluetoothGatt.GATT_SUCCESS) {
                            Log.e(TAG, "❌ Connected but with error status: $statusName - closing connection")
                            appendServiceLog("ERROR: Connected to ${device.address} but with error status: $statusName - closing connection")
                        gatt.close()
                        connectedDevices.remove(device.address)
                        return
                    }
                    
                    Log.i(TAG, "✅ Connected to ${device.address} (plugin: $pluginId)")
                    updateNotification("Connected to ${device.address}")
                    
                    serviceScope.launch {
                        // Get the plugin for this device
                        val plugin = if (pluginId != null) {
                            pluginRegistry.getLoadedBlePlugin(pluginId)
                        } else null
                        
                        // Notify plugin of connection
                        plugin?.onDeviceConnected(device)
                        
                        // Legacy app behavior: Immediately start service discovery
                        // The BLE stack will handle bonding/encryption automatically when needed
                        Log.i(TAG, "Starting service discovery (bond state: ${device.bondState})...")
                        updateNotification("Connected - Discovering services...")
                        
                        try {
                            gatt.discoverServices()
                        } catch (e: SecurityException) {
                            Log.e(TAG, "Permission denied for service discovery", e)
                        }
                        
                        // Publish availability
                        publishAvailability(device, true)
                        
                        // Resume scanning for passive plugins (Mopeka, etc.)
                        // GATT connections don't block passive BLE scanning
                        resumeScanningForPassivePlugins()
                    }
                }
                
                BluetoothProfile.STATE_DISCONNECTED -> {
                    // Log disconnect reason for debugging
                    val disconnectReason = when (status) {
                        BluetoothGatt.GATT_SUCCESS -> "normal disconnect"
                        8 -> "GATT_CONN_TIMEOUT - connection timed out"
                        19 -> "GATT_CONN_TERMINATE_PEER_USER - gateway terminated connection"
                        22 -> "GATT_CONN_TERMINATE_LOCAL_HOST - we terminated connection"
                        34 -> "GATT_CONN_LMP_TIMEOUT - link manager timeout"
                        62 -> "GATT_CONN_FAIL_ESTABLISH - failed to establish connection"
                        133 -> "GATT_ERROR(133) - possible bond/encryption issue"
                        else -> "status=$status"
                    }
                    Log.i(TAG, "🔌 Disconnected from ${device.address}: $disconnectReason")
                    
                    // NOTE: Legacy app does NOT have automatic retry logic
                    // Status 133 usually means stale bond - user must manually re-pair
                    // Automatic retries can worsen bond instability
                    if (status == 133) {
                        Log.e(TAG, "⚠️ GATT_ERROR(133) - This usually means:")
                        Log.e(TAG, "   - Stale bond info (gateway forgot the bond)")
                        Log.e(TAG, "   - Go to Android Settings > Bluetooth > Forget device, then re-pair")
                        updateNotification("Bond error - please re-pair device")
                    }
                    
                    // If gateway terminated with status 19, it might be a protocol issue
                    if (status == 19) {
                        Log.w(TAG, "⚠️ Gateway terminated connection - this may indicate:")
                        Log.w(TAG, "   - Protocol issue (missing heartbeat, wrong commands)")
                        Log.w(TAG, "   - Authentication/encryption mismatch")
                        Log.w(TAG, "   - Gateway busy with another client")
                    }
                    
                    updateNotification("Disconnected: $disconnectReason")
                    
                    connectedDevices.remove(device.address)
                    pollingJobs[device.address]?.cancel()
                    pollingJobs.remove(device.address)
                    pendingBondDevices.remove(device.address)  // Clean up pending bonds
                    
                    serviceScope.launch {
                        // Get the plugin for this device
                        val plugin = if (pluginId != null) {
                            pluginRegistry.getLoadedBlePlugin(pluginId)
                        } else null
                        
                        plugin?.onDeviceDisconnected(device)
                        publishAvailability(device, false)
                        
                        // Note: Plugin remains loaded for quick reconnection
                        // Plugins only unload on service stop or critical memory pressure
                    }
                    
                    gatt.close()
                    
                    // Resume scanning if no devices connected
                    if (connectedDevices.isEmpty()) {
                        startScanning()
                    }
                }
            }
        }
        
        override fun onServicesDiscovered(gatt: BluetoothGatt, status: Int) {
            if (status == BluetoothGatt.GATT_SUCCESS) {
                Log.i(TAG, "✅ Services discovered for ${gatt.device.address}")
                appendServiceLog("Services discovered for ${gatt.device.address}")
                
                // Log all discovered services and characteristics
                for (service in gatt.services) {
                    Log.i(TAG, "Service: ${service.uuid}")
                    for (char in service.characteristics) {
                        Log.i(TAG, "  Char: ${char.uuid} properties=0x${char.properties.toString(16)}")
                    }
                }
                
                // NOTE: Legacy app does NOT request MTU - skip MTU negotiation
                // MTU requests can trigger bond instability on some devices
                // The default MTU (23 bytes) is sufficient for the OneControl protocol
                
                // Brief delay for BLE stack to stabilize after service discovery
                serviceScope.launch(Dispatchers.Main) {
                    delay(100)  // Minimal delay - legacy app starts immediately
                    Log.d(TAG, "Starting plugin setup...")
                    
                    // Get the plugin for this device
                    val deviceInfo = connectedDevices[gatt.device.address]
                    val pluginId = deviceInfo?.second
                    val plugin = if (pluginId != null) {
                        pluginRegistry.getLoadedBlePlugin(pluginId)
                    } else null
                    
                    if (plugin != null) {
                        // Create GATT operations interface for plugin
                        val gattOps = GattOperationsImpl(gatt)
                        
                        // Call plugin setup (authentication, notification subscription, etc.)
                        val setupResult = plugin.onServicesDiscovered(gatt.device, gattOps)
                        if (setupResult.isFailure) {
                            Log.e(TAG, "Plugin setup failed: ${setupResult.exceptionOrNull()?.message}")
                        }
                    }
                    
                    // Publish Home Assistant discovery
                    publishDiscovery(gatt.device)
                }
            } else {
                Log.w(TAG, "Service discovery failed for ${gatt.device.address}: $status")
            }
        }
        
        // New API 33+ callback - called on Android 13+
        // CRITICAL: Call plugin DIRECTLY on BLE callback thread (like legacy app)
        // Do NOT use serviceScope.launch - that causes timing/ordering issues
        override fun onCharacteristicChanged(
            gatt: BluetoothGatt,
            characteristic: BluetoothGattCharacteristic,
            value: ByteArray
        ) {
            val uuid = characteristic.uuid.toString().lowercase()
            Log.i(TAG, "📨📨📨 onCharacteristicChanged (API33+) CALLED for $uuid: ${value.size} bytes")
            // Direct call on BLE thread - matches legacy app behavior
            handleCharacteristicNotificationDirect(gatt.device, uuid, value)
        }
        
        // Legacy callback for API < 33 (deprecated but still needed for some devices)
        // CRITICAL: Call plugin DIRECTLY on BLE callback thread (like legacy app)
        @Deprecated("Deprecated in API 33")
        @Suppress("DEPRECATION")
        override fun onCharacteristicChanged(
            gatt: BluetoothGatt,
            characteristic: BluetoothGattCharacteristic
        ) {
            val uuid = characteristic.uuid.toString().lowercase()
            @Suppress("DEPRECATION")
            val value = characteristic.value ?: byteArrayOf()
            Log.i(TAG, "📨📨📨 onCharacteristicChanged (legacy) CALLED for $uuid: ${value.size} bytes")
            // Direct call on BLE thread - matches legacy app behavior
            handleCharacteristicNotificationDirect(gatt.device, uuid, value)
        }
        
        // Legacy callback for API < 33
        @Suppress("DEPRECATION")
        override fun onCharacteristicRead(
            gatt: BluetoothGatt,
            characteristic: BluetoothGattCharacteristic,
            status: Int
        ) {
            val uuid = characteristic.uuid.toString().lowercase()
            @Suppress("DEPRECATION")
            val value = characteristic.value ?: byteArrayOf()
            Log.d(TAG, "📖 onCharacteristicRead (legacy) callback: uuid=$uuid, status=$status, ${value.size} bytes")
            handleReadCallback(uuid, value, status)
        }
        
        // New callback for API 33+
        override fun onCharacteristicRead(
            gatt: BluetoothGatt,
            characteristic: BluetoothGattCharacteristic,
            value: ByteArray,
            status: Int
        ) {
            val uuid = characteristic.uuid.toString().lowercase()
            Log.d(TAG, "📖 onCharacteristicRead (API33+) callback: uuid=$uuid, status=$status, ${value.size} bytes")
            handleReadCallback(uuid, value, status)
        }
        
        private val onMtuChangedListeners = mutableMapOf<String, (Int, Boolean) -> Unit>()

        private fun handleReadCallback(uuid: String, value: ByteArray, status: Int) {
            val deferred = pendingReads.remove(uuid)
            if (deferred != null) {
                if (status == BluetoothGatt.GATT_SUCCESS) {
                    Log.d(TAG, "✅ Read success for $uuid: ${value.size} bytes")
                    deferred.complete(Result.success(value))
                } else {
                    Log.e(TAG, "❌ Read failed for $uuid: status=$status")
                    deferred.complete(Result.failure(Exception("GATT read failed: status=$status")))
                }
            } else {
                Log.w(TAG, "⚠️ onCharacteristicRead: No pending deferred for $uuid")
            }
        }

        override fun onMtuChanged(gatt: BluetoothGatt, mtu: Int, status: Int) {
            val device = gatt.device
            val success = status == BluetoothGatt.GATT_SUCCESS
            Log.i(TAG, "onMtuChanged for ${device.address}: mtu=$mtu, status=$status, success=$success")
            onMtuChangedListeners[device.address]?.invoke(mtu, success)
        }
        
        override fun onCharacteristicWrite(
            gatt: BluetoothGatt,
            characteristic: BluetoothGattCharacteristic,
            status: Int
        ) {
            val uuid = characteristic.uuid.toString().lowercase()
            val deferred = pendingWrites.remove(uuid)
            
            if (deferred != null) {
                if (status == BluetoothGatt.GATT_SUCCESS) {
                    Log.d(TAG, "Write success for $uuid")
                    deferred.complete(Result.success(Unit))
                } else {
                    Log.e(TAG, "Write failed for $uuid: status=$status")
                    deferred.complete(Result.failure(Exception("GATT write failed: status=$status")))
                }
            }
        }
        
        override fun onDescriptorWrite(
            gatt: BluetoothGatt,
            descriptor: BluetoothGattDescriptor,
            status: Int
        ) {
            val charUuid = descriptor.characteristic.uuid.toString().lowercase()
            val deferred = pendingDescriptorWrites.remove(charUuid)
            
            if (status == BluetoothGatt.GATT_SUCCESS) {
                Log.d(TAG, "✅ Descriptor write success for $charUuid")
                deferred?.complete(Result.success(Unit))
            } else {
                Log.e(TAG, "❌ Descriptor write failed for $charUuid: status=$status")
                deferred?.complete(Result.failure(Exception("Descriptor write failed: status=$status")))
            }
        }
    }

    /**
     * Handle characteristic notification from BLE device.
     * CRITICAL: Called DIRECTLY on BLE callback thread (not via coroutine) to match legacy app behavior.
     * Plugin receives notification immediately, can queue work for background processing.
     */
    private fun handleCharacteristicNotificationDirect(
        device: BluetoothDevice,
        characteristicUuid: String,
        value: ByteArray
    ) {
        // Get the plugin for this device
        val deviceInfo = connectedDevices[device.address]
        val pluginId = deviceInfo?.second ?: return
        val plugin = pluginRegistry.getLoadedBlePlugin(pluginId) ?: return
        
        try {
            // Call plugin directly on BLE thread (like legacy app)
            val stateUpdates = plugin.onCharacteristicNotification(device, characteristicUuid, value)
            
            // Publish state updates asynchronously (MQTT publishing can be async)
            if (stateUpdates.isNotEmpty()) {
                val output = outputPlugin ?: return
                serviceScope.launch {
                    for ((topicSuffix, payload) in stateUpdates) {
                        val deviceId = plugin.getDeviceId(device)
                        output.publishState("device/$deviceId/$topicSuffix", payload, retained = true)
                    }
                }
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error handling notification", e)
        }
    }
    
    /**
     * Start periodic polling if plugin requires it.
     */
    private fun startPollingIfNeeded(device: BluetoothDevice, plugin: BlePluginInterface) {
        val intervalMs = plugin.getPollingIntervalMs() ?: return
        
        val job = serviceScope.launch {
            while (isActive) {
                delay(intervalMs)
                
                try {
                    plugin.performPeriodicPoll(device)
                } catch (e: Exception) {
                    Log.e(TAG, "Error during polling", e)
                }
            }
        }
        
        pollingJobs[device.address] = job
        Log.d(TAG, "Started polling for ${device.address} (interval: ${intervalMs}ms)")
    }
    
    /**
     * Publish Home Assistant discovery payloads.
     */
    private suspend fun publishDiscovery(device: BluetoothDevice) {
        val output = outputPlugin ?: return
        
        // Get the plugin for this device
        val deviceInfo = connectedDevices[device.address]
        val pluginId = deviceInfo?.second ?: return
        val plugin = pluginRegistry.getLoadedBlePlugin(pluginId) ?: return
        
        try {
            val discoveryPayloads = plugin.getDiscoveryPayloads(device)
            
            for ((topic, payload) in discoveryPayloads) {
                output.publishDiscovery(topic, payload)
            }
            
            Log.i(TAG, "Published ${discoveryPayloads.size} discovery payloads for ${device.address}")
        } catch (e: Exception) {
            Log.e(TAG, "Error publishing discovery", e)
        }
    }
    
    /**
     * Publish device availability.
     */
    private suspend fun publishAvailability(device: BluetoothDevice, online: Boolean) {
        outputPlugin?.publishAvailability(online)
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
            } catch (e: SecurityException) {
                Log.e(TAG, "Permission denied for disconnect", e)
            }
        }
        
        connectedDevices.clear()
        
        // Create a copy to avoid ConcurrentModificationException
        for ((_, job) in pollingJobs.toList()) {
            job.cancel()
        }
        pollingJobs.clear()
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
    
    /**
     * GATT operations implementation for plugins.
     * Provides async methods for reading/writing characteristics and managing notifications.
     */
    private inner class GattOperationsImpl(private val gatt: BluetoothGatt) : BlePluginInterface.GattOperations {
        
        override suspend fun readCharacteristic(uuid: String): Result<ByteArray> = withContext(Dispatchers.Main) {
            val characteristic = findCharacteristic(uuid)
            if (characteristic == null) {
                Log.e(TAG, "❌ readCharacteristic: Characteristic not found: $uuid")
                return@withContext Result.failure(Exception("Characteristic not found: $uuid"))
            }
            
            Log.i(TAG, "📖 readCharacteristic: uuid=$uuid, props=0x${characteristic.properties.toString(16)}")
            
            // Clear any cached value (matches legacy app behavior)
            @Suppress("DEPRECATION")
            characteristic.value = null
            
            val normalizedUuid = uuid.lowercase()
            val deferred = CompletableDeferred<Result<ByteArray>>()
            pendingReads[normalizedUuid] = deferred
            
            try {
                @Suppress("DEPRECATION")
                val success = gatt.readCharacteristic(characteristic)
                Log.i(TAG, "📖 readCharacteristic initiated: success=$success for $uuid")
                if (!success) {
                    pendingReads.remove(normalizedUuid)
                    return@withContext Result.failure(Exception("Failed to initiate read for $uuid"))
                }
                
                // Wait for callback with timeout (increased to 10s for slow gateways)
                withTimeout(10000) {
                    deferred.await()
                }
            } catch (e: TimeoutCancellationException) {
                pendingReads.remove(normalizedUuid)
                Result.failure(Exception("Read timeout for $uuid"))
            } catch (e: SecurityException) {
                pendingReads.remove(normalizedUuid)
                Result.failure(Exception("Permission denied for read: $uuid"))
            } catch (e: Exception) {
                pendingReads.remove(normalizedUuid)
                Result.failure(e)
            }
        }
        
        override suspend fun writeCharacteristic(uuid: String, value: ByteArray): Result<Unit> = withContext(Dispatchers.Main) {
            val characteristic = findCharacteristic(uuid)
            if (characteristic == null) {
                return@withContext Result.failure(Exception("Characteristic not found: $uuid"))
            }
            
            val normalizedUuid = uuid.lowercase()
            val deferred = CompletableDeferred<Result<Unit>>()
            pendingWrites[normalizedUuid] = deferred
            
            try {
                // Use legacy API on main thread - this is what the working original app does
                @Suppress("DEPRECATION")
                characteristic.value = value
                @Suppress("DEPRECATION")
                val success = gatt.writeCharacteristic(characteristic)
                
                if (!success) {
                    pendingWrites.remove(normalizedUuid)
                    return@withContext Result.failure(Exception("Failed to initiate write for $uuid"))
                }
                
                // Wait for callback with timeout
                withTimeout(5000) {
                    deferred.await()
                }
            } catch (e: TimeoutCancellationException) {
                pendingWrites.remove(normalizedUuid)
                Result.failure(Exception("Write timeout for $uuid"))
            } catch (e: SecurityException) {
                pendingWrites.remove(normalizedUuid)
                Result.failure(Exception("Permission denied for write: $uuid"))
            } catch (e: Exception) {
                pendingWrites.remove(normalizedUuid)
                Result.failure(e)
            }
        }
        
        override suspend fun writeCharacteristicNoResponse(uuid: String, value: ByteArray): Result<Unit> = withContext(Dispatchers.Main) {
            val characteristic = findCharacteristic(uuid)
            if (characteristic == null) {
                Log.e(TAG, "❌ writeCharacteristicNoResponse: Characteristic not found: $uuid")
                return@withContext Result.failure(Exception("Characteristic not found: $uuid"))
            }
            
            Log.d(TAG, "📝 writeCharacteristicNoResponse: uuid=$uuid, props=0x${characteristic.properties.toString(16)}, ${value.size} bytes")
            
            try {
                // Use legacy API on main thread - this is what the working original app does
                @Suppress("DEPRECATION")
                characteristic.value = value
                @Suppress("DEPRECATION")
                characteristic.writeType = BluetoothGattCharacteristic.WRITE_TYPE_NO_RESPONSE
                @Suppress("DEPRECATION")
                val success = gatt.writeCharacteristic(characteristic)
                
                if (success) {
                    Log.d(TAG, "✅ No-response write initiated for $uuid")
                    return@withContext Result.success(Unit)
                } else {
                    Log.e(TAG, "❌ No-response write returned false for $uuid")
                    return@withContext Result.failure(Exception("Failed to initiate no-response write for $uuid"))
                }
            } catch (e: Exception) {
                Log.e(TAG, "❌ writeCharacteristicNoResponse exception: ${e.message}", e)
                return@withContext Result.failure(e)
            }
        }
        
        override suspend fun enableNotifications(uuid: String): Result<Unit> {
            val characteristic = findCharacteristic(uuid)
            if (characteristic == null) {
                return Result.failure(Exception("Characteristic not found: $uuid"))
            }
            
            val normalizedUuid = uuid.lowercase()
            
            return try {
                // First: Enable local notifications on main thread
                val localSuccess = withContext(Dispatchers.Main) {
                    gatt.setCharacteristicNotification(characteristic, true)
                }
                if (!localSuccess) {
                    return Result.failure(Exception("Failed to enable local notifications for $uuid"))
                }
                
                // CRITICAL: BLE stack needs time to process setCharacteristicNotification before CCCD write
                // Without this delay, notifications may not work properly (gateway disconnects)
                delay(100)
                
                // Write descriptor to enable notifications on remote device
                val descriptor = characteristic.getDescriptor(
                    UUID.fromString("00002902-0000-1000-8000-00805f9b34fb") // Client Characteristic Configuration
                )
                
                if (descriptor != null) {
                    val deferred = CompletableDeferred<Result<Unit>>()
                    pendingDescriptorWrites[normalizedUuid] = deferred
                    
                    val descriptorValue = BluetoothGattDescriptor.ENABLE_NOTIFICATION_VALUE
                    
                    val writeSuccess = withContext(Dispatchers.Main) {
                        // Use legacy API on main thread
                        @Suppress("DEPRECATION")
                        descriptor.value = descriptorValue
                        @Suppress("DEPRECATION")
                        gatt.writeDescriptor(descriptor)
                    }
                    
                    if (!writeSuccess) {
                        pendingDescriptorWrites.remove(normalizedUuid)
                        return Result.failure(Exception("Failed to initiate descriptor write for $uuid"))
                    }
                    
                    // Wait for callback with timeout
                    withTimeout(5000) {
                        deferred.await()
                    }
                    
                    Log.d(TAG, "Enabled notifications for $uuid")
                    Result.success(Unit)
                } else {
                    Log.w(TAG, "No CCCD descriptor found for $uuid - notifications may not work")
                    Result.success(Unit)
                }
            } catch (e: TimeoutCancellationException) {
                pendingDescriptorWrites.remove(normalizedUuid)
                Result.failure(Exception("Descriptor write timeout for $uuid"))
            } catch (e: SecurityException) {
                pendingDescriptorWrites.remove(normalizedUuid)
                Result.failure(Exception("Permission denied for notifications: $uuid"))
            } catch (e: Exception) {
                pendingDescriptorWrites.remove(normalizedUuid)
                Result.failure(e)
            }
        }
        
        override suspend fun disableNotifications(uuid: String): Result<Unit> = withContext(Dispatchers.Main) {
            val characteristic = findCharacteristic(uuid)
            if (characteristic == null) {
                return@withContext Result.failure(Exception("Characteristic not found: $uuid"))
            }
            
            try {
                gatt.setCharacteristicNotification(characteristic, false)
                
                val descriptor = characteristic.getDescriptor(
                    UUID.fromString("00002902-0000-1000-8000-00805f9b34fb")
                )
                
                if (descriptor != null) {
                    val descriptorValue = BluetoothGattDescriptor.DISABLE_NOTIFICATION_VALUE
                    
                    // Use legacy API on main thread
                    @Suppress("DEPRECATION")
                    descriptor.value = descriptorValue
                    @Suppress("DEPRECATION")
                    gatt.writeDescriptor(descriptor)
                    
                    Log.d(TAG, "Disabled notifications for $uuid")
                }
                
                Result.success(Unit)
            } catch (e: SecurityException) {
                Result.failure(Exception("Permission denied for notifications: $uuid"))
            } catch (e: Exception) {
                Result.failure(e)
            }
        }
        
        /**
         * Find a characteristic by UUID across all services.
         */
        private fun findCharacteristic(uuid: String): BluetoothGattCharacteristic? {
            val targetUuid = UUID.fromString(uuid)
            for (service in gatt.services) {
                for (characteristic in service.characteristics) {
                    if (characteristic.uuid == targetUuid) {
                        return characteristic
                    }
                }
            }
            return null
        }
        
        /**
         * Check if a service exists on the connected device.
         * This is a non-triggering check that doesn't perform any GATT reads/writes.
         * IMPORTANT: Used to detect gateway type without triggering encryption!
         */
        override fun hasService(uuid: String): Boolean {
            val targetUuid = UUID.fromString(uuid)
            return gatt.services.any { it.uuid == targetUuid }
        }
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
                _pluginStatuses.value.forEach { (pluginId, status) ->
                    out.appendLine("  $pluginId:")
                    out.appendLine("    Connected: ${status.connected}")
                    out.appendLine("    Authenticated: ${status.authenticated}")
                    out.appendLine("    Data Healthy: ${status.dataHealthy}")
                }
                out.appendLine("")
                
                out.appendLine("Active Plugins:")
                blePlugin?.let { out.appendLine("  BLE: ${it.javaClass.simpleName}") }
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
            _pluginStatuses.value.forEach { (pluginId, status) ->
                appendLine("  $pluginId:")
                appendLine("    Connected: ${status.connected}")
                appendLine("    Authenticated: ${status.authenticated}")
                appendLine("    Data Healthy: ${status.dataHealthy}")
            }
            appendLine("")
            
            appendLine("Active Plugins:")
            blePlugin?.let { appendLine("  BLE: ${it.javaClass.simpleName}") }
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
            kotlinx.coroutines.delay(500)
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

