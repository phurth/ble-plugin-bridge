package com.blemqttbridge.plugins.blescanner

import android.bluetooth.BluetoothAdapter
import android.bluetooth.BluetoothManager
import android.bluetooth.le.BluetoothLeScanner
import android.bluetooth.le.ScanCallback
import android.bluetooth.le.ScanResult
import android.bluetooth.le.ScanSettings
import android.content.Context
import android.os.Handler
import android.os.Looper
import android.provider.Settings
import android.util.Log
import com.blemqttbridge.core.interfaces.MqttPublisher
import org.json.JSONArray
import org.json.JSONObject

/**
 * BLE Scanner Plugin - Proof of Concept
 * 
 * Demonstrates plugin architecture with:
 * - Independent BLE scanning (not connected to any device)
 * - MQTT command handling (button to trigger scan)
 * - HA discovery for its own device
 * - State publishing (scanning status, discovered devices)
 */
class BleScannerPlugin(
    private val context: Context,
    private val mqttPublisher: MqttPublisher
) {
    companion object {
        private const val TAG = "BleScannerPlugin"
        const val PLUGIN_ID = "blescanner"  // Must match web UI and ServiceStateManager
        private const val SCAN_DURATION_MS = 60_000L // 60 seconds
        
        // HA Device Info
        private const val DEVICE_NAME = "BLE Scanner"
        private const val DEVICE_MANUFACTURER = "phurth"
        private const val DEVICE_MODEL = "BLE scanner plugin for the Android BLE to MQTT Bridge"
        
        /**
         * Get a unique device suffix based on Bluetooth adapter MAC address.
         * This is stable across app upgrades, unlike Android ID which can change.
         * Returns last 6 characters to keep identifiers short.
         */
        private fun getDeviceSuffix(context: Context): String {
            return try {
                val bluetoothAdapter = android.bluetooth.BluetoothAdapter.getDefaultAdapter()
                val btMac = bluetoothAdapter?.address
                
                // Always use BT MAC if available - it's stable and device-specific
                // The BT MAC is the physical radio address and won't change with Android system updates
                if (!btMac.isNullOrEmpty() && btMac != "02:00:00:00:00:00") {
                    // Use last 6 chars of BT MAC (e.g., "0c9919" from "C0:09:25:0C:99:19")
                    val suffix = btMac.replace(":", "").takeLast(6).lowercase()
                    Log.d(TAG, "Using Bluetooth MAC suffix for device identification: $suffix")
                    suffix
                } else {
                    // Fallback only if BT MAC truly unavailable
                    // This should rarely happen, but provides safety net
                    Log.w(TAG, "Bluetooth MAC unavailable (btMac=$btMac), falling back to Android ID")
                    val androidId = Settings.Secure.getString(
                        context.contentResolver,
                        Settings.Secure.ANDROID_ID
                    )
                    val suffix = androidId?.takeLast(6)?.lowercase() ?: "unknown"
                    Log.w(TAG, "Using Android ID suffix (temporary fallback): $suffix")
                    suffix
                }
            } catch (e: Exception) {
                Log.e(TAG, "Failed to get device suffix, using 'unknown' fallback", e)
                "unknown"
            }
        }
    }
    
    private val handler = Handler(Looper.getMainLooper())
    private var bluetoothAdapter: BluetoothAdapter? = null
    private var bleScanner: BluetoothLeScanner? = null
    
    private var isScanning = false
    private val discoveredDevices = mutableMapOf<String, DiscoveredDevice>()
    
    data class DiscoveredDevice(
        val mac: String,
        val name: String?,
        val rssi: Int,
        val lastSeen: Long
    )
    
    private val scanCallback = object : ScanCallback() {
        override fun onScanResult(callbackType: Int, result: ScanResult) {
            val device = result.device
            val mac = device.address
            val name = device.name ?: result.scanRecord?.deviceName
            
            discoveredDevices[mac] = DiscoveredDevice(
                mac = mac,
                name = name,
                rssi = result.rssi,
                lastSeen = System.currentTimeMillis()
            )
            
            Log.d(TAG, "Discovered: $mac - ${name ?: "Unknown"} (RSSI: ${result.rssi})")
        }
        
        override fun onScanFailed(errorCode: Int) {
            Log.e(TAG, "Scan failed with error: $errorCode")
            isScanning = false
            publishScanningState()
        }
    }
    
    private val stopScanRunnable = Runnable {
        stopScan()
    }
    
    /**
     * Initialize the scanner plugin.
     */
    fun initialize(): Boolean {
        val bluetoothManager = context.getSystemService(Context.BLUETOOTH_SERVICE) as? BluetoothManager
        bluetoothAdapter = bluetoothManager?.adapter
        bleScanner = bluetoothAdapter?.bluetoothLeScanner
        
        if (bleScanner == null) {
            Log.e(TAG, "BLE Scanner not available")
            return false
        }
        
        Log.i(TAG, "✅ BLE Scanner Plugin initialized")
        
        // Publish HA discovery
        publishDiscovery()
        
        // Subscribe to scan command
        subscribeToCommands()
        
        // Publish initial state
        publishScanningState()
        publishDeviceList()
        
        // Schedule periodic publishing of device list (every 5 seconds)
        // This allows Home Assistant to see discovered devices in real-time
        handler.postDelayed(periodicPublishRunnable, 5000)
        
        return true
    }
    
    /**
     * Receive a scan result from the BLE service's shared scanner.
     * This is called by BaseBleService when it discovers a BLE device.
     */
    fun onScanResult(device: android.bluetooth.BluetoothDevice, rssi: Int, name: String?) {
        val mac = device.address
        discoveredDevices[mac] = DiscoveredDevice(
            mac = mac,
            name = name ?: device.name,
            rssi = rssi,
            lastSeen = System.currentTimeMillis()
        )
        Log.d(TAG, "📡 Received scan result: $mac - ${name ?: device.name ?: "Unknown"} (RSSI: $rssi)")
        
        // Update plugin status to show we're receiving scan results
        mqttPublisher.updatePluginStatus(PLUGIN_ID, connected = true, authenticated = true, dataHealthy = true)
    }
    
    /**
     * Start BLE scanning for SCAN_DURATION_MS.
     */
    @Suppress("MissingPermission")
    fun startScan() {
        if (isScanning) {
            Log.w(TAG, "Already scanning")
            return
        }
        
        val scanner = bleScanner
        if (scanner == null) {
            Log.e(TAG, "Scanner not available")
            return
        }
        
        // Clear previous results
        discoveredDevices.clear()
        
        val settings = ScanSettings.Builder()
            .setScanMode(ScanSettings.SCAN_MODE_LOW_LATENCY)
            .build()
        
        try {
            scanner.startScan(null, settings, scanCallback)
            isScanning = true
            Log.i(TAG, "🔍 Started BLE scan for ${SCAN_DURATION_MS / 1000}s")
            
            publishScanningState()
            
            // Schedule stop
            handler.postDelayed(stopScanRunnable, SCAN_DURATION_MS)
            
            // Publish device list periodically during scan
            schedulePeriodicPublish()
            
        } catch (e: SecurityException) {
            Log.e(TAG, "Permission denied for BLE scan", e)
        }
    }
    
    /**
     * Stop BLE scanning.
     */
    @Suppress("MissingPermission")
    fun stopScan() {
        if (!isScanning) return
        
        handler.removeCallbacks(stopScanRunnable)
        handler.removeCallbacks(periodicPublishRunnable)
        
        try {
            bleScanner?.stopScan(scanCallback)
        } catch (e: Exception) {
            Log.e(TAG, "Error stopping scan", e)
        }
        
        isScanning = false
        Log.i(TAG, "⏹️ Stopped BLE scan. Found ${discoveredDevices.size} devices")
        
        publishScanningState()
        publishDeviceList()
    }
    
    private val periodicPublishRunnable = object : Runnable {
        override fun run() {
            // Always publish device list (not just during active scan)
            // This keeps Home Assistant updated with discovered devices
            publishDeviceList()
            handler.postDelayed(this, 5000) // Update every 5 seconds
        }
    }
    
    private fun schedulePeriodicPublish() {
        handler.postDelayed(periodicPublishRunnable, 5000)
    }
    
    /**
     * Clean up resources.
     */
    fun cleanup() {
        stopScan()
        handler.removeCallbacks(periodicPublishRunnable)  // Stop periodic publishing
        Log.i(TAG, "BLE Scanner Plugin cleaned up")
    }
    
    // ==================== MQTT PUBLISHING ====================
    
    private fun publishScanningState() {
        val state = if (isScanning) "ON" else "OFF"
        mqttPublisher.publishState("ble_scanner/scanning/state", state, retained = true)
        Log.d(TAG, "Published scanning state: $state")
    }
    
    private fun publishDeviceList() {
        val count = discoveredDevices.size
        
        // Build JSON array of devices
        val devicesJson = JSONArray()
        discoveredDevices.values.forEach { device ->
            val obj = JSONObject().apply {
                put("mac", device.mac)
                put("name", device.name ?: "Unknown")
                put("rssi", device.rssi)
            }
            devicesJson.put(obj)
        }
        
        // Publish sensor state (device count) with attributes
        val stateJson = JSONObject().apply {
            put("count", count)
            put("devices", devicesJson)
        }
        
        mqttPublisher.publishState("ble_scanner/devices/state", stateJson.toString(), retained = true)
        Log.d(TAG, "Published device list: $count devices")
    }
    
    private fun subscribeToCommands() {
        mqttPublisher.subscribeToCommands("ble_scanner/scan/set") { _, payload ->
            Log.i(TAG, "Received scan command: $payload")
            when (payload.uppercase()) {
                "PRESS", "ON", "1" -> startScan()
                "OFF", "0" -> stopScan()
            }
        }
        Log.i(TAG, "Subscribed to scan commands")
    }
    
    // ==================== HA DISCOVERY ====================
    
    private fun publishDiscovery() {
        val topicPrefix = mqttPublisher.topicPrefix
        val deviceSuffix = getDeviceSuffix(context)
        val deviceId = "ble_scanner_${deviceSuffix}"
        
        // Get app version
        val appVersion = try {
            context.packageManager.getPackageInfo(context.packageName, 0).versionName ?: "unknown"
        } catch (e: Exception) {
            "unknown"
        }
        
        // Device info for all entities
        val deviceInfo = JSONObject().apply {
            put("identifiers", JSONArray().put(deviceId))
            put("name", "$DEVICE_NAME ${deviceSuffix.uppercase()}")
            put("manufacturer", DEVICE_MANUFACTURER)
            put("model", DEVICE_MODEL)
            put("sw_version", appVersion)
        }
        
        // 1. Scanning binary sensor
        val scanningDiscovery = JSONObject().apply {
            put("name", "Scanning")
            put("unique_id", "${deviceId}_scanning")
            put("device", deviceInfo)
            put("state_topic", "$topicPrefix/ble_scanner/scanning/state")
            put("payload_on", "ON")
            put("payload_off", "OFF")
            put("device_class", "running")
            put("icon", "mdi:bluetooth-search")
        }
        mqttPublisher.publishDiscovery(
            "$topicPrefix/binary_sensor/$deviceId/scanning/config",
            scanningDiscovery.toString()
        )
        
        // 2. Device count sensor with attributes
        val devicesDiscovery = JSONObject().apply {
            put("name", "Devices Found")
            put("unique_id", "${deviceId}_devices")
            put("device", deviceInfo)
            put("state_topic", "$topicPrefix/ble_scanner/devices/state")
            put("value_template", "{{ value_json.count }}")
            put("json_attributes_topic", "$topicPrefix/ble_scanner/devices/state")
            put("json_attributes_template", "{{ {'devices': value_json.devices} | tojson }}")
            put("unit_of_measurement", "devices")
            put("icon", "mdi:bluetooth")
        }
        mqttPublisher.publishDiscovery(
            "$topicPrefix/sensor/$deviceId/devices/config",
            devicesDiscovery.toString()
        )
        
        // 3. Scan button
        val buttonDiscovery = JSONObject().apply {
            put("name", "Start Scan")
            put("unique_id", "${deviceId}_scan_button")
            put("device", deviceInfo)
            put("command_topic", "$topicPrefix/ble_scanner/scan/set")
            put("payload_press", "PRESS")
            put("icon", "mdi:magnify")
        }
        mqttPublisher.publishDiscovery(
            "$topicPrefix/button/${deviceId}_scan_button/config",
            buttonDiscovery.toString()
        )
        
        Log.i(TAG, "✅ Published HA discovery for BLE Scanner")
    }
}
