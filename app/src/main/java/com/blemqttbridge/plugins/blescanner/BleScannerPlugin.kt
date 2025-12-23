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
        const val PLUGIN_ID = "ble_scanner"
        private const val SCAN_DURATION_MS = 60_000L // 60 seconds
        
        // HA Device Info
        private const val DEVICE_ID = "ble_scanner"
        private const val DEVICE_NAME = "BLE Scanner"
        private const val DEVICE_MANUFACTURER = "BLE MQTT Bridge"
        private const val DEVICE_MODEL = "Scanner Plugin"
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
        
        return true
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
            if (isScanning) {
                publishDeviceList()
                handler.postDelayed(this, 5000) // Update every 5 seconds during scan
            }
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
        
        // Device info for all entities
        val deviceInfo = JSONObject().apply {
            put("identifiers", JSONArray().put(DEVICE_ID))
            put("name", DEVICE_NAME)
            put("manufacturer", DEVICE_MANUFACTURER)
            put("model", DEVICE_MODEL)
            put("sw_version", "1.0.0")
        }
        
        // 1. Scanning binary sensor
        val scanningDiscovery = JSONObject().apply {
            put("name", "Scanning")
            put("unique_id", "${DEVICE_ID}_scanning")
            put("device", deviceInfo)
            put("state_topic", "$topicPrefix/ble_scanner/scanning/state")
            put("payload_on", "ON")
            put("payload_off", "OFF")
            put("device_class", "running")
            put("icon", "mdi:bluetooth-search")
        }
        mqttPublisher.publishDiscovery(
            "$topicPrefix/binary_sensor/$DEVICE_ID/scanning/config",
            scanningDiscovery.toString()
        )
        
        // 2. Device count sensor with attributes
        val devicesDiscovery = JSONObject().apply {
            put("name", "Devices Found")
            put("unique_id", "${DEVICE_ID}_devices")
            put("device", deviceInfo)
            put("state_topic", "$topicPrefix/ble_scanner/devices/state")
            put("value_template", "{{ value_json.count }}")
            put("json_attributes_topic", "$topicPrefix/ble_scanner/devices/state")
            put("json_attributes_template", "{{ {'devices': value_json.devices} | tojson }}")
            put("unit_of_measurement", "devices")
            put("icon", "mdi:bluetooth")
        }
        mqttPublisher.publishDiscovery(
            "$topicPrefix/sensor/$DEVICE_ID/devices/config",
            devicesDiscovery.toString()
        )
        
        // 3. Scan button
        val buttonDiscovery = JSONObject().apply {
            put("name", "Start Scan")
            put("unique_id", "${DEVICE_ID}_scan_button")
            put("device", deviceInfo)
            put("command_topic", "$topicPrefix/ble_scanner/scan/set")
            put("payload_press", "PRESS")
            put("icon", "mdi:magnify")
        }
        mqttPublisher.publishDiscovery(
            "$topicPrefix/button/$DEVICE_ID/scan/config",
            buttonDiscovery.toString()
        )
        
        Log.i(TAG, "✅ Published HA discovery for BLE Scanner")
    }
}
