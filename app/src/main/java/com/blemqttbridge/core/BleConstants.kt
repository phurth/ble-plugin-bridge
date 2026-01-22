package com.blemqttbridge.core

/**
 * Central location for BLE timing and configuration constants.
 * Extracted to eliminate magic numbers and improve maintainability.
 */
object BleConstants {
    
    // Connection timing
    const val BLE_RECONNECT_DELAY_MS = 2000L
    const val BLE_SETTLE_DELAY_MS = 500L
    const val BLE_CONNECTION_DELAY_MS = 100L
    
    // GATT operation timeouts
    const val GATT_READ_TIMEOUT_MS = 10000L
    const val GATT_WRITE_TIMEOUT_MS = 5000L
    
    // Client Characteristic Configuration Descriptor UUID
    // Used to enable notifications on characteristics
    const val CCCD_UUID = "00002902-0000-1000-8000-00805f9b34fb"
    
    // Connection handling
    const val MAX_GATT_RETRIES = 3
    const val GATT_133_RETRY_DELAY_MS = 2000L
    
    // Bonding/Pairing
    const val PAIRING_RETRY_DELAY_MS = 1000L
    
    // Service discovery
    const val SERVICE_DISCOVERY_RETRY_DELAY_MS = 2000L
    
    // Keep-alive and heartbeat
    const val KEEPALIVE_INTERVAL_MS = 30 * 60 * 1000L  // 30 minutes
}

/**
 * Logging and diagnostics constants
 */
object DiagnosticsConstants {
    const val MAX_DEBUG_LOG_LINES = 2000
    const val MAX_SERVICE_LOG_LINES = 1000
    const val MAX_BLE_TRACE_LINES = 1000
    const val TRACE_MAX_DURATION_MS = 10 * 60 * 1000L  // 10 minutes
}
