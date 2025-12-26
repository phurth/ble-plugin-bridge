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
import com.blemqttbridge.R
import com.blemqttbridge.core.interfaces.BleDevicePlugin
import com.blemqttbridge.core.interfaces.BlePluginInterface
import com.blemqttbridge.core.interfaces.MqttPublisher
import com.blemqttbridge.core.interfaces.PluginConfig
import com.blemqttbridge.core.interfaces.OutputPluginInterface
import com.blemqttbridge.plugins.blescanner.BleScannerPlugin
import com.blemqttbridge.plugins.output.MqttOutputPlugin
import kotlinx.coroutines.*
import java.util.UUID

/**
 * Base BLE service with plugin hooks.
 * Manages BLE scanning, connections, and delegates protocol handling to plugins.
 */
class BaseBleService : Service() {
    
    companion object {
        private const val TAG = "BaseBleService"
        private const val NOTIFICATION_ID = 1
        private const val CHANNEL_ID = "ble_bridge_service"
        
        const val ACTION_START_SCAN = "com.blemqttbridge.START_SCAN"
        const val ACTION_STOP_SCAN = "com.blemqttbridge.STOP_SCAN"
        const val ACTION_STOP_SERVICE = "com.blemqttbridge.STOP_SERVICE"
        const val ACTION_DISCONNECT = "com.blemqttbridge.DISCONNECT"
        const val ACTION_EXPORT_DEBUG_LOG = "com.blemqttbridge.EXPORT_DEBUG_LOG"
        const val ACTION_START_TRACE = "com.blemqttbridge.START_TRACE"
        const val ACTION_STOP_TRACE = "com.blemqttbridge.STOP_TRACE"
        
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
    private lateinit var bluetoothLeScanner: BluetoothLeScanner
    private lateinit var pluginRegistry: PluginRegistry
    private lateinit var memoryManager: MemoryManager
    
    private var blePlugin: BlePluginInterface? = null
    private var outputPlugin: OutputPluginInterface? = null
    private var remoteControlManager: RemoteControlManager? = null
    private var bleScannerPlugin: BleScannerPlugin? = null
    
    // Connected devices map: device address -> (BluetoothGatt, pluginId)
    private val connectedDevices = mutableMapOf<String, Pair<BluetoothGatt, String>>()
    
    // Polling jobs for devices that need periodic updates
    private val pollingJobs = mutableMapOf<String, Job>()
    
    // Pending GATT operations: characteristic UUID -> result deferred
    private val pendingReads = mutableMapOf<String, CompletableDeferred<Result<ByteArray>>>()
    private val pendingWrites = mutableMapOf<String, CompletableDeferred<Result<Unit>>>()
    private val pendingDescriptorWrites = mutableMapOf<String, CompletableDeferred<Result<Unit>>>()
    
    // Devices currently undergoing bonding process
    private val pendingBondDevices = mutableSetOf<String>()
    
    private var isScanning = false
    
    // Debug logging and trace
    private val debugLogBuffer = ArrayDeque<String>()
    private var traceEnabled = false
    private var traceWriter: java.io.BufferedWriter? = null
    private var traceFile: java.io.File? = null
    private var traceBytes: Long = 0
    private var traceStartedAt: Long = 0
    private var traceTimeout: Runnable? = null
    private val handler = android.os.Handler(android.os.Looper.getMainLooper())
    
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
            _pluginStatuses.value = _pluginStatuses.value + (pluginId to status)
            Log.d(TAG, "📊 Updated plugin status: $pluginId - connected=$connected, authenticated=$authenticated, dataHealthy=$dataHealthy")
        }
        
