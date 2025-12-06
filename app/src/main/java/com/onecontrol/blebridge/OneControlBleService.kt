package com.onecontrol.blebridge

import android.app.*
import android.bluetooth.*
import android.bluetooth.le.BluetoothLeScanner
import android.bluetooth.le.ScanCallback
import android.bluetooth.le.ScanFilter
import android.bluetooth.le.ScanResult
import android.bluetooth.le.ScanSettings
import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.content.IntentFilter
import android.os.Binder
import android.os.Build
import android.os.Handler
import android.os.IBinder
import android.os.Looper
import android.util.Log
import androidx.core.app.NotificationCompat
import androidx.localbroadcastmanager.content.LocalBroadcastManager
import org.eclipse.paho.client.mqttv3.*
import java.util.*

/**
 * Foreground service that manages BLE connection to OneControl gateway
 * and publishes data to MQTT (Home Assistant)
 */
class OneControlBleService : Service() {
    
    companion object {
        private const val TAG = "OneControlBleService"
        private const val VERBOSE_LOGGING = false  // flip on only when debugging to reduce log churn
        private const val NOTIFICATION_ID = 1
        private const val CHANNEL_ID = "onecontrol_ble_channel"
        const val ACTION_LOG = "com.onecontrol.blebridge.ACTION_LOG"
        const val EXTRA_LOG_MESSAGE = "log_message"
        const val ACTION_SERVICE_STATE = "com.onecontrol.blebridge.ACTION_SERVICE_STATE"
        const val EXTRA_SERVICE_RUNNING = "service_running"
        const val EXTRA_STATUS_PAIRED = "status_paired"
        const val EXTRA_STATUS_BLE_CONNECTED = "status_ble_connected"
        const val EXTRA_STATUS_AUTHENTICATED = "status_authenticated"
        const val EXTRA_STATUS_DATA_HEALTHY = "status_data_healthy"
        const val EXTRA_STATUS_MQTT_CONNECTED = "status_mqtt_connected"
        
        @Volatile
        var isServiceRunning = false
            private set
        
        // Configuration (should be moved to SharedPreferences or config file)
        private const val GATEWAY_MAC = "24:DC:C3:ED:1E:0A"  // TODO: Make configurable
        private const val GATEWAY_PIN = "090336"  // TODO: Make configurable
        private const val GATEWAY_CYPHER = 0x8100080DL  // TODO: Make configurable
        private const val MQTT_BROKER = "tcp://10.115.19.131:1883"
        private const val MQTT_CLIENT_ID = "onecontrol_ble_bridge"
        private const val MQTT_TOPIC_PREFIX = "onecontrol/ble"
        private const val MQTT_USERNAME = "mqtt"
        private const val MQTT_PASSWORD = "mqtt"
    }
    
    private val handler = Handler(Looper.getMainLooper())
    
    inner class LocalBinder : Binder() {
        fun getService(): OneControlBleService = this@OneControlBleService
        
        /**
         * Start scanning/connecting to gateway
         * Called by MainActivity when user presses "Start BLE Bridge"
         */
        fun startConnection() {
            Log.i(TAG, "🔵 startConnection() called from MainActivity")
            broadcastLog("🔵 startConnection() called")
            if (!isConnected && bluetoothAdapter != null && bluetoothAdapter!!.isEnabled) {
                Log.i(TAG, "✅ Starting connection - not connected, adapter ready")
                handler.post {
                    reconnectToBondedDevice()  // Try bonded device first, then scan
                }
            } else {
                Log.w(TAG, "⚠️ Cannot start connection - isConnected=$isConnected, adapter=${bluetoothAdapter != null}, enabled=${bluetoothAdapter?.isEnabled}")
                broadcastLog("⚠️ Cannot start: connected=$isConnected, adapter=${bluetoothAdapter != null}")
            }
        }
        
        /**
         * Stop scanning and disconnect
         * Called by MainActivity when user presses "Stop BLE Bridge"
         */
        fun stopConnection() {
            handler.post {
                disconnect()
            }
        }
    }
    
    private val binder = LocalBinder()
    
    // BLE components
    private var bluetoothAdapter: BluetoothAdapter? = null
    private var bluetoothLeScanner: BluetoothLeScanner? = null
    private var bluetoothGatt: BluetoothGatt? = null
    private var scanCallback: ScanCallback? = null
    
    // Service/Characteristic references
    private var canService: BluetoothGattService? = null
    private var authService: BluetoothGattService? = null
    private var dataService: BluetoothGattService? = null
    private var unlockChar: BluetoothGattCharacteristic? = null
    private var seedChar: BluetoothGattCharacteristic? = null
    private var keyChar: BluetoothGattCharacteristic? = null
    private var canWriteChar: BluetoothGattCharacteristic? = null  // CAN TX (write to gateway)
    private var canReadChar: BluetoothGattCharacteristic? = null   // CAN RX (read from gateway)
    private var dataWriteChar: BluetoothGattCharacteristic? = null
    private var dataReadChar: BluetoothGattCharacteristic? = null
    
    // State
    private var isConnected = false
    private var isBonded = false
    private var isAuthenticated = false
    private var isUnlocked = false
    private var connectionState = ConnectionState.DISCONNECTED
    private var currentDevice: BluetoothDevice? = null
    private var reconnectAttempts = 0
    private var lastDisconnectTime = 0L
    private val MAX_RECONNECT_ATTEMPTS = 3
    private val RECONNECT_DELAY_MS = 5000L  // 5 seconds between reconnect attempts
    
    // Heartbeat/Keepalive
    private var heartbeatRunnable: Runnable? = null
    private val HEARTBEAT_INTERVAL_MS = 5000L  // Send heartbeat every 5 seconds
    
    // MyRvLink Command tracking
    private var nextCommandId: UShort = 1u  // ClientCommandId starts at 1
    private var deviceTableId: Byte = 0x00  // Will be updated when we get gateway info
    private val DEFAULT_DEVICE_TABLE_ID: Byte = 0x08  // From HCI capture
    private var gatewayInfoReceived = false  // Track if we've received GatewayInformation event
    private var isStarted = false  // MyRvLink Start() equivalent - wait for GatewayInfo before starting
    private var notificationSubscriptionsPending = 0  // Track pending descriptor writes
    private var allNotificationsSubscribed = false  // Flag when all subscriptions complete
    private var lastStatusSnapshot = StatusSnapshot()

    // Debug flag to prevent sending test command multiple times
    private var debugTestLightCommandSent = false
    
    // Device status tracking
    private val deviceStatuses = mutableMapOf<String, DeviceStatus>()  // Key: "deviceTableId:deviceId"

    // Active stream reading
    private val notificationQueue = java.util.concurrent.ConcurrentLinkedQueue<ByteArray>()
    private var streamReadingThread: Thread? = null
    private var shouldStopStreamReading = false
    private var isStreamReadingActive = false  // Flag to prevent multiple starts
    private val cobsByteDecoder = CobsByteDecoder(useCrc = true)
    private val streamReadLock = Object()
    
    // MQTT
    private var mqttClient: MqttClient? = null
    private var mqttConnected = false
    private val haDiscoveryPublished = mutableSetOf<String>()  // Track HA discovery per device
    private var diagDiscoveryPublished = false
    
    enum class ConnectionState {
        DISCONNECTED,
        SCANNING,
        CONNECTING,
        CONNECTED,
        DISCOVERING_SERVICES,
        UNLOCKING,
        AUTHENTICATING,
        READY
    }
    
    override fun onBind(intent: Intent): IBinder = binder
    
