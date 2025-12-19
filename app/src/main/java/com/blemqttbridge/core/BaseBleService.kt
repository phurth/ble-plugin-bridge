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
import com.blemqttbridge.core.interfaces.BlePluginInterface
import com.blemqttbridge.core.interfaces.OutputPluginInterface
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
        
        const val EXTRA_BLE_PLUGIN_ID = "ble_plugin_id"
        const val EXTRA_OUTPUT_PLUGIN_ID = "output_plugin_id"
        const val EXTRA_BLE_CONFIG = "ble_config"
        const val EXTRA_OUTPUT_CONFIG = "output_config"
    }
    
    private val serviceScope = CoroutineScope(Dispatchers.Default + SupervisorJob())
    
    private lateinit var bluetoothAdapter: BluetoothAdapter
    private lateinit var bluetoothLeScanner: BluetoothLeScanner
    private lateinit var pluginRegistry: PluginRegistry
    private lateinit var memoryManager: MemoryManager
    
    private var blePlugin: BlePluginInterface? = null
    private var outputPlugin: OutputPluginInterface? = null
    
    // Connected devices map: device address -> (BluetoothGatt, pluginId)
    private val connectedDevices = mutableMapOf<String, Pair<BluetoothGatt, String>>()
    
    // Polling jobs for devices that need periodic updates
    private val pollingJobs = mutableMapOf<String, Job>()
    
    // Pending GATT operations: characteristic UUID -> result deferred
    private val pendingReads = mutableMapOf<String, CompletableDeferred<Result<ByteArray>>>()
    private val pendingWrites = mutableMapOf<String, CompletableDeferred<Result<Unit>>>()
    
    // Devices currently undergoing bonding process
    private val pendingBondDevices = mutableSetOf<String>()
    
    private var isScanning = false
    
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
                Log.i(TAG, "Service marked as running")
                
                val blePluginId = intent.getStringExtra(EXTRA_BLE_PLUGIN_ID) ?: "onecontrol"
                val outputPluginId = intent.getStringExtra(EXTRA_OUTPUT_PLUGIN_ID) ?: "mqtt"
                
                // Load configuration from SharedPreferences
                val bleConfig = AppConfig.getBlePluginConfig(applicationContext, blePluginId)
                val outputConfig = AppConfig.getMqttConfig(applicationContext)
                
                serviceScope.launch {
                    initializePlugins(blePluginId, outputPluginId, bleConfig, outputConfig)
                    startScanning()
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
        }
        
        return START_STICKY
    }
    
    override fun onBind(intent: Intent?): IBinder? = null
    
    override fun onDestroy() {
        super.onDestroy()
        Log.i(TAG, "Service destroyed")
        
        // Mark service as stopped
        ServiceStateManager.setServiceRunning(applicationContext, false)
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
        }
        
        // Load BLE plugin
        blePlugin = pluginRegistry.getBlePlugin(blePluginId, applicationContext, bleConfig)
        if (blePlugin == null) {
            Log.e(TAG, "Failed to load BLE plugin: $blePluginId")
            updateNotification("Error: BLE plugin failed to load")
            return
        }
        
        Log.i(TAG, "Plugins initialized successfully")
        memoryManager.logMemoryUsage()
    }
    
    /**
     * Start BLE scanning for devices.
     */
    private fun startScanning() {
        if (isScanning) {
            Log.d(TAG, "Already scanning")
            return
        }
        
        val scanSettings = ScanSettings.Builder()
            .setScanMode(ScanSettings.SCAN_MODE_LOW_LATENCY)
            .build()
        
        try {
            bluetoothLeScanner.startScan(null, scanSettings, scanCallback)
            isScanning = true
            updateNotification("Scanning for devices...")
            Log.i(TAG, "BLE scan started")
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
        override fun onScanResult(callbackType: Int, result: ScanResult) {
            val device = result.device
            val scanRecord = result.scanRecord?.bytes
            
            // Check if we already have this device
            if (connectedDevices.containsKey(device.address)) {
                return
            }
            
            // Check if any plugin can handle this device
            val pluginId = pluginRegistry.findPluginForDevice(device, scanRecord)
            if (pluginId != null) {
                Log.i(TAG, "Found matching device: ${device.address} -> plugin: $pluginId")
                
                // Stop scanning (we found a device)
                stopScanning()
                
                // Load plugin and connect to device
                serviceScope.launch {
                    // Load the plugin if not already loaded
                    val plugin = pluginRegistry.getBlePlugin(pluginId, applicationContext, emptyMap())
                    if (plugin != null) {
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
     * Connect to a BLE device.
     */
    private suspend fun connectToDevice(device: BluetoothDevice, pluginId: String) {
        Log.i(TAG, "Connecting to ${device.address} (plugin: $pluginId)")
        updateNotification("Connecting to ${device.address}...")
        
        try {
            val gatt = device.connectGatt(
                applicationContext,
                false, // autoConnect = false for faster connection
                gattCallback,
                BluetoothDevice.TRANSPORT_LE
            )
            
            connectedDevices[device.address] = Pair(gatt, pluginId)
        } catch (e: SecurityException) {
            Log.e(TAG, "Permission denied for BLE connect", e)
            updateNotification("Error: BLE permission denied")
        }
    }
    
    /**
     * Bond state receiver for pairing events.
     */
    private val bondStateReceiver = object : BroadcastReceiver() {
        override fun onReceive(context: Context, intent: Intent) {
            val action = intent.action
            if (BluetoothDevice.ACTION_BOND_STATE_CHANGED == action) {
                val device = intent.getParcelableExtra<BluetoothDevice>(BluetoothDevice.EXTRA_DEVICE)
                val bondState = intent.getIntExtra(BluetoothDevice.EXTRA_BOND_STATE, BluetoothDevice.ERROR)
                val previousBondState = intent.getIntExtra(BluetoothDevice.EXTRA_PREVIOUS_BOND_STATE, BluetoothDevice.ERROR)
                
                device?.let {
                    if (pendingBondDevices.contains(it.address)) {
                        Log.i(TAG, "🔗 Bond state changed for ${it.address}: $previousBondState -> $bondState")
                        
                        when (bondState) {
                            BluetoothDevice.BOND_BONDED -> {
                                Log.i(TAG, "✅✅✅ Device ${it.address} bonded successfully!")
                                pendingBondDevices.remove(it.address)
                                updateNotification("Bonded - Discovering services...")
                                
                                // Now safe to discover services - find the gatt connection
                                val gatt = connectedDevices[it.address]?.first
                                if (gatt != null) {
                                    serviceScope.launch {
                                        delay(500) // Brief settle delay
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
                                    Log.e(TAG, "❌ Bonding failed for ${it.address}!")
                                    updateNotification("Pairing failed")
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
            
            when (newState) {
                BluetoothProfile.STATE_CONNECTED -> {
                    Log.i(TAG, "Connected to ${device.address} (plugin: $pluginId)")
                    updateNotification("Connected to ${device.address}")
                    
                    serviceScope.launch {
                        // Get the plugin for this device
                        val plugin = if (pluginId != null) {
                            pluginRegistry.getLoadedBlePlugin(pluginId)
                        } else null
                        
                        // Notify plugin of connection
                        plugin?.onDeviceConnected(device)
                        
                        // Check bond state - pairing required for OneControl gateways
                        val bondState = device.bondState
                        Log.i(TAG, "Bond state after connection: $bondState (${when(bondState) {
                            BluetoothDevice.BOND_BONDED -> "BONDED"
                            BluetoothDevice.BOND_BONDING -> "BONDING"
                            BluetoothDevice.BOND_NONE -> "NONE"
                            else -> "UNKNOWN"
                        }})")
                        
                        if (bondState != BluetoothDevice.BOND_BONDED) {
                            Log.i(TAG, "🔗 Device not bonded - initiating explicit bonding...")
                            updateNotification("Connected - Pairing...")
                            pendingBondDevices.add(device.address)
                            
                            try {
                                val bondResult = device.createBond()
                                if (bondResult) {
                                    Log.i(TAG, "✅ Bonding initiated - waiting for user to accept pairing dialog...")
                                    // Don't proceed until bonding completes (handled by bondStateReceiver)
                                } else {
                                    Log.e(TAG, "❌ Failed to initiate bonding!")
                                    pendingBondDevices.remove(device.address)
                                    updateNotification("Pairing failed - retrying...")
                                }
                            } catch (e: SecurityException) {
                                Log.e(TAG, "Permission denied for bonding", e)
                                pendingBondDevices.remove(device.address)
                                updateNotification("Error: Pairing permission denied")
                            }
                        } else {
                            Log.i(TAG, "✅ Device already bonded - proceeding with service discovery")
                            updateNotification("Connected - Discovering services...")
                            
                            // Already bonded - safe to discover services immediately
                            try {
                                gatt.discoverServices()
                            } catch (e: SecurityException) {
                                Log.e(TAG, "Permission denied for service discovery", e)
                            }
                        }
                        
                        // Start polling if needed (after authentication)
                        if (plugin != null && bondState == BluetoothDevice.BOND_BONDED) {
                            startPollingIfNeeded(device, plugin)
                        }
                        
                        // Publish availability
                        publishAvailability(device, true)
                    }
                }
                
                BluetoothProfile.STATE_DISCONNECTED -> {
                    Log.i(TAG, "Disconnected from ${device.address}")
                    updateNotification("Disconnected")
                    
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
                Log.i(TAG, "Services discovered for ${gatt.device.address}")
                
                // Log all discovered services and characteristics
                for (service in gatt.services) {
                    Log.d(TAG, "Service: ${service.uuid}")
                    for (char in service.characteristics) {
                        Log.d(TAG, "  Char: ${char.uuid} properties=0x${char.properties.toString(16)}")
                    }
                }
                
                serviceScope.launch {
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
        
        override fun onCharacteristicChanged(
            gatt: BluetoothGatt,
            characteristic: BluetoothGattCharacteristic,
            value: ByteArray
        ) {
            serviceScope.launch {
                handleCharacteristicNotification(gatt.device, characteristic.uuid.toString(), value)
            }
        }
        
        override fun onCharacteristicRead(
            gatt: BluetoothGatt,
            characteristic: BluetoothGattCharacteristic,
            value: ByteArray,
            status: Int
        ) {
            val uuid = characteristic.uuid.toString().lowercase()
            val deferred = pendingReads.remove(uuid)
            
            if (deferred != null) {
                if (status == BluetoothGatt.GATT_SUCCESS) {
                    Log.d(TAG, "Read success for $uuid: ${value.size} bytes")
                    deferred.complete(Result.success(value))
                } else {
                    Log.e(TAG, "Read failed for $uuid: status=$status")
                    deferred.complete(Result.failure(Exception("GATT read failed: status=$status")))
                }
            }
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
    }
    
    /**
     * Handle characteristic notification from BLE device.
     */
    private suspend fun handleCharacteristicNotification(
        device: BluetoothDevice,
        characteristicUuid: String,
        value: ByteArray
    ) {
        val output = outputPlugin ?: return
        
        // Get the plugin for this device
        val deviceInfo = connectedDevices[device.address]
        val pluginId = deviceInfo?.second ?: return
        val plugin = pluginRegistry.getLoadedBlePlugin(pluginId) ?: return
        
        try {
            val stateUpdates = plugin.onCharacteristicNotification(device, characteristicUuid, value)
            
            for ((topicSuffix, payload) in stateUpdates) {
                val deviceId = plugin.getDeviceId(device)
                output.publishState("device/$deviceId/$topicSuffix", payload, retained = true)
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
        
        override suspend fun readCharacteristic(uuid: String): Result<ByteArray> = withContext(Dispatchers.IO) {
            val characteristic = findCharacteristic(uuid)
            if (characteristic == null) {
                return@withContext Result.failure(Exception("Characteristic not found: $uuid"))
            }
            
            val normalizedUuid = uuid.lowercase()
            val deferred = CompletableDeferred<Result<ByteArray>>()
            pendingReads[normalizedUuid] = deferred
            
            try {
                val success = gatt.readCharacteristic(characteristic)
                if (!success) {
                    pendingReads.remove(normalizedUuid)
                    return@withContext Result.failure(Exception("Failed to initiate read for $uuid"))
                }
                
                // Wait for callback with timeout
                withTimeout(5000) {
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
        
        override suspend fun writeCharacteristic(uuid: String, value: ByteArray): Result<Unit> = withContext(Dispatchers.IO) {
            val characteristic = findCharacteristic(uuid)
            if (characteristic == null) {
                return@withContext Result.failure(Exception("Characteristic not found: $uuid"))
            }
            
            val normalizedUuid = uuid.lowercase()
            val deferred = CompletableDeferred<Result<Unit>>()
            pendingWrites[normalizedUuid] = deferred
            
            try {
                // Android 13+ (API 33) uses new writeCharacteristic signature
                val success = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
                    gatt.writeCharacteristic(
                        characteristic,
                        value,
                        BluetoothGattCharacteristic.WRITE_TYPE_DEFAULT
                    ) == BluetoothStatusCodes.SUCCESS
                } else {
                    @Suppress("DEPRECATION")
                    characteristic.value = value
                    @Suppress("DEPRECATION")
                    gatt.writeCharacteristic(characteristic)
                }
                
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
        
        override suspend fun enableNotifications(uuid: String): Result<Unit> = withContext(Dispatchers.IO) {
            val characteristic = findCharacteristic(uuid)
            if (characteristic == null) {
                return@withContext Result.failure(Exception("Characteristic not found: $uuid"))
            }
            
            try {
                // Enable local notifications
                val success = gatt.setCharacteristicNotification(characteristic, true)
                if (!success) {
                    return@withContext Result.failure(Exception("Failed to enable notifications for $uuid"))
                }
                
                // Write descriptor to enable notifications on remote device
                val descriptor = characteristic.getDescriptor(
                    UUID.fromString("00002902-0000-1000-8000-00805f9b34fb") // Client Characteristic Configuration
                )
                
                if (descriptor != null) {
                    val descriptorValue = BluetoothGattDescriptor.ENABLE_NOTIFICATION_VALUE
                    
                    if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
                        gatt.writeDescriptor(descriptor, descriptorValue)
                    } else {
                        @Suppress("DEPRECATION")
                        descriptor.value = descriptorValue
                        @Suppress("DEPRECATION")
                        gatt.writeDescriptor(descriptor)
                    }
                    
                    Log.d(TAG, "Enabled notifications for $uuid")
                } else {
                    Log.w(TAG, "No CCCD descriptor found for $uuid")
                }
                
                Result.success(Unit)
            } catch (e: SecurityException) {
                Result.failure(Exception("Permission denied for notifications: $uuid"))
            } catch (e: Exception) {
                Result.failure(e)
            }
        }
        
        override suspend fun disableNotifications(uuid: String): Result<Unit> = withContext(Dispatchers.IO) {
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
                    
                    if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
                        gatt.writeDescriptor(descriptor, descriptorValue)
                    } else {
                        @Suppress("DEPRECATION")
                        descriptor.value = descriptorValue
                        @Suppress("DEPRECATION")
                        gatt.writeDescriptor(descriptor)
                    }
                    
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
    }
}