        override fun updateMqttStatus(connected: Boolean) {
            Log.i(TAG, "📊 updateMqttStatus: connected=$connected (was ${_mqttConnected.value})")
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
        Log.i(TAG, "Service created")
        
        val bluetoothManager = getSystemService(Context.BLUETOOTH_SERVICE) as BluetoothManager
        bluetoothAdapter = bluetoothManager.adapter
        bluetoothLeScanner = bluetoothAdapter.bluetoothLeScanner
        
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
        
        createNotificationChannel()
        startForeground(NOTIFICATION_ID, createNotification("Service starting..."))
    }
    
    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        Log.d(TAG, "onStartCommand: ${intent?.action}")
        
        when (intent?.action) {
            ACTION_START_SCAN -> {
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
                    initializeMultiplePlugins(enabledBlePlugins, outputPluginId, outputConfig)
                    // Note: initializeMultiplePlugins now calls reconnectToBondedDevices() which 
                    // either connects to bonded devices or falls back to scanning
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
        }
        
        return START_STICKY
    }
    
    override fun onBind(intent: Intent?): IBinder? = null
    
    override fun onDestroy() {
        super.onDestroy()
        Log.i(TAG, "Service destroyed")
        
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
            
            // Initialize BLE Scanner plugin only if enabled
            if (ServiceStateManager.isBlePluginEnabled(applicationContext, BleScannerPlugin.PLUGIN_ID)) {
                bleScannerPlugin = BleScannerPlugin(applicationContext, mqttPublisher)
                if (bleScannerPlugin?.initialize() == true) {
                    Log.i(TAG, "BLE Scanner plugin initialized")
                } else {
                    Log.w(TAG, "BLE Scanner plugin failed to initialize")
                    bleScannerPlugin = null
                }
            } else {
                Log.i(TAG, "BLE Scanner plugin is disabled, skipping initialization")
                bleScannerPlugin = null
            }
        }
        
        // Load BLE plugin - support both legacy (BlePluginInterface) and new (BleDevicePlugin) architecture
        blePlugin = pluginRegistry.getBlePlugin(blePluginId, applicationContext, bleConfig)
        if (blePlugin == null) {
            // Plugin might be a BleDevicePlugin (new architecture) - check if it exists
            val devicePlugin = pluginRegistry.getDevicePlugin(blePluginId, applicationContext)
            if (devicePlugin == null) {
                Log.e(TAG, "Failed to load BLE plugin: $blePluginId (not found in registry)")
                updateNotification("Error: BLE plugin failed to load")
                return
            } else {
                Log.i(TAG, "Loaded device plugin: $blePluginId (new architecture - plugin-owned GATT callback)")
            }
        } else {
            Log.i(TAG, "Loaded legacy plugin: $blePluginId (BlePluginInterface - forwarding callback)")
        }
        
        Log.i(TAG, "Plugins initialized successfully")
        memoryManager.logMemoryUsage()
        