    // Broadcast receiver for bonding state changes
    private val bondStateReceiver = object : BroadcastReceiver() {
        override fun onReceive(context: Context, intent: Intent) {
            val action = intent.action
            if (BluetoothDevice.ACTION_BOND_STATE_CHANGED == action) {
                val device = intent.getParcelableExtra<BluetoothDevice>(BluetoothDevice.EXTRA_DEVICE)
                val bondState = intent.getIntExtra(BluetoothDevice.EXTRA_BOND_STATE, BluetoothDevice.ERROR)
                val previousBondState = intent.getIntExtra(BluetoothDevice.EXTRA_PREVIOUS_BOND_STATE, BluetoothDevice.ERROR)
                
                device?.let {
                    if (it.address.equals(GATEWAY_MAC, ignoreCase = true) || it.address == currentDevice?.address) {
                        Log.i(TAG, "🔗 Bond state changed for ${it.address}: $previousBondState -> $bondState")
                        when (bondState) {
                            BluetoothDevice.BOND_BONDED -> {
                                Log.i(TAG, "✅✅✅ Device bonded successfully!")
                                isBonded = true
                                updateNotification("Bonded - Discovering services...")
                                broadcastServiceState()
                                // Proceed with service discovery if connected
                                if (isConnected && bluetoothGatt != null && !services_discovered_) {
                                    handler.postDelayed({
                                        connectionState = ConnectionState.DISCOVERING_SERVICES
                                        bluetoothGatt?.discoverServices()
                                    }, 500)
                                }
                            }
                            BluetoothDevice.BOND_BONDING -> {
                                Log.i(TAG, "⏳ Bonding in progress...")
                                updateNotification("Pairing in progress...")
                            }
                            BluetoothDevice.BOND_NONE -> {
                                if (previousBondState == BluetoothDevice.BOND_BONDING) {
                                    Log.e(TAG, "❌ Bonding failed!")
                                    updateNotification("Pairing failed - try again")
                                } else {
                                    Log.w(TAG, "Bond removed")
                                    isBonded = false
                                    broadcastServiceState()
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    
    private var services_discovered_ = false
    
    override fun onCreate() {
        super.onCreate()
        Log.d(TAG, "Service onCreate")
        createNotificationChannel()
        startForeground(NOTIFICATION_ID, createNotification("Initializing..."))
        isServiceRunning = true
        broadcastServiceState()
        
        // Register bond state receiver
        val filter = IntentFilter(BluetoothDevice.ACTION_BOND_STATE_CHANGED)
        registerReceiver(bondStateReceiver, filter)
        Log.d(TAG, "Registered bond state receiver")
        
        // Initialize Bluetooth
        val bluetoothManager = getSystemService(Context.BLUETOOTH_SERVICE) as BluetoothManager
        bluetoothAdapter = bluetoothManager.adapter
        
        if (bluetoothAdapter == null) {
            Log.e(TAG, "❌ Bluetooth adapter is null - Bluetooth not available!")
            updateNotification("Bluetooth not available")
            return
        }
        
        if (!bluetoothAdapter!!.isEnabled) {
            Log.e(TAG, "❌ Bluetooth is not enabled!")
            updateNotification("Bluetooth disabled")
            return
        }
        
        bluetoothLeScanner = bluetoothAdapter?.bluetoothLeScanner
        
        if (bluetoothLeScanner == null) {
            Log.e(TAG, "❌ BluetoothLeScanner is null - BLE not supported!")
            updateNotification("BLE not supported")
            return
        }
        
        Log.i(TAG, "✅ Bluetooth initialized - adapter: ${bluetoothAdapter!!.name}, enabled: ${bluetoothAdapter!!.isEnabled}")
        
        // Initialize MQTT in background thread to avoid blocking onCreate
        handler.post {
            Thread {
                initializeMqtt()
            }.start()
        }
        
        // Auto-start: Begin scanning/connecting automatically when service is ready
        handler.postDelayed({
            Log.i(TAG, "🚀 Auto-starting BLE connection...")
            broadcastLog("🚀 Auto-starting connection...")
            reconnectToBondedDevice()  // Try bonded device first, then scan
        }, 1000)  // Wait 1 second for everything to initialize
    }
    
    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        Log.d(TAG, "Service onStartCommand")
        // Auto-start connection if not already connected
        if (!isConnected && bluetoothAdapter != null && bluetoothAdapter!!.isEnabled) {
            Log.i(TAG, "🚀 onStartCommand: Auto-starting connection...")
            handler.postDelayed({
                reconnectToBondedDevice()  // Try bonded device first, then scan
            }, 500)
        } else {
            Log.d(TAG, "onStartCommand: Already connected or adapter not ready (connected=$isConnected, adapter=${bluetoothAdapter != null})")
        }
        return START_STICKY  // Restart if killed
    }
    
    override fun onDestroy() {
        Log.d(TAG, "Service onDestroy")
        try {
            unregisterReceiver(bondStateReceiver)
        } catch (e: Exception) {
            Log.d(TAG, "Receiver not registered or already unregistered")
        }
        stopScanning()
        disconnect()
        disconnectMqtt()
        isServiceRunning = false
        broadcastServiceState()
        super.onDestroy()
    }
    
    private fun createNotificationChannel() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val channel = NotificationChannel(
                CHANNEL_ID,
                "OneControl BLE Bridge",
                NotificationManager.IMPORTANCE_LOW
            ).apply {
                description = "OneControl BLE Bridge Service"
            }
            val notificationManager = getSystemService(NotificationManager::class.java)
            notificationManager.createNotificationChannel(channel)
        }
    }
    
    private fun createNotification(text: String): Notification {
        val intent = Intent(this, MainActivity::class.java)
        val pendingIntent = PendingIntent.getActivity(
            this, 0, intent,
            PendingIntent.FLAG_IMMUTABLE or PendingIntent.FLAG_UPDATE_CURRENT
        )
        
        return NotificationCompat.Builder(this, CHANNEL_ID)
            .setContentTitle("OneControl BLE Bridge")
            .setContentText(text)
            .setSmallIcon(android.R.drawable.ic_dialog_info)
            .setContentIntent(pendingIntent)
            .setOngoing(true)
            .build()
    }
    
    private fun updateNotification(text: String) {
        val notificationManager = getSystemService(NotificationManager::class.java)
        notificationManager.notify(NOTIFICATION_ID, createNotification(text))
    }
    
    private fun broadcastLog(message: String) {
        val intent = Intent(ACTION_LOG).apply {
            putExtra(EXTRA_LOG_MESSAGE, message)
        }
        LocalBroadcastManager.getInstance(this).sendBroadcast(intent)
    }
    
    private fun broadcastServiceState() {
        lastStatusSnapshot = StatusSnapshot(
            serviceRunning = isServiceRunning,
            paired = isBonded,
            bleConnected = isConnected,
            authenticated = isAuthenticated && isUnlocked,
            dataHealthy = computeDataHealthy(),
            mqttConnected = mqttConnected
        )

        val intent = Intent(ACTION_SERVICE_STATE).apply {
            putExtra(EXTRA_SERVICE_RUNNING, lastStatusSnapshot.serviceRunning)
            putExtra(EXTRA_STATUS_PAIRED, lastStatusSnapshot.paired)
            putExtra(EXTRA_STATUS_BLE_CONNECTED, lastStatusSnapshot.bleConnected)
            putExtra(EXTRA_STATUS_AUTHENTICATED, lastStatusSnapshot.authenticated)
            putExtra(EXTRA_STATUS_DATA_HEALTHY, lastStatusSnapshot.dataHealthy)
            putExtra(EXTRA_STATUS_MQTT_CONNECTED, lastStatusSnapshot.mqttConnected)
        }
        LocalBroadcastManager.getInstance(this).sendBroadcast(intent)
        publishDiagnosticsState()
    }

    private var lastDataTimestampMs: Long = 0L
    private val DATA_HEALTH_TIMEOUT_MS = 15_000L  // consider healthy if data seen within 15s

    private fun computeDataHealthy(): Boolean {
        val now = System.currentTimeMillis()
        val hasRecentData = lastDataTimestampMs > 0 && (now - lastDataTimestampMs) <= DATA_HEALTH_TIMEOUT_MS
        return isConnected && hasRecentData
    }

    data class StatusSnapshot(
        val serviceRunning: Boolean = false,
        val paired: Boolean = false,
        val bleConnected: Boolean = false,
        val authenticated: Boolean = false,
        val dataHealthy: Boolean = false,
        val mqttConnected: Boolean = false
    )

    fun getStatusSnapshot(): StatusSnapshot = lastStatusSnapshot.copy()

    private fun publishDiagnosticsDiscovery() {
        if (diagDiscoveryPublished || mqttClient == null) return
        diagDiscoveryPublished = true

        publishHaDiagnosticBinarySensor(
            objectId = "service_running",
            name = "Service Running",
            stateTopic = "$MQTT_TOPIC_PREFIX/diag/service_running"
        )
        publishHaDiagnosticBinarySensor(
            objectId = "paired",
            name = "Paired",
            stateTopic = "$MQTT_TOPIC_PREFIX/diag/paired"
        )
        publishHaDiagnosticBinarySensor(
            objectId = "ble_connected",
            name = "BLE Connected",
            stateTopic = "$MQTT_TOPIC_PREFIX/diag/ble_connected"
        )
        publishHaDiagnosticBinarySensor(
            objectId = "data_healthy",
            name = "Data Healthy",
            stateTopic = "$MQTT_TOPIC_PREFIX/diag/data_healthy"
        )
        publishHaDiagnosticBinarySensor(
            objectId = "mqtt_connected",
            name = "MQTT Connected",
            stateTopic = "$MQTT_TOPIC_PREFIX/diag/mqtt_connected"
        )
    }

    private fun publishHaDiagnosticBinarySensor(objectId: String, name: String, stateTopic: String) {
        val payload = """
            {
              "name": "$name",
              "unique_id": "onecontrol_diag_$objectId",
              "state_topic": "$stateTopic",
              "payload_on": "ON",
              "payload_off": "OFF",
              "device": {
                "identifiers": ["onecontrol_ble"],
                "manufacturer": "Lippert",
                "model": "OneControl Gateway",
                "name": "OneControl Gateway"
              }
            }
        """.trimIndent()
        publishMqttRaw("homeassistant/binary_sensor/$objectId/config", payload, retain = true)
    }

    private fun publishDiagnosticsState() {
        if (!mqttConnected) return
        publishMqtt("diag/service_running", if (isServiceRunning) "ON" else "OFF", retain = true)
        publishMqtt("diag/paired", if (isBonded) "ON" else "OFF", retain = true)
        publishMqtt("diag/ble_connected", if (isConnected) "ON" else "OFF", retain = true)
        publishMqtt("diag/data_healthy", if (computeDataHealthy()) "ON" else "OFF", retain = true)
        publishMqtt("diag/mqtt_connected", if (mqttConnected) "ON" else "OFF", retain = true)
    }
    
    // MARK: - BLE Scanning
    
    private fun startScanning() {
        if (isConnected) {
            Log.w(TAG, "Already connected, skipping scan")
            return
        }
        
        if (bluetoothLeScanner == null) {
            Log.e(TAG, "❌ Cannot start scanning: BluetoothLeScanner is null")
            updateNotification("BLE scanner unavailable")
            return
        }
        
        if (bluetoothAdapter == null || !bluetoothAdapter!!.isEnabled) {
            Log.e(TAG, "❌ Cannot start scanning: Bluetooth not enabled")
            updateNotification("Bluetooth disabled")
            return
        }
        
        Log.i(TAG, "🔍 Starting BLE scan for gateway: $GATEWAY_MAC")
        updateNotification("Scanning for gateway...")
        connectionState = ConnectionState.SCANNING
        
        // Don't filter by MAC - scan all devices and check each one
        // This is more reliable as the gateway may not always advertise its MAC
        val settings = ScanSettings.Builder()
            .setScanMode(ScanSettings.SCAN_MODE_LOW_LATENCY)
            .build()
        
        scanCallback = object : ScanCallback() {
            override fun onScanResult(callbackType: Int, result: ScanResult) {
                val deviceAddress = result.device.address
                val deviceName = result.device.name ?: "Unknown"
                val scanRecord = result.scanRecord
                
                Log.d(TAG, "📡 BLE advertisement: $deviceName ($deviceAddress), RSSI: ${result.rssi}")
                
                // Check if this is our gateway by MAC address
                val isTargetMac = deviceAddress.equals(GATEWAY_MAC, ignoreCase = true)
                
                // Check for discovery service UUID in service UUIDs
                val serviceUuids = scanRecord?.serviceUuids ?: emptyList()
                val hasDiscoveryService = serviceUuids.any { uuid ->
                    uuid.toString().lowercase() == Constants.DISCOVERY_SERVICE_UUID.lowercase()
                }
                
                // Check for discovery service in service data
                val serviceData = scanRecord?.serviceData ?: emptyMap()
                val hasDiscoveryServiceData = serviceData.keys.any { uuid ->
                    uuid.toString().lowercase() == Constants.DISCOVERY_SERVICE_UUID.lowercase()
                }
                
                // Log service UUIDs for debugging
                if (serviceUuids.isNotEmpty()) {
                    Log.d(TAG, "  Service UUIDs: ${serviceUuids.joinToString { it.toString() }}")
                }
                
                // Check if this matches our gateway
                if (isTargetMac || hasDiscoveryService || hasDiscoveryServiceData) {
                    Log.i(TAG, "✅✅✅ Gateway found! MAC: $deviceAddress, Name: $deviceName")
                    Log.i(TAG, "  Has discovery service: $hasDiscoveryService")
                    Log.i(TAG, "  Has discovery service data: $hasDiscoveryServiceData")
                    stopScanning()
                    connectToDevice(result.device)
                }
            }
            
            override fun onScanFailed(errorCode: Int) {
                Log.e(TAG, "❌ BLE scan failed: $errorCode")
                updateNotification("Scan failed: $errorCode")
                // Retry after a delay
                handler.postDelayed({ startScanning() }, 2000)
            }
        }
        
        // Start scanning without MAC filter - scan all devices
        try {
            bluetoothLeScanner?.startScan(null, settings, scanCallback)
            Log.i(TAG, "🔍 BLE scan started (scanning all devices)")
        } catch (e: SecurityException) {
            Log.e(TAG, "❌ BLE scan failed - SecurityException: ${e.message}")
            Log.e(TAG, "   This usually means BLUETOOTH_SCAN permission is missing or not granted")
            updateNotification("Scan permission denied")
        } catch (e: Exception) {
            Log.e(TAG, "❌ BLE scan failed - Exception: ${e.message}", e)
            updateNotification("Scan failed: ${e.message}")
        }
    }
    
    private fun stopScanning() {
        scanCallback?.let {
            bluetoothLeScanner?.stopScan(it)
            scanCallback = null
        }
    }
    
    // MARK: - BLE Connection
    
    private fun reconnectToBondedDevice() {
        if (isConnected || bluetoothAdapter == null) {
            return
        }
        
        // Get bonded devices and find our gateway
        val bondedDevices = bluetoothAdapter!!.bondedDevices
        val gatewayDevice = bondedDevices.find { 
            it.address.equals(GATEWAY_MAC, ignoreCase = true) 
        }
        
        if (gatewayDevice != null) {
            Log.i(TAG, "🔗 Found bonded gateway - reconnecting directly...")
            connectToDevice(gatewayDevice)
        } else {
            Log.w(TAG, "Gateway not found in bonded devices - starting scan...")
            startScanning()
        }
    }
    
    private fun connectToDevice(device: BluetoothDevice) {
        if (isConnected) {
            Log.w(TAG, "Already connected")
            return
        }
        
        val deviceName = device.name ?: "Unknown Device"
        Log.i(TAG, "🔗 Connecting to $deviceName (${device.address})...")
        updateNotification("Connecting to $deviceName...")
        connectionState = ConnectionState.CONNECTING
        
        // Connect with autoConnect=false (direct connection)
        bluetoothGatt = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
            device.connectGatt(this, false, gattCallback, BluetoothDevice.TRANSPORT_LE)
        } else {
            device.connectGatt(this, false, gattCallback)
        }
    }
    
    private val gattCallback = object : BluetoothGattCallback() {
        override fun onConnectionStateChange(gatt: BluetoothGatt, status: Int, newState: Int) {
            when (newState) {
                BluetoothProfile.STATE_CONNECTED -> {
                    val deviceName = gatt.device.name ?: gatt.device.address
                    Log.i(TAG, "✅ BLE Connected to $deviceName!")
                    isConnected = true
                    connectionState = ConnectionState.CONNECTED
                    currentDevice = gatt.device
                    services_discovered_ = false
                    reconnectAttempts = 0  // Reset reconnect counter on successful connection
                    lastDisconnectTime = 0
                    broadcastServiceState()
                    
                    // Check if already bonded
                    val bondState = gatt.device.bondState
                    isBonded = (bondState == BluetoothDevice.BOND_BONDED)
                    Log.i(TAG, "Bond state after connection: $bondState (${when(bondState) {
                        BluetoothDevice.BOND_BONDED -> "BONDED"
                        BluetoothDevice.BOND_BONDING -> "BONDING"
                        BluetoothDevice.BOND_NONE -> "NONE"
                        else -> "UNKNOWN"
                    }})")
                    
                    if (!isBonded) {
                        Log.i(TAG, "🔗 Device not bonded - initiating explicit bonding...")
                        updateNotification("Connected - Pairing...")
                        // Explicitly create bond (matches C# CreateBond())
                        val bondResult = gatt.device.createBond()
                        if (bondResult) {
                            Log.i(TAG, "✅ Bonding initiated - waiting for user to accept pairing dialog...")
                            // Don't proceed until bonding completes (handled by receiver)
                        } else {
                            Log.e(TAG, "❌ Failed to initiate bonding!")
                            updateNotification("Pairing failed - retrying...")
                            // Retry after delay
                            handler.postDelayed({
                                if (!isBonded && isConnected) {
                                    gatt.device.createBond()
                                }
                            }, 2000)
                        }
                    } else {
                        Log.i(TAG, "✅ Device already bonded - proceeding with service discovery")
                        updateNotification("Connected - Discovering services...")
                        handler.postDelayed({
                            connectionState = ConnectionState.DISCOVERING_SERVICES
                            gatt.discoverServices()
                        }, 500)
                    }
                }
                BluetoothProfile.STATE_DISCONNECTED -> {
                    stopHeartbeat()  // Stop heartbeat on disconnect
                    // Keepalive reads removed - no longer needed
                    
                    val statusMsg = when (status) {
                        BluetoothGatt.GATT_SUCCESS -> "Normal disconnect"
                        8 -> "Connection timeout"
                        19 -> "Remote device connection failure"
                        22 -> "GATT connection error"
                        133 -> "GATT error (0x85)"
                        else -> "Disconnect status: $status (0x${status.toString(16)})"
                    }
                    Log.w(TAG, "❌ BLE Disconnected - $statusMsg")
                    isConnected = false
                    isAuthenticated = false
                    isUnlocked = false
                    connectionState = ConnectionState.DISCONNECTED
                    broadcastServiceState()
                    val disconnectedDevice = gatt.device
                    val now = System.currentTimeMillis()
                    bluetoothGatt = null
                    updateNotification("Disconnected: $statusMsg")
                    
                    // Only reconnect if:
                    // 1. Service is still running
                    // 2. It's been at least RECONNECT_DELAY_MS since last disconnect
                    // 3. We haven't exceeded max reconnect attempts
                    // 4. It's not a "normal disconnect" that happened right after connection (might be gateway rejecting)
                    if (isServiceRunning && 
                        (now - lastDisconnectTime > RECONNECT_DELAY_MS) &&
                        reconnectAttempts < MAX_RECONNECT_ATTEMPTS) {
                        
                        // If it's a normal disconnect right after being ready, wait longer
                        // This might be the gateway closing the connection intentionally
                        val delay = if (status == BluetoothGatt.GATT_SUCCESS && connectionState == ConnectionState.READY) {
                            Log.i(TAG, "Normal disconnect after ready state - waiting longer before reconnect")
                            RECONNECT_DELAY_MS * 2  // Wait 10 seconds
                        } else {
                            RECONNECT_DELAY_MS
                        }
                        
                        reconnectAttempts++
                        lastDisconnectTime = now
                        Log.i(TAG, "🔄 Scheduling reconnect attempt $reconnectAttempts/$MAX_RECONNECT_ATTEMPTS in ${delay}ms...")
                        handler.postDelayed({
                            reconnectToBondedDevice()
                        }, delay)
                    } else {
                        if (reconnectAttempts >= MAX_RECONNECT_ATTEMPTS) {
                            Log.w(TAG, "Max reconnect attempts reached - stopping reconnection")
                            updateNotification("Disconnected - max reconnect attempts reached")
                        } else {
                            Log.d(TAG, "Not reconnecting: serviceRunning=$isServiceRunning, timeSinceLastDisconnect=${now - lastDisconnectTime}ms")
                        }
                    }
                }
            }
        }
        
        override fun onServicesDiscovered(gatt: BluetoothGatt, status: Int) {
            if (status != BluetoothGatt.GATT_SUCCESS) {
                val errorMsg = when (status) {
                    133 -> "GATT_INTERNAL_ERROR (0x85) - Try reconnecting"
                    8 -> "GATT_CONN_TIMEOUT"
                    19 -> "GATT_CONN_TERMINATE_PEER_USER"
                    22 -> "GATT_CONN_L2C_FAILURE"
                    else -> "Unknown error: $status (0x${status.toString(16)})"
                }
                Log.e(TAG, "❌ Service discovery failed: $errorMsg")
                updateNotification("Service discovery failed: $errorMsg")
                // Try to reconnect after a delay
                handler.postDelayed({
                    if (isServiceRunning && !isConnected) {
                        Log.i(TAG, "Retrying connection after service discovery failure...")
                        reconnectToBondedDevice()
                    }
                }, 3000)
                return
            }
            
            services_discovered_ = true
            val services = gatt.services
            Log.i(TAG, "✅✅✅ Services discovered: ${services.size} services found")
            broadcastLog("✅ Services discovered: ${services.size} services")
            
            // Log all services first
            for (service in services) {
                Log.i(TAG, "  📦 Service UUID: ${service.uuid}")
                broadcastLog("  Service: ${service.uuid}")
            }
            
            // Wait a bit before discovering characteristics to avoid GATT errors
            handler.postDelayed({
                discoverCharacteristics(gatt)
            }, 200)
        }
        
        override fun onCharacteristicRead(
            gatt: BluetoothGatt,
            characteristic: BluetoothGattCharacteristic,
            status: Int
        ) {
            if (status != BluetoothGatt.GATT_SUCCESS) {
                val errorMsg = when (status) {
                    133 -> "GATT_INTERNAL_ERROR (0x85)"
                    5 -> "GATT_INSUFFICIENT_AUTHENTICATION"
                    15 -> "GATT_INSUFFICIENT_ENCRYPTION"
                    else -> "Error: $status (0x${status.toString(16)})"
                }
                Log.e(TAG, "❌ Characteristic read failed: $errorMsg for ${characteristic.uuid}")
                broadcastLog("❌ Read failed: $errorMsg")
                
                // If it's an auth error, try to bond again
                if (status == 5 || status == 15) {
                    if (gatt.device.bondState != BluetoothDevice.BOND_BONDED) {
                        Log.i(TAG, "Insufficient auth - attempting to bond...")
                        gatt.device.createBond()
                    }
                }
                return
            }
            
            handleCharacteristicRead(characteristic)
        }
        
        override fun onCharacteristicWrite(
            gatt: BluetoothGatt,
            characteristic: BluetoothGattCharacteristic,
            status: Int
        ) {
            val uuid = characteristic.uuid.toString().lowercase()
            if (status != BluetoothGatt.GATT_SUCCESS) {
                val errorMsg = when (status) {
                    133 -> "GATT_INTERNAL_ERROR (0x85)"
                    5 -> "GATT_INSUFFICIENT_AUTHENTICATION"
                    15 -> "GATT_INSUFFICIENT_ENCRYPTION"
                    else -> "Error: $status (0x${status.toString(16)})"
                }
                Log.e(TAG, "❌ Characteristic write failed: $errorMsg for $uuid")
                broadcastLog("❌ Write failed: $errorMsg for $uuid")
                
                // If it's an auth error, try to bond again
                if (status == 5 || status == 15) {
                    if (gatt.device.bondState != BluetoothDevice.BOND_BONDED) {
                        Log.i(TAG, "Insufficient auth - attempting to bond...")
                        gatt.device.createBond()
                    }
                }
                return
            }
            
            // Log successful writes
            if (uuid == Constants.DATA_WRITE_CHAR_UUID.lowercase() || uuid == Constants.CAN_WRITE_CHAR_UUID.lowercase()) {
                val data = characteristic.value
                Log.i(TAG, "✅ Write successful to $uuid: ${data?.size ?: 0} bytes")
                broadcastLog("✅ Write OK: ${data?.size ?: 0} bytes")
            }
            handleCharacteristicWrite(characteristic)
        }
        
        // Android 13+ (API 33+) uses this signature
        @Suppress("OVERRIDE_DEPRECATION")
        override fun onCharacteristicChanged(
            gatt: BluetoothGatt,
            characteristic: BluetoothGattCharacteristic,
            value: ByteArray
        ) {
            val uuid = characteristic.uuid.toString().lowercase()
            Log.i(TAG, "📨📨📨 onCharacteristicChanged (Android 13+) CALLED for $uuid: ${value.size} bytes")
            Log.i(TAG, "📨 Notification received from $uuid: ${value.size} bytes")
            if (value.isNotEmpty()) {
                val hex = value.joinToString(" ") { "%02X".format(it) }
                Log.d(TAG, "📨 Data: $hex")
                // Set the value on the characteristic for legacy code compatibility
                characteristic.value = value
                handleCharacteristicNotification(characteristic)
            } else {
                Log.w(TAG, "📨 Empty notification from $uuid")
            }
        }
        
        // Older Android versions use this signature (deprecated in API 33)
        @Deprecated("Deprecated in API 33")
        override fun onCharacteristicChanged(
            gatt: BluetoothGatt,
            characteristic: BluetoothGattCharacteristic
        ) {
            val data = characteristic.value
            val uuid = characteristic.uuid.toString().lowercase()
            Log.i(TAG, "📨📨📨 onCharacteristicChanged (legacy) CALLED for $uuid: ${data?.size ?: 0} bytes")
            Log.i(TAG, "📨 Notification received from $uuid: ${data?.size ?: 0} bytes")
            if (data != null && data.isNotEmpty()) {
                val hex = data.joinToString(" ") { "%02X".format(it) }
                Log.d(TAG, "📨 Data: $hex")
                handleCharacteristicNotification(characteristic)
            } else {
                Log.w(TAG, "📨 Empty notification from $uuid")
            }
        }
        
        override fun onMtuChanged(gatt: BluetoothGatt, mtu: Int, status: Int) {
            if (status == BluetoothGatt.GATT_SUCCESS) {
                Log.i(TAG, "✅ MTU changed to $mtu")
            } else {
                Log.w(TAG, "MTU change failed: $status")
            }
        }
        
        override fun onDescriptorWrite(
            gatt: BluetoothGatt,
            descriptor: BluetoothGattDescriptor,
            status: Int
        ) {
            val charUuid = descriptor.characteristic.uuid.toString().lowercase()
            val descriptorUuid = descriptor.uuid.toString().lowercase()
            if (status == BluetoothGatt.GATT_SUCCESS) {
                Log.i(TAG, "✅ Descriptor write successful for $charUuid (descriptor: $descriptorUuid, pending: $notificationSubscriptionsPending)")
                
                notificationSubscriptionsPending--
                if (notificationSubscriptionsPending <= 0 && !allNotificationsSubscribed) {
                    allNotificationsSubscribed = true
                    Log.i(TAG, "✅ All notification subscriptions complete")
                    broadcastLog("✅ All notifications subscribed")
                    broadcastServiceState()

                    // For CAN Service gateways, we can start stream + initial command + heartbeat
                    // immediately once notifications are enabled.
                    // For Data Service gateways, align with HCI capture / protocol:
                    //  - Start the stream reader as soon as CCCD is enabled
                    //  - Wait for GatewayInformation before sending GetDevices / starting heartbeat
                    val isDataServiceGateway = (canService == null && dataService != null)
                    Log.d(TAG, "🔍 onDescriptorWrite: canService=${canService != null}, dataService=${dataService != null}, isDataServiceGateway=$isDataServiceGateway")
                    if (!isDataServiceGateway) {
                        Log.i(TAG, "✅ CAN Service: Ready to send commands")
                        // Start immediately - no delay needed
                        startActiveStreamReading()
                        sendInitialCanCommand()
                        startHeartbeat()
                    } else {
                        Log.i(TAG, "✅ Data Service: Notifications enabled - starting stream reader and sending initial command")
                        // Start stream reader immediately to catch responses
                        startActiveStreamReading()
                        // CRITICAL: CAN-based gateways need an initial command to "wake up" and start sending data
                        // Even though this is a Data Service gateway, it's a CAN device underneath
                        // Send GetDevices to trigger GatewayInformation and data flow
                        handler.postDelayed({
                            Log.i(TAG, "📤 Sending initial GetDevices to wake up CAN gateway")
                            broadcastLog("📤 Requesting devices from gateway...")
                            sendInitialCanCommand()
                            startHeartbeat()
                        }, 500)  // Small delay to ensure stream reader is ready
                    }
                } else {
                    Log.d(TAG, "  → Still waiting for ${notificationSubscriptionsPending} more descriptor writes...")
                }
            } else {
                val errorMsg = when (status) {
                    133 -> "GATT_INTERNAL_ERROR (0x85)"
                    5 -> "GATT_INSUFFICIENT_AUTHENTICATION"
                    15 -> "GATT_INSUFFICIENT_ENCRYPTION"
                    else -> "Error: $status (0x${status.toString(16)})"
                }
                Log.e(TAG, "❌ Descriptor write failed for $charUuid: $errorMsg (descriptor: $descriptorUuid, pending: $notificationSubscriptionsPending)")
                notificationSubscriptionsPending--
                // If all pending writes are done (even if some failed), proceed
                if (notificationSubscriptionsPending <= 0 && !allNotificationsSubscribed) {
                    allNotificationsSubscribed = true
                    Log.w(TAG, "⚠️ Some descriptor writes failed, but proceeding anyway")
                    broadcastServiceState()
                    
                    // Same policy as success path: CAN Service can send immediately,
                    // Data Service only starts the reader here and waits for GatewayInformation.
                    val isDataServiceGateway = (canService == null && dataService != null)
                    if (!isDataServiceGateway) {
                        sendInitialCanCommand()
                        startHeartbeat()
                    } else {
                        Log.i(TAG, "✅ Data Service: Notifications (partially) enabled - starting stream reader IMMEDIATELY")
                        startActiveStreamReading()
                    }
                }
            }
        }
        
        // onDescriptorRead override removed; we no longer re‑read CCCD for verification
    }
    
    private fun discoverCharacteristics(gatt: BluetoothGatt) {
        Log.i(TAG, "🔍 Discovering ALL characteristics...")
        broadcastLog("🔍 Discovering ALL characteristics...")
        updateNotification("Discovering characteristics...")
        
        // Log ALL services and their characteristics
        val services = gatt.services
        Log.i(TAG, "📋 Found ${services.size} services total")
        broadcastLog("📋 Found ${services.size} services total")
        
        // Track which services we found
        var foundCanService = false
        var foundDataService = false
        
        for (service in services) {
            val serviceUuid = service.uuid.toString().lowercase()
            Log.i(TAG, "  📦 Service: $serviceUuid")
            broadcastLog("  📦 Service: $serviceUuid")
            
            // Check for CAN Service
            if (serviceUuid == Constants.CAN_SERVICE_UUID.lowercase()) {
                foundCanService = true
                Log.i(TAG, "    ✅ THIS IS THE CAN SERVICE!")
                broadcastLog("    ✅ CAN Service found!")
            }
            
            // Check for Data Service
            if (serviceUuid == Constants.DATA_SERVICE_UUID.lowercase()) {
                foundDataService = true
                Log.i(TAG, "    ✅ THIS IS THE DATA SERVICE!")
                broadcastLog("    ✅ Data Service found!")
            }
            
            val characteristics = service.characteristics
            Log.i(TAG, "    └─ ${characteristics.size} characteristics:")
            broadcastLog("    └─ ${characteristics.size} characteristics:")
            
            for (char in characteristics) {
                val charUuid = char.uuid.toString()
                val properties = char.properties
                val props = mutableListOf<String>()
                if (properties and BluetoothGattCharacteristic.PROPERTY_READ != 0) props.add("READ")
                if (properties and BluetoothGattCharacteristic.PROPERTY_WRITE != 0) props.add("WRITE")
                if (properties and BluetoothGattCharacteristic.PROPERTY_WRITE_NO_RESPONSE != 0) props.add("WRITE_NO_RESPONSE")
                if (properties and BluetoothGattCharacteristic.PROPERTY_NOTIFY != 0) props.add("NOTIFY")
                if (properties and BluetoothGattCharacteristic.PROPERTY_INDICATE != 0) props.add("INDICATE")
                
                val propsStr = props.joinToString(", ")
                // Include instanceId to help correlate with HCI handles
                Log.i(TAG, "      • $charUuid [id=${char.instanceId}, $propsStr]")
                broadcastLog("      • $charUuid [$propsStr]")
            }
        }
        
        // Summary of service discovery
        Log.i(TAG, "📋 Service Discovery Summary:")
        Log.i(TAG, "  CAN Service (00000000): ${if (foundCanService) "✅ FOUND" else "❌ NOT FOUND"}")
        Log.i(TAG, "  Data Service (00000030): ${if (foundDataService) "✅ FOUND" else "❌ NOT FOUND"}")
        broadcastLog("📋 CAN: ${if (foundCanService) "✅" else "❌"}, Data: ${if (foundDataService) "✅" else "❌"}")
        
        // Find CAN service (for unlock and CAN read/write)
        canService = gatt.getService(UUID.fromString(Constants.CAN_SERVICE_UUID))
        if (canService != null) {
            Log.i(TAG, "✅ Found CAN service")
            unlockChar = canService!!.getCharacteristic(UUID.fromString(Constants.UNLOCK_CHAR_UUID))
            canWriteChar = canService!!.getCharacteristic(UUID.fromString(Constants.CAN_WRITE_CHAR_UUID))
            canReadChar = canService!!.getCharacteristic(UUID.fromString(Constants.CAN_READ_CHAR_UUID))
            if (unlockChar != null) Log.i(TAG, "✅ Found unlock characteristic")
            if (canWriteChar != null) Log.i(TAG, "✅ Found CAN write characteristic (TX)")
            if (canReadChar != null) Log.i(TAG, "✅ Found CAN read characteristic (RX)")
            
            // Log all CAN service characteristics
            val canChars = canService!!.characteristics
            Log.i(TAG, "📋 CAN service has ${canChars.size} characteristics:")
            for (char in canChars) {
                val props = getCharacteristicProperties(char)
                Log.i(TAG, "  • ${char.uuid} [${props.joinToString(", ")}]")
            }
        }
        
        // Find Auth service (for TEA authentication)
        authService = gatt.getService(UUID.fromString(Constants.AUTH_SERVICE_UUID))
        if (authService != null) {
            Log.i(TAG, "✅ Found auth service")
            seedChar = authService!!.getCharacteristic(UUID.fromString(Constants.SEED_CHAR_UUID))
            keyChar = authService!!.getCharacteristic(UUID.fromString(Constants.KEY_CHAR_UUID))
            if (seedChar != null) Log.i(TAG, "✅ Found seed characteristic")
            if (keyChar != null) Log.i(TAG, "✅ Found key characteristic")
        }
        
        // Find Data service (for CAN-over-BLE)
        dataService = gatt.getService(UUID.fromString(Constants.DATA_SERVICE_UUID))
        if (dataService != null) {
            Log.i(TAG, "✅ Found data service")
            dataWriteChar = dataService!!.getCharacteristic(UUID.fromString(Constants.DATA_WRITE_CHAR_UUID))
            dataReadChar = dataService!!.getCharacteristic(UUID.fromString(Constants.DATA_READ_CHAR_UUID))
            if (dataWriteChar != null) Log.i(TAG, "✅ Found data write characteristic")
            if (dataReadChar != null) Log.i(TAG, "✅ Found data read characteristic")
        }
        
        // Check if this is a Data Service gateway - if so, skip all delays and start immediately
        val isDataServiceGateway = (canService == null && dataService != null)
        
        if (isDataServiceGateway) {
            Log.i(TAG, "🚀 Data Service gateway - starting communication immediately (no MTU/unlock delays)")
            // Request MTU in background, but don't wait for it
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP) {
                Log.d(TAG, "Requesting MTU size ${Constants.BLE_MTU_SIZE}...")
                gatt.requestMtu(Constants.BLE_MTU_SIZE)
            }
            // Skip unlock/auth and go straight to ready state
            handler.postDelayed({
                onReady()
            }, 100)  // Minimal delay just to ensure service discovery is complete
        } else {
            // CAN Service gateway - use normal flow with MTU and unlock
            // Request MTU (matches C# code)
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP) {
                Log.d(TAG, "Requesting MTU size ${Constants.BLE_MTU_SIZE}...")
                gatt.requestMtu(Constants.BLE_MTU_SIZE)
            }
            
            // Wait a bit for MTU negotiation, then unlock
            handler.postDelayed({
                unlockGateway()
            }, Constants.BOND_SETTLE_DELAY_MS)
        }
    }
    
