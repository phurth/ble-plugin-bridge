package com.blemqttbridge.plugins.onecontrol.protocol

/**
 * Constants for OneControl BLE Gateway integration
 * Based on analysis of C# decompiled code and Python integration
 */
object Constants {
    // BLE Service UUIDs
    const val AUTH_SERVICE_UUID = "00000010-0200-a58e-e411-afe28044e62c"
    const val SEED_CHAR_UUID = "00000011-0200-a58e-e411-afe28044e62c"  // SEED for TEA encryption
    const val UNLOCK_STATUS_CHAR_UUID = "00000012-0200-a58e-e411-afe28044e62c"  // Unlock status (reads "Unlocked" after KEY write)
    const val KEY_CHAR_UUID = "00000013-0200-a58e-e411-afe28044e62c"  // KEY for authentication/data mode
    const val AUTH_STATUS_CHAR_UUID = "00000014-0200-a58e-e411-afe28044e62c"  // Auth Service status (READ, NOTIFY)
    
    // CAN Service (contains unlock characteristic and CAN read/write)
    const val CAN_SERVICE_UUID = "00000000-0200-a58e-e411-afe28044e62c"
    const val UNLOCK_CHAR_UUID = "00000005-0200-a58e-e411-afe28044e62c"
    const val CAN_WRITE_CHAR_UUID = "00000001-0200-a58e-e411-afe28044e62c"  // CAN TX (write to gateway)
    const val CAN_READ_CHAR_UUID = "00000002-0200-a58e-e411-afe28044e62c"   // CAN RX (read from gateway)
    
    // Data Service (for CAN-over-BLE communication - alternative/legacy?)
    const val DATA_SERVICE_UUID = "00000030-0200-a58e-e411-afe28044e62c"
    const val DATA_WRITE_CHAR_UUID = "00000033-0200-a58e-e411-afe28044e62c"
    const val DATA_READ_CHAR_UUID = "00000034-0200-a58e-e411-afe28044e62c"
    
    // Discovery Service (in advertisements)
    const val DISCOVERY_SERVICE_UUID = "00000041-0200-a58e-e411-afe28044e62c"
    
    // Manufacturer ID (Lippert Components)
    const val LCI_MANUFACTURER_ID = 1479  // 0x05C7
    
    // TEA Encryption Constants
    const val TEA_DELTA = 0x9E3779B9L
    const val TEA_CONSTANT_1 = 0x43729561L
    const val TEA_CONSTANT_2 = 0x7265746EL
    const val TEA_CONSTANT_3 = 0x7421ED44L
    const val TEA_CONSTANT_4 = 0x5378A963L
    const val TEA_ROUNDS = 32
    
    // Connection timeouts (milliseconds)
    const val CONNECTION_TIMEOUT_MS = 30000L
    const val PAIRING_TIMEOUT_MS = 30000L
    const val AUTH_TIMEOUT_MS = 10000L
    const val UNLOCK_TIMEOUT_MS = 5000L
    
    // Delays (milliseconds)
    const val BOND_SETTLE_DELAY_MS = 2000L
    const val UNLOCK_VERIFY_DELAY_MS = 1000L
    const val SERVICE_DISCOVERY_DELAY_MS = 500L
    
    // MTU size
    const val BLE_MTU_SIZE = 185
    
    // MQTT defaults (can be configured)
    const val MQTT_DEFAULT_BROKER = "homeassistant.local"
    const val MQTT_DEFAULT_PORT = 1883
    const val MQTT_DEFAULT_TOPIC_PREFIX = "onecontrol/ble"
}