        // CRITICAL: Try to reconnect to bonded devices first!
        // Many BLE devices don't actively advertise when bonded - they wait for reconnection
        reconnectToBondedDevices()
    }
    
    /**
     * Initialize multiple BLE plugins.
     * Loads all enabled plugins and prepares for multi-device connections.
     */
    private suspend fun initializeMultiplePlugins(
        enabledBlePlugins: Set<String>,
        outputPluginId: String,
        outputConfig: Map<String, String>
    ) {
        Log.i(TAG, "Initializing ${enabledBlePlugins.size} BLE plugins: $enabledBlePlugins")
        
        // Load output plugin first (needed for publishing)
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
            
            // Initialize BLE Scanner plugin only if enabled
            if (ServiceStateManager.isBlePluginEnabled(applicationContext, BleScannerPlugin.PLUGIN_ID)) {
                bleScannerPlugin = BleScannerPlugin(applicationContext, mqttPublisher)
                if (bleScannerPlugin?.initialize() == true) {
                    Log.i(TAG, "BLE Scanner plugin initialized")
                } else {
                    Log.w(TAG, "BLE Scanner plugin failed to initialize")
                    bleScannerPlugin = null
                }
            } else {
                Log.i(TAG, "BLE Scanner plugin is disabled, skipping initialization")
                bleScannerPlugin = null
            }
        }
        
        // Load ALL enabled BLE plugins
        var loadedCount = 0
        for (pluginId in enabledBlePlugins) {
            val bleConfig = AppConfig.getBlePluginConfig(applicationContext, pluginId)
            Log.i(TAG, "Loading plugin: $pluginId with config: $bleConfig")
            
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
            Log.e(TAG, "No plugins were loaded!")
            updateNotification("Error: No plugins loaded")
            return
        }
        
        Log.i(TAG, "✓ Loaded $loadedCount/${enabledBlePlugins.size} plugins successfully")
        memoryManager.logMemoryUsage()
        
        // Try to reconnect to bonded devices or scan for new ones
        reconnectToBondedDevices()
    }

    /**
     * Connect to known devices - both bonded and configured.
     * 
     * This replaces continuous scanning with direct connections:
     * 1. Bonded devices: Connect directly (they may not advertise when bonded)
     * 2. Configured devices: Connect directly using the user-provided MAC address
     * 
     * Scanning is only needed for device discovery, not for connecting to known devices.
     */
    private fun reconnectToBondedDevices() {
        val devicesToConnect = mutableMapOf<String, String>() // MAC -> pluginId
        
        // 1. Check bonded devices
        try {
            val bondedDevices = bluetoothAdapter.bondedDevices
            Log.i(TAG, "Checking ${bondedDevices.size} bonded device(s) for plugin matches...")
            
            for (device in bondedDevices) {
                val pluginId = pluginRegistry.findPluginForDevice(device, null, applicationContext)
                if (pluginId != null) {
                    Log.i(TAG, "🔗 Found bonded device matching plugin: ${device.address} -> $pluginId")
                    devicesToConnect[device.address] = pluginId
                }
            }
        } catch (e: SecurityException) {
            Log.e(TAG, "Permission denied for accessing bonded devices", e)
        }
        
        // 2. Check configured device MACs from device plugins
        for (pluginId in pluginRegistry.getRegisteredBlePlugins()) {
            val devicePlugin = pluginRegistry.getDevicePlugin(pluginId, applicationContext)
            if (devicePlugin != null) {
                try {
                    val config = PluginConfig(AppConfig.getBlePluginConfig(applicationContext, pluginId))
                    devicePlugin.initialize(applicationContext, config)
                    val configuredMacs = devicePlugin.getConfiguredDevices()
                    for (mac in configuredMacs) {
                        if (!devicesToConnect.containsKey(mac.uppercase())) {
                            Log.i(TAG, "🔗 Found configured device: $mac -> $pluginId")
                            devicesToConnect[mac.uppercase()] = pluginId
                        }
                    }
                } catch (e: Exception) {
                    Log.w(TAG, "Could not get configured devices for plugin $pluginId: ${e.message}")
                }
            }
        }
        
        // 3. Connect to all known devices
        if (devicesToConnect.isEmpty()) {
            Log.w(TAG, "No known devices to connect to")
            updateNotification("No devices configured")
            return
        }
        
        Log.i(TAG, "✓ Found ${devicesToConnect.size} device(s) to connect")
        
        for ((mac, pluginId) in devicesToConnect) {
            serviceScope.launch {
                try {
                    val device = bluetoothAdapter.getRemoteDevice(mac)
                    Log.i(TAG, "🔗 Connecting directly to $mac (plugin: $pluginId)")
                    connectToDevice(device, pluginId)
                } catch (e: Exception) {
                    Log.e(TAG, "Failed to get remote device $mac: ${e.message}")
                }
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
        
        // Get target MAC addresses from loaded BlePluginInterface plugins (legacy)
        for ((pluginId, plugin) in pluginRegistry.getLoadedBlePlugins()) {
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
        
        // Get target MAC addresses from BleDevicePlugin plugins (new architecture)
        // This includes plugins like EasyTouch that use the new callback architecture
        for (pluginId in pluginRegistry.getRegisteredBlePlugins()) {
            val devicePlugin = pluginRegistry.getDevicePlugin(pluginId, applicationContext)
            if (devicePlugin != null) {
                // Initialize with config so it knows the configured MAC
                try {
                    val config = PluginConfig(AppConfig.getBlePluginConfig(applicationContext, pluginId))
                    devicePlugin.initialize(applicationContext, config)
                    val configuredMacs = devicePlugin.getConfiguredDevices()
                    for (mac in configuredMacs) {
                        if (addedMacs.add(mac.uppercase())) {
                            Log.d(TAG, "Adding scan filter for MAC: $mac (device plugin: $pluginId)")
                            scanFilters.add(
                                ScanFilter.Builder()
                                    .setDeviceAddress(mac.uppercase())
                                    .build()
                            )
                        }
                    }
                } catch (e: Exception) {
                    Log.w(TAG, "Could not get configured devices for plugin $pluginId: ${e.message}")
                }
            }
        }
        
        // If no specific targets, add a permissive filter (allows screen-off scanning)
        if (scanFilters.isEmpty()) {
            Log.w(TAG, "No target MACs configured - using unfiltered scan (may not work with screen off)")
        }
        
        val scanSettings = ScanSettings.Builder()
            .setScanMode(ScanSettings.SCAN_MODE_LOW_LATENCY)
            .build()
        
        try {
            // Use filters if available, otherwise null (unfiltered)
            val filters = if (scanFilters.isNotEmpty()) scanFilters else null
            bluetoothLeScanner.startScan(filters, scanSettings, scanCallback)
            isScanning = true
            updateNotification("Scanning for devices...")
            Log.i(TAG, "BLE scan started with ${scanFilters.size} filter(s)")
        } catch (e: SecurityException) {
            Log.e(TAG, "Permission denied for BLE scan", e)
            updateNotification("Error: BLE permission denied")
        }
    }
    
    /**
     * Stop BLE scanning.
     */
    private fun stopScanning() {
        if (!isScanning) return
        
        try {
            bluetoothLeScanner.stopScan(scanCallback)
            isScanning = false
            Log.i(TAG, "BLE scan stopped")
        } catch (e: SecurityException) {
            Log.e(TAG, "Permission denied for stopping scan", e)
        }
    }
    
    /**
     * BLE scan callback.
     */
    private val scanCallback = object : ScanCallback() {
        private var scanResultCount = 0
        
        override fun onScanResult(callbackType: Int, result: ScanResult) {
            val device = result.device
            val scanRecord = result.scanRecord?.bytes
            
            scanResultCount++
            // Log every 10th device to avoid spam, but always log our target
            if (device.address.contains("24:DC:C3", ignoreCase = true) || 
                device.address.contains("1E:0A", ignoreCase = true) ||
                scanResultCount % 10 == 1) {
                Log.d(TAG, "📡 Scan result #$scanResultCount: ${device.address} (name: ${device.name ?: "?"})")
            }
            
            // Check if we already have this device
            if (connectedDevices.containsKey(device.address)) {
                return
            }
            
            // Check if any plugin can handle this device
            val pluginId = pluginRegistry.findPluginForDevice(device, scanRecord, applicationContext)
            if (pluginId != null) {
                Log.i(TAG, "✅ Found matching device: ${device.address} -> plugin: $pluginId")
                
                // Stop scanning (we found a device)
                stopScanning()
                
                // Load plugin and connect to device
                serviceScope.launch {
                    // Check if plugin can be loaded (either legacy BlePluginInterface or new BleDevicePlugin)
                    val legacyPlugin = pluginRegistry.getBlePlugin(pluginId, applicationContext, emptyMap())
                    val devicePlugin = pluginRegistry.getDevicePlugin(pluginId, applicationContext)
                    
                    if (legacyPlugin != null || devicePlugin != null) {
                        connectToDevice(device, pluginId)
                    } else {
                        Log.e(TAG, "Failed to load plugin $pluginId for device ${device.address}")
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
            val devicePlugin = pluginRegistry.getDevicePlugin(pluginId, applicationContext)
            
            val callback = if (devicePlugin != null) {
                // NEW: Plugin provides its own callback
                Log.i(TAG, "Using plugin-owned GATT callback for ${device.address}")
                
                // Initialize plugin if needed
                val config = PluginConfig(AppConfig.getBlePluginConfig(applicationContext, pluginId))
                devicePlugin.initialize(applicationContext, config)
                
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
            
            // NOTE: Do NOT call createBond() explicitly here!
            // The legacy app does not call createBond() - it lets the BLE stack
            // handle bonding automatically when accessing encrypted characteristics.
            // Calling createBond() explicitly can cause bond instability and status 133 errors.
            Log.i(TAG, "Connected GATT - bond state: ${device.bondState} (${when(device.bondState) {
                BluetoothDevice.BOND_BONDED -> "BONDED"
                BluetoothDevice.BOND_BONDING -> "BONDING"
                BluetoothDevice.BOND_NONE -> "NONE"
                else -> "UNKNOWN"
            }})")
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
        
        // Resume scanning if no devices connected
        if (connectedDevices.isEmpty()) {
            serviceScope.launch {
                delay(1000)
                startScanning()
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
        
        for ((_, deviceInfo) in connectedDevices) {
            val (gatt, _) = deviceInfo
            try {
                gatt.disconnect()
            } catch (e: SecurityException) {
                Log.e(TAG, "Permission denied for disconnect", e)
            }
        }
        
        connectedDevices.clear()
        
        for ((_, job) in pollingJobs) {
            job.cancel()
        }
        pollingJobs.clear()
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
     * Append a message to the debug log buffer (limited to MAX_DEBUG_LOG_LINES).
     * Automatically mirrors to trace file if trace is active.
     */
    private fun appendDebugLog(message: String) {
        val ts = System.currentTimeMillis()
        val formatted = "${java.text.SimpleDateFormat("yyyy-MM-dd HH:mm:ss.SSS", java.util.Locale.US).format(java.util.Date(ts))} - $message"
        debugLogBuffer.addLast(formatted)
        while (debugLogBuffer.size > MAX_DEBUG_LOG_LINES) {
            debugLogBuffer.removeFirst()
        }
        logTrace(message) // mirror into trace if enabled
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
        _traceActive.value = false
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
            if (!dir.exists()) dir.mkdirs()
            val ts = java.text.SimpleDateFormat("yyyyMMdd_HHmmss", java.util.Locale.US).format(java.util.Date())
            val file = java.io.File(dir, "debug_${ts}.txt")
            file.bufferedWriter().use { out ->
                out.appendLine("BLE-MQTT Plugin Bridge debug log")
                out.appendLine("Timestamp: $ts")
                out.appendLine("Service running: ${_serviceRunning.value}")
                out.appendLine("Plugin statuses:")
                _pluginStatuses.value.forEach { (pluginId, status) ->
                    out.appendLine("  $pluginId: connected=${status.connected}, auth=${status.authenticated}, dataHealthy=${status.dataHealthy}")
                }
                out.appendLine("MQTT connected: ${_mqttConnected.value}")
                out.appendLine("Trace active: $traceEnabled")
                traceFile?.let { out.appendLine("Trace file: ${it.absolutePath}") }
                out.appendLine("Active plugins:")
                blePlugin?.let { out.appendLine("  - BLE: ${it.javaClass.simpleName}") }
                outputPlugin?.let { out.appendLine("  - Output: ${it.javaClass.simpleName}") }
                out.appendLine("")
                out.appendLine("Recent events:")
                debugLogBuffer.forEach { line -> out.appendLine(line) }
            }
            Log.i(TAG, "📝 Debug log exported: ${file.absolutePath}")
            file
        } catch (e: Exception) {
            Log.e(TAG, "Failed to write debug log: ${e.message}")
            null
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
}