    private fun getCharacteristicProperties(char: BluetoothGattCharacteristic): List<String> {
        val props = mutableListOf<String>()
        val properties = char.properties
        if (properties and BluetoothGattCharacteristic.PROPERTY_READ != 0) props.add("READ")
        if (properties and BluetoothGattCharacteristic.PROPERTY_WRITE != 0) props.add("WRITE")
        if (properties and BluetoothGattCharacteristic.PROPERTY_WRITE_NO_RESPONSE != 0) props.add("WRITE_NO_RESPONSE")
        if (properties and BluetoothGattCharacteristic.PROPERTY_NOTIFY != 0) props.add("NOTIFY")
        if (properties and BluetoothGattCharacteristic.PROPERTY_INDICATE != 0) props.add("INDICATE")
        return props
    }
    
    // MARK: - Unlock Gateway
    
    private fun unlockGateway() {
        // Check if this is a Data Service gateway (no unlock needed)
        val isDataServiceGateway = (canService == null && dataService != null)
        
        if (isDataServiceGateway) {
            Log.i(TAG, "⏭️ Data Service gateway detected - skipping unlock")
            broadcastLog("⏭️ Data Service - no unlock needed")
            isUnlocked = true  // Mark as unlocked since Data Service doesn't need it
            broadcastServiceState()
            handler.postDelayed({ authenticate() }, 500)
            return
        }
        
        if (isUnlocked || unlockChar == null || bluetoothGatt == null) {
            if (unlockChar == null) {
                Log.w(TAG, "Unlock characteristic not found - may already be unlocked")
                // Proceed to authentication anyway
                handler.postDelayed({ authenticate() }, 500)
            }
            return
        }
        
        Log.i(TAG, "Unlocking gateway with PIN...")
        updateNotification("Unlocking gateway...")
        connectionState = ConnectionState.UNLOCKING
        
        // Read unlock status first
        unlockChar!!.value = null
        bluetoothGatt!!.readCharacteristic(unlockChar)
    }
    
    private fun handleUnlockRead(data: ByteArray) {
        if (data.isEmpty()) {
            Log.e(TAG, "Unlock read returned empty data")
            return
        }
        
        val status = data[0].toInt() and 0xFF
        Log.d(TAG, "Unlock status: 0x${status.toString(16)}")
        
        if (status > 0) {
            Log.i(TAG, "✅ Gateway already unlocked")
            isUnlocked = true
            broadcastServiceState()
            connectionState = ConnectionState.CONNECTED
            handler.postDelayed({ authenticate() }, 500)
            return
        }
        
        // Gateway is locked - write PIN
        Log.d(TAG, "Gateway is locked, writing PIN...")
        val pinBytes = GATEWAY_PIN.toByteArray(Charsets.UTF_8)
        unlockChar!!.value = pinBytes
        unlockChar!!.writeType = BluetoothGattCharacteristic.WRITE_TYPE_DEFAULT
        
        // Try writing twice (as per C# code)
        var writeSuccess = bluetoothGatt!!.writeCharacteristic(unlockChar)
        if (!writeSuccess) {
            Log.w(TAG, "First unlock write failed, retrying...")
            handler.postDelayed({
                bluetoothGatt!!.writeCharacteristic(unlockChar)
            }, 500)
        }
    }
    
    private fun handleUnlockWrite() {
        // Wait 1 second then verify unlock
        handler.postDelayed({
            unlockChar!!.value = null
            bluetoothGatt!!.readCharacteristic(unlockChar)
        }, Constants.UNLOCK_VERIFY_DELAY_MS)
    }
    
    private fun verifyUnlock(data: ByteArray) {
        if (data.isEmpty()) {
            Log.e(TAG, "Unlock verification read returned empty data")
            return
        }
        
        val status = data[0].toInt() and 0xFF
        if (status > 0) {
            Log.i(TAG, "✅ Gateway unlocked successfully!")
            isUnlocked = true
            broadcastServiceState()
            connectionState = ConnectionState.CONNECTED
            updateNotification("Gateway unlocked")
            handler.postDelayed({ authenticate() }, 500)
        } else {
            Log.e(TAG, "❌ Gateway unlock failed (status=0x${status.toString(16)})")
            updateNotification("Unlock failed")
        }
    }
    
    // MARK: - Authentication (Data Service Challenge-Response)
    
    /**
     * Calculate authentication KEY from challenge using BleDeviceUnlockManager.Encrypt() algorithm
     * From decompiled code: MyRvLinkBleGatewayScanResult.RvLinkKeySeedCypher = 612643285
     * Byte order: BIG-ENDIAN for both challenge and KEY
     */
    private fun calculateAuthKey(seed: Long): ByteArray {
        val cypher = 612643285L  // MyRvLink RvLinkKeySeedCypher = 0x2483FFD5
        
        var cypherVar = cypher
        var seedVar = seed
        var num = 2654435769L  // TEA delta = 0x9E3779B9
        
        // BleDeviceUnlockManager.Encrypt() algorithm
        for (i in 0 until 32) {
            seedVar += ((cypherVar shl 4) + 1131376761L) xor (cypherVar + num) xor ((cypherVar shr 5) + 1919510376L)
            seedVar = seedVar and 0xFFFFFFFFL
            cypherVar += ((seedVar shl 4) + 1948272964L) xor (seedVar + num) xor ((seedVar shr 5) + 1400073827L)
            cypherVar = cypherVar and 0xFFFFFFFFL
            num += 2654435769L
            num = num and 0xFFFFFFFFL
        }
        
        // Return as big-endian bytes
        val result = seedVar.toInt()
        return byteArrayOf(
            ((result shr 24) and 0xFF).toByte(),
            ((result shr 16) and 0xFF).toByte(),
            ((result shr 8) and 0xFF).toByte(),
            ((result shr 0) and 0xFF).toByte()
        )
    }
    
    // MARK: - Authentication (TEA - Legacy for CAN Service gateways)
    
    private fun authenticate() {
        // Check if this is a Data Service gateway (no TEA auth needed)
        val isDataServiceGateway = (canService == null && dataService != null)
        
        if (isDataServiceGateway) {
            Log.i(TAG, "⏭️ Data Service gateway detected - skipping TEA authentication")
            broadcastLog("⏭️ Data Service - no auth needed")
            onReady()
            return
        }
        
        if (isAuthenticated || seedChar == null || keyChar == null || bluetoothGatt == null) {
            if (seedChar == null || keyChar == null) {
                Log.w(TAG, "Auth characteristics not found - skipping authentication")
                onReady()
            }
            return
        }
        
        Log.i(TAG, "Starting authentication (TEA key/seed exchange)...")
        updateNotification("Authenticating...")
        connectionState = ConnectionState.AUTHENTICATING
        
        // Read seed
        seedChar!!.value = null
        bluetoothGatt!!.readCharacteristic(seedChar)
    }
    
    private fun handleSeedRead(data: ByteArray) {
        if (data.size < 4) {
            Log.e(TAG, "Seed data too short: ${data.size} bytes")
            return
        }
        
        // Extract 32-bit seed (little-endian)
        val seed = (data[3].toLong() and 0xFF shl 24) or
                   (data[2].toLong() and 0xFF shl 16) or
                   (data[1].toLong() and 0xFF shl 8) or
                   (data[0].toLong() and 0xFF)
        
        Log.d(TAG, "Read seed: 0x${seed.toString(16)}")
        
        // Encrypt seed with TEA
        val encrypted = TeaEncryption.encrypt(GATEWAY_CYPHER.toLong(), seed)
        Log.d(TAG, "Encrypted key: 0x${encrypted.toString(16)}")
        
        // Write encrypted key back
        val keyBytes = byteArrayOf(
            (encrypted and 0xFF).toByte(),
            ((encrypted shr 8) and 0xFF).toByte(),
            ((encrypted shr 16) and 0xFF).toByte(),
            ((encrypted shr 24) and 0xFF).toByte()
        )
        
        keyChar!!.value = keyBytes
        keyChar!!.writeType = BluetoothGattCharacteristic.WRITE_TYPE_DEFAULT
        bluetoothGatt!!.writeCharacteristic(keyChar)
    }
    
    private fun handleKeyWrite() {
        Log.i(TAG, "✅ Authentication complete!")
        isAuthenticated = true
        broadcastServiceState()
        onReady()
    }
    
    // MARK: - Ready State
    
    private fun onReady() {
        // Guard against repeated calls (can happen due to multiple async callbacks)
        if (connectionState == ConnectionState.READY) {
            Log.d(TAG, "onReady() called but already in READY state, ignoring")
            return
        }
        
        // Check if this is a Data Service gateway (no TEA auth needed)
        val isDataServiceGateway = (canService == null && dataService != null)
        
        if (isDataServiceGateway) {
            Log.i(TAG, "✅ Gateway ready! Connected (Data Service - no auth needed)")
            connectionState = ConnectionState.READY
            reconnectAttempts = 0
            updateNotification("Connected & Ready (Data Service)")
            isAuthenticated = true  // Mark as authenticated since Data Service doesn't need TEA
            broadcastServiceState()
            
            // Reset subscription tracking
            notificationSubscriptionsPending = 0
            allNotificationsSubscribed = false
            
            // AUTHENTICATION: The Auth Service has KEY/SEED characteristics for a reason!
            // Even though DirectConnectionMyRvLinkBle doesn't explicitly show KEY writes,
            // the HCI capture shows them, and the Auth Service exists on our gateway.
            // 
            // Since this is a CAN device, let's try authentication:
            //   1. Read unlock status (challenge)
            //   2. Write KEY (response) 
            //   3. Read unlock status again (should be "Unlocked")
            //   4. Enable notifications (including Auth Service for CAN events)
            //   5. Send GetDevices command
            
            Log.i(TAG, "🔑 Starting authentication sequence for CAN gateway...")
            broadcastLog("🔑 Authenticating...")
            
            authService?.let { service ->
                // Step 1: Read unlock status to get challenge
                val unlockChar = service.getCharacteristic(UUID.fromString(Constants.UNLOCK_STATUS_CHAR_UUID))
                if (unlockChar != null && bluetoothGatt != null) {
                    Log.i(TAG, "🔑 Step 1: Reading unlock status (challenge)...")
                    bluetoothGatt!!.readCharacteristic(unlockChar)
                    
                    // Step 2 & 3 happen in handleCharacteristicRead when response arrives
                    // Step 4 will happen in enableDataNotifications after auth completes
                } else {
                    Log.w(TAG, "⚠️ Unlock characteristic not found, enabling notifications anyway")
                    handler.postDelayed({
                        enableDataNotifications()
                    }, 200)
                }
            } ?: run {
                Log.w(TAG, "⚠️ Auth service not found, enabling notifications anyway")
                handler.postDelayed({
                    enableDataNotifications()
                }, 200)
            }
        } else {
            Log.i(TAG, "✅ Gateway ready! Connected, unlocked, and authenticated")
            connectionState = ConnectionState.READY
            reconnectAttempts = 0
            updateNotification("Connected & Ready")
            
            // Reset subscription tracking
            notificationSubscriptionsPending = 0
            allNotificationsSubscribed = false
            
            // CAN Service gateways enable notifications immediately
            enableDataNotifications()
        }
    }
    
    private fun enableDataNotifications() {
        // Compute gateway type
        val isDataServiceGateway = (canService == null && dataService != null)
        
        // Subscribe to CAN/data notifications if available
        // Official app subscribes to CAN READ (00000002) in CAN Service
        // Since this gateway uses Data Service, subscribe to Data Read (00000034)
        val readChar = canReadChar ?: dataReadChar
        readChar?.let { char ->
            try {
                val charName = if (char == canReadChar) "CAN read" else "Data read"
                val props = char.properties
                val hasNotify = (props and BluetoothGattCharacteristic.PROPERTY_NOTIFY) != 0
                val hasIndicate = (props and BluetoothGattCharacteristic.PROPERTY_INDICATE) != 0
                Log.i(TAG, "📝 Enabling notifications for $charName (${char.uuid})")
                Log.i(TAG, "📝 Characteristic properties: 0x${props.toString(16)} (NOTIFY=$hasNotify, INDICATE=$hasIndicate)")
                Log.i(TAG, "📝 Characteristic instanceId: ${char.instanceId}, service: ${char.service.uuid}")
                val notifyResult = bluetoothGatt?.setCharacteristicNotification(char, true)
                Log.i(TAG, "📝 setCharacteristicNotification result: $notifyResult")
                Log.i(TAG, "📝 bluetoothGatt instance: ${bluetoothGatt?.device?.address}, connected: ${bluetoothGatt?.device?.address != null}")
                
                // Increment pending count BEFORE posting the delayed handler to avoid race condition
                notificationSubscriptionsPending++
                Log.i(TAG, "📝 Queued $charName notification subscription (pending: $notificationSubscriptionsPending)")
                
                // Small delay before writing descriptor (BLE stack needs time to process setCharacteristicNotification)
                handler.postDelayed({
                    val descriptor = char.getDescriptor(
                        UUID.fromString("00002902-0000-1000-8000-00805f9b34fb")
                    )
                    if (descriptor != null) {
                        descriptor.value = BluetoothGattDescriptor.ENABLE_NOTIFICATION_VALUE
                        if (bluetoothGatt?.writeDescriptor(descriptor) == true) {
                            Log.i(TAG, "✅ Subscribing to $charName notifications")
                            broadcastLog("📝 Subscribing to $charName...")
                        } else {
                            Log.e(TAG, "❌ Failed to write descriptor for $charName - writeDescriptor returned false, retrying...")
                            // Retry once after another delay
                            handler.postDelayed({
                                if (bluetoothGatt?.writeDescriptor(descriptor) == true) {
                                    Log.i(TAG, "✅ Retry successful: Subscribing to $charName notifications")
                                } else {
                                    Log.e(TAG, "❌ Retry also failed for $charName descriptor write")
                                    // Decrement since we're giving up
                                    notificationSubscriptionsPending--
                                }
                            }, 200)
                        }
                    } else {
                        Log.e(TAG, "❌ Descriptor not found for $charName")
                    }
                }, 100)  // 100ms delay after setCharacteristicNotification
            } catch (e: Exception) {
                Log.e(TAG, "Failed to subscribe to notifications: ${e.message}", e)
                broadcastLog("❌ Failed to subscribe: ${e.message}")
            }
        }
        
        // For Data Service gateways, we now avoid any active characteristic reads
        // (including periodic "keepalive" reads of the version characteristic),
        // because the HCI capture shows the official app relies solely on
        // notifications to feed the COBS stream. The active stream reader is
        // started from onDescriptorWrite once CCCD writes complete, and the
        // heartbeat is started from onGatewayInfoReceived() after
        // GatewayInformation arrives.
        
        // CRITICAL: For CAN-based gateways (even those with Data Service), we need to subscribe
        // to Auth Service notification characteristics (00000011, 00000014) in addition to
        // the Data Service read characteristic (00000034).
        // 
        // User confirmed their gateway is a CAN device that talks to OneControl via CAN,
        // even though it exposes Data Service over BLE.
        authService?.let { service ->
            // Subscribe to 00000011 (SEED - READ, NOTIFY)
            val char11 = service.getCharacteristic(UUID.fromString("00000011-0200-a58e-e411-afe28044e62c"))
            char11?.let {
                try {
                    bluetoothGatt?.setCharacteristicNotification(it, true)
                    val descriptor = it.getDescriptor(UUID.fromString("00002902-0000-1000-8000-00805f9b34fb"))
                    descriptor?.value = BluetoothGattDescriptor.ENABLE_NOTIFICATION_VALUE
                    if (bluetoothGatt?.writeDescriptor(descriptor) == true) {
                        notificationSubscriptionsPending++
                        Log.i(TAG, "📝 Subscribing to Auth Service 00000011/SEED (pending: $notificationSubscriptionsPending)")
                    } else {
                        Log.w(TAG, "Failed to write descriptor for 00000011")
                    }
                } catch (e: Exception) {
                    Log.w(TAG, "Failed to subscribe to 00000011: ${e.message}")
                }
            }
            
            // Subscribe to 00000014 (READ, NOTIFY)
            handler.postDelayed({
                val char14 = service.getCharacteristic(UUID.fromString("00000014-0200-a58e-e411-afe28044e62c"))
                char14?.let {
                    try {
                        bluetoothGatt?.setCharacteristicNotification(it, true)
                        val descriptor = it.getDescriptor(UUID.fromString("00002902-0000-1000-8000-00805f9b34fb"))
                        descriptor?.value = BluetoothGattDescriptor.ENABLE_NOTIFICATION_VALUE
                        if (bluetoothGatt?.writeDescriptor(descriptor) == true) {
                            notificationSubscriptionsPending++
                            Log.i(TAG, "📝 Subscribing to Auth Service 00000014 (pending: $notificationSubscriptionsPending)")
                        } else {
                            Log.w(TAG, "Failed to write descriptor for 00000014")
                        }
                    } catch (e: Exception) {
                        Log.w(TAG, "Failed to subscribe to 00000014: ${e.message}")
                    }
                }
            }, 150)  // Small delay between subscription requests
        }
        
        // If no subscriptions were initiated, mark as complete and send commands / start stream.
        // (This happens if descriptor writes complete before we check, or if writes failed)
        if (notificationSubscriptionsPending == 0) {
            allNotificationsSubscribed = true
            Log.w(TAG, "⚠️ No notification subscriptions pending - starting communication")
            if (!isDataServiceGateway) {
                // For CAN Service, still wait a bit before sending
                handler.postDelayed({
                    sendInitialCanCommand()
                    startHeartbeat()
                    startActiveStreamReading()
                }, 500)
            } else {
                // For Data Service, align with HCI / working behavior:
                //  - Start stream reader once notifications are (or should be) enabled
                //  - Wait for GatewayInformation before sending GetDevices / heartbeat
                handler.postDelayed({
                    startActiveStreamReading()
                }, 500)
            }
        }
        
        // Read unknown characteristics to understand their purpose (only for CAN Service)
        if (!isDataServiceGateway) {
            handler.postDelayed({
                readUnknownCharacteristics()
            }, 1000)
        }
        
        // Publish ready state to MQTT
        publishMqtt("status", "ready")
        
        // Note: Connection should stay alive as long as notifications are active
        // If it disconnects, we'll reconnect automatically (see disconnect handler)
    }
    
    /**
     * Encode MyRvLink GetDevices command
     * Format: [ClientCommandId (2 bytes, little-endian)][CommandType=0x01][DeviceTableId][StartDeviceId][MaxDeviceRequestCount]
     */
    private fun encodeGetDevicesCommand(commandId: UShort, deviceTableId: Byte): ByteArray {
        return byteArrayOf(
            (commandId.toInt() and 0xFF).toByte(),           // ClientCommandId low byte
            ((commandId.toInt() shr 8) and 0xFF).toByte(),   // ClientCommandId high byte
            0x01.toByte(),                                   // CommandType: GetDevices
            deviceTableId,                                    // DeviceTableId
            0x00.toByte(),                                    // StartDeviceId (0 = start from beginning)
            0xFF.toByte()                                     // MaxDeviceRequestCount (255 = get all)
        )
    }
    
    /**
     * Get next command ID (increments and wraps around)
     */
    private fun getNextCommandId(): UShort {
        val id = nextCommandId
        nextCommandId = if (nextCommandId >= 0xFFFEu) 1u else (nextCommandId + 1u).toUShort()
        return id
    }
    
    /**
     * Send initial MyRvLink GetDevices command to "wake up" the gateway
     * This is what the official app sends to establish communication
     */
    private fun sendInitialCanCommand() {
        if (!isConnected || !isAuthenticated || bluetoothGatt == null) {
            Log.w(TAG, "Cannot send CAN command - not ready")
            return
        }
        
        // Try to send a simple CAN message to trigger data flow
        // Based on C# code, CAN messages are sent via CAN_WRITE (00000001) or DATA_WRITE (00000033)
        val writeChar = canWriteChar ?: dataWriteChar
        if (writeChar == null) {
            Log.w(TAG, "No CAN write characteristic available")
            return
        }
        
        try {
            // Send MyRvLink GetDevices command
            // Note: DeviceTableId might be 0 initially - we'll update it when we receive GatewayInformation event
            val commandId = getNextCommandId()
            val effectiveTableId = if (deviceTableId == 0x00.toByte()) DEFAULT_DEVICE_TABLE_ID else deviceTableId
            val command = encodeGetDevicesCommand(commandId, effectiveTableId)
            val commandHex = command.joinToString(" ") { "%02X".format(it) }
            if (deviceTableId == 0x00.toByte()) {
                Log.w(TAG, "📤 Initial GetDevices: deviceTableId unknown, using default 0x${DEFAULT_DEVICE_TABLE_ID.toString(16)}")
            }
            Log.d(TAG, "📤 Initial GetDevices: CommandId=0x${commandId.toString(16)}, DeviceTableId=0x${effectiveTableId.toString(16)}, raw=$commandHex")
            
            val encoded = CobsDecoder.encode(command, prependStartFrame = true, useCrc = true)
            val encodedHex = encoded.joinToString(" ") { "%02X".format(it) }
            Log.d(TAG, "📤 Initial GetDevices encoded: $encodedHex")
            
            writeChar.value = encoded
            writeChar.writeType = BluetoothGattCharacteristic.WRITE_TYPE_NO_RESPONSE
            val writeResult = bluetoothGatt?.writeCharacteristic(writeChar)
            
            if (writeResult == false) {
                Log.e(TAG, "❌ writeCharacteristic returned FALSE - command may not have been sent!")
                broadcastLog("❌ Write failed - command not sent")
            } else {
                Log.i(TAG, "📤 Sent initial GetDevices command (${encoded.size} bytes encoded, writeResult=$writeResult)")
                broadcastLog("📤 Sent initial GetDevices")
            }
        } catch (e: Exception) {
            Log.e(TAG, "Failed to send initial CAN command: ${e.message}", e)
            broadcastLog("❌ Failed to send CAN command: ${e.message}")
        }
    }
    
    /**
     * Start heartbeat/keepalive mechanism
     * Sends MyRvLink GetDevices command periodically to keep connection alive
     * This mimics what the official app does - sends real commands to maintain active communication
     */
    private fun startHeartbeat() {
        stopHeartbeat()  // Stop any existing heartbeat
        
        heartbeatRunnable = object : Runnable {
            override fun run() {
                val connected = isConnected
                val authenticated = isAuthenticated
                val gatt = bluetoothGatt
                
                Log.i(TAG, "💓 Heartbeat tick: connected=$connected, authenticated=$authenticated, gatt=${gatt != null}")
                
                if (connected && authenticated && gatt != null) {
                    // Send MyRvLink GetDevices command as keepalive
                    // This is what the official app does - sends real commands to maintain connection
                    val writeChar = canWriteChar ?: dataWriteChar
                    if (writeChar != null) {
                        try {
                            val commandId = getNextCommandId()
                            val effectiveTableId = if (deviceTableId == 0x00.toByte()) DEFAULT_DEVICE_TABLE_ID else deviceTableId
                            val command = encodeGetDevicesCommand(commandId, effectiveTableId)
                            
                            val encoded = CobsDecoder.encode(command, prependStartFrame = true, useCrc = true)
                            writeChar.value = encoded
                            writeChar.writeType = BluetoothGattCharacteristic.WRITE_TYPE_NO_RESPONSE
                            val writeResult = gatt.writeCharacteristic(writeChar)
                            Log.i(TAG, "💓 Heartbeat: Sent GetDevices (CommandId=0x${commandId.toString(16)}, ${encoded.size} bytes encoded, writeResult=$writeResult)")
                            broadcastLog("💓 Heartbeat: GetDevices sent")
                        } catch (e: Exception) {
                            Log.w(TAG, "Heartbeat CAN write failed: ${e.message}", e)
                            // Fallback to read if write fails
                            authService?.let { service ->
                                val char14 = service.getCharacteristic(UUID.fromString("00000014-0200-a58e-e411-afe28044e62c"))
                                char14?.let {
                                    try {
                                        it.value = null
                                        gatt.readCharacteristic(it)
                                        Log.i(TAG, "💓 Heartbeat: Read characteristic 00000014 (fallback)")
                                    } catch (e2: Exception) {
                                        Log.w(TAG, "Heartbeat read failed: ${e2.message}")
                                    }
                                }
                            }
                        }
                    } else {
                        Log.w(TAG, "💓 Heartbeat: No write characteristic available")
                        // Fallback to read if no write characteristic
                        authService?.let { service ->
                            val char14 = service.getCharacteristic(UUID.fromString("00000014-0200-a58e-e411-afe28044e62c"))
                            char14?.let {
                                try {
                                    it.value = null
                                    gatt.readCharacteristic(it)
                                    Log.i(TAG, "💓 Heartbeat: Read characteristic 00000014")
                                } catch (e: Exception) {
                                    Log.w(TAG, "Heartbeat read failed: ${e.message}")
                                }
                            }
                        }
                    }
                    
                    // Schedule next heartbeat
                    handler.postDelayed(this, HEARTBEAT_INTERVAL_MS)
                } else {
                    Log.w(TAG, "💓 Heartbeat: Skipping - not ready (connected=$connected, authenticated=$authenticated, gatt=${gatt != null})")
                }
            }
        }
        
        // Start first heartbeat after a short delay
        handler.postDelayed(heartbeatRunnable!!, HEARTBEAT_INTERVAL_MS)
        Log.i(TAG, "💓 Heartbeat started (every ${HEARTBEAT_INTERVAL_MS}ms)")
        broadcastLog("💓 Heartbeat started")
    }
    
    private fun stopHeartbeat() {
        heartbeatRunnable?.let {
            handler.removeCallbacks(it)
            heartbeatRunnable = null
            Log.d(TAG, "💓 Heartbeat stopped")
        }
    }
    
    // MARK: - Keepalive Reads (REMOVED - was causing disconnect loops)
    // The official app does NOT perform periodic characteristic reads for keepalive.
    // It relies solely on notifications and periodic GetDevices commands (heartbeat).
    
    // MARK: - Active Stream Reading (like official app's ReadAsync loop)
    
    /**
     * Start active stream reading loop
     * Based on DirectConnectionMyRvLinkBle.BackgroundOperationAsync()
     * Continuously reads from notification queue and decodes byte-by-byte
     */
    private fun startActiveStreamReading() {
        // Don't start if already running
        if (isStreamReadingActive) {
            Log.d(TAG, "🔄 Active stream reading already active, skipping start")
            return
        }
        
        stopActiveStreamReading()  // Stop any existing thread
        
        isStreamReadingActive = true
        shouldStopStreamReading = false
        streamReadingThread = Thread {
            val readBuffer = ByteArray(255)  // Same size as official app
            Log.i(TAG, "🔄 Active stream reading started")
            broadcastLog("🔄 Active stream reading started")
            
            Log.i(TAG, "🔄 Thread starting - shouldStop=$shouldStopStreamReading, isConnected=$isConnected")
            
            while (!shouldStopStreamReading && isConnected) {
                try {
                    // Wait for data with 8-second timeout (like official app)
                    var hasData = false
                    Log.i(TAG, "🔄 Stream reading loop: waiting for data (queue size: ${notificationQueue.size})")
                    synchronized(streamReadLock) {
                        if (notificationQueue.isEmpty()) {
                            Log.i(TAG, "⏳ Queue empty, waiting up to 8 seconds for notifications...")
                            streamReadLock.wait(8000)  // 8-second timeout
                            Log.i(TAG, "⏰ Wait completed (queue size now: ${notificationQueue.size})")
                        }
                        hasData = notificationQueue.isNotEmpty()
                    }
                    Log.i(TAG, "🔄 After wait: hasData=$hasData, isConnected=$isConnected, shouldStop=$shouldStopStreamReading")
                    
                    if (!hasData) {
                        // Timeout and no queued notifications.
                        // IMPORTANT: Do NOT perform active reads here.
                        // The official app only uses notifications to feed the COBS stream.
                        // Our previous active-read fallback produced 244 bytes of zeros and
                        // corrupted the COBS decoder. Instead, just loop and keep waiting.
                        if (!isConnected || shouldStopStreamReading) {
                            continue
                        }
                        // Small backoff to avoid a tight loop while idle
                        Thread.sleep(250)
                        continue
                    }
                    
                    // Process all queued notification packets
                    while (notificationQueue.size > 0 && !shouldStopStreamReading) {
                        val notificationData: ByteArray = notificationQueue.poll() ?: continue
                        
                        Log.i(TAG, "📥 Processing queued notification: ${notificationData.size} bytes")
                        try {
                            // Log first 20 bytes to inspect format
                            val preview = notificationData.slice(0 until minOf(20, notificationData.size)).joinToString(" ") { "%02X".format(it) }
                            Log.i(TAG, "📥 First 20 bytes: $preview")
                            val hasFrameDelimiter = notificationData.indexOf(0x00.toByte()) >= 0
                            Log.i(TAG, "📥 Contains frame delimiter (0x00): $hasFrameDelimiter")
                        } catch (e: Exception) {
                            Log.e(TAG, "Error logging notification preview: ${e.message}", e)
                        }
                        
                        // Feed bytes one at a time to COBS decoder (like official app)
                        var bytesProcessed = 0
                        for (byte in notificationData) {
                            bytesProcessed++
                            val decodedFrame = cobsByteDecoder.decodeByte(byte)
                            if (decodedFrame != null) {
                                // Complete frame received - process it
                                Log.i(TAG, "✅ Decoded COBS frame: ${decodedFrame.size} bytes (after processing $bytesProcessed bytes) - ${decodedFrame.joinToString(" ") { "%02X".format(it) }}")
                                broadcastLog("✅ Decoded frame: ${decodedFrame.size} bytes")
                                processDecodedFrame(decodedFrame)
                            }
                        }
                        if (bytesProcessed > 0 && notificationData.size > 0) {
                            Log.i(TAG, "📥 Processed all $bytesProcessed bytes, but no complete COBS frame found")
                        }
                    }
                } catch (e: InterruptedException) {
                    Log.d(TAG, "Stream reading thread interrupted")
                    break
                } catch (e: Exception) {
                    Log.e(TAG, "Error in stream reading loop: ${e.message}", e)
                    // Continue loop
                }
            }
            
            isStreamReadingActive = false
            Log.i(TAG, "🔄 Active stream reading stopped")
            broadcastLog("🔄 Active stream reading stopped")
        }.apply {
            name = "OneControlStreamReader"
            isDaemon = true
            start()
        }
        broadcastServiceState()
    }
    
    private fun stopActiveStreamReading() {
        isStreamReadingActive = false
        shouldStopStreamReading = true
        synchronized(streamReadLock) {
            streamReadLock.notify()  // Wake up thread to exit
        }
        streamReadingThread?.interrupt()
        streamReadingThread?.join(1000)
        streamReadingThread = null
        notificationQueue.clear()
        cobsByteDecoder.reset()
        broadcastServiceState()
    }
    
    /**
     * Process a decoded COBS frame
     * Based on DirectConnectionMyRvLinkBle.BackgroundOperationAsync() line 284
     */
    private fun processDecodedFrame(decodedFrame: ByteArray) {
        if (decodedFrame.isEmpty()) {
            return
        }
        
        Log.d(TAG, "📦 Processing decoded frame: ${decodedFrame.size} bytes - ${decodedFrame.joinToString(" ") { "%02X".format(it) }}")
        
        // Check if this is a Data Service gateway (no V2MessageType layer)
        val isDataServiceGateway = (canService == null && dataService != null)
        
        if (isDataServiceGateway) {
            // Data Service: COBS-decoded data is MyRvLink events directly
            // No V2MessageType parsing needed - pass directly to MyRvLinkEventFactory
            Log.d(TAG, "📦 Data Service: Processing as MyRvLink event directly (no V2MessageType)")
            processMyRvLinkEvent(decodedFrame)
        } else {
            // CAN Service: COBS-decoded data contains V2MessageType messages
            // Check if this is a V2MessageType message
            val firstByte = decodedFrame[0].toInt() and 0xFF
            if (firstByte in 1..3) {
                // This is a V2MessageType message - parse it
                val canMessages = CanMessageParser.parseV2Message(decodedFrame)
                Log.d(TAG, "📦 Parsed V2MessageType (0x${firstByte.toString(16)}): ${canMessages.size} CAN messages")
                
                // Convert each CAN message to MyRvLink event
                for (canMessage in canMessages) {
                    val eventData = CanMessageParser.extractMyRvLinkEventData(canMessage)
                    if (eventData != null) {
                        processMyRvLinkEvent(eventData)
                    }
                }
            } else {
                // Not a V2MessageType - might be MyRvLink event directly or command response
                processMyRvLinkEvent(decodedFrame)
            }
        }
    }
    
    /**
     * Read unknown characteristics to understand their purpose
     */
    private fun readUnknownCharacteristics() {
        if (bluetoothGatt == null) return
        
        Log.i(TAG, "🔍 Reading unknown characteristics...")
        broadcastLog("🔍 Reading unknown characteristics...")
        
        // Read from Auth Service
        authService?.let { service ->
            // 00000011 - READ, NOTIFY
            val char11 = service.getCharacteristic(UUID.fromString("00000011-0200-a58e-e411-afe28044e62c"))
            char11?.let {
                Log.i(TAG, "Reading characteristic 00000011...")
                broadcastLog("Reading 00000011...")
                it.value = null
                bluetoothGatt!!.readCharacteristic(it)
            }
            
            // 00000014 - READ, NOTIFY
            handler.postDelayed({
                val char14 = service.getCharacteristic(UUID.fromString("00000014-0200-a58e-e411-afe28044e62c"))
                char14?.let {
                    Log.i(TAG, "Reading characteristic 00000014...")
                    broadcastLog("Reading 00000014...")
                    it.value = null
                    bluetoothGatt!!.readCharacteristic(it)
                }
            }, 500)
        }
        
        // Read from Data Service
        dataService?.let { service ->
            // 00000031 - READ
            handler.postDelayed({
                val char31 = service.getCharacteristic(UUID.fromString("00000031-0200-a58e-e411-afe28044e62c"))
                char31?.let {
                    Log.i(TAG, "Reading characteristic 00000031...")
                    broadcastLog("Reading 00000031...")
                    it.value = null
                    bluetoothGatt!!.readCharacteristic(it)
                }
            }, 1000)
        }
        
        // Read Device Info characteristics
        handler.postDelayed({
            val deviceInfoService = bluetoothGatt!!.getService(UUID.fromString("0000180a-0000-1000-8000-00805f9b34fb"))
            deviceInfoService?.let { service ->
                // Firmware Revision
                val fwRev = service.getCharacteristic(UUID.fromString("00002a26-0000-1000-8000-00805f9b34fb"))
                fwRev?.let {
                    Log.i(TAG, "Reading firmware revision...")
                    broadcastLog("Reading firmware revision...")
                    it.value = null
                    bluetoothGatt!!.readCharacteristic(it)
                }
                
                // Manufacturer Name
                handler.postDelayed({
                    val mfgName = service.getCharacteristic(UUID.fromString("00002a29-0000-1000-8000-00805f9b34fb"))
                    mfgName?.let {
                        Log.i(TAG, "Reading manufacturer name...")
                        broadcastLog("Reading manufacturer name...")
                        it.value = null
                        bluetoothGatt!!.readCharacteristic(it)
                    }
                }, 500)
            }
        }, 1500)
    }
    
    // MARK: - Characteristic Handlers
    
    private fun handleCharacteristicRead(characteristic: BluetoothGattCharacteristic) {
        val uuid = characteristic.uuid.toString().lowercase()
        val data = characteristic.value ?: return
        
        when {
            uuid == Constants.UNLOCK_CHAR_UUID.lowercase() -> {
                if (connectionState == ConnectionState.UNLOCKING) {
                    if (!isUnlocked) {
                        handleUnlockRead(data)
                    } else {
                        verifyUnlock(data)
                    }
                }
            }
            uuid == Constants.SEED_CHAR_UUID.lowercase() -> {
                // 00000011 - SEED characteristic for TEA encryption
                Log.i(TAG, "📖 SEED characteristic (00000011) read")
                handleSeedRead(data)
            }
            uuid == "00000012-0200-a58e-e411-afe28044e62c" -> {
                // Unlock status characteristic - used for challenge-response authentication
                // HCI capture shows: 
                //   - First read returns 4-byte challenge (e.g., b0:0a:12:6e)
                //   - App writes KEY value (calculated from challenge)
                //   - Second read returns "Unlocked"
                val unlockStatus = try {
                    String(data, Charsets.UTF_8)
                } catch (e: Exception) {
                    data.joinToString(" ") { "%02X".format(it) }
                }
                Log.i(TAG, "📖 Unlock status (00000012): $unlockStatus (${data.size} bytes)")
                broadcastLog("📖 Unlock status: $unlockStatus")
                
                if (unlockStatus.contains("Unlocked", ignoreCase = true)) {
                    // Auth successful!
                    Log.i(TAG, "✅ Gateway confirms UNLOCKED - authentication complete!")
                    broadcastLog("✅ Gateway unlocked!")
                    
                    // Now enable notifications and start communication
                    handler.postDelayed({
                        enableDataNotifications()
                    }, 200)
                } else if (data.size == 4) {
                    // This is the challenge! Calculate and write KEY response
                    Log.i(TAG, "🔑 Step 2: Received challenge, calculating KEY response...")
                    val challenge = data.joinToString(" ") { "%02X".format(it) }
                    broadcastLog("🔑 Challenge: $challenge")
                    
                    // Calculate KEY using BleDeviceUnlockManager.Encrypt() algorithm
                    // Cypher constant for MyRvLink gateways: 612643285 (0x2483FFD5)
                    // Byte order: BIG-ENDIAN for both seed and key
                    val seedBigEndian = ((data[0].toInt() and 0xFF) shl 24) or
                                       ((data[1].toInt() and 0xFF) shl 16) or
                                       ((data[2].toInt() and 0xFF) shl 8) or
                                       ((data[3].toInt() and 0xFF) shl 0)
                    val keyValue = calculateAuthKey(seedBigEndian.toLong() and 0xFFFFFFFFL)
                    
                    authService?.let { service ->
                        keyChar?.let { key ->
                            key.value = keyValue
                            key.writeType = BluetoothGattCharacteristic.WRITE_TYPE_NO_RESPONSE
                            val writeResult = bluetoothGatt?.writeCharacteristic(key)
                            val keyHex = keyValue.joinToString(" ") { "%02X".format(it) }
                            Log.i(TAG, "🔑 KEY write: $writeResult, calculated value: $keyHex")
                            broadcastLog("🔑 Sent KEY response")
                            
                            // Step 3: Read unlock status again to verify
                            handler.postDelayed({
                                val unlockChar = service.getCharacteristic(UUID.fromString(Constants.UNLOCK_STATUS_CHAR_UUID))
                                if (unlockChar != null && bluetoothGatt != null) {
                                    Log.i(TAG, "🔑 Step 3: Reading unlock status to verify...")
                                    bluetoothGatt!!.readCharacteristic(unlockChar)
                                }
                            }, 500)
                        }
                    }
                } else {
                    Log.w(TAG, "⚠️ Gateway not unlocked, unexpected response size: ${data.size} bytes")
                    broadcastLog("⚠️ Auth failed: $unlockStatus")
                }
            }
            uuid == "00000014-0200-a58e-e411-afe28044e62c" -> {
                Log.i(TAG, "📖 Characteristic 00000014 value: ${data.joinToString(" ") { "%02X".format(it) }}")
                broadcastLog("📖 00000014: ${data.joinToString(" ") { "%02X".format(it) }}")
                if (data.isNotEmpty()) {
                    val value = data[0].toInt() and 0xFF
                    Log.i(TAG, "  → Value: 0x${value.toString(16)} ($value)")
                    broadcastLog("  → Value: 0x${value.toString(16)}")
                }
            }
            uuid == "00000031-0200-a58e-e411-afe28044e62c" -> {
                Log.i(TAG, "📖 Characteristic 00000031 value: ${data.joinToString(" ") { "%02X".format(it) }}")
                broadcastLog("📖 00000031: ${data.joinToString(" ") { "%02X".format(it) }}")
                if (data.size >= 4) {
                    // Try to interpret as 32-bit value
                    val value = (data[3].toInt() and 0xFF shl 24) or
                               (data[2].toInt() and 0xFF shl 16) or
                               (data[1].toInt() and 0xFF shl 8) or
                               (data[0].toInt() and 0xFF)
                    Log.i(TAG, "  → 32-bit value: 0x${value.toString(16)} ($value)")
                    broadcastLog("  → 32-bit: 0x${value.toString(16)}")
                }
                // Version read completed (notifications are already enabled)
            }
            uuid == "00002a26-0000-1000-8000-00805f9b34fb" -> {
                val fwRev = String(data, Charsets.UTF_8)
                Log.i(TAG, "📖 Firmware Revision: $fwRev")
                broadcastLog("📖 Firmware: $fwRev")
            }
            uuid == "00002a29-0000-1000-8000-00805f9b34fb" -> {
                val mfgName = String(data, Charsets.UTF_8)
                Log.i(TAG, "📖 Manufacturer: $mfgName")
                broadcastLog("📖 Manufacturer: $mfgName")
            }
            else -> {
                Log.d(TAG, "Read from unknown characteristic: $uuid = ${data.joinToString(" ") { "%02X".format(it) }}")
            }
        }
    }
    
    private fun handleCharacteristicWrite(characteristic: BluetoothGattCharacteristic) {
        val uuid = characteristic.uuid.toString().lowercase()
        
        when {
            uuid == Constants.UNLOCK_CHAR_UUID.lowercase() -> {
                handleUnlockWrite()
            }
            uuid == Constants.KEY_CHAR_UUID.lowercase() -> {
                handleKeyWrite()
            }
        }
    }
    
    private fun handleCharacteristicNotification(characteristic: BluetoothGattCharacteristic) {
        val data = characteristic.value ?: return
        val uuid = characteristic.uuid.toString().lowercase()

        // Mark that we have seen inbound data for health tracking
        lastDataTimestampMs = System.currentTimeMillis()
        
        if (VERBOSE_LOGGING) {
            Log.i(TAG, "📨 Notification from $uuid: ${data.size} bytes")
            broadcastLog("📨 Notification: ${data.size} bytes")
        }
        
        // Queue notification data for active stream reading (like official app)
        // Official app: Notifications queue data, ReadAsync actively consumes it
        if (uuid == Constants.CAN_READ_CHAR_UUID.lowercase() || uuid == Constants.DATA_READ_CHAR_UUID.lowercase()) {
            // Queue the raw notification data
            notificationQueue.offer(data)
            synchronized(streamReadLock) {
                streamReadLock.notify()  // Wake up the reading thread
            }
        } else if (VERBOSE_LOGGING) {
            // For other characteristics, just log (debug only)
            Log.d(TAG, "📨 ${uuid}: ${data.joinToString(" ") { "%02X".format(it) }}")
        }
        
        // Optional debug MQTT mirror of raw notifications
        if (VERBOSE_LOGGING) {
            val topic = when {
                uuid == Constants.CAN_READ_CHAR_UUID.lowercase() -> "can_rx"
                uuid == Constants.DATA_READ_CHAR_UUID.lowercase() -> "data_rx"
                else -> "notification"
            }
            publishMqtt(topic, data.joinToString(" ") { "%02X".format(it) })
        }
    }
    
    /**
     * Process MyRvLink event or command response
     * Based on MyRvLinkEventFactory.TryDecodeEvent() and DirectConnectionMyRvLink.OnReceivedEvent()
     */
    private fun processMyRvLinkEvent(data: ByteArray) {
        if (data.isEmpty()) return

        // First try to decode as MyRvLink event. Some event frames (e.g., tank status)
        // can look like "valid" command IDs in the first two bytes; if we check command
        // responses first we drop real events.
        val event = MyRvLinkEventFactory.tryDecodeEvent(data)
        if (event != null) {
            handleMyRvLinkEvent(event)
            return
        }

        // Otherwise treat as command response
        if (MyRvLinkEventFactory.isCommandResponse(data)) {
            val commandId = MyRvLinkEventFactory.extractCommandId(data)
            if (commandId != null) {
                val commandType = data[2].toInt() and 0xFF
                Log.i(TAG, "📦 Command Response: CommandId=0x${commandId.toString(16)}, Type=0x${commandType.toString(16)}, size=${data.size} bytes")
                broadcastLog("📦 Command Response: ID=0x${commandId.toString(16)}, Type=0x${commandType.toString(16)}")
                
                when (commandType) {
                    0x01 -> handleGetDevicesResponse(data)          // GetDevices
                    0x02 -> handleGetDevicesMetadataResponse(data)  // GetDevicesMetadata
                }
            }
            return
        }

        // Not a recognized event/response - log for debugging
        Log.d(TAG, "⚠️ Unknown data format: ${data.size} bytes - ${data.joinToString(" ") { "%02X".format(it) }}")
    }

    /**
     * Handle decoded MyRvLink event
     * Based on DirectConnectionMyRvLink.OnReceivedEvent()
     */
    private fun handleMyRvLinkEvent(event: MyRvLinkEvent) {
        when (event.eventType) {
            MyRvLinkEventType.GatewayInformation -> {
                handleGatewayInformationEvent(event.rawData)
            }
            MyRvLinkEventType.DeviceCommand -> {
                Log.i(TAG, "📨 DeviceCommand event received")
                broadcastLog("📨 DeviceCommand event")
            }
            MyRvLinkEventType.DeviceOnlineStatus -> {
                Log.i(TAG, "📡 DeviceOnlineStatus event received")
                broadcastLog("📡 DeviceOnlineStatus event")
            }
            MyRvLinkEventType.RvStatus -> {
                Log.i(TAG, "🏠 RvStatus event received")
                broadcastLog("🏠 RvStatus event")
                parseStatusMessage(event.rawData)
            }
            MyRvLinkEventType.RelayBasicLatchingStatusType1 -> {
                Log.i(TAG, "🔌 RelayBasicLatchingStatusType1 event received")
                broadcastLog("🔌 Relay status event")
                handleRelayBasicLatchingStatusType1(event.rawData)
            }
            MyRvLinkEventType.RelayBasicLatchingStatusType2 -> {
                Log.i(TAG, "🔌 RelayBasicLatchingStatusType2 event received")
                broadcastLog("🔌 Relay status type 2 event")
                handleRelayBasicLatchingStatusType2(event.rawData)
            }
            MyRvLinkEventType.DimmableLightStatus -> {
                Log.i(TAG, "💡 DimmableLightStatus event received")
                broadcastLog("💡 DimmableLight status event")
                handleDimmableLightStatus(event.rawData)
            }
            MyRvLinkEventType.RgbLightStatus -> {
                Log.i(TAG, "🌈 RgbLightStatus event received")
                broadcastLog("🌈 RGB light status event")
                handleRgbLightStatus(event.rawData)
            }
            MyRvLinkEventType.RelayHBridgeMomentaryStatusType2 -> {
                Log.i(TAG, "🪟 RelayHBridgeMomentaryStatusType2 event received")
                broadcastLog("🪟 H-bridge status event")
                handleRelayHBridgeStatus(event.rawData)
            }
            MyRvLinkEventType.TankSensorStatus, MyRvLinkEventType.TankSensorStatusV2 -> {
                Log.i(TAG, "💧 TankSensorStatus event received")
                broadcastLog("💧 TankSensorStatus event")
            }
            MyRvLinkEventType.RealTimeClock -> {
                Log.i(TAG, "🕐 RealTimeClock event received")
                broadcastLog("🕐 RealTimeClock event")
            }
            else -> {
                Log.i(TAG, "📨 ${event.eventType.name} event received (${event.rawData.size} bytes)")
                broadcastLog("📨 ${event.eventType.name} event")
            }
        }
    }

    /**
     * Handle GatewayInformation event
     * Format: [0x01][ProtocolVersion][Options][DeviceCount][DeviceTableId][DeviceTableCrc (4 bytes)][DeviceMetadataTableCrc (4 bytes)]
     * Based on MyRvLinkGatewayInformation.Decode()
     */
    private fun handleGatewayInformationEvent(data: ByteArray) {
        if (data.size < 13) {
            Log.w(TAG, "GatewayInformation event too short: ${data.size} bytes")
            return
        }

        val protocolVersion = data[1].toInt() and 0xFF
        val options = data[2].toInt() and 0xFF
        val deviceCount = data[3].toInt() and 0xFF
        val newDeviceTableId = data[4]
        val deviceTableCrc = ((data[8].toInt() and 0xFF) shl 24) or
                            ((data[7].toInt() and 0xFF) shl 16) or
                            ((data[6].toInt() and 0xFF) shl 8) or
                            (data[5].toInt() and 0xFF)
        val deviceMetadataTableCrc = ((data[12].toInt() and 0xFF) shl 24) or
                                    ((data[11].toInt() and 0xFF) shl 16) or
                                    ((data[10].toInt() and 0xFF) shl 8) or
                                    (data[9].toInt() and 0xFF)

        Log.i(TAG, "📡 GatewayInformation: ProtocolVersion=0x${protocolVersion.toString(16)}, DeviceCount=$deviceCount, DeviceTableId=0x${newDeviceTableId.toString(16)}, DeviceTableCrc=0x${deviceTableCrc.toString(16)}, DeviceMetadataTableCrc=0x${deviceMetadataTableCrc.toString(16)}")
        broadcastLog("📡 GatewayInfo: TableId=0x${newDeviceTableId.toString(16)}, Devices=$deviceCount")

        // Update DeviceTableId
        if (newDeviceTableId != 0x00.toByte()) {
            deviceTableId = newDeviceTableId
            Log.i(TAG, "✅ Updated DeviceTableId: 0x${deviceTableId.toString(16)}")
            broadcastLog("✅ DeviceTableId: 0x${deviceTableId.toString(16)}")
        }

        // Mark that we've received GatewayInformation
        if (!gatewayInfoReceived) {
            gatewayInfoReceived = true
            broadcastServiceState()
            onGatewayInfoReceived()
        }
    }

    /**
     * Called when GatewayInformation is received for the first time
     * This is equivalent to MyRvLink Start() - we can now send commands
     */
    private fun onGatewayInfoReceived() {
        if (isStarted) {
            return  // Already started
        }

        Log.i(TAG, "✅ GatewayInformation received - starting MyRvLink layer")
        broadcastLog("✅ Gateway ready - starting communication")

        isStarted = true

        // Now we can send GetDevices command
        handler.postDelayed({
            sendInitialCanCommand()
        }, 500)

        // Start heartbeat
        startHeartbeat()
    }

    private fun parseDeviceIdMessage(data: ByteArray) {
        if (data.size < 7) return
        
        // Device ID message format (from Python code):
        // Byte 0: Message type (0x02)
        // Bytes 1-2: Product ID (little-endian)
        // Byte 3: Device type
        // Bytes 4-6: Additional data
        
        val productId = ((data[2].toInt() and 0xFF) shl 8) or (data[1].toInt() and 0xFF)
        val deviceType = data[3].toInt() and 0xFF
        
        Log.i(TAG, "  📱 Device ID - Product: 0x${productId.toString(16)}, Type: 0x${deviceType.toString(16)}")
        broadcastLog("  📱 Device: Product=0x${productId.toString(16)}, Type=0x${deviceType.toString(16)}")
    }
    
    private fun parseStatusMessage(data: ByteArray) {
        if (data.size < 6) return

        // RvStatus: eventType(0x07), voltage(2 bytes 8.8 BE), temp(2 bytes 8.8 BE), flags(1)
        val voltageRaw = ((data[1].toInt() and 0xFF) shl 8) or (data[2].toInt() and 0xFF)
        val voltage = if (voltageRaw == 0xFFFF) null else voltageRaw.toFloat() / 256f

        voltage?.let {
            publishHaVoltage()
            publishMqtt("system/voltage", String.format("%.3f", it), retain = true)
        }
    }
    
    // MARK: - Disconnect
    
    private fun disconnect() {
        stopHeartbeat()  // Stop heartbeat when disconnecting
        // Keepalive reads removed - no longer needed
        stopActiveStreamReading()  // Stop active stream reading
        stopScanning()
        bluetoothGatt?.disconnect()
        bluetoothGatt?.close()
        bluetoothGatt = null
        isConnected = false
        isAuthenticated = false
        isUnlocked = false
        connectionState = ConnectionState.DISCONNECTED
        publishDiagnosticsState()
        broadcastServiceState()
    }
    
    // MARK: - MQTT Integration
    
    private fun initializeMqtt() {
        Log.d(TAG, "Initializing MQTT client...")
        try {
            mqttClient = MqttClient(MQTT_BROKER, MQTT_CLIENT_ID, null)
            mqttClient?.setCallback(object : MqttCallback {
                override fun connectionLost(cause: Throwable?) {
                    Log.w(TAG, "MQTT connection lost: ${cause?.message}")
                    mqttConnected = false
                    broadcastServiceState()
                    // Retry connection in background thread
                    handler.postDelayed({ 
                        Thread { connectMqtt() }.start()
                    }, 5000)
                }
                
                override fun messageArrived(topic: String?, message: MqttMessage?) {
                    val payload = message?.toString() ?: ""
                    Log.i(TAG, "MQTT message: $topic = $payload")
                    if (topic == null) return
                    handleMqttCommand(topic, payload)
                }
                
                override fun deliveryComplete(token: IMqttDeliveryToken?) {
                    // Message delivery complete
                }
            })
            connectMqtt()
        } catch (e: Exception) {
            Log.e(TAG, "Failed to create MQTT client: ${e.message}", e)
        }
    }
    
    private fun connectMqtt() {
        if (mqttClient == null) return
        // If client reports connected, ensure post-connect steps and bail early
        if (mqttClient?.isConnected == true) {
            mqttConnected = true
            onMqttConnected()
            return
        }
        
        try {
            val options = MqttConnectOptions().apply {
                isAutomaticReconnect = true
                isCleanSession = true
                connectionTimeout = 30
                keepAliveInterval = 60
                userName = MQTT_USERNAME
                password = MQTT_PASSWORD.toCharArray()
            }
            
            Log.d(TAG, "Connecting to MQTT broker: $MQTT_BROKER")
            // Use blocking connect in background thread
            Thread {
                try {
                    mqttClient?.connect(options)
                    handler.post {
                        Log.i(TAG, "✅ MQTT connected")
                        mqttConnected = true
                        onMqttConnected()
                    }
                } catch (e: Exception) {
                    val msg = e.message ?: ""
                    Log.e(TAG, "MQTT connection failed: $msg")
                    mqttConnected = false
                    publishDiagnosticsState()
                    broadcastServiceState()
                    // If the client thinks it's already connected, force a disconnect and retry
                    if (msg.contains("Client is connected", ignoreCase = true) || mqttClient?.isConnected == true) {
                        safeDisconnectMqtt()
                        handler.postDelayed({ connectMqtt() }, 1000)
                    } else {
                        handler.postDelayed({ connectMqtt() }, 5000)
                    }
                }
            }.start()
        } catch (e: Exception) {
            Log.e(TAG, "MQTT connect error: ${e.message}", e)
        }
    }

    private fun onMqttConnected() {
        publishDiagnosticsDiscovery()
        publishDiagnosticsState()
        broadcastServiceState()
        publishMqtt("status", "connected", retain = true)
        publishHaVoltage()
        subscribeMqttCommands()
        publishDiscoveryForKnownDevices()
    }

    private fun handleMqttCommand(topic: String, payload: String) {
        // Expect topics under onecontrol/ble/command/... (or legacy onecontrol-ble/command/...)
        if (!topic.startsWith("$MQTT_TOPIC_PREFIX/command/") && !topic.startsWith("onecontrol-ble/command/")) return
        val prefixStripped = if (topic.startsWith("$MQTT_TOPIC_PREFIX/command/")) {
            topic.removePrefix("$MQTT_TOPIC_PREFIX/command/")
        } else {
            topic.removePrefix("onecontrol-ble/command/")
        }
        val parts = prefixStripped.split("/")
        if (parts.size < 3) return
        val kind = parts[0]
        val tableId = parts[1].toIntOrNull()
        val deviceId = parts[2].toIntOrNull()
        if (tableId == null) return

        when (kind) {
            "switch" -> {
                if (deviceId == null) return
                val turnOn = payload.equals("on", true) || payload == "1" || payload.equals("true", true)
                Log.i(TAG, "MQTT cmd switch table=$tableId device=$deviceId turnOn=$turnOn")
                controlSwitch(deviceId.toByte(), turnOn)
            }
            "dimmable" -> {
                // HA may send brightness on a trailing /brightness topic; handle both forms.
                if (parts.size >= 4 && parts[3].equals("brightness", true)) {
                    val brightnessRaw = payload.toIntOrNull() ?: return
                    val brightnessPct = ((brightnessRaw * 100) / 255).coerceIn(0, 100)
                    Log.i(TAG, "MQTT cmd dimmable(brightness topic) table=$tableId device=$deviceId brightnessRaw=$brightnessRaw pct=$brightnessPct")
                    if (deviceId != null) controlDimmableLight(deviceId.toByte(), brightnessPct)
                    return
                }
                if (deviceId == null) return
                val parsed = parseLightCommandPayload(payload)
                if (parsed != null) {
                    Log.i(TAG, "MQTT cmd dimmable table=$tableId device=$deviceId brightness=$parsed")
                    controlDimmableLight(deviceId.toByte(), parsed.coerceIn(0, 100))
                } else {
                    Log.w(TAG, "MQTT dimmable command payload not understood: $payload")
                }
            }
            "cover" -> {
                Log.w(TAG, "Cover command handling not implemented yet: $topic = $payload")
            }
        }
    }

    private fun subscribeMqttCommands() {
        try {
            mqttClient?.subscribe("$MQTT_TOPIC_PREFIX/command/#", 0)
            mqttClient?.subscribe("onecontrol-ble/command/#", 0) // legacy prefix fallback
            Log.i(TAG, "📡 Subscribed to MQTT commands at $MQTT_TOPIC_PREFIX/command/# and onecontrol-ble/command/#")
        } catch (e: Exception) {
            Log.w(TAG, "Failed to subscribe to MQTT commands: ${e.message}")
        }
    }

    private fun parseLightCommandPayload(payload: String): Int? {
        // Try JSON (schema=json)
        val trimmed = payload.trim()
        if (trimmed.startsWith("{") && trimmed.endsWith("}")) {
            try {
                val json = org.json.JSONObject(trimmed)
                if (json.has("brightness")) {
                    val b = json.getInt("brightness")
                    return (b * 100 / 255).coerceIn(0, 100)
                }
                if (json.has("state")) {
                    val state = json.getString("state")
                    return if (state.equals("ON", true)) 100 else 0
                }
            } catch (_: Exception) {
                // ignore
            }
        }
        // Legacy plain payloads
        val brightness = payload.toIntOrNull()
        if (brightness != null) return brightness
        if (payload.equals("on", true) || payload.equals("true", true)) return 100
        if (payload.equals("off", true) || payload.equals("false", true)) return 0
        return null
    }
    
    private fun disconnectMqtt() {
        try {
            mqttClient?.disconnect()
            mqttConnected = false
            publishDiagnosticsState()
            broadcastServiceState()
        } catch (e: Exception) {
            Log.e(TAG, "MQTT disconnect error: ${e.message}")
        }
    }

    private fun safeDisconnectMqtt() {
        try {
            mqttClient?.disconnectForcibly(1000, 1000)
        } catch (_: Exception) {
            // ignore
        }
        mqttConnected = false
        publishDiagnosticsState()
        broadcastServiceState()
    }
    
    private fun publishMqtt(topic: String, payload: String, retain: Boolean = false) {
        if (!mqttConnected || mqttClient == null) return
        
        try {
            val fullTopic = "$MQTT_TOPIC_PREFIX/$topic"
            val message = MqttMessage(payload.toByteArray())
            message.qos = 0
            message.isRetained = retain
            
            // Use blocking publish in background thread
            Thread {
                try {
                    mqttClient?.publish(fullTopic, message)
                    if (VERBOSE_LOGGING) Log.d(TAG, "MQTT published: $fullTopic = $payload")
                } catch (e: Exception) {
                    Log.w(TAG, "MQTT publish failed: ${e.message}")
                }
            }.start()
        } catch (e: Exception) {
            Log.e(TAG, "MQTT publish error: ${e.message}", e)
        }
    }

    /**
     * Publish to an absolute MQTT topic (no app prefix). Useful for HA discovery/config.
     */
    private fun publishMqttRaw(topic: String, payload: String, retain: Boolean = false) {
        if (!mqttConnected || mqttClient == null) return

        try {
            val message = MqttMessage(payload.toByteArray()).apply {
                qos = 0
                isRetained = retain
            }

            Thread {
                try {
                    mqttClient?.publish(topic, message)
                    if (VERBOSE_LOGGING) Log.d(TAG, "MQTT published raw: $topic = $payload (retain=$retain)")
                } catch (e: Exception) {
                    Log.w(TAG, "MQTT raw publish failed: ${e.message}")
                }
            }.start()
        } catch (e: Exception) {
            Log.e(TAG, "MQTT raw publish error: ${e.message}", e)
        }
    }

    /**
     * Publish Home Assistant discovery for a device if not already sent.
     * We expose read-only entities so HA can at least reflect state.
     */
    private fun publishHaDiscovery(tableId: Byte, deviceId: Byte, supportsBrightness: Boolean) {
        val keyHex = "%02x%02x".format(tableId.toUByte().toInt(), deviceId.toUByte().toInt())
        val baseKey = "0x${"%02x".format(tableId.toUByte().toInt())}:${"%02x".format(deviceId.toUByte().toInt())}"
        val objectId = "light_$keyHex"
        val deviceName = "light_$keyHex"
        val stateTopic = "$MQTT_TOPIC_PREFIX/device/${tableId.toUByte()}/${deviceId.toUByte()}/state"
        val brightnessStateTopic = "$MQTT_TOPIC_PREFIX/device/${tableId.toUByte()}/${deviceId.toUByte()}/brightness"
        val commandTopic = "$MQTT_TOPIC_PREFIX/command/dimmable/${tableId.toUByte()}/${deviceId.toUByte()}"

        // Use MQTT light with brightness if supported, else switch
        if (supportsBrightness) {
            val key = "$objectId:light"
            if (haDiscoveryPublished.add(key)) {
                val payload = """
                    {
                      "name": "$deviceName",
                      "unique_id": "${objectId}_light",
                      "state_topic": "$stateTopic",
                      "command_topic": "$commandTopic",
                      "brightness_state_topic": "$brightnessStateTopic",
                      "brightness_command_topic": "$commandTopic",
                      "on_command_type": "first",
                      "payload_on": "ON",
                      "payload_off": "OFF",
                      "brightness_state_topic": "$brightnessStateTopic",
                      "device": {
                        "identifiers": ["onecontrol_ble", "$objectId"],
                        "manufacturer": "Lippert",
                        "model": "OneControl Gateway",
                        "name": "OneControl Gateway"
                      }
                    }
                """.trimIndent()
                publishMqttRaw("homeassistant/light/$objectId/config", payload, retain = true)
                // Initial retained state to avoid "unknown" in HA
                publishMqtt("device/${tableId}/${deviceId}/state", "OFF", retain = true)
                publishMqtt("device/${tableId}/${deviceId}/brightness", "0", retain = true)
            }
        } else {
            publishHaDiscoverySwitch(tableId, deviceId)
        }
    }

    private fun publishHaDiscoverySwitch(tableId: Byte, deviceId: Byte) {
        val keyHex = "%02x%02x".format(tableId.toUByte().toInt(), deviceId.toUByte().toInt())
        val baseKey = "0x${"%02x".format(tableId.toUByte().toInt())}:${"%02x".format(deviceId.toUByte().toInt())}"
        val objectId = "switch_$keyHex"
        val deviceName = "switch_$keyHex"
        val stateTopic = "$MQTT_TOPIC_PREFIX/device/${tableId.toUByte()}/${deviceId.toUByte()}/state"
        val commandTopic = "$MQTT_TOPIC_PREFIX/command/switch/${tableId.toUByte()}/${deviceId.toUByte()}"
        val key = "$objectId:switch"
        if (haDiscoveryPublished.add(key)) {
            val payload = """
                {
                  "name": "$deviceName",
                  "unique_id": "${objectId}_switch",
                  "state_topic": "$stateTopic",
                  "command_topic": "$commandTopic",
                  "payload_on": "ON",
                  "payload_off": "OFF",
                  "device": {
                    "identifiers": ["onecontrol_ble", "$objectId"],
                    "manufacturer": "Lippert",
                    "model": "OneControl Gateway",
                    "name": "OneControl Gateway"
                  }
                }
            """.trimIndent()
            publishMqttRaw("homeassistant/switch/$objectId/config", payload, retain = true)
        }
    }

    private fun publishHaDiscoveryCover(tableId: Byte, deviceId: Byte) {
        val keyHex = "%02x%02x".format(tableId.toUByte().toInt(), deviceId.toUByte().toInt())
        val baseKey = "0x${"%02x".format(tableId.toUByte().toInt())}:${"%02x".format(deviceId.toUByte().toInt())}"
        val objectId = "cover_$keyHex"
        val deviceName = "cover_$keyHex"
        val stateTopic = "$MQTT_TOPIC_PREFIX/device/${tableId.toUByte()}/${deviceId.toUByte()}/position"
        val commandTopic = "$MQTT_TOPIC_PREFIX/command/cover/${tableId.toUByte()}/${deviceId.toUByte()}"
        val key = "$objectId:cover"
        if (haDiscoveryPublished.add(key)) {
            val payload = """
                {
                  "name": "$deviceName",
                  "unique_id": "${objectId}_cover",
                  "state_topic": "$stateTopic",
                  "command_topic": "$commandTopic",
                  "payload_open": "OPEN",
                  "payload_close": "CLOSE",
                  "payload_stop": "STOP",
                  "device_class": "awning",
                  "optimistic": true,
                  "device": {
                    "identifiers": ["onecontrol_ble", "$objectId"],
                    "manufacturer": "Lippert",
                    "model": "OneControl Gateway",
                    "name": "OneControl Gateway"
                  }
                }
            """.trimIndent()
            publishMqttRaw("homeassistant/cover/$objectId/config", payload, retain = true)
            publishMqtt("device/${tableId}/${deviceId}/position", "unknown", retain = true)
        }
    }

    private fun publishHaVoltage() {
        val objectId = "sensor_voltage"
        if (haDiscoveryPublished.add(objectId)) {
            val payload = """
                {
                  "name": "System Voltage",
                  "unique_id": "system_voltage",
                  "state_topic": "$MQTT_TOPIC_PREFIX/system/voltage",
                  "unit_of_measurement": "V",
                  "device_class": "voltage",
                  "state_class": "measurement",
                  "device": {
                    "identifiers": ["onecontrol_ble", "system_voltage"],
                    "manufacturer": "Lippert",
                    "model": "OneControl Gateway",
                    "name": "OneControl Gateway"
                  }
                }
            """.trimIndent()
            publishMqttRaw("homeassistant/sensor/$objectId/config", payload, retain = true)
        }
    }

    /**
     * Re-publish discovery for devices we've already seen to restore entities after HA cache clear.
     */
    private fun publishDiscoveryForKnownDevices() {
        // Republish for any devices we've already seen
        deviceStatuses.values.forEach { status ->
            when (status) {
                is DeviceStatus.Relay -> publishHaDiscoverySwitch(status.deviceTableId, status.deviceId)
                is DeviceStatus.DimmableLight -> publishHaDiscovery(status.deviceTableId, status.deviceId, supportsBrightness = true)
                is DeviceStatus.RgbLight -> publishHaDiscovery(status.deviceTableId, status.deviceId, supportsBrightness = false)
                is DeviceStatus.Cover -> publishHaDiscoveryCover(status.deviceTableId, status.deviceId)
            }
        }
    }

    
    // MARK: - Device Status Handling
    
    /**
     * Handle RelayBasicLatchingStatusType1 event
     * Format: [EventType (1)][DeviceTableId (1)][DeviceId (1)][State (1)]...
     */
    private fun handleRelayBasicLatchingStatusType1(data: ByteArray) {
        val statuses = DeviceStatusParser.parseRelayBasicLatchingStatusType1(data)
        for (status in statuses) {
            val key = "${status.deviceTableId}:${status.deviceId}"
            val isOn = status.isOn
            deviceStatuses[key] = DeviceStatus.Relay(status.deviceTableId, status.deviceId, isOn)

            Log.i(TAG, "🔌 Relay ${status.deviceId} (TableId=${status.deviceTableId}): ${if (isOn) "ON" else "OFF"}")
            broadcastLog("🔌 Relay ${status.deviceId}: ${if (isOn) "ON" else "OFF"}")

            publishHaDiscovery(status.deviceTableId, status.deviceId, supportsBrightness = false)
            publishMqtt("device/${status.deviceTableId}/${status.deviceId}/state", if (isOn) "ON" else "OFF", retain = true)
            publishMqtt("device/${status.deviceTableId}/${status.deviceId}/type", "relay")
        }
    }

    /**
     * Handle RelayBasicLatchingStatusType2 event (coarse parsing).
     * Format observed: [EventType][DeviceTableId][DeviceId][State?...]
     * We at least surface HA discovery and a best-effort state.
     */
    private fun handleRelayBasicLatchingStatusType2(data: ByteArray) {
        if (data.size < 3) {
            Log.w(TAG, "RelayBasicLatchingStatusType2 too short: ${data.size} bytes")
            return
        }
        val tableId = data[1]
        val deviceId = data[2]
        val statusByte = if (data.size > 3) data[3].toInt() and 0xFF else 0xFF
        // Per decompiled LogicalDeviceRelayStatusType2: raw output state in low nibble
        val rawOutputState = statusByte and 0x0F
        val isOn = when (rawOutputState) {
            0x01 -> true
            0x00 -> false
            else -> null
        }

        publishHaDiscoverySwitch(tableId, deviceId)
        publishMqtt("device/${tableId}/${deviceId}/type", "relay")
        if (isOn != null) {
            val key = "${tableId}:${deviceId}"
            deviceStatuses[key] = DeviceStatus.Relay(tableId, deviceId, isOn)
            publishMqtt("device/${tableId}/${deviceId}/state", if (isOn) "ON" else "OFF", retain = true)
        }
    }
    
    /**
     * Handle DimmableLightStatus event
     * Format: [EventType (1)][DeviceTableId (1)][DeviceId (1)][Status (8 bytes)]...
     */
    private fun handleDimmableLightStatus(data: ByteArray) {
        val statuses = DeviceStatusParser.parseDimmableLightStatus(data)
        for (status in statuses) {
            val key = "${status.deviceTableId}:${status.deviceId}"
            val brightness = status.brightness ?: 0
            val isOn = status.isOn ?: (brightness > 0)
            deviceStatuses[key] = DeviceStatus.DimmableLight(status.deviceTableId, status.deviceId, isOn, brightness)

            Log.i(TAG, "💡 Dimmable Light ${status.deviceId} (TableId=${status.deviceTableId}): ${if (isOn) "ON" else "OFF"}, Brightness=$brightness")
            broadcastLog("💡 Light ${status.deviceId}: ${if (isOn) "ON" else "OFF"} @ $brightness")

            publishHaDiscovery(status.deviceTableId, status.deviceId, supportsBrightness = true)
            publishMqtt("device/${status.deviceTableId}/${status.deviceId}/state", if (isOn) "ON" else "OFF", retain = true)
            publishMqtt("device/${status.deviceTableId}/${status.deviceId}/brightness", brightness.toString(), retain = true)
            publishMqtt("device/${status.deviceTableId}/${status.deviceId}/type", "dimmable_light")
        }
    }
    
    /**
     * Handle RgbLightStatus event
     * Format: [EventType (1)][DeviceTableId (1)][DeviceId (1)][Status (8 bytes)]...
     */
    private fun handleRgbLightStatus(data: ByteArray) {
        val statuses = DeviceStatusParser.parseRgbLightStatus(data)
        for (status in statuses) {
            val key = "${status.deviceTableId}:${status.deviceId}"
            deviceStatuses[key] = DeviceStatus.RgbLight(status.deviceTableId, status.deviceId, status.statusBytes)

            Log.i(TAG, "🌈 RGB Light ${status.deviceId} (TableId=${status.deviceTableId}): Status updated")
            broadcastLog("🌈 RGB Light ${status.deviceId}: Status updated")

            publishHaDiscovery(status.deviceTableId, status.deviceId, supportsBrightness = false)
            publishMqtt("device/${status.deviceTableId}/${status.deviceId}/type", "rgb_light")
        }
    }

    /**
     * Handle H-bridge momentary status (slide/awning). Minimal parse to expose discovery.
     * Format observed: [EventType][DeviceTableId][DeviceId][...]
     */
    private fun handleRelayHBridgeStatus(data: ByteArray) {
        if (data.size < 3) {
            Log.w(TAG, "RelayHBridge status too short: ${data.size} bytes")
            return
        }
        val tableId = data[1]
        val deviceId = data[2]

        publishHaDiscoveryCover(tableId, deviceId)
        publishMqtt("device/${tableId}/${deviceId}/type", "cover")
        // Position/state parsing TBD; publish unknown placeholder
        publishMqtt("device/${tableId}/${deviceId}/position", "unknown", retain = true)
        val key = "${tableId}:${deviceId}"
        deviceStatuses[key] = DeviceStatus.Cover(tableId, deviceId)
    }
    
    // MARK: - Device Enumeration (GetDevices / GetDevicesMetadata)
    
    data class DeviceDefinition(
        val deviceTableId: Byte,
        val deviceId: Byte,
        val protocol: Int,
        val deviceType: Int,
        val deviceInstance: Int,
        val productId: Int,
        val mac: String?,
        val functionName: Int? = null,
        val functionInstance: Int? = null,
        val rawCapability: Int? = null,
        val circuitId: Long? = null,
        val softwarePartNumber: String? = null
    )
    
    private val deviceDefinitions = mutableMapOf<String, DeviceDefinition>()  // key: "tableId:deviceId"
    
    private fun deviceKey(tableId: Byte, deviceId: Byte): String =
        "${tableId.toUByte().toString(16)}:${deviceId.toUByte().toString(16)}"
    
    /**
     * Decode MyRvLink GetDevices response and populate deviceDefinitions.
     * Wire format (Data Service, as observed):
     *  `ClientCommandId (2 LE)` + `CommandType=0x01` + `ExtendedData...`
     *
     * ExtendedData (per C# MyRvLinkCommandGetDevicesResponse):
     *  byte 0: DeviceTableId
     *  byte 1: StartDeviceId
     *  byte 2: DeviceCount
     *  bytes 3+: repeated device entries:
     *      `[Protocol][PayloadSize][Payload bytes...]`
     *
     * For IdsCan devices (Protocol=2):
     *  PayloadSize = 10, layout:
     *      byte 2:  DeviceType
     *      byte 3:  DeviceInstance
     *      bytes 4‑5: ProductId (LE)
     *      bytes 6‑11: Product MAC (6 bytes)
     */
    private fun handleGetDevicesResponse(data: ByteArray) {
        if (data.size < 6) {
            Log.w(TAG, "GetDevices response too short: ${data.size} bytes")
            return
        }
        val extended = data.copyOfRange(3, data.size)
        if (extended.size < 3) {
            Log.w(TAG, "GetDevices extended data too short: ${extended.size} bytes")
            return
        }
        
        val tableId = extended[0]
        val startDeviceId = extended[1].toInt() and 0xFF
        val deviceCount = extended[2].toInt() and 0xFF
        
        // Update DeviceTableId if we didn't have one yet
        if (deviceTableId == 0x00.toByte() && tableId != 0x00.toByte()) {
            deviceTableId = tableId
            Log.i(TAG, "✅ Updated DeviceTableId from GetDevices response: 0x${deviceTableId.toString(16)}")
            broadcastLog("✅ DeviceTableId: 0x${deviceTableId.toString(16)}")
        }
        
        var offset = 3
        var index = 0
        Log.i(TAG, "📋 GetDevices: tableId=0x${tableId.toString(16)}, startId=$startDeviceId, count=$deviceCount, extLen=${extended.size}")
        
        while (index < deviceCount && offset + 2 <= extended.size) {
            val protocol = extended[offset].toInt() and 0xFF
            val payloadSize = extended[offset + 1].toInt() and 0xFF
            val entrySize = payloadSize + 2
            if (offset + entrySize > extended.size) {
                Log.w(TAG, "GetDevices entry truncated at index=$index, offset=$offset, payloadSize=$payloadSize, extLen=${extended.size}")
                break
            }
            
            val deviceId = ((startDeviceId + index) and 0xFF).toByte()
            if (protocol == 2 && payloadSize == 10) {
                // IdsCan physical device
                val base = offset
                val deviceType = extended[base + 2].toInt() and 0xFF
                val deviceInstance = extended[base + 3].toInt() and 0xFF
                val productId = ((extended[base + 5].toInt() and 0xFF) shl 8) or (extended[base + 4].toInt() and 0xFF)
                val macBytes = extended.copyOfRange(base + 6, base + 12)
                val macStr = macBytes.joinToString(":") { "%02X".format(it) }
                
                val def = DeviceDefinition(
                    deviceTableId = tableId,
                    deviceId = deviceId,
                    protocol = protocol,
                    deviceType = deviceType,
                    deviceInstance = deviceInstance,
                    productId = productId,
                    mac = macStr
                )
                val key = deviceKey(tableId, deviceId)
                deviceDefinitions[key] = def
                
                Log.i(TAG, "📋 Device[$key]: proto=$protocol, type=0x${deviceType.toString(16)}, instance=$deviceInstance, productId=0x${productId.toString(16)}, mac=$macStr")
                broadcastLog("📋 Device[$key]: type=0x${deviceType.toString(16)}, inst=$deviceInstance")
                
                // Publish metadata to MQTT
                publishDeviceMeta(def)
            } else {
                Log.i(TAG, "📋 Device entry index=$index uses unsupported protocol=$protocol, payloadSize=$payloadSize")
            }
            
            offset += entrySize
            index++
        }
        
        if (index != deviceCount) {
            Log.w(TAG, "GetDevices decoded $index devices, expected $deviceCount")
        }
    }
    
    /**
     * Decode MyRvLink GetDevicesMetadata response and merge metadata into existing devices.
     * Wire format is similar to GetDevices, but entries are MyRvLinkDeviceIdsCanMetadata.
     */
    private fun handleGetDevicesMetadataResponse(data: ByteArray) {
        if (data.size < 6) {
            Log.w(TAG, "GetDevicesMetadata response too short: ${data.size} bytes")
            return
        }
        val extended = data.copyOfRange(3, data.size)
        if (extended.size < 3) {
            Log.w(TAG, "GetDevicesMetadata extended data too short: ${extended.size} bytes")
            return
        }
        
        val tableId = extended[0]
        val startDeviceId = extended[1].toInt() and 0xFF
        val deviceCount = extended[2].toInt() and 0xFF
        
        var offset = 3
        var index = 0
        Log.i(TAG, "📋 GetDevicesMetadata: tableId=0x${tableId.toString(16)}, startId=$startDeviceId, count=$deviceCount, extLen=${extended.size}")
        
        while (index < deviceCount && offset + 2 <= extended.size) {
            val protocol = extended[offset].toInt() and 0xFF
            val payloadSize = extended[offset + 1].toInt() and 0xFF
            val entrySize = payloadSize + 2
            if (offset + entrySize > extended.size) {
                Log.w(TAG, "GetDevicesMetadata entry truncated at index=$index, offset=$offset, payloadSize=$payloadSize, extLen=${extended.size}")
                break
            }
            
            val deviceId = ((startDeviceId + index) and 0xFF).toByte()
            if (protocol == 2 && payloadSize == 17) {
                val base = offset
                val functionName = ((extended[base + 3].toInt() and 0xFF) shl 8) or (extended[base + 2].toInt() and 0xFF)
                val functionInstance = extended[base + 4].toInt() and 0xFF
                val rawCap = extended[base + 5].toInt() and 0xFF
                val circuitId = ((extended[base + 10].toLong() and 0xFF) shl 24) or
                                 ((extended[base + 9].toLong() and 0xFF) shl 16) or
                                 ((extended[base + 8].toLong() and 0xFF) shl 8) or
                                 (extended[base + 7].toLong() and 0xFF)
                val partBytes = extended.copyOfRange(base + 11, base + 19).takeWhile { it != 0.toByte() }
                val partNumber = partBytes.toByteArray().toString(Charsets.UTF_8)
                
                val key = deviceKey(tableId, deviceId)
                val existing = deviceDefinitions[key]
                val updated = if (existing != null) {
                    existing.copy(
                        functionName = functionName,
                        functionInstance = functionInstance,
                        rawCapability = rawCap,
                        circuitId = circuitId,
                        softwarePartNumber = partNumber
                    )
                } else {
                    DeviceDefinition(
                        deviceTableId = tableId,
                        deviceId = deviceId,
                        protocol = protocol,
                        deviceType = 0,
                        deviceInstance = 0,
                        productId = 0,
                        mac = null,
                        functionName = functionName,
                        functionInstance = functionInstance,
                        rawCapability = rawCap,
                        circuitId = circuitId,
                        softwarePartNumber = partNumber
                    )
                }
                deviceDefinitions[key] = updated
                
                Log.i(TAG, "📋 Metadata[$key]: fn=0x${functionName.toString(16)}, inst=$functionInstance, cap=0x${rawCap.toString(16)}, circuitId=0x${circuitId.toString(16)}, part=$partNumber")
                broadcastLog("📋 Meta[$key]: fn=0x${functionName.toString(16)}, inst=$functionInstance")
                
                publishDeviceMeta(updated)
            } else {
                Log.i(TAG, "📋 Metadata entry index=$index uses unsupported protocol=$protocol, payloadSize=$payloadSize")
            }
            
            offset += entrySize
            index++
        }
        
        if (index != deviceCount) {
            Log.w(TAG, "GetDevicesMetadata decoded $index entries, expected $deviceCount")
        }
    }
    
    private fun publishDeviceMeta(def: DeviceDefinition) {
        val key = deviceKey(def.deviceTableId, def.deviceId)
        val json = buildString {
            append("{")
            append("\"deviceTableId\":").append(def.deviceTableId.toUByte().toInt()).append(',')
            append("\"deviceId\":").append(def.deviceId.toUByte().toInt()).append(',')
            append("\"protocol\":").append(def.protocol).append(',')
            append("\"deviceType\":").append(def.deviceType).append(',')
            append("\"deviceInstance\":").append(def.deviceInstance).append(',')
            append("\"productId\":").append(def.productId)
            def.mac?.let { append(",\"mac\":\"").append(it).append('"') }
            def.functionName?.let { append(",\"functionName\":").append(it) }
            def.functionInstance?.let { append(",\"functionInstance\":").append(it) }
            def.rawCapability?.let { append(",\"rawCapability\":").append(it) }
            def.circuitId?.let { append(",\"circuitId\":").append(it) }
            def.softwarePartNumber?.let { append(",\"softwarePartNumber\":\"").append(it).append('"') }
            append("}")
        }
        publishMqtt("device/meta/$key", json)
    }
    
    // MARK: - Device Control
    
    /**
     * Control a switch (relay)
     * @param deviceId Device ID (0-255)
     * @param turnOn true to turn on, false to turn off
     */
    fun controlSwitch(deviceId: Byte, turnOn: Boolean) {
        if (!isConnected || !isAuthenticated || bluetoothGatt == null) {
            Log.w(TAG, "Cannot control switch - not ready")
            return
        }

        if (deviceTableId == 0x00.toByte()) {
            Log.w(TAG, "DeviceTableId not yet known - cannot send switch command")
            return
        }

        val writeChar = canWriteChar ?: dataWriteChar
        if (writeChar == null) {
            Log.w(TAG, "No write characteristic available")
            return
        }

        try {
            val commandId = getNextCommandId()
            val command = MyRvLinkCommandBuilder.buildActionSwitch(
                clientCommandId = commandId,
                deviceTableId = deviceTableId,
                switchState = turnOn,
                deviceIds = listOf(deviceId)
            )

            val encoded = CobsDecoder.encode(command, prependStartFrame = true, useCrc = true)
            writeChar.value = encoded
            writeChar.writeType = BluetoothGattCharacteristic.WRITE_TYPE_NO_RESPONSE

            val writeResult = bluetoothGatt?.writeCharacteristic(writeChar)
            Log.i(TAG, "📤 Switch control: DeviceId=$deviceId, State=${if (turnOn) "ON" else "OFF"}, CommandId=0x${commandId.toString(16)}, writeResult=$writeResult")
            broadcastLog("📤 Switch $deviceId: ${if (turnOn) "ON" else "OFF"}")

            // Publish to MQTT
            publishMqtt("command/switch/$deviceId", if (turnOn) "ON" else "OFF")
        } catch (e: Exception) {
            Log.e(TAG, "Failed to send switch command: ${e.message}", e)
            broadcastLog("❌ Switch command failed: ${e.message}")
        }
    }
    
    /**
     * Control a dimmable light
     * @param deviceId Device ID (0-255)
     * @param brightness Brightness 0-100 (0 = off, >0 = on)
     */
    fun controlDimmableLight(deviceId: Byte, brightness: Int) {
        if (!isConnected || !isAuthenticated || bluetoothGatt == null) {
            Log.w(TAG, "Cannot control dimmable light - not ready")
            return
        }

        if (deviceTableId == 0x00.toByte()) {
            Log.w(TAG, "DeviceTableId not yet known - cannot send dimmable command")
            return
        }

        val writeChar = canWriteChar ?: dataWriteChar
        if (writeChar == null) {
            Log.w(TAG, "No write characteristic available")
            return
        }

        try {
            val tableId = if (deviceTableId == 0x00.toByte()) DEFAULT_DEVICE_TABLE_ID else deviceTableId
            if (brightness <= 0) {
                sendDimmableCommand(writeChar, tableId, deviceId, MyRvLinkCommandEncoder.DimmableLightCommand.Off, 0)
                publishMqtt("command/dimmable/$deviceId/brightness", "0")
                return
            }

            // Turn on with minimal "on" value, then send Settings with scaled brightness to lock the level.
            val onCmdId = getNextCommandId()
            sendDimmableCommand(writeChar, tableId, deviceId, MyRvLinkCommandEncoder.DimmableLightCommand.On, 1, onCmdId)

            val scaledBrightness = (brightness.coerceIn(1, 100) * 255 / 100).coerceIn(1, 255)
            val settingsCmdId = getNextCommandId()
            // small delay to let the gateway accept ON before Settings brightness
            handler.postDelayed({
                sendDimmableCommand(writeChar, tableId, deviceId, MyRvLinkCommandEncoder.DimmableLightCommand.Settings, scaledBrightness, settingsCmdId)
            }, 120)

            publishMqtt("command/dimmable/$deviceId/brightness", brightness.toString())
        } catch (e: Exception) {
            Log.e(TAG, "Failed to send dimmable command: ${e.message}", e)
            broadcastLog("❌ Dimmable command failed: ${e.message}")
        }
    }

    private fun sendDimmableCommand(
        writeChar: BluetoothGattCharacteristic,
        tableId: Byte,
        deviceId: Byte,
        command: MyRvLinkCommandEncoder.DimmableLightCommand,
        brightness: Int,
        commandId: UShort = getNextCommandId()
    ) {
        val commandBytes = MyRvLinkCommandEncoder.encodeActionDimmable(
            commandId = commandId,
            deviceTableId = tableId,
            deviceId = deviceId,
            command = command,
            brightness = brightness
        )

        val encoded = CobsDecoder.encode(commandBytes, prependStartFrame = true, useCrc = true)
        writeChar.value = encoded
        writeChar.writeType = BluetoothGattCharacteristic.WRITE_TYPE_DEFAULT

        val writeResult = bluetoothGatt?.writeCharacteristic(writeChar)
        Log.i(
            TAG,
            "📤 Dimmable cmd: cmd=${command.name}, DeviceId=$deviceId, Brightness=$brightness, CommandId=0x${commandId.toString(16)}, writeResult=$writeResult"
        )
        broadcastLog("📤 Light $deviceId (${command.name}): $brightness")
    }
    
    /**
     * Get current device status
     * @param deviceId Device ID
     * @return DeviceStatus or null if not found
     */
    fun getDeviceStatus(deviceId: Byte): DeviceStatus? {
        return deviceStatuses.values.find { it.deviceId == deviceId }
    }

    /**
     * Get all device statuses
     */
    fun getAllDeviceStatuses(): Map<String, DeviceStatus> {
        return deviceStatuses.toMap()
    }
}

sealed class DeviceStatus {
    abstract val deviceTableId: Byte
    abstract val deviceId: Byte

    data class Relay(
        override val deviceTableId: Byte,
        override val deviceId: Byte,
        val isOn: Boolean
    ) : DeviceStatus()

    data class DimmableLight(
        override val deviceTableId: Byte,
        override val deviceId: Byte,
        val isOn: Boolean,
        val brightness: Int
    ) : DeviceStatus()

    data class RgbLight(
        override val deviceTableId: Byte,
        override val deviceId: Byte,
        val statusBytes: ByteArray
    ) : DeviceStatus() {
        override fun equals(other: Any?): Boolean {
            if (this === other) return true
            if (javaClass != other?.javaClass) return false

            other as RgbLight

            if (deviceTableId != other.deviceTableId) return false
            if (deviceId != other.deviceId) return false
            if (!statusBytes.contentEquals(other.statusBytes)) return false

            return true
        }

        override fun hashCode(): Int {
            var result = deviceTableId.toInt()
            result = 31 * result + deviceId.toInt()
            result = 31 * result + statusBytes.contentHashCode()
            return result
        }
    }

    data class Cover(
        override val deviceTableId: Byte,
        override val deviceId: Byte
    ) : DeviceStatus()
}
