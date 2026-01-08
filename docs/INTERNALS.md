# BLE Plugin Bridge - Internal Architecture Documentation

> **Purpose:** This document provides comprehensive technical documentation for the BLE Plugin Bridge Android application. It is designed to enable future LLM-assisted development, particularly for adding new entity types to the OneControl plugin or creating entirely new device plugins.

> **Current Version:** v2.4.9  
> **Last Updated:** January 7, 2026  
> **Version History:** See [GitHub Releases](https://github.com/phurth/ble-plugin-bridge/releases) for complete changelog

---

## Table of Contents

0. [Quick Reference for LLMs](#0-quick-reference-for-llms)
   - [Common Tasks](#common-tasks)
   - [Key Files](#key-files)
   - [Recent Critical Changes](#recent-critical-changes)

1. [High-Level Architecture](#1-high-level-architecture)
   - [Overview](#overview)
   - [Key Principles](#key-principles)

2. [Service Layer](#2-service-layer)
   - [BaseBleService](#basebleservice)
   - [StateFlow Companion Objects](#stateflow-companion-objects)
   - [MqttPublisher Implementation](#mqttpublisher-implementation)
   - [Connection Flow](#connection-flow)

3. [Plugin System](#3-plugin-system)
   - [Plugin Interfaces](#plugin-interfaces)
   - [BleDevicePlugin (New Architecture)](#bledeviceplugin-new-architecture)
   - [MqttPublisher Interface](#mqttpublisher-interface)
   - [PluginRegistry](#pluginregistry)

4. [OneControl Protocol Deep Dive](#4-onecontrol-protocol-deep-dive)
   - [Overview](#overview-1)
   - [File Structure](#file-structure)
   - [BLE Service UUIDs](#ble-service-uuids)
   - [Authentication Flow](#authentication-flow)
   - [TEA Encryption](#tea-encryption-authentication-key-calculation)
   - [COBS Framing](#cobs-framing)
   - [Stream Reading](#stream-reading)
   - [Event Types (MyRvLink Protocol)](#event-types-myrvlink-protocol)
   - [Event Parsing Examples](#event-parsing-examples)
     - [Relay Status](#relay-status)
     - [Dimmable Light](#dimmable-light)
     - [Tank Sensors](#tank-sensors)
     - [Cover/Slide/Awning Sensors](#coverslideawning-sensors-state-only)
   - [Command Building](#command-building)
   - [Command Handling](#command-handling)
   - [Guard Checks in republishDiscoveryWithFriendlyName()](#guard-checks-in-republishdiscoverywithfriendlyname)
   - [Friendly Name Race Condition](#friendly-name-race-condition)
   - [Device Metadata Retrieval](#device-metadata-retrieval-getdevicesmetadata)

5. [EasyTouch Thermostat Protocol](#5-easytouch-thermostat-protocol)
   - [Overview](#overview-2)
   - [File Structure](#file-structure-1)
   - [BLE Service UUIDs](#ble-service-uuids-1)
   - [Authentication Flow](#authentication-flow-1)
   - [Read-After-Write Communication Pattern](#read-after-write-communication-pattern)
   - [Capability Discovery](#capability-discovery-get-config)
   - [MAV & SPL Arrays](#mav-mode-available-bitmask)
   - [Write Retry with Delays](#write-retry-with-delays)
   - [JSON Command Format](#json-command-format)
   - [Status Response Format](#status-response-format)
   - [Home Assistant Integration](#home-assistant-integration)
   - [Command Handling](#command-handling-1)
   - [Key Differences from OneControl](#key-differences-from-onecontrol)

6. [GoPower Solar Controller Protocol](#6-gopower-solar-controller-protocol)
   - [Overview](#overview-3)
   - [Protocol Characteristics](#protocol-characteristics)
   - [Notification Data Format](#notification-data-format)
   - [Key Implementation Details](#key-implementation-details)
   - [Home Assistant Entities](#home-assistant-entities)
   - [Plugin Structure](#plugin-structure)
   - [Configuration Requirements](#configuration-requirements)
   - [Reboot Command](#reboot-command)
   - [Diagnostic Sensors](#diagnostic-sensors)
   - [Troubleshooting](#troubleshooting)
   - [Known Limitations](#known-limitations)

7. [MQTT Integration](#7-mqtt-integration)
   - [MqttOutputPlugin](#mqttoutputplugin)
   - [Topic Structure](#topic-structure-v248)
   - [Graceful Error Handling](#graceful-error-handling)
   - [Multi-Gateway Support](#75-multi-gateway-support-v248)
     - [Overview](#overview-4)
     - [Device Identification Strategy](#device-identification-strategy)
     - [Implementation](#implementation)
     - [System Diagnostics](#system-diagnostics-in-multi-gateway-setup)
     - [Deployment Example](#multi-gateway-deployment-example)
     - [Migration Notes](#migration-notes)

8. [Home Assistant Discovery](#8-home-assistant-discovery)
   - [Discovery Payload Format](#discovery-payload-format)
   - [Device Info (Grouping)](#device-info-grouping)
   - [Discovery Topic Pattern](#discovery-topic-pattern)
   - [Entity Model Architecture](#entity-model-architecture-phase-3-refactoring)
   - [Using Entity Models in Event Handlers](#using-entity-models-in-event-handlers)
   - [Command Handling with Entity Models](#command-handling-with-entity-models)
   - [Step-by-Step: Adding a New Entity Type](#step-by-step-adding-a-new-entity-type-example-rgb-light)

9. [State Management & Status Indicators](#9-state-management--status-indicators)
   - [Per-Plugin Status Architecture](#per-plugin-status-architecture-v231)
   - [MQTT Diagnostic Sensors](#mqtt-diagnostic-sensors-per-plugin)
   - [SettingsViewModel UI](#settingsviewmodel-ui-per-plugin-status-v232)
   - [SettingsScreen Indicators](#settingsscreen-per-plugin-indicators)
   - [BLE Scanner Conditional Initialization](#ble-scanner-conditional-initialization)
   - [System Diagnostic Sensors](#system-diagnostic-sensors)
   - [Settings Screen](#settings-screen)

10. [Creating New Plugins](#10-creating-new-plugins)
    - [Plugin Template](#plugin-template)
    - [Register the Plugin](#register-the-plugin)

11. [Debug Logging & Performance](#11-debug-logging--performance)
    - [Overview](#overview-5)
    - [DebugLog Utility](#debuglog-utility)
    - [Usage Guidelines](#usage-guidelines)
    - [BLE Trace Capture](#ble-trace-capture)
    - [Performance Impact](#performance-impact)
    - [Emoji Removal](#emoji-removal)
    - [Best Practices](#best-practices)

12. [Common Pitfalls & Solutions](#12-common-pitfalls--solutions)
    - [MQTT Publish Exceptions](#1-mqtt-publish-exceptions)
    - [Stale Status Indicators](#2-stale-status-indicators)
    - [BLE GATT Error 133](#3-ble-gatt-error-133)
    - [Wrong Byte Order](#4-wrong-byte-order)
    - [Missing COBS Encoding](#5-missing-cobs-encoding)
    - [Notification Subscription Race Condition](#6-notification-subscription-race-condition)
    - [Dimmable Bouncing](#7-dimmable-bouncing)
    - [BLE Trace Logging](#8-ble-trace-logging)

13. [Background Operation](#13-background-operation)
    - [Battery Optimization & Background Execution](#battery-optimization--background-execution)
    - [Defense Layers](#defense-layers)
    - [Recommended Settings](#recommended-settings)

- [Appendix: File Quick Reference](#appendix-file-quick-reference)

---

## 0. Quick Reference for LLMs

### Common Tasks

- **Adding new OneControl entity type:** See [Section 11](#11-adding-new-entity-types)
- **Creating new plugin:** See [Section 12](#12-creating-new-plugins)
- **Understanding MQTT topics:** See [Section 7.5 (Multi-Gateway)](#75-multi-gateway-support-v248) and [Section 7](#7-mqtt-integration)
- **Debugging connection issues:** See [Section 13 (Common Pitfalls)](#13-common-pitfalls)
- **Multi-gateway deployment:** See [Section 7.5](#75-multi-gateway-support-v248)

### Key Files

- **Gateway device:** `MqttOutputPlugin.kt` (includes multi-gateway device ID suffix)
- **Scanner device:** `BleScannerPlugin.kt` (includes multi-gateway device ID suffix)
- **OneControl:** `OneControlDevicePlugin.kt`
- **EasyTouch:** `EasyTouchDevicePlugin.kt`
- **GoPower:** `GoPowerDevicePlugin.kt`
- **Entity models:** `OneControlEntity.kt`
- **Service layer:** `BaseBleService.kt`

### Recent Critical Changes

**v2.4.9 (January 2026):**
- **EasyTouch Watchdog Fix:** Fixed false "stale connection" detection causing disconnects every 5 minutes
  - Root cause: `lastSuccessfulOperationTime` not updated during status polling
  - Status responses now update timestamp to prevent watchdog false positives
  - Watchdog only triggers on genuine zombie states or actual stale connections (5+ min with no responses)
- **EasyTouch Setpoint Fixes:** Resolved temperature setpoint changes reverting in Home Assistant
  - Implemented optimistic state updates for instant UI feedback
  - Extended suppression window from 2s to 8s (skips 2 polling cycles instead of 1)
  - Added verification status read 4s after command to confirm change applied
  - Fixed float parsing: Home Assistant sends "64.0", code now handles floats via `toFloatOrNull()?.toInt()`
- **Watchdog Infinite Loop Fix:** Prevented zombie state cleanup from rescheduling itself
  - Added `shouldContinue` flag to break loop when cleanup() is triggered
  - Watchdog now stops cleanly after detecting and resolving zombie states
- Connection robustness improvements: Added GATT 133 retry logic to GoPower and EasyTouch
- Per-plugin watchdog: Added connection health monitoring and zombie state detection
- Persistent metadata cache: OneControl friendly names survive app restarts

**v2.4.8 (January 2026):**
- Multi-gateway support: Gateway and scanner devices now use Android device ID suffix
- OneControl guard check fix: Restored guards preventing 4x entity duplication
- Device IDs now include suffix: `ble_mqtt_bridge_{android_id}`, `ble_scanner_{android_id}`
- EasyTouch fan mode fix: Fixed "Invalid fan_modes mode: off" error

**v2.4.7 (January 2026):**
- BLE notification race condition fix (servicesDiscovered/mtuReady flags)
- BLE trace logging for all plugins

**v2.4.6 (January 2026):**
- Android TV power fix (prevents service kill on TV standby)

---

## 1. High-Level Architecture

### Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        Android App                               │
├─────────────────────────────────────────────────────────────────┤
│  UI Layer                                                        │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │  SettingsScreen (Compose)  ←──  SettingsViewModel           ││
│  │       │                              │                       ││
│  │       │  collects StateFlows         │  collects from        ││
│  │       │                              │  BaseBleService       ││
│  │       ▼                              ▼                       ││
│  │  Status Indicators: BLE, MQTT, Data Healthy                  ││
│  └─────────────────────────────────────────────────────────────┘│
├─────────────────────────────────────────────────────────────────┤
│  Service Layer                                                   │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │  BaseBleService (Foreground Service)                        ││
│  │    ├── PluginRegistry                                       ││
│  │    ├── MqttPublisher (interface impl)                       ││
│  │    ├── BLE Scanning                                         ││
│  │    └── Device Connection Management                         ││
│  └─────────────────────────────────────────────────────────────┘│
├─────────────────────────────────────────────────────────────────┤
│  Plugin Layer                                                    │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │ OneControlDevice│  │ BleScannerPlugin│  │ MqttOutputPlugin│ │
│  │ Plugin          │  │                 │  │                 │ │
│  │   │             │  │                 │  │                 │ │
│  │   ▼             │  │                 │  │                 │ │
│  │ OneControlGatt  │  │                 │  │                 │ │
│  │ Callback        │  │                 │  │                 │ │
│  │   │             │  │                 │  │                 │ │
│  │   ▼             │  │                 │  │                 │ │
│  │ Protocol Classes│  │                 │  │                 │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                     External Systems                             │
│  ┌──────────────────┐              ┌──────────────────────────┐ │
│  │ OneControl BLE   │              │ MQTT Broker              │ │
│  │ Gateway (RV)     │              │ (Home Assistant)         │ │
│  └──────────────────┘              └──────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

### Key Principles

1. **Plugin Ownership of GATT Callback**: Each `BleDevicePlugin` creates and owns its `BluetoothGattCallback`. The service does NOT forward callback events - the plugin receives them directly.

2. **Clean Interface Separation**: Plugins communicate state via `MqttPublisher` interface, which abstracts away MQTT details.

3. **StateFlow for UI**: All status indicators use Kotlin `StateFlow` from companion objects for thread-safe observation.

4. **Graceful Error Handling**: MQTT publish errors are caught and logged, never crashing the app.

---

## 2. Service Layer

### BaseBleService

**Location:** `app/src/main/java/com/blemqttbridge/core/BaseBleService.kt`

The foreground service that orchestrates everything.

#### StateFlow Companion Objects

```kotlin
companion object {
    // These are observed by SettingsViewModel for UI status indicators
    private val _serviceRunning = MutableStateFlow(false)
    val serviceRunning: StateFlow<Boolean> = _serviceRunning
    
    private val _bleConnected = MutableStateFlow(false)
    val bleConnected: StateFlow<Boolean> = _bleConnected
    
    private val _dataHealthy = MutableStateFlow(false)
    val dataHealthy: StateFlow<Boolean> = _dataHealthy
    
    private val _devicePaired = MutableStateFlow(false)
    val devicePaired: StateFlow<Boolean> = _devicePaired
    
    private val _mqttConnected = MutableStateFlow(false)
    val mqttConnected: StateFlow<Boolean> = _mqttConnected
}
```

**CRITICAL:** The ViewModel gates all status indicators by `serviceRunning` to prevent stale indicators when the service is stopped.

#### MqttPublisher Implementation

The service implements `MqttPublisher` interface which plugins use:

```kotlin
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
    
    override fun updateDiagnosticStatus(dataHealthy: Boolean) {
        _dataHealthy.value = dataHealthy
    }
    
    override fun updateBleStatus(connected: Boolean, paired: Boolean) {
        _bleConnected.value = connected
        _devicePaired.value = paired
    }
    
    override fun updateMqttStatus(connected: Boolean) {
        _mqttConnected.value = connected
    }
    
    override fun subscribeToCommands(topicPattern: String, callback: (topic: String, payload: String) -> Unit) {
        serviceScope.launch {
            outputPlugin?.subscribeToCommands(topicPattern, callback)
        }
    }
}
```

#### Connection Flow

1. Service starts → `initializePlugins()` loads plugins
2. `reconnectToBondedDevices()` checks for already-bonded devices
3. If bonded device found matching a plugin → direct `connectGatt()`
4. Otherwise → `startScanning()` for devices
5. On scan match → `connectToDevice()` with plugin-provided callback

```kotlin
private suspend fun connectToDevice(device: BluetoothDevice, pluginId: String) {
    val devicePlugin = pluginRegistry.getDevicePlugin(pluginId, applicationContext)
    
    val callback = if (devicePlugin != null) {
        // Plugin provides its own callback (NEW architecture)
        val config = PluginConfig(AppConfig.getBlePluginConfig(applicationContext, pluginId))
        devicePlugin.initialize(applicationContext, config)
        
        val onDisconnect: (BluetoothDevice, Int) -> Unit = { dev, status ->
            handleDeviceDisconnect(dev, status, pluginId)
        }
        
        devicePlugin.createGattCallback(device, applicationContext, mqttPublisher, onDisconnect)
    } else {
        // Legacy forwarding callback (OLD architecture)
        gattCallback
    }
    
    val gatt = device.connectGatt(this, false, callback, BluetoothDevice.TRANSPORT_LE)
    connectedDevices[device.address] = Pair(gatt, pluginId)
}
```

---

## 3. Plugin System

### Plugin Interfaces

**Location:** `app/src/main/java/com/blemqttbridge/core/interfaces/`

#### BleDevicePlugin (New Architecture)

```kotlin
interface BleDevicePlugin {
    val pluginId: String
    val displayName: String
    
    /**
     * Check if this plugin handles a given device
     */
    fun matchesDevice(device: BluetoothDevice, scanRecord: ByteArray?): Boolean
    
    /**
     * Get configured device addresses (for scan filters)
     */
    fun getConfiguredDevices(): List<String>
    
    /**
     * Create the GATT callback for device communication.
     * 
     * CRITICAL: Plugin owns this callback completely. All characteristic reads,
     * writes, and notifications are handled here without forwarding.
     */
    fun createGattCallback(
        device: BluetoothDevice,
        context: Context,
        mqttPublisher: MqttPublisher,
        onDisconnect: (BluetoothDevice, Int) -> Unit
    ): BluetoothGattCallback
    
    /**
     * Get MQTT base topic for this device
     * Example: "onecontrol/24:DC:C3:ED:1E:0A"
     */
    fun getMqttBaseTopic(device: BluetoothDevice): String
    
    /**
     * Handle MQTT command
     */
    fun handleCommand(
        device: BluetoothDevice,
        commandTopic: String,
        payload: String
    ): Result<Unit>
    
    /**
     * Lifecycle methods
     */
    fun initialize(context: Context, config: PluginConfig)
    fun destroy()
    fun onGattConnected(device: BluetoothDevice, gatt: BluetoothGatt)
}
```

#### MqttPublisher Interface

```kotlin
interface MqttPublisher {
    val topicPrefix: String  // Usually "homeassistant"
    
    fun publishState(topic: String, payload: String, retained: Boolean)
    fun publishDiscovery(topic: String, payload: String)
    fun publishAvailability(topic: String, online: Boolean)
    fun isConnected(): Boolean
    
    // Status updates for UI
    fun updateDiagnosticStatus(dataHealthy: Boolean)
    fun updateBleStatus(connected: Boolean, paired: Boolean)
    fun updateMqttStatus(connected: Boolean)
    
    // Command subscription
    fun subscribeToCommands(topicPattern: String, callback: (topic: String, payload: String) -> Unit)
}
```

### PluginRegistry

**Location:** `app/src/main/java/com/blemqttbridge/core/PluginRegistry.kt`

Lazy-loads plugins on demand. Factory registration pattern:

```kotlin
class PluginRegistry {
    // Plugin factories (registered at startup)
    private val devicePluginFactories = mutableMapOf<String, (Context) -> BleDevicePlugin>()
    
    init {
        // Register OneControl plugin factory
        registerDevicePluginFactory("onecontrol_v2") { ctx ->
            OneControlDevicePlugin()
        }
    }
    
    fun getDevicePlugin(pluginId: String, context: Context): BleDevicePlugin? {
        return devicePluginFactories[pluginId]?.invoke(context)
    }
    
    fun findPluginForDevice(device: BluetoothDevice, scanRecord: ByteArray?): String? {
        // Check all registered plugins to see if any match this device
        for ((pluginId, factory) in devicePluginFactories) {
            val plugin = factory(device.context)
            if (plugin.matchesDevice(device, scanRecord)) {
                return pluginId
            }
        }
        return null
    }
}
```

---

## 4. OneControl Protocol Deep Dive

### Overview

The OneControl BLE Gateway uses a custom protocol for RV device control. Understanding this is critical for adding new entity support.

### File Structure

```
app/src/main/java/com/blemqttbridge/plugins/onecontrol/
├── OneControlDevicePlugin.kt      # Plugin implementation (1958 lines)
└── protocol/
    ├── Constants.kt               # UUIDs, encryption constants
    ├── TeaEncryption.kt           # TEA encryption algorithm
    ├── CobsDecoder.kt             # COBS encoding (for commands)
    ├── CobsByteDecoder.kt         # COBS byte-by-byte decoding (for responses)
    ├── Crc8.kt                    # CRC8 checksum
    ├── MyRvLinkCommandBuilder.kt  # Build device commands
    └── HomeAssistantMqttDiscovery.kt  # HA discovery payloads
```

### BLE Service UUIDs

```kotlin
// Authentication Service (0x0010)
AUTH_SERVICE_UUID = "00000010-0200-a58e-e411-afe28044e62c"
SEED_CHARACTERISTIC_UUID = "00000011-..."     // READ, NOTIFY - challenge value
UNLOCK_STATUS_CHARACTERISTIC_UUID = "00000012-..."  // READ - "Unlocked" or challenge
KEY_CHARACTERISTIC_UUID = "00000013-..."      // WRITE_NO_RESPONSE - auth key

// Data Service (0x0030)
DATA_SERVICE_UUID = "00000030-0200-a58e-e411-afe28044e62c"
DATA_WRITE_CHARACTERISTIC_UUID = "00000033-..."  // WRITE - commands to gateway
DATA_READ_CHARACTERISTIC_UUID = "00000034-..."   // NOTIFY - events from gateway
```

### Authentication Flow

**File:** `OneControlDevicePlugin.kt`, method `checkAndStartAuthentication()`

**CRITICAL:** The authentication sequence requires careful synchronization between `onServicesDiscovered()` and `onMtuChanged()` callbacks, as they execute on different threads and can race:

```
1. onConnectionStateChange(STATE_CONNECTED)
   ↓
2. discoverServices()
   ↓
3. onServicesDiscovered() → cache characteristics, set servicesDiscovered = true
   ↓                         call checkAndStartAuthentication()
4. requestMtu(185)
   ↓
5. onMtuChanged() → set mtuReady = true
   ↓               call checkAndStartAuthentication()
6. checkAndStartAuthentication() → only proceeds if BOTH flags true
   ↓
7. Read UNLOCK_STATUS (00000012) 
   ↓
8. handleUnlockStatusRead() - receives 4-byte challenge
   ↓
9. calculateAuthKey(challenge) - BIG-ENDIAN TEA encryption
   ↓
10. Write KEY to 00000013 (WRITE_TYPE_NO_RESPONSE)
   ↓
11. Re-read UNLOCK_STATUS - should now return "Unlocked"
   ↓
12. enableDataNotifications() - subscribe to 00000034
   ↓
13. onAllNotificationsSubscribed() → startActiveStreamReading()
```

**Race Condition Fix (v2.4.7):** Prior to v2.4.7, `onMtuChanged()` would directly call `startAuthentication()`, but this could fire before `onServicesDiscovered()` completed caching characteristic references, causing notifications to fail silently. The synchronization flags ensure both callbacks complete before authentication begins.

#### TEA Encryption (Authentication Key Calculation)

**File:** `OneControlDevicePlugin.kt`, method `calculateAuthKey()`

```kotlin
private fun calculateAuthKey(seed: Long): ByteArray {
    val cypher = 612643285L  // 0x2483FFD5 - MyRvLink constant
    
    var cypherVar = cypher
    var seedVar = seed
    var num = 2654435769L  // TEA delta = 0x9E3779B9
    
    // 32 rounds of TEA encryption
    for (i in 0 until 32) {
        seedVar += ((cypherVar shl 4) + 1131376761L) xor 
                   (cypherVar + num) xor 
                   ((cypherVar shr 5) + 1919510376L)
        seedVar = seedVar and 0xFFFFFFFFL
        cypherVar += ((seedVar shl 4) + 1948272964L) xor 
                     (seedVar + num) xor 
                     ((seedVar shr 5) + 1400073827L)
        cypherVar = cypherVar and 0xFFFFFFFFL
        num += 2654435769L
        num = num and 0xFFFFFFFFL
    }
    
    // Return as BIG-ENDIAN bytes
    val result = seedVar.toInt()
    return byteArrayOf(
        ((result shr 24) and 0xFF).toByte(),
        ((result shr 16) and 0xFF).toByte(),
        ((result shr 8) and 0xFF).toByte(),
        ((result shr 0) and 0xFF).toByte()
    )
}
```

### COBS Framing

All data over the DATA_READ/DATA_WRITE characteristics uses COBS (Consistent Overhead Byte Stuffing) with CRC8.

**Encoding (for commands):**

```kotlin
// CobsDecoder.encode()
val rawCommand = MyRvLinkCommandBuilder.buildActionSwitch(...)
val encoded = CobsDecoder.encode(rawCommand, prependStartFrame = true, useCrc = true)
// Result: [0x00] [code_byte] [data...] [CRC8] [0x00]
```

**Decoding (for events):**

```kotlin
// CobsByteDecoder - stateful, accumulates bytes
val decoder = CobsByteDecoder(useCrc = true)

// In notification handler:
for (byte in notificationData) {
    val decodedFrame = decoder.decodeByte(byte)
    if (decodedFrame != null) {
        // Complete frame received
        processDecodedFrame(decodedFrame)
    }
}
```

### Stream Reading

**File:** `OneControlDevicePlugin.kt`, method `startActiveStreamReading()`

Notifications are queued and processed on a background thread:

```kotlin
private val notificationQueue = ConcurrentLinkedQueue<ByteArray>()
private var streamReadingThread: Thread? = null
private val cobsByteDecoder = CobsByteDecoder(useCrc = true)

private fun startActiveStreamReading() {
    streamReadingThread = Thread {
        while (!shouldStopStreamReading && isConnected) {
            synchronized(streamReadLock) {
                if (notificationQueue.isEmpty()) {
                    streamReadLock.wait(8000)  // 8s timeout
                }
            }
            
            while (notificationQueue.isNotEmpty()) {
                val notificationData = notificationQueue.poll() ?: continue
                
                // Feed bytes to COBS decoder
                for (byte in notificationData) {
                    val decodedFrame = cobsByteDecoder.decodeByte(byte)
                    if (decodedFrame != null) {
                        processDecodedFrame(decodedFrame)
                    }
                }
            }
        }
    }
}
```

### Event Types (MyRvLink Protocol)

**File:** `OneControlDevicePlugin.kt`, method `processDecodedFrame()`

The first byte of a decoded frame indicates the event type:

| Event Type | Hex  | Description | Handler Method |
|------------|------|-------------|----------------|
| GatewayInformation | 0x01 | Device table ID | `handleGatewayInformationEvent()` |
| DeviceCommand | 0x02 | Command acknowledgment | (logged only) |
| DeviceOnlineStatus | 0x03 | Device online/offline | `handleDeviceOnlineStatus()` |
| DeviceLockStatus | 0x04 | Device lock state | `handleDeviceLockStatus()` |
| RelayBasicLatching | 0x05, 0x06 | Switch/light ON/OFF | `handleRelayStatus()` |
| RvStatus | 0x07 | System voltage/temp | `handleRvStatus()` |
| DimmableLightStatus | 0x08 | Dimmer brightness | `handleDimmableLightStatus()` |
| HvacStatus | 0x0B | HVAC state | `handleHvacStatus()` |
| TankSensorStatus | 0x0C | Tank level | `handleTankStatus()` |
| RelayHBridgeMomentary | 0x0D, 0x0E | Cover/slide position | `handleHBridgeStatus()` |
| HourMeterStatus | 0x0F | Generator hours | `handleGenericEvent()` |
| Leveler4DeviceStatus | 0x10 | Leveling system | `handleGenericEvent()` |
| DeviceSessionStatus | 0x1A | Session heartbeat | (logged only) |
| TankSensorStatusV2 | 0x1B | Tank level v2 | `handleTankStatusV2()` |
| RealTimeClock | 0x20 | Gateway time | `handleRealTimeClock()` |

### Event Parsing Examples

#### Relay Status

```kotlin
private fun handleRelayStatus(data: ByteArray) {
    if (data.size < 5) return
    
    val tableId = data[1].toInt() and 0xFF    // Device table ID
    val deviceId = data[2].toInt() and 0xFF   // Device ID within table
    val statusByte = data[3].toInt() and 0xFF
    val rawOutputState = statusByte and 0x0F  // State in LOW nibble
    val isOn = rawOutputState == 0x01         // 0x01 = ON, 0x00 = OFF
    
    // Check for extended format with DTC data (9 bytes)
    val dtcCode = if (data.size >= 9) {
        ((data[5].toInt() and 0xFF) shl 8) or (data[6].toInt() and 0xFF)
    } else 0
    
    // Publish to MQTT
    val stateTopic = "onecontrol/${device.address}/device/$tableId/$deviceId/state"
    mqttPublisher.publishState(stateTopic, if (isOn) "ON" else "OFF", true)
    
    // Publish DTC attributes if present and device is gas appliance
    if (dtcCode != 0 && deviceName.contains("gas", ignoreCase = true)) {
        val attributes = JSONObject().apply {
            put("dtc_code", dtcCode)
            put("dtc_name", DtcCodes.getName(dtcCode))
            put("fault", DtcCodes.isFault(dtcCode))
            put("status_byte", "0x%02x".format(statusByte))
        }
        val attrTopic = "onecontrol/${device.address}/device/$tableId/$deviceId/attributes"
        mqttPublisher.publishState(attrTopic, attributes.toString(), true)
    }
    
    // Publish HA discovery if first time seeing this device
    val keyHex = "%02x%02x".format(tableId, deviceId)
    val discoveryKey = "switch_$keyHex"
    if (haDiscoveryPublished.add(discoveryKey)) {
        val attrTopic = if (dtcCode != 0 && deviceName.contains("gas", ignoreCase = true)) {
            "onecontrol/${device.address}/device/$tableId/$deviceId/attributes"
        } else null
        val discovery = HomeAssistantMqttDiscovery.getSwitchDiscovery(
            gatewayMac = device.address,
            deviceAddr = deviceId,
            deviceName = deviceName,
            stateTopic = stateTopic,
            commandTopic = commandTopic,
            attributesTopic = attrTopic,
            appVersion = appVersion
        )
        mqttPublisher.publishDiscovery(discoveryTopic, discovery.toString())
    }
}
```

**Diagnostic Trouble Codes (DTC):**

OneControl relay status messages come in two formats:

1. **Standard Format (5 bytes):**
   - Byte 0: Event type
   - Byte 1: Table ID
   - Byte 2: Device ID
   - Byte 3: Status byte (0x40=off, 0x41=on)
   - Byte 4: Reserved

2. **Extended Format (9 bytes):**
   - Bytes 0-4: Same as standard format
   - **Bytes 5-6: DTC code (16-bit big-endian)**
   - Bytes 7-8: Reserved

When extended format is detected (9 bytes), the plugin extracts the DTC code and publishes diagnostic information **only for gas appliances** (devices with "gas" in the name). This filtering is intentional because gas appliances (water heaters, furnaces) use DSI (Direct Spark Ignition) systems that generate meaningful diagnostic codes, while non-gas devices don't have relevant DTC data.

**DTC Attributes Published to MQTT:**

Topic: `ble-mqtt-bridge/{gatewayMac}/device/{tableId}/{deviceId}/attributes`

```json
{
  "dtc_code": 1589,
  "dtc_name": "WATER_HEATER_IGNITION_FAILURE",
  "fault": true,
  "status_byte": "0x41"
}
```

These attributes are automatically exposed in Home Assistant via the `json_attributes_topic` in the switch discovery message, allowing automations to trigger on specific fault conditions:

```yaml
automation:
  trigger:
    - platform: state
      entity_id: switch.water_heater
      attribute: fault
      to: true
```

**DTC Code Reference:**

The plugin includes 1934 diagnostic trouble code mappings (codes 0-1933) sourced from the IDS.Core.IDS_CAN library. The `DtcCodes.kt` helper object provides:

- `getName(code: Int)`: Returns human-readable name or "DTC_$code" if unknown
- `isFault(code: Int)`: Returns `code != 0`

Common DTC codes include:
- **0**: No fault (normal operation)
- **1589**: WATER_HEATER_IGNITION_FAILURE (most common DSI fault)
- **1590**: WATER_HEATER_FLAME_LOSS
- **1591**: WATER_HEATER_OVERHEAT

The full DTC reference is maintained in `docs/onecontrol_plugin_docs/DTC_REFERENCE.md`.


#### Dimmable Light

```kotlin
private fun handleDimmableLightStatus(data: ByteArray) {
    if (data.size < 5) return
    
    val tableId = data[1].toInt() and 0xFF
    val deviceId = data[2].toInt() and 0xFF
    val modeByte = data[3].toInt() and 0xFF   // 0=Off, 1=On, 2=Blink, 3=Swell
    val brightness = data[4].toInt() and 0xFF // 0-255
    val isOn = modeByte > 0
    
    // Pending guard: suppress mismatching status during command execution
    val key = "$tableId:$deviceId"
    val pending = pendingDimmable[key]
    if (pending != null) {
        val (desired, ts) = pending
        val age = System.currentTimeMillis() - ts
        if (age <= DIMMER_PENDING_WINDOW_MS && brightness != desired) {
            return  // Don't publish stale status during dimming
        }
    }
    
    // Track last known brightness for restore-on-ON
    if (brightness > 0) {
        lastKnownDimmableBrightness[key] = brightness
    }
    
    mqttPublisher.publishState("$baseTopic/device/$tableId/$deviceId/state", if (isOn) "ON" else "OFF", true)
    mqttPublisher.publishState("$baseTopic/device/$tableId/$deviceId/brightness", brightness.toString(), true)
}
```

#### Tank Sensors

**File:** `OneControlDevicePlugin.kt`, methods `handleTankStatus()` (V1) and `handleTankStatusV2()` (V2)

OneControl supports two tank sensor event formats:

**TankSensorStatus (0x0C) - V1 Format:**
- Multiple tanks reported in a single event
- Format: `[0x0C][tableId][deviceId1][level1][deviceId2][level2]...`
- 2 bytes per tank: `[deviceId, level]`

**TankSensorStatusV2 (0x1B) - V2 Format:**
- Single tank per event
- Format: `[0x1B][tableId][deviceId][level]`
- May include extended status (8 bytes with battery, quality, accelerometer data)

```kotlin
private fun handleTankStatus(data: ByteArray) {
    if (data.size < 5) return
    
    val tableId = data[1].toInt() and 0xFF
    val deviceId = data[2].toInt() and 0xFF
    val level = data[3].toInt() and 0xFF  // 0-100 percentage
    
    // Create entity instance
    val entity = OneControlEntity.Tank(
        tableId = tableId,
        deviceId = deviceId,
        level = level
    )
    
    // Publish state and discovery
    publishEntityState(
        entityType = EntityType.TANK_SENSOR,
        tableId = entity.tableId,
        deviceId = entity.deviceId,
        discoveryKey = "tank_${entity.key}",
        state = mapOf("level" to entity.level.toString())
    ) { friendlyName, _, prefix, baseTopic ->
        val stateTopic = "$baseTopic/device/${entity.tableId}/${entity.deviceId}/level"
        discoveryBuilder.buildSensor(
            sensorName = friendlyName,
            stateTopic = "$prefix/$stateTopic",
            unit = "%",
            icon = "mdi:gauge"
        )
    }
}
```

**Tank Level Values:**

Tank sensors send a single byte percentage value (0-100). Some tanks use discrete values for low-precision sensors:
- `0x00` (0) → EMPTY
- `0x21` (33) → 1/3 full
- `0x42` (66) → 2/3 full  
- `0x64` (100) → FULL

High-precision tanks (fuel, LP) report exact percentages (0-100). Low-precision tanks (fresh/grey/black water) typically use the 4 discrete values above.

**MQTT Topics:**
- State: `onecontrol/{MAC}/device/{tableId}/{deviceId}/level`
- Payload: Simple numeric string (e.g., "0", "33", "66", "100")
- Discovery: Home Assistant sensor with `unit_of_measurement: "%"` and `icon: "mdi:gauge"`

**Reference:** See [tank_findings.md](onecontrol_plugin_docs/tank_findings.md) for complete details on:
- Extended status format (8-byte with battery level, measurement quality, accelerometer data)
- Tank precision types (Low/Medium/High) from capability detection
- Fluid types (Water vs Fuel) from capability byte
- Official app display logic (discrete levels for low-precision, exact % for high-precision)

#### Cover/Slide/Awning Sensors (State-Only)

> ⚠️ **SAFETY DESIGN DECISION**: Cover control was intentionally disabled in v2.0.1 (December 2025) after testing revealed dangerous operating conditions. RV awnings and slides have **no automatic safety mechanisms**:
> - Awning motor: **19A current draw** at full extension
> - Slide motor: **39A current draw** at limits
> - **No limit switches** detected
> - **No overcurrent protection** present
> - Motors rely entirely on **operator judgment** to prevent damage
> 
> Remote control could cause equipment damage or personal injury if operated without visual confirmation. Therefore, covers are published as **state-only sensors**, not controllable entities.

**Event Handling:**

H-bridge status events (0x0D, 0x0E - `RelayHBridgeMomentary`) are parsed by `handleHBridgeStatus()`:

```kotlin
private fun handleHBridgeStatus(data: ByteArray) {
    if (data.size < 4) return
    
    val tableId = data[1].toInt() and 0xFF
    val deviceId = data[2].toInt() and 0xFF
    val status = data[3].toInt() and 0xFF
    val position = if (data.size >= 5) data[4].toInt() and 0xFF else 0xFF
    
    // SAFETY: RV awnings/slides have no limit switches or overcurrent protection.
    // Motors rely on operator judgment - remote control is unsafe.
    // Exposing as state sensor only.
    
    val entity = OneControlEntity.Cover(
        tableId = tableId,
        deviceId = deviceId,
        status = status,
        position = position
    )
    
    publishCoverState(entity)
}
```

**Status byte mapping:**
- `0xC0` → `stopped`
- `0xC2` → `opening` (extending)
- `0xC3` → `closing` (retracting)
- Other → `unknown`

**Position byte:**
- `0xFF` if position unavailable
- `0x00` to `0x64` (0-100%) if supported

**Entity Model:**

The Cover entity in [OneControlEntity.kt](OneControlEntity.kt) includes the `haState` property for Home Assistant integration:

```kotlin
/**
 * SAFETY NOTE: Cover control is disabled. RV awnings/slides have no limit switches
 * or overcurrent protection - motors rely on operator judgment. This entity is
 * STATE-ONLY for safety reasons.
 */
data class Cover(
    override val tableId: Int,
    override val deviceId: Int,
    val status: Int,
    val position: Int = 0xFF
) : OneControlEntity() {
    /**
     * Home Assistant cover state based on status byte
     */
    val haState: String
        get() = when (status) {
            0xC2 -> "opening"   // Extending
            0xC3 -> "closing"   // Retracting
            0xC0 -> "stopped"   // Stopped
            else -> "unknown"
        }
}
```

**Publishing Cover State:**

Covers are published as **sensor entities** (not cover entities):

```kotlin
private fun publishCoverState(entity: OneControlEntity.Cover) {
    val keyHex = "%02x%02x".format(entity.tableId, entity.deviceId)
    val baseTopic = "onecontrol/${device.address}"
    val stateTopic = "$baseTopic/cover_state/$keyHex/state"
    
    // Publish state
    mqttPublisher.publishState(stateTopic, entity.haState, true)
    
    // Publish discovery once
    val discoveryKey = "cover_state_$keyHex"
    if (haDiscoveryPublished.add(discoveryKey)) {
        val deviceName = getFunctionName(entity.tableId, entity.deviceId) 
            ?: "Cover ${keyHex.uppercase()}"
        val discovery = HomeAssistantMqttDiscovery.getCoverStateSensorDiscovery(
            gatewayMac = device.address,
            deviceAddr = (entity.tableId shl 8) or entity.deviceId,
            deviceName = deviceName,
            stateTopic = stateTopic,
            appVersion = BuildConfig.VERSION_NAME
        )
        
        val discoveryTopic = "homeassistant/sensor/onecontrol_ble_${device.address.replace(":", "")}/$discoveryKey/config"
        mqttPublisher.publishDiscovery(discoveryTopic, discovery.toString())
    }
}
```

**Key points:**
- Published to `homeassistant/sensor/...` (not `cover`)
- Discovery key: `cover_state_{tableId}{deviceId}`
- State topic only - **no command topic**
- Icon: `mdi:window-shutter`

**Command Rejection:**

Cover commands are explicitly rejected in `handleEntityCommand()`:

```kotlin
is OneControlEntity.Cover -> {
    Log.w(TAG, "⚠️ Cover control is disabled for safety - use physical controls")
    Result.failure(Exception("Cover control disabled for safety"))
}
```

This prevents any remote operation via MQTT or other interfaces.

**Discovery Configuration:**

From [HomeAssistantMqttDiscovery.kt](app/src/main/java/com/blemqttbridge/plugins/onecontrol/protocol/HomeAssistantMqttDiscovery.kt):

```kotlin
/**
 * SAFETY: Cover state sensor discovery (state-only, no control)
 * 
 * RV awnings and slides have no limit switches or overcurrent protection.
 * The motors rely entirely on operator judgment to avoid damage.
 * Therefore, we expose these as state-only sensors rather than controllable covers.
 * 
 * States: open, opening, closed, closing, stopped
 */
fun getCoverStateSensorDiscovery(
    gatewayMac: String,
    deviceAddr: Int,
    deviceName: String,
    stateTopic: String,
    appVersion: String? = null
): JSONObject {
    val uniqueId = "onecontrol_ble_${gatewayMac.replace(":", "")}_cover_state_${deviceAddr.toString(16)}"
    val objectId = "cover_state_${deviceAddr.toString(16).padStart(4, '0')}"
    
    return JSONObject().apply {
        put("unique_id", uniqueId)
        put("name", deviceName)
        put("default_entity_id", "sensor.$objectId")
        put("device", getDeviceInfo(gatewayMac, appVersion))
        
        // State only - no commands
        put("state_topic", stateTopic)
        
        // Icon shows awning/slide
        put("icon", "mdi:window-shutter")
    }
}
```

**Result in Home Assistant:**
- Entity type: `sensor.cover_state_XXXX`
- States: `opening`, `closing`, `stopped`, `unknown`
- No control buttons (read-only)
- Suitable for automation triggers or display

**Testing Notes (v2.0.1):**

From commit 311c7b8 testing documentation:

> **Awning motor:** Drew 19A continuously when fully extended with motor still engaged. No automatic cutoff.
> 
> **Slide motor:** Drew 39A at full extension. No automatic cutoff detected.
> 
> **Conclusion:** Both motors rely entirely on the operator to release the switch. Remote control without visual confirmation is unsafe and could damage equipment or cause injury.

**Legacy Controllable Cover Implementation:**

> ⚠️ **DEPRECATED - DO NOT USE**: A legacy controllable cover implementation existed in earlier versions but was intentionally removed in v2.0.1 for safety reasons. See commit 311c7b8 for the safety rationale and testing results. The current state-only sensor approach is the safe and correct implementation.

### Command Building

**File:** `protocol/MyRvLinkCommandBuilder.kt`

#### Switch Command (0x40)

```kotlin
fun buildActionSwitch(
    clientCommandId: UShort,
    deviceTableId: Byte,
    switchState: Boolean,
    deviceIds: List<Byte>
): ByteArray {
    val stateByte = if (switchState) 0x01.toByte() else 0x00.toByte()
    val command = ByteArray(5 + deviceIds.size)
    
    command[0] = (clientCommandId.toInt() and 0xFF).toByte()      // CmdId LSB
    command[1] = ((clientCommandId.toInt() shr 8) and 0xFF).toByte()  // CmdId MSB
    command[2] = 0x40.toByte()  // CommandType: ActionSwitch
    command[3] = deviceTableId
    command[4] = stateByte
    // Remaining bytes: device IDs
    
    return command
}
```

#### Dimmable Command (0x43)

```kotlin
fun buildActionDimmable(
    clientCommandId: UShort,
    deviceTableId: Byte,
    deviceId: Byte,
    brightness: Int
): ByteArray {
    val modeByte = if (brightness == 0) 0x00.toByte() else 0x01.toByte()
    val brightnessByte = brightness.coerceIn(0, 255).toByte()
    
    return byteArrayOf(
        (clientCommandId.toInt() and 0xFF).toByte(),
        ((clientCommandId.toInt() shr 8) and 0xFF).toByte(),
        0x43.toByte(),  // CommandType: ActionDimmable
        deviceTableId,
        deviceId,
        modeByte,
        brightnessByte,
        0x00.toByte()   // Reserved
    )
}
```

#### HVAC Command (0x45)

```kotlin
fun buildActionHvac(
    clientCommandId: UShort,
    deviceTableId: Byte,
    deviceId: Byte,
    heatMode: Int,      // 0=Off, 1=Heating, 2=Cooling, 3=Both, 4=RunSchedule
    heatSource: Int,    // 0=PreferGas, 1=PreferHeatPump, 2=Other, 3=Reserved
    fanMode: Int,       // 0=Auto, 1=High, 2=Low
    lowTripTempF: Int,  // Heat setpoint
    highTripTempF: Int  // Cool setpoint
): ByteArray {
    // Pack command byte: HeatMode (bits 0-2), HeatSource (bits 4-5), FanMode (bits 6-7)
    val commandByte = ((heatMode and 0x07) or
                      ((heatSource and 0x03) shl 4) or
                      ((fanMode and 0x03) shl 6)).toByte()
    
    return byteArrayOf(
        (clientCommandId.toInt() and 0xFF).toByte(),
        ((clientCommandId.toInt() shr 8) and 0xFF).toByte(),
        0x45.toByte(),  // CommandType: ActionHvac
        deviceTableId,
        deviceId,
        commandByte,
        lowTripTempF.coerceIn(0, 255).toByte(),
        highTripTempF.coerceIn(0, 255).toByte()
    )
}
```

### Command Handling

**File:** `OneControlDevicePlugin.kt`, method `handleCommand()`

Commands arrive via MQTT subscription:

```kotlin
fun handleCommand(commandTopic: String, payload: String): Result<Unit> {
    // Parse topic: onecontrol/{MAC}/command/{type}/{tableId}/{deviceId}
    val parts = commandTopic.split("/")
    val commandIndex = parts.indexOf("command")
    
    val kind = parts[commandIndex + 1]      // "switch", "dimmable", "cover", etc.
    val tableId = parts[commandIndex + 2].toInt()
    val deviceId = parts[commandIndex + 3].toInt()
    
    return when (kind) {
        "switch" -> controlSwitch(tableId.toByte(), deviceId.toByte(), payload)
        "dimmable" -> controlDimmableLight(tableId.toByte(), deviceId.toByte(), payload)
        else -> Result.failure(Exception("Unknown command type: $kind"))
    }
}
```

### Guard Checks in republishDiscoveryWithFriendlyName()

**Problem:** Removing `haDiscoveryPublished.contains()` guard checks causes massive entity duplication.

**Symptom:** OneControl devices show 4x expected entities in Home Assistant (e.g., 56 entities instead of 16).

**Root Cause:** Without guards, the `republishDiscoveryWithFriendlyName()` function publishes ALL entity types (switch, light, cover_state, tank) for EVERY device, regardless of actual device type. A switch device gets switch+light+cover+tank discoveries published.

**Solution:** Guard checks MUST remain:

```kotlin
// CORRECT: Only republish entity types that were already published
if (haDiscoveryPublished.contains("switch_$keyHex")) {
    // Republish switch discovery with friendly name
}
if (haDiscoveryPublished.contains("light_$keyHex")) {
    // Republish light discovery with friendly name
}
if (haDiscoveryPublished.contains("tank_$keyHex")) {
    // Republish tank discovery with friendly name
}
if (haDiscoveryPublished.contains("cover_state_$keyHex")) {
    // Republish cover state discovery with friendly name
}
```

**Why Guards Are Needed:**
- Guards determine entity type based on what was actually published from device state events
- Only devices that published switch discovery get switch republished with friendly name
- Prevents publishing irrelevant entity types for each device
- Without guards, every device receives all 4 entity type discoveries

**Historical Context:** 
- Guards were temporarily removed in v2.4.7 to fix a friendly name race condition
- This created a worse bug: 4x entity duplication
- Restored in v2.4.8 with protective comments explaining their purpose
- Accepts that friendly names might be delayed if metadata arrives before first state (rare edge case)

**File:** `OneControlDevicePlugin.kt`, method `republishDiscoveryWithFriendlyName()` (lines ~1826-1900)

### Friendly Name Race Condition

**Fixed in v2.4.7:** Previously, if `GetDevicesMetadata` response (with friendly names) arrived **before** entity state events, the friendly names were never applied because `republishDiscoveryWithFriendlyName()` checked `haDiscoveryPublished.contains()` first.

**Solution:** Always publish all entity types when metadata arrives, adding them to the tracking set:

```kotlin
private fun republishDiscoveryWithFriendlyName(tableId: Int, deviceId: Int, friendlyName: String) {
    // Publish switch discovery - even if not in haDiscoveryPublished yet
    val switchDiscovery = HomeAssistantMqttDiscovery.getSwitchDiscovery(...)
    mqttPublisher.publishDiscovery(switchDiscoveryTopic, switchDiscovery.toString())
    haDiscoveryPublished.add("switch_$keyHex")
    
    // Publish light, cover, tank discoveries...
    // (always publish, regardless of timing)
}
```

**Result:** Entities now show friendly names like "Awning" instead of "cover_160a", regardless of metadata/state arrival order.

### Device Metadata Retrieval (GetDevicesMetadata)

OneControl devices are identified by table ID and device ID (e.g., `0x08:0x05`), which results in generic names like "Switch 0805". To provide human-readable names like "Water Pump" or "Fresh Tank", the plugin retrieves metadata using the `GetDevicesMetadata` command (0x02).

**Reference:** See [METADATA_RETRIEVAL.md](onecontrol_plugin_docs/METADATA_RETRIEVAL.md) for complete protocol details.

#### Command Format (6 bytes)

```kotlin
private fun encodeGetDevicesMetadataCommand(commandId: UShort, deviceTableId: Byte): ByteArray {
    return byteArrayOf(
        (commandId.toInt() and 0xFF).toByte(),           // Command ID LSB
        ((commandId.toInt() shr 8) and 0xFF).toByte(),   // Command ID MSB
        0x02.toByte(),                                    // CommandType: GetDevicesMetadata
        deviceTableId,                                    // Device table to query
        0x00.toByte(),                                    // Start device ID
        0xFF.toByte()                                     // Max count (255)
    )
}
```

#### Response Parsing (Event Type 0x02)

The gateway responds with metadata entries containing function names:

```kotlin
private fun handleGetDevicesMetadataResponse(data: ByteArray) {
    val tableId = data[4].toInt() and 0xFF
    val startId = data[5].toInt() and 0xFF
    val count = data[6].toInt() and 0xFF
    
    var offset = 7
    var index = 0
    
    while (index < count && offset + 2 < data.size) {
        val protocol = data[offset].toInt() and 0xFF
        val payloadSize = data[offset + 1].toInt() and 0xFF
        
        if (protocol == 2 && payloadSize == 17) {  // IDS CAN device
            // CRITICAL: Function name is BIG-ENDIAN (unlike rest of protocol)
            val funcNameHi = data[offset + 2].toInt() and 0xFF
            val funcNameLo = data[offset + 3].toInt() and 0xFF
            val funcName = (funcNameHi shl 8) or funcNameLo  // Big-endian!
            val funcInstance = data[offset + 4].toInt() and 0xFF
            
            // Map function name ID to friendly string
            val friendlyName = FunctionNameMapper.getFriendlyName(funcName, funcInstance)
            
            // Cache metadata for this device
            val deviceId = (startId + index) and 0xFF
            val deviceAddr = (tableId shl 8) or deviceId
            
            deviceMetadata[deviceAddr] = DeviceMetadata(
                deviceTableId = tableId,
                deviceId = deviceId,
                functionName = funcName,
                functionInstance = funcInstance,
                friendlyName = friendlyName
            )
            
            // Update any already-published entities with friendly name
            republishDiscoveryWithFriendlyName(tableId, deviceId, friendlyName)
        }
        
        offset += payloadSize + 2
        index++
    }
    
    // Save to persistent cache
    saveMetadataToCache()
}
```

#### Critical Bug: Big-Endian Function Names

**The function name field uses big-endian byte order**, contrary to the little-endian convention used elsewhere in the protocol. This was discovered by analyzing the decompiled official app's `GetValueUInt16()` method, which defaults to big-endian.

**Example:** Raw bytes `00 60` represent:
- **Wrong (little-endian):** `0x6000 = 24576` → Unknown device
- **Correct (big-endian):** `0x0060 = 96` → "Slide"

#### Function Name Mapping

The `FunctionNameMapper.kt` object contains 445 function name mappings from the IDS CAN protocol:

```kotlin
object FunctionNameMapper {
    private val functionNames = mapOf(
        3 to "Gas Water Heater",
        5 to "Water Pump",
        49 to "Awning Light",
        67 to "Fresh Tank",
        96 to "Slide",
        105 to "Awning",
        172 to "Interior Light",
        // ... 438 more entries
    )
    
    fun getFriendlyName(functionName: Int, functionInstance: Int): String {
        val baseName = functionNames[functionName] ?: "Unknown Device $functionName"
        return if (functionInstance > 0) "$baseName $functionInstance" else baseName
    }
}
```

**Complete reference:** [FUNCTION_NAME_IDS.md](onecontrol_plugin_docs/FUNCTION_NAME_IDS.md) lists all 445 function name IDs organized by category (water/plumbing, HVAC, lighting, slides/awnings, etc.).

#### Timing & Caching

**Trigger locations:**
1. **Primary trigger:** 500ms after receiving first `GatewayInformation` event (most common)
2. **Backup trigger:** 1500ms after all notifications subscribed (fallback if GatewayInformation doesn't fire first)

The `metadataRequested` flag ensures the command is only sent once per connection, regardless of which trigger fires first.

**Persistence (v2.4.9):** Metadata is cached in `SharedPreferences` at `onecontrol_cache` with key `metadata_{MAC}` (colons removed). The cache:
- Loads automatically in plugin `init{}` block (before any connection)
- Saves after each successful metadata retrieval
- Survives app restarts and reconnections
- Eliminates ~500-1500ms delay on subsequent connections

**Race condition handling:** Device status events arrive immediately after authentication, but metadata takes ~500-1500ms to retrieve. Devices discovered before metadata arrives use fallback names (e.g., "Switch 0805"). Once metadata arrives, `republishDiscoveryWithFriendlyName()` updates these entities with friendly names.

**File:** `OneControlDevicePlugin.kt`, methods `loadMetadataFromCache()`, `saveMetadataToCache()`, `sendGetDevicesMetadataCommand()`, `handleGetDevicesMetadataResponse()`

---

## 5. EasyTouch Thermostat Protocol

### Overview

The EasyTouch (Micro-Air) thermostat uses a simpler JSON-over-BLE protocol compared to OneControl. The plugin communicates via a single BLE characteristic using plaintext JSON commands and responses.

**Reference:** [ha-micro-air-easytouch](https://github.com/k3vmcd/ha-micro-air-easytouch) HACS integration

### File Structure

```
app/src/main/java/com/blemqttbridge/plugins/easytouch/
├── EasyTouchDevicePlugin.kt       # Plugin implementation
└── protocol/
    └── EasyTouchConstants.kt      # UUIDs, status indices, mode mappings
```

### BLE Service UUIDs

```kotlin
// Service UUID
SERVICE_UUID = "6e400001-b5a3-f393-e0a9-e50e24dcca9e"

// Characteristics
RX_CHARACTERISTIC_UUID = "6e400002-b5a3-f393-e0a9-e50e24dcca9e"  // WRITE - commands TO device
TX_CHARACTERISTIC_UUID = "6e400003-b5a3-f393-e0a9-e50e24dcca9e"  // NOTIFY - events FROM device
```

**Note:** This is the Nordic UART Service (NUS) profile - common in BLE devices.

### Authentication Flow

Unlike OneControl's TEA encryption, EasyTouch uses a simple password-based authentication:

```
1. onServicesDiscovered()
   ↓
2. Request MTU = 256
   ↓
3. onMtuChanged() → enableNotifications()
   ↓
4. Send password command: {"Type":"Password","Password":"<password>"}
   ↓
5. Receive response: {"Type":"Password","Password":"<password>"}
   ↓
6. On match → authenticated, start polling
```

**File:** `EasyTouchDevicePlugin.kt`, method `authenticate()`

```kotlin
private fun authenticate() {
    val passwordCommand = """{"Type":"Password","Password":"$thermostatPassword"}"""
    writeCommand(passwordCommand)
}
```

### Read-After-Write Communication Pattern

**CRITICAL:** EasyTouch does NOT push status updates automatically. The plugin must poll for status, matching the official app's behavior.

**File:** `EasyTouchDevicePlugin.kt`, polling implementation:

```kotlin
private val statusPollRunnable = object : Runnable {
    override fun run() {
        if (isAuthenticated && isConnected) {
            writeCommand("""{"Type":"Get Status","Zone":0}""")
            statusPollHandler.postDelayed(this, STATUS_POLL_INTERVAL_MS)
        }
    }
}

companion object {
    const val STATUS_POLL_INTERVAL_MS = 4000L  // 4 seconds, matches official app
}
```

**Reference:** Official app `U_Thermostat.java` line 663: `mStatusHandler.postDelayed(this.statusRunnable, 4000)`

### Capability Discovery (Get Config)

After authentication, the plugin requests device configuration to discover supported modes and setpoint limits. This prevents showing unsupported modes (e.g., "dry" on devices without dehumidification).

**Connection Sequence:**
```
1. Connect → Authenticate
2. Request Config for zones 0-3
3. Parse MAV bitmask and setpoint limits
4. Start status polling
5. Publish discovery (with actual device capabilities)
```

**Get Config Command:**
```json
{"Type":"Get Config","Zone":0}
```

**Config Response:**
```json
{
  "Type": "Response",
  "RT": "Config",
  "CFG": {
    "Zone": 0,
    "MAV": 2070,
    "MA": [0,128,192,...],
    "FA": [16,34,194,...],
    "SPL": [55, 95, 40, 95]
  }
}
```

**CRITICAL:** Config data is nested inside `CFG` object, not at root level.

### MAV (Mode AVailable) Bitmask

The `MAV` field is a bitmask indicating which HVAC modes the device supports:

| Bit | Value | Mode | HA Mode |
|-----|-------|------|---------|
| 0 | 1 | Off | off |
| 1 | 2 | Fan Only | fan_only |
| 2 | 4 | Cool | cool |
| 4 | 16 | Heat | heat |
| 6 | 64 | Dry | dry |
| 11 | 2048 | Auto | auto |

**Example:** MAV=2070 = 2+4+16+2048 = fan_only, cool, heat, auto (NO dry mode)

**Parsing:**
```kotlin
private fun getModesFromBitmask(mav: Int): List<String> {
    if (mav == 0) return defaultModes  // Fallback if no MAV
    
    val modes = mutableListOf("off")  // Always include off
    for (bit in 0..15) {
        if ((mav and (1 shl bit)) != 0) {
            DEVICE_TO_HA_MODE[bit]?.let { modes.add(it) }
        }
    }
    return modes
}
```

### SPL (Setpoint Limits) Array

The `SPL` array contains temperature limits: `[minCool, maxCool, minHeat, maxHeat]`

**Example:** `SPL=[55, 95, 40, 95]` means:
- Cool setpoint: 55-95°F
- Heat setpoint: 40-95°F

These values are used in Home Assistant discovery for `min_temp` and `max_temp`.

### Write Retry with Delays

BLE write operations can fail on first attempt. The plugin implements retry logic:

```kotlin
private fun writeJsonCommand(command: JSONObject) {
    mainHandler.postDelayed({
        writeAttempt(command, attempt = 1, maxAttempts = 3)
    }, 100)  // 100ms initial delay
}

private fun writeAttempt(command: JSONObject, attempt: Int, maxAttempts: Int) {
    val success = writeCharacteristic(jsonCmdCharacteristic, commandBytes)
    
    if (!success && attempt < maxAttempts) {
        val backoffDelay = 200L * attempt  // Exponential backoff
        mainHandler.postDelayed({
            writeAttempt(command, attempt + 1, maxAttempts)
        }, backoffDelay)
    }
}
```

### JSON Command Format

All commands are plaintext JSON strings written to the RX characteristic.

#### Get Status

```json
{"Type":"Get Status","Zone":0}
```

Response returns the full thermostat state (see Status Response section).

#### Set Mode

```json
{"Type":"Set Hvac Mode","Mode":4}
```

Mode values:
| Value | Mode |
|-------|------|
| 0 | Off |
| 1 | Fan Only |
| 2 | Cool |
| 4 | Heat |
| 11 | Auto |

#### Set Fan Speed

```json
{"Type":"Set Fan Speed","Speed":1}
```

Fan speed values:
| Value | Speed |
|-------|-------|
| 1 | Auto |
| 2 | Low |
| 3 | Medium |
| 4 | High |

#### Set Temperature

```json
{"Type":"Set Setpoint Zone X","SetpointTemp":72}
```

Where X is zone number (1-4). Temperature is in Fahrenheit.

#### System Power Toggle

```json
{"Type":"Toggle System Power"}
```

This toggles the global system power on/off.

### Status Response Format

Status responses contain several arrays with device state:

```json
{
  "Type": "Status",
  "Serial": "352016935",
  "PRM": [1, 8, 0, ...],
  "Z_sts": [zone1_data..., zone2_data..., zone3_data..., zone4_data...],
  "Total_Zones": 1
}
```

### Z_sts Array Structure (Per Zone)

Each zone occupies indices 0-16 in segments. For zone N, offset = (N-1) * 17.

**File:** `EasyTouchConstants.kt`

```kotlin
object StatusIndex {
    const val MODE = 10           // Current mode (0, 1, 2, 4, 11)
    const val AMBIENT_TEMP = 12   // Current temperature (Fahrenheit)
    const val SETPOINT_TEMP = 13  // Target temperature (Fahrenheit)
    const val FAN_SPEED = 14      // Fan speed (1, 2, 3, 4)
    const val STATUS_FLAGS = 15   // Status byte (bit 0 = running)
}
```

**Parsing Example:**

```kotlin
private fun parseZoneStsData(zoneSts: List<Int>, zoneIndex: Int) {
    val offset = zoneIndex * 17  // Each zone is 17 values
    
    val mode = zoneSts.getOrNull(offset + StatusIndex.MODE) ?: 0
    val ambientTemp = zoneSts.getOrNull(offset + StatusIndex.AMBIENT_TEMP) ?: 70
    val setpointTemp = zoneSts.getOrNull(offset + StatusIndex.SETPOINT_TEMP) ?: 72
    val fanSpeed = zoneSts.getOrNull(offset + StatusIndex.FAN_SPEED) ?: 1
    val statusFlags = zoneSts.getOrNull(offset + StatusIndex.STATUS_FLAGS) ?: 0
    val isRunning = (statusFlags and 0x01) != 0
}
```

### PRM Array System Flags

The PRM array contains system-level flags:

```kotlin
// PRM[1] bit flags
// Bit 0: wifiConnected
// Bit 1: awsConnected  
// Bit 2: pushNotify
// Bit 3: systemPower  ← CRITICAL for mode reporting

val systemPower = (prm.getOrNull(1) ?: 0) and 0x08 != 0  // Bit 3
```

**CRITICAL:** When `systemPower` is `false`, the mode should be reported as "off" to Home Assistant regardless of the underlying mode setting. This matches the official app behavior.

```kotlin
private fun getHvacMode(mode: Int, systemPower: Boolean): String {
    if (!systemPower) return "off"  // System power overrides mode
    return when (mode) {
        1 -> "fan_only"
        2 -> "cool"
        4 -> "heat"
        11 -> "heat_cool"  // Auto mode
        else -> "off"
    }
}
```

### Home Assistant Integration

#### Climate Entity Discovery

Discovery uses actual device capabilities from config response:

```kotlin
private fun publishDiscovery(zone: Int, totalZones: Int) {
    val zoneName = if (totalZones > 1) "Zone $zone" else "Climate"
    val uniqueId = "easytouch_${serialNumber}_zone$zone"
    
    // Use actual modes from MAV bitmask (from Get Config response)
    val zoneConfig = zoneConfigs[zone]
    val supportedModes = if (zoneConfig != null && zoneConfig.availableModesBitmask != 0) {
        getModesFromBitmask(zoneConfig.availableModesBitmask)
    } else {
        listOf("off", "heat", "cool", "auto", "fan_only", "dry")  // Fallback
    }
    
    // Use actual temp limits from SPL array
    val minTemp = zoneConfig?.minHeatSetpoint ?: 60
    val maxTemp = zoneConfig?.maxCoolSetpoint ?: 90
    
    val discovery = JSONObject().apply {
        put("name", zoneName)
        put("object_id", uniqueId)
        put("unique_id", uniqueId)
        put("modes", JSONArray(supportedModes))  // Device-specific modes
        put("fan_modes", JSONArray(listOf("auto", "low", "medium", "high")))
        put("temperature_unit", "F")
        put("min_temp", minTemp)
        put("max_temp", maxTemp)
        // ... state/command topics
    }
}
```

#### State Publishing

State includes current and target temperatures. For Auto (heat_cool) mode, both high/low setpoints are published:

```kotlin
private fun publishZoneState(zone: Int) {
    val stateTopic = "$baseTopic/zone_$zone/state"
    
    // Determine which temps to publish based on mode
    val (targetTemp, targetTempHigh, targetTempLow) = when (mode) {
        11 -> Triple(null, coolSetpoint, heatSetpoint)  // Auto mode: high/low
        2 -> Triple(coolSetpoint, null, null)           // Cool mode
        4 -> Triple(heatSetpoint, null, null)           // Heat mode
        else -> Triple(null, null, null)                // Off/Fan
    }
    
    // Publish individual topics (HA climate needs separate topics)
    mqttPublisher.publishState("$stateTopic/temperature", targetTemp?.toString() ?: "None", true)
    mqttPublisher.publishState("$stateTopic/temperature_high", targetTempHigh?.toString() ?: "None", true)
    mqttPublisher.publishState("$stateTopic/temperature_low", targetTempLow?.toString() ?: "None", true)
    mqttPublisher.publishState("$stateTopic/current_temperature", ambientTemp.toString(), true)
    mqttPublisher.publishState("$stateTopic/mode", getHvacMode(mode, systemPower), true)
    mqttPublisher.publishState("$stateTopic/fan_mode", getFanMode(fanSpeed), true)
    mqttPublisher.publishState("$stateTopic/action", if (isRunning) getAction(mode) else "idle", true)
}
```

**Note:** Publishing "None" to unused temperature topics allows HA to properly hide the irrelevant setpoint UI elements.

### Command Handling

Commands use the "Change" format with a Changes object containing all fields to modify:

```kotlin
override fun handleCommand(commandTopic: String, payload: String): Result<Unit> {
    return when {
        commandTopic.endsWith("/mode/set") -> {
            val modeValue = when (payload.lowercase()) {
                "off" -> 0
                "fan_only" -> 1
                "cool" -> 2
                "heat" -> 4
                "heat_cool", "auto" -> 11
                else -> return Result.failure(...)
            }
            // CRITICAL: Include power=1 for modes other than off
            val command = JSONObject().apply {
                put("Type", "Change")
                put("Changes", JSONObject().apply {
                    put("zone", zoneNum)
                    put("mode", modeValue)
                    put("power", if (modeValue == 0) 0 else 1)  // Required!
                })
            }
            writeJsonCommand(command)
            Result.success(Unit)
        }
        commandTopic.endsWith("/temperature/set") -> {
            val temp = payload.toIntOrNull() ?: return Result.failure(...)
            val setpointKey = when (currentMode) {
                2 -> "cool_sp"
                4 -> "heat_sp"
                else -> "cool_sp"
            }
            val command = JSONObject().apply {
                put("Type", "Change")
                put("Changes", JSONObject().apply {
                    put("zone", zoneNum)
                    put(setpointKey, temp)
                })
            }
            writeJsonCommand(command)
            Result.success(Unit)
        }
        // ... fan_mode, temperature_high, temperature_low handling
    }
}
```

**CRITICAL:** Mode change commands MUST include `"power": 1` or the thermostat won't actually change modes.

### Key Differences from OneControl

| Aspect | OneControl | EasyTouch |
|--------|------------|-----------|
| Protocol | Binary + COBS + CRC + TEA encryption | Plain JSON strings |
| Authentication | 32-round TEA encryption | Simple password JSON |
| Status Updates | Event-driven (notifications) | Polling required (4s interval) |
| Capability Discovery | Device catalog from GetDevices | MAV bitmask from Get Config |
| Service UUID | Custom (0x0010, 0x0030) | Nordic UART (NUS) |
| Entity Types | Switches, lights, covers, HVAC, tanks | Climate only |
| Multi-device | Gateway manages many devices | Single thermostat (up to 4 zones) |
| Write Reliability | Generally reliable | Requires retry with delays |

---

## 6. GoPower Solar Controller Protocol

### Overview

The GoPower plugin connects to GoPower solar charge controllers (e.g., GP-PWM-30-SB) commonly found in RVs. Unlike OneControl and EasyTouch, the GoPower controller uses a **read-only** protocol - it broadcasts solar system status via BLE notifications but does not accept commands.

**Location:** `app/src/main/java/com/blemqttbridge/plugins/gopower/`

### Protocol Characteristics

| Characteristic | Value |
|---------------|-------|
| Connection Type | BLE notifications + write commands |
| Pairing | Not required (no bonding needed) |
| Authentication | None |
| Service UUID | `0000fff0-0000-1000-8000-00805f9b34fb` |
| Notify UUID | `0000fff1-0000-1000-8000-00805f9b34fb` |
| Write UUID | `0000fff2-0000-1000-8000-00805f9b34fb` |
| Data Format | Semicolon-delimited ASCII string (32 fields) |
| Update Rate | ~1 second (polled via 0x20 command) |
| Encoding | ASCII text, fields separated by semicolons |

### Notification Data Format

The controller responds to a poll command (0x20) with a semicolon-delimited ASCII string containing 32 fields:

```
Format: "f0;f1;f2;...;f31"
Example: "00123;00456;00789;..."

Key Field Mapping (0-indexed):
Field 0:  PV Voltage (divide by 10 for volts)
Field 2:  PV Current (divide by 100 for amps)  
Field 7:  Battery Voltage (divide by 10 for volts)
Field 12: Battery SOC percentage
Field 14: Controller Temperature (°C)
Field 15: Model ID (e.g., 20480 = GP-PWM-30-SB)
Field 19: "Ah Today" - Daily energy in Ah (divide by 100)
Field 26: Firmware version
```

**Note:** The protocol uses semicolons, not commas. Fields are zero-padded 5-digit integers.

### Key Implementation Details

#### Energy Calculation (Wh)

The plugin converts the controller's "Ah Today" value (Field 19) to Watt-hours using the current battery voltage:

```kotlin
// GoPowerDevicePlugin.kt
val ahToday = fields.getOrNull(GoPowerConstants.FIELD_AH_TODAY)
    ?.toIntOrNull()?.let { it / 100.0 } ?: 0.0
val batteryVoltage = fields.getOrNull(GoPowerConstants.FIELD_BATTERY_VOLTAGE)
    ?.toIntOrNull()?.let { it / 10.0 } ?: 0.0
val energyWh = ahToday * batteryVoltage
```

**Note:** This provides an approximate Wh value. True Wh would require integrating power over time, but Ah × V gives a reasonable estimate for display purposes.

#### Model Number Detection

The controller reports a model ID in Field 15. Known mappings:\n\n```kotlin\n// GoPowerConstants.kt\nconst val MODEL_GP_PWM_30_SB = 20480\n\n// GoPowerDevicePlugin.kt\nval modelId = fields.getOrNull(GoPowerConstants.FIELD_MODEL_ID)?.toIntOrNull() ?: 0\nval modelName = when (modelId) {\n    GoPowerConstants.MODEL_GP_PWM_30_SB -> \"GP-PWM-30-SB\"\n    else -> \"Unknown ($modelId)\"\n}\n```

#### Polling Protocol

Unlike OneControl (event-driven) or EasyTouch (JSON polling), GoPower uses a simple byte command:

```kotlin
private fun parseStatusNotification(data: ByteArray) {
    val dataString = String(data, Charsets.UTF_8).trim()
    val fields = dataString.split(";")  // Semicolon-delimited
    
    // Validate field count (expect 32 fields)
    if (fields.size < 27) {
        Log.w(TAG, "Incomplete data: ${fields.size} fields")
        return
    }
    
    try {
        // Parse sensors with scaling
        val pvVoltage = fields[FIELD_PV_VOLTAGE].toIntOrNull()?.let { it / 10.0 } ?: 0.0
        val pvCurrent = fields[FIELD_PV_CURRENT].toIntOrNull()?.let { it / 100.0 } ?: 0.0
        val batteryVoltage = fields[FIELD_BATTERY_VOLTAGE].toIntOrNull()?.let { it / 10.0 } ?: 0.0
        val batteryPercent = fields[FIELD_BATTERY_SOC].toIntOrNull() ?: 0
        val temperature = fields[FIELD_TEMPERATURE].toIntOrNull() ?: 0
        val ahToday = fields[FIELD_AH_TODAY].toIntOrNull()?.let { it / 100.0 } ?: 0.0
        
        // Calculate derived values
        val pvPower = pvVoltage * pvCurrent
        val energyWh = ahToday * batteryVoltage
        
        // Publish to MQTT
        publishSensorData(pvVoltage, pvCurrent, pvPower, batteryVoltage, ...)
        
    } catch (e: Exception) {
        Log.e(TAG, "Error parsing status", e)
    }
}
```

### Home Assistant Entities

The plugin publishes the following sensors to Home Assistant:

| Entity Type | Entity ID | Description | Unit | Device Class |
|------------|-----------|-------------|------|--------------|
| Sensor | `pv_voltage` | Solar panel voltage | V | voltage |
| Sensor | `pv_current` | Solar panel current | A | current |
| Sensor | `pv_power` | Solar input power | W | power |
| Sensor | `battery_voltage` | Battery voltage | V | voltage |
| Sensor | `battery_soc` | Battery state of charge | % | battery |
| Sensor | `temperature` | Controller temperature | °C | temperature |
| Sensor | `energy` | Daily energy (Ah × voltage) | Wh | energy |
| Text Sensor | `model_number` | Device model number | - | diagnostic |
| Text Sensor | `firmware_version` | Firmware version | - | diagnostic |
| Binary Sensor | `connected` | BLE connection status | - | connectivity |
| Binary Sensor | `data_healthy` | Data health status | - | connectivity |
| Button | `reboot` | Reboot controller | - | restart |

### Plugin Structure

```
plugins/gopower/
├── GoPowerDevicePlugin.kt          # Main plugin implementation
├── protocol/
│   └── GoPowerConstants.kt         # UUIDs, field indices, commands
```

### Configuration Requirements

Unlike OneControl, GoPower does not require pairing or authentication:

1. Configure the controller MAC address in app settings
2. Enable the GoPower plugin
3. Enable the BLE Service

The plugin polls the controller at 1-second intervals.

### Key Differences from Other Plugins

| Feature | OneControl | EasyTouch | GoPower |
|---------|-----------|-----------|---------|
| Pairing Required | Yes (bonding) | No | No |
| Authentication | TEA encryption | Password auth | None |
| Commands | Full bidirectional | Climate control | Reboot only (via unlock) |
| Notification Type | Event-driven | JSON status | Semicolon-delimited ASCII |
| Data Format | COBS-encoded binary | JSON | Semicolon-delimited ASCII (32 fields) |
| Heartbeat Needed | Yes (GetDevices) | No | Yes (0x20 poll command) |
| Multi-device | Yes (gateway) | Yes (zones) | No (single controller) |

### Reboot Command

The GoPower controller supports a reboot command, but requires an unlock sequence first:

```kotlin
// GoPowerConstants.kt
val UNLOCK_COMMAND = "&G++0900".toByteArray(Charsets.US_ASCII)
val REBOOT_COMMAND = "&LDD0100".toByteArray(Charsets.US_ASCII)
const val UNLOCK_DELAY_MS = 200L

// GoPowerDevicePlugin.kt
private fun sendRebootCommand() {
    // Send unlock sequence first
    writeCharacteristic?.let { char ->
        char.value = GoPowerConstants.UNLOCK_COMMAND
        currentGatt?.writeCharacteristic(char)
    }
    
    // Wait for unlock to process, then send reboot
    handler.postDelayed({
        writeCharacteristic?.let { char ->
            char.value = GoPowerConstants.REBOOT_COMMAND
            currentGatt?.writeCharacteristic(char)
        }
    }, GoPowerConstants.UNLOCK_DELAY_MS)
}
```

**Note:** The reboot command is exposed as a button entity in Home Assistant but may not actually reboot all controller models.

### Diagnostic Sensors

The GoPower plugin publishes diagnostic sensors for device identification:

```kotlin
// Published on connect and when data is first received
publishDiagnosticSensor(
    objectId = "model_number",
    name = "Model Number",
    value = modelName  // "GP-PWM-30-SB"
)

publishDiagnosticSensor(
    objectId = "firmware_version",
    name = "Firmware Version",
    value = firmware  // e.g., "V1.5"
)
```

These sensors are marked with `entity_category: "diagnostic"` and appear in the device diagnostics section in Home Assistant.

### Per-Plugin Status Tracking

The GoPower plugin uses the centralized per-plugin status tracking system:

```kotlin
// Update status in MqttPublisher
mqttPublisher.updatePluginStatus(
    pluginId = "gopower",
    connected = isConnected,
    authenticated = isConnected,  // No auth needed, so use connected state
    dataHealthy = isReceivingData
)
```

This allows the UI to display GoPower's connection status independently from other plugins.

### Troubleshooting

| Issue | Cause | Solution |
|-------|-------|----------|
| No data received | Controller out of range | Move Android device closer |
| Sensors not appearing | Discovery not published | Check MQTT logs, verify baseTopic |
| Reboot doesn't work | Controller ignores BLE reboot | Physical power cycle required |
| Energy shows 0 | No solar production yet | Normal when panels not producing |

### Known Limitations

- **Field 5 (fault code)** contains historical faults, not current faults - removed from sensors
- **Serial number** field was unreliable and removed
- **Reboot command** sends correctly but controller may not respond
- **Load sensors** were removed as not all models support load output

---

## 7. MQTT Integration

### MqttOutputPlugin

**Location:** `app/src/main/java/com/blemqttbridge/plugins/output/MqttOutputPlugin.kt`

Uses Eclipse Paho MQTT client with:
- Automatic reconnect
- LWT (Last Will and Testament) for availability
- QoS 1 for reliability

#### Topic Structure (v2.4.8+)

```
homeassistant/                                    # prefix (configurable)
├── availability                                  # online/offline LWT (global)
├── binary_sensor/ble_mqtt_bridge_{suffix}/       # Gateway device (unique per Android device)
│   └── availability/config                       # Gateway availability discovery
├── sensor/ble_mqtt_bridge_{suffix}/              # System diagnostics (unique per Android device)
│   ├── battery_level/config
│   ├── wifi_signal/config
│   ├── ram_available/config
│   └── ... (other diagnostics)
├── sensor/ble_scanner_{suffix}/                  # Scanner device (unique per Android device)
│   ├── scan_status/config
│   ├── devices_found/config
│   └── scan_button/config
└── onecontrol/{MAC}/                             # BLE device namespace (unique via MAC)
    ├── status                                    # ready/offline
    ├── gateway                                   # gateway info JSON
    ├── system/
    │   ├── voltage                               # system voltage
    │   └── temperature                           # system temperature
    ├── device/{tableId}/{deviceId}/
    │   ├── state                                 # ON/OFF
    │   ├── brightness                            # 0-255 (dimmables)
    │   ├── level                                 # 0-100 (tanks)
    │   └── hvac                                  # HVAC JSON
    └── command/{type}/{tableId}/{deviceId}       # commands from HA
        └── brightness                            # brightness subcommand
```

**Note:** `{suffix}` is the last 6 characters of Android ANDROID_ID (e.g., `7c7123`). This enables multi-gateway deployments where multiple Android devices can run the bridge simultaneously without device ID conflicts.

### Graceful Error Handling

**CRITICAL:** The `publish()` function MUST NOT throw exceptions:

```kotlin
private suspend fun publish(topic: String, payload: String, retained: Boolean) = 
    suspendCancellableCoroutine { continuation ->
        try {
            val client = mqttClient
            if (client == null) {
                Log.e(TAG, "Cannot publish - client is null")
                continuation.resume(Unit)  // DON'T throw!
                return@suspendCancellableCoroutine
            }
            
            // Check connection status safely
            val connected = try {
                client.isConnected
            } catch (e: IllegalArgumentException) {
                Log.w(TAG, "isConnected check failed: ${e.message}")
                false
            }
            
            if (!connected) {
                Log.w(TAG, "Cannot publish - not connected")
                continuation.resume(Unit)  // DON'T throw!
                return@suspendCancellableCoroutine
            }
            
            // ... actual publish ...
            
        } catch (e: Exception) {
            Log.e(TAG, "Publish error", e)
            continuation.resume(Unit)  // NEVER resumeWithException!
        }
    }
```

---

## 7.5 Multi-Gateway Support (v2.4.8+)

### Overview

Multiple Android devices can run the bridge simultaneously without device ID conflicts. Each device publishes unique gateway and scanner devices while BLE device entities (OneControl, EasyTouch, GoPower) remain unique via their MAC addresses.

**Use Case:** RV deployments with multiple Android devices (front TV and rear TV) monitoring the same OneControl gateway, or separate gateways for different zones.

### Device Identification Strategy

**Gateway Device:**
- Uses Android ANDROID_ID (last 6 characters) for uniqueness
- Device ID format: `ble_mqtt_bridge_{suffix}` (e.g., `ble_mqtt_bridge_7c7123`)
- Device name format: `BLE MQTT Bridge {SUFFIX}` (e.g., `BLE MQTT Bridge 7C7123`)
- System diagnostics (battery, WiFi, RAM) associated with this unique device

**Scanner Device:**
- Uses same Android ID suffix
- Device ID format: `ble_scanner_{suffix}` (e.g., `ble_scanner_7c7123`)
- Device name format: `BLE Scanner {SUFFIX}` (e.g., `BLE Scanner 7C7123`)
- Scanner entities (status, device count, scan button) associated with this unique device

**BLE Device Entities:**
- OneControl, EasyTouch, GoPower entities continue using their own MAC addresses
- No changes to entity identification
- Ensures no conflicts across multiple gateways

### Implementation

**Files:** `MqttOutputPlugin.kt`, `BleScannerPlugin.kt`

Both plugins implement identical suffix extraction:

```kotlin
private fun getDeviceSuffix(context: Context): String {
    val androidId = Settings.Secure.getString(
        context.contentResolver,
        Settings.Secure.ANDROID_ID
    ) ?: "unknown"
    return androidId.takeLast(6)
}

// Usage in gateway device discovery (MqttOutputPlugin.kt)
val deviceSuffix = getDeviceSuffix(context)
val nodeId = "ble_mqtt_bridge_${deviceSuffix}"
val deviceName = "BLE MQTT Bridge ${deviceSuffix.uppercase()}"

// Usage in scanner device discovery (BleScannerPlugin.kt)
val deviceSuffix = getDeviceSuffix(context)
val deviceId = "ble_scanner_${deviceSuffix}"
val deviceName = "BLE Scanner ${deviceSuffix.uppercase()}"
```

**Why last 6 characters?**
- Full Android ID is 16 hex characters (64-bit)
- Last 6 characters provide 16.7 million unique values (24-bit)
- Sufficient for RV/marine multi-device deployments
- Compact enough for MQTT topic readability
- Fallback: If Android ID unavailable, returns "unknown" (should never happen on real devices)

### System Diagnostics in Multi-Gateway Setup

Each Android device publishes its own system diagnostics under its unique gateway device:

**Device 1 (suffix: 7c7123):**
- `homeassistant/sensor/ble_mqtt_bridge_7c7123/battery_level/state` → 85%
- `homeassistant/sensor/ble_mqtt_bridge_7c7123/wifi_signal/state` → -45 dBm
- `homeassistant/sensor/ble_mqtt_bridge_7c7123/ram_available/state` → 2048 MB

**Device 2 (suffix: abc456):**
- `homeassistant/sensor/ble_mqtt_bridge_abc456/battery_level/state` → 100% (plugged in)
- `homeassistant/sensor/ble_mqtt_bridge_abc456/wifi_signal/state` → -60 dBm
- `homeassistant/sensor/ble_mqtt_bridge_abc456/ram_available/state` → 1024 MB

This allows monitoring each Android device's health independently in Home Assistant.

### Multi-Gateway Deployment Example

**Scenario:** RV with front and rear Android TVs

**Front TV (Android ID: 6f714d7e291cf0d2):**
- Gateway device: `ble_mqtt_bridge_f0d2`
- Scanner device: `ble_scanner_f0d2`
- Monitors OneControl gateway at MAC `24:DC:C3:ED:1E:0A`

**Rear TV (Android ID: 3a82bc4f5a1b9d7e):**
- Gateway device: `ble_mqtt_bridge_9d7e`
- Scanner device: `ble_scanner_9d7e`
- Also monitors same OneControl gateway at MAC `24:DC:C3:ED:1E:0A`

**Home Assistant sees:**
- 2 gateway devices: `BLE MQTT Bridge F0D2`, `BLE MQTT Bridge 9D7E`
- 2 scanner devices: `BLE Scanner F0D2`, `BLE Scanner 9D7E`
- 1 OneControl device: `OneControl BLE Gateway` (with all switches, lights, etc.)

Both Android devices can control the same OneControl devices without conflicts. The OneControl entities use the gateway's MAC address for identification, not the Android device ID.

### Migration Notes

**Upgrading from v2.4.7 or earlier:**

1. **Gateway Device Change:** Your gateway device in Home Assistant will appear with a new ID
   - Old: `ble_mqtt_bridge`
   - New: `ble_mqtt_bridge_7c7123` (example suffix)

2. **Scanner Device Change:** Your scanner device will also have a new ID
   - Old: `ble_scanner`
   - New: `ble_scanner_7c7123` (example suffix)

3. **OneControl/EasyTouch/GoPower Entities:** No changes - they continue using MAC addresses

4. **Cleanup:** Remove old `ble_mqtt_bridge` and `ble_scanner` devices from Home Assistant after confirming new devices are working

---

## 8. Home Assistant Discovery

### Discovery Payload Format

**File:** `protocol/HomeAssistantMqttDiscovery.kt`

Example switch discovery:

```kotlin
fun getSwitchDiscovery(
    gatewayMac: String,
    deviceAddr: Int,
    deviceName: String,
    stateTopic: String,
    commandTopic: String,
    appVersion: String?
): JSONObject {
    val uniqueId = "onecontrol_ble_${gatewayMac.replace(":", "")}_switch_${deviceAddr.toString(16)}"
    
    return JSONObject().apply {
        put("unique_id", uniqueId)
        put("name", deviceName)
        put("device", getDeviceInfo(gatewayMac, appVersion))
        put("state_topic", stateTopic)
        put("command_topic", commandTopic)
        put("payload_on", "ON")
        put("payload_off", "OFF")
        put("optimistic", false)
        put("icon", "mdi:electric-switch")
    }
}
```

### Device Info (Grouping)

All entities are grouped under one device:

```kotlin
fun getDeviceInfo(gatewayMac: String, appVersion: String?): JSONObject {
    return JSONObject().apply {
        put("identifiers", JSONArray().put("onecontrol_ble_v2_$gatewayMac"))
        put("name", "OneControl BLE Gateway")
        put("model", "OneControl Gateway")
        put("sw_version", "App Version: ${appVersion ?: "MyRvLink Protocol v6"}")
        put("connections", JSONArray().put(JSONArray().put("mac").put(gatewayMac)))
    }
}
```

### Discovery Topic Pattern

```
homeassistant/{component}/{node_id}/{object_id}/config

Examples:
homeassistant/switch/onecontrol_ble_24dcc3ed1e0a/switch_0801/config
homeassistant/light/onecontrol_ble_24dcc3ed1e0a/light_0802/config
homeassistant/sensor/onecontrol_ble_24dcc3ed1e0a/system_voltage/config
homeassistant/binary_sensor/onecontrol_ble_24dcc3ed1e0a/device_paired/config
```

---

### Entity Model Architecture (Phase 3 Refactoring)

**Added in v2.3.0**: The OneControl plugin now uses a sealed class hierarchy for type-safe entity models. All entity types inherit from `OneControlEntity` and include their identifying information plus current state.

**File:** `OneControlEntity.kt`

```kotlin
sealed class OneControlEntity {
    abstract val tableId: Int
    abstract val deviceId: Int
    
    val key: String get() = "%02x%02x".format(tableId, deviceId)
    val address: String get() = "$tableId:$deviceId"
    
    data class Switch(
        override val tableId: Int,
        override val deviceId: Int,
        val isOn: Boolean
    ) : OneControlEntity() {
        val state: String get() = if (isOn) "ON" else "OFF"
    }
    
    data class DimmableLight(
        override val tableId: Int,
        override val deviceId: Int,
        val brightness: Int,
        val mode: Int
    ) : OneControlEntity() {
        val isOn: Boolean get() = mode > 0
        val state: String get() = if (isOn) "ON" else "OFF"
    }
    
    data class Tank(
        override val tableId: Int,
        override val deviceId: Int,
        val level: Int
    ) : OneControlEntity()
    
    data class Cover(
        override val tableId: Int,
        override val deviceId: Int,
        val status: Int,
        val position: Int = 0xFF
    ) : OneControlEntity() {
        val haState: String
            get() = when (status) {
                0xC2 -> "opening"
                0xC3 -> "closing"
                0xC0 -> "stopped"
                else -> "unknown"
            }
    }
    
    data class SystemVoltageSensor(
        override val tableId: Int,
        override val deviceId: Int,
        val voltage: Float
    ) : OneControlEntity()
    
    data class SystemTemperatureSensor(
        override val tableId: Int,
        override val deviceId: Int,
        val temperature: Float
    ) : OneControlEntity()
}
```

**Benefits:**
- Type safety: Compiler catches missing properties at build time
- Centralization: Single source of truth for entity state
- Testability: Easy to create entity instances for unit tests
- Maintainability: Clear documentation of what each entity contains

### Using Entity Models in Event Handlers

Entity models are created in event handlers and then published using the centralized `publishEntityState()` method:

**Example: Switch Handler**

```kotlin
private fun handleRelayStatus(data: ByteArray) {
    if (data.size < 5) return
    
    val tableId = data[1].toInt() and 0xFF
    val deviceId = data[2].toInt() and 0xFF
    val statusByte = data[3].toInt() and 0xFF
    val rawOutputState = statusByte and 0x0F
    val isOn = rawOutputState == 0x01
    
    // Create entity instance
    val entity = OneControlEntity.Switch(
        tableId = tableId,
        deviceId = deviceId,
        isOn = isOn
    )
    
    // Publish state and discovery using centralized method
    publishEntityState(
        entityType = EntityType.SWITCH,
        tableId = entity.tableId,
        deviceId = entity.deviceId,
        discoveryKey = "switch_${entity.key}",
        state = mapOf("state" to entity.state)
    ) { friendlyName, deviceAddr, prefix, baseTopic ->
        val stateTopic = "$baseTopic/device/${entity.tableId}/${entity.deviceId}/state"
        val commandTopic = "$baseTopic/command/switch/${entity.tableId}/${entity.deviceId}"
        discoveryBuilder.buildSwitch(
            deviceAddr = deviceAddr,
            deviceName = friendlyName,
            stateTopic = "$prefix/$stateTopic",
            commandTopic = "$prefix/$commandTopic"
        )
    }
}
```

**Example: Dimmable Light Handler**

```kotlin
private fun handleDimmableLightStatus(data: ByteArray) {
    if (data.size < 5) return
    
    val tableId = data[1].toInt() and 0xFF
    val deviceId = data[2].toInt() and 0xFF
    val modeByte = data[3].toInt() and 0xFF
    val brightness = data[4].toInt() and 0xFF
    
    // Create entity instance
    val entity = OneControlEntity.DimmableLight(
        tableId = tableId,
        deviceId = deviceId,
        brightness = brightness,
        mode = modeByte
    )
    
    // Publish with brightness
    publishEntityState(
        entityType = EntityType.LIGHT,
        tableId = entity.tableId,
        deviceId = entity.deviceId,
        discoveryKey = "light_${entity.key}",
        state = mapOf(
            "state" to entity.state,
            "brightness" to entity.brightness.toString()
        )
    ) { friendlyName, deviceAddr, prefix, baseTopic ->
        val stateTopic = "$baseTopic/device/${entity.tableId}/${entity.deviceId}/state"
        val brightnessTopic = "$baseTopic/device/${entity.tableId}/${entity.deviceId}/brightness"
        val commandTopic = "$baseTopic/command/dimmable/${entity.tableId}/${entity.deviceId}"
        discoveryBuilder.buildDimmableLight(
            deviceAddr = deviceAddr,
            deviceName = friendlyName,
            stateTopic = "$prefix/$stateTopic",
            commandTopic = "$prefix/$commandTopic",
            brightnessTopic = "$prefix/$brightnessTopic"
        )
    }
}
```

### Command Handling with Entity Models

Commands are handled using a `when` expression on the sealed class, which provides exhaustive type checking:

```kotlin
override fun handleCommand(topic: String, payload: String): Result<Unit> {
    // Parse entity from topic
    val entity = parseEntityFromTopic(topic) ?: 
        return Result.failure(Exception("Unknown entity"))
    
    // Type-safe command dispatch
    return when (entity) {
        is OneControlEntity.Switch -> 
            controlSwitch(entity.tableId.toByte(), entity.deviceId.toByte(), payload)
        
        is OneControlEntity.DimmableLight -> 
            controlDimmableLight(entity.tableId.toByte(), entity.deviceId.toByte(), payload)
        
        is OneControlEntity.Cover -> {
            Log.w(TAG, "⚠️ Cover control is disabled for safety")
            Result.failure(Exception("Cover control disabled for safety"))
        }
        
        is OneControlEntity.Tank,
        is OneControlEntity.SystemVoltageSensor,
        is OneControlEntity.SystemTemperatureSensor -> {
            Log.w(TAG, "⚠️ Sensors are read-only")
            Result.failure(Exception("Sensors are read-only"))
        }
    }
}
```

The compiler ensures all entity types are handled. If you add a new entity type to the sealed class, the compiler will force you to handle it in the `when` expression.

### Step-by-Step: Adding a New Entity Type (Example: RGB Light)

1. **Add entity model to `OneControlEntity.kt`:**

```kotlin
data class RgbLight(
    override val tableId: Int,
    override val deviceId: Int,
    val red: Int,
    val green: Int,
    val blue: Int,
    val brightness: Int
) : OneControlEntity() {
    val isOn: Boolean get() = brightness > 0
    val state: String get() = if (isOn) "ON" else "OFF"
    val rgbColor: String get() = "$red,$green,$blue"
}
```

2. **Add entity type enum value:**

```kotlin
enum class EntityType(val haComponent: String, val topicPrefix: String) {
    SWITCH("switch", "switch"),
    LIGHT("light", "light"),
    RGB_LIGHT("light", "rgb_light"),  // NEW
    COVER_SENSOR("sensor", "cover_state"),
    TANK_SENSOR("sensor", "tank"),
    SYSTEM_SENSOR("sensor", "system")
}
```

3. **Add event handler in `processDecodedFrame()`:**

```kotlin
0x09 -> {
    Log.i(TAG, "📦 RgbLightStatus event")
    handleRgbLightStatus(decodedFrame)
}
```

4. **Implement the handler:**

```kotlin
private fun handleRgbLightStatus(data: ByteArray) {
    if (data.size < 7) return
    
    val tableId = data[1].toInt() and 0xFF
    val deviceId = data[2].toInt() and 0xFF
    val red = data[3].toInt() and 0xFF
    val green = data[4].toInt() and 0xFF
    val blue = data[5].toInt() and 0xFF
    val brightness = data[6].toInt() and 0xFF
    
    // Create entity instance
    val entity = OneControlEntity.RgbLight(
        tableId = tableId,
        deviceId = deviceId,
        red = red,
        green = green,
        blue = blue,
        brightness = brightness
    )
    
    // Publish with RGB color
    publishEntityState(
        entityType = EntityType.RGB_LIGHT,
        tableId = entity.tableId,
        deviceId = entity.deviceId,
        discoveryKey = "rgb_${entity.key}",
        state = mapOf(
            "state" to entity.state,
            "brightness" to entity.brightness.toString(),
            "rgb" to entity.rgbColor
        )
    ) { friendlyName, deviceAddr, prefix, baseTopic ->
        val stateTopic = "$baseTopic/device/${entity.tableId}/${entity.deviceId}/state"
        val rgbTopic = "$baseTopic/device/${entity.tableId}/${entity.deviceId}/rgb"
        val commandTopic = "$baseTopic/command/rgb/${entity.tableId}/${entity.deviceId}"
        discoveryBuilder.buildRgbLight(
            deviceAddr = deviceAddr,
            deviceName = friendlyName,
            stateTopic = "$prefix/$stateTopic",
            commandTopic = "$prefix/$commandTopic",
            rgbTopic = "$prefix/$rgbTopic"
        )
    }
}
```

5. **Add discovery builder method in `HomeAssistantMqttDiscovery.kt`:**

```kotlin
fun buildRgbLight(
    deviceAddr: Int,
    deviceName: String,
    stateTopic: String,
    commandTopic: String,
    rgbTopic: String
): JSONObject {
    val uniqueId = "onecontrol_ble_${this@HomeAssistantMqttDiscovery.gatewayMac}_rgb_${deviceAddr.toString(16)}"
    return JSONObject().apply {
        put("unique_id", uniqueId)
        put("name", deviceName)
        put("device", deviceInfo)
        put("state_topic", stateTopic)
        put("command_topic", commandTopic)
        put("rgb_state_topic", rgbTopic)
        put("rgb_command_topic", commandTopic)
        put("schema", "template")
        put("command_on_template", "{ \"state\": \"ON\", \"rgb\": \"{{ red }},{{ green }},{{ blue }}\" }")
        put("command_off_template", "{ \"state\": \"OFF\" }")
        put("icon", "mdi:lightbulb-multiple")
    }
}
```

6. **Add command handling:**

```kotlin
private fun handleEntityCommand(entity: OneControlEntity, payload: String): Result<Unit> {
    return when (entity) {
        is OneControlEntity.Switch -> controlSwitch(...)
        is OneControlEntity.DimmableLight -> controlDimmableLight(...)
        is OneControlEntity.RgbLight -> controlRgbLight(...)  // NEW
        // ... rest of handlers
    }
}

private fun controlRgbLight(tableId: Byte, deviceId: Byte, payload: String): Result<Unit> {
    val json = JSONObject(payload)
    val state = json.optString("state", "")
    val rgb = json.optString("rgb", "")
    
    // Parse RGB values and send command
    // Implementation depends on protocol
    return Result.success(Unit)
}
```

---

## 9. State Management & Status Indicators

### Per-Plugin Status Architecture (v2.3.1+)

Starting in v2.3.1, status indicators are tracked **per-plugin** instead of globally. This allows each plugin (EasyTouch, GoPower, OneControl) to have independent connection and health status in both the app UI and Home Assistant MQTT sensors.

#### PluginStatus Data Class
    
    // UUID of the BLE service that identifies your device
    private val TARGET_SERVICE_UUID = UUID.fromString("...")
    
    override fun matchesDevice(device: BluetoothDevice, scanRecord: ByteArray?): Boolean {
        // Option 1: Match by MAC prefix
        return device.address.startsWith("AA:BB:CC", ignoreCase = true)
        
        // Option 2: Match by service UUID in scan record
        // return scanRecord?.containsServiceUuid(TARGET_SERVICE_UUID) ?: false
    }
    
    override fun getConfiguredDevices(): List<String> {
        // Return MAC addresses to filter scans (required for screen-off scanning)
        return listOf("AA:BB:CC:DD:EE:FF")
    }
    
    override fun createGattCallback(
        device: BluetoothDevice,
        context: Context,
        mqttPublisher: MqttPublisher,
        onDisconnect: (BluetoothDevice, Int) -> Unit
    ): BluetoothGattCallback {
        return MyGattCallback(device, mqttPublisher, onDisconnect)
    }
    
    override fun getMqttBaseTopic(device: BluetoothDevice): String {
        return "mydevice/${device.address}"
    }
    
    override fun handleCommand(
        device: BluetoothDevice,
        commandTopic: String,
        payload: String
    ): Result<Unit> {
        // Route commands to appropriate method
        return Result.success(Unit)
    }
    
    // Inner class: GATT callback that handles all BLE communication
    inner class MyGattCallback(
        private val device: BluetoothDevice,
        private val mqttPublisher: MqttPublisher,
        private val onDisconnect: (BluetoothDevice, Int) -> Unit
    ) : BluetoothGattCallback() {
        
        override fun onConnectionStateChange(gatt: BluetoothGatt, status: Int, newState: Int) {
            when (newState) {
                BluetoothProfile.STATE_CONNECTED -> {
                    mqttPublisher.updateBleStatus(connected = true, paired = false)
                    gatt.discoverServices()
                }
                BluetoothProfile.STATE_DISCONNECTED -> {
                    mqttPublisher.updateBleStatus(connected = false, paired = false)
                    onDisconnect(device, status)
                }
            }
        }
        
        override fun onServicesDiscovered(gatt: BluetoothGatt, status: Int) {
            // Find your characteristics
            // Enable notifications
            // Start reading data
        }
        
        override fun onCharacteristicChanged(
            gatt: BluetoothGatt,
            characteristic: BluetoothGattCharacteristic,
            value: ByteArray
        ) {
            // Process incoming data
            // Publish to MQTT via mqttPublisher
        }
    }
}
```

### Register the Plugin

In `PluginRegistry.kt`:

```kotlin
init {
    registerDevicePluginFactory("my_device") { ctx ->
        MyDevicePlugin()
    }
}
```

---

## 10. Creating New Plugins

### Plugin Template

Starting in v2.3.1, status indicators are tracked **per-plugin** instead of globally. This allows each plugin (EasyTouch, GoPower, OneControl) to have independent connection and health status in both the app UI and Home Assistant MQTT sensors.

**Key Change:** Replaced global `StateFlow` values with a `Map<String, PluginStatus>` keyed by plugin ID.

#### PluginStatus Data Class

**Location:** `app/src/main/java/com/blemqttbridge/mqtt/MqttPublisher.kt`

```kotlin
data class PluginStatus(
    val pluginId: String,
    val connected: Boolean,      // BLE connection status
    val authenticated: Boolean,  // Authentication status (if applicable)
    val dataHealthy: Boolean     // Receiving valid data
)
```

#### BaseBleService State Management

**Location:** `app/src/main/java/com/blemqttbridge/core/BaseBleService.kt`

```kotlin
companion object {
    // Per-plugin status tracking (v2.3.1+)
    private val _pluginStatuses = MutableStateFlow<Map<String, PluginStatus>>(emptyMap())
    val pluginStatuses: StateFlow<Map<String, PluginStatus>> = _pluginStatuses.asStateFlow()
    
    // Global MQTT status (still global as it's shared across plugins)
    private val _mqttConnected = MutableStateFlow(false)
    val mqttConnected: StateFlow<Boolean> = _mqttConnected.asStateFlow()
    
    // Service running status
    private val _serviceRunning = MutableStateFlow(false)
    val serviceRunning: StateFlow<Boolean> = _serviceRunning.asStateFlow()
}
```

#### MqttPublisher Interface Update

Plugins call `updatePluginStatus()` to report their individual status:

```kotlin
interface MqttPublisher {
    // Per-plugin status update (v2.3.1+)
    fun updatePluginStatus(
        pluginId: String,
        connected: Boolean,
        authenticated: Boolean,
        dataHealthy: Boolean
    )
    
    // Deprecated global methods (kept for backwards compatibility)
    @Deprecated("Use updatePluginStatus() instead")
    fun updateBleStatus(connected: Boolean, paired: Boolean)
    
    @Deprecated("Use updatePluginStatus() instead")
    fun updateDiagnosticStatus(dataHealthy: Boolean)
}
```

#### Plugin Implementation Example

Each plugin calls `updatePluginStatus()` with its own ID:

```kotlin
// GoPowerDevicePlugin.kt
class GoPowerGattCallback(...) : BluetoothGattCallback() {
    override fun onConnectionStateChange(gatt: BluetoothGatt, status: Int, newState: Int) {
        when (newState) {
            BluetoothProfile.STATE_CONNECTED -> {
                mqttPublisher.updatePluginStatus(
                    pluginId = "gopower",
                    connected = true,
                    authenticated = true,  // No auth needed for GoPower
                    dataHealthy = isReceivingData
                )
            }
            BluetoothProfile.STATE_DISCONNECTED -> {
                mqttPublisher.updatePluginStatus(
                    pluginId = "gopower",
                    connected = false,
                    authenticated = false,
                    dataHealthy = false
                )
            }
        }
    }
}

// EasyTouchDevicePlugin.kt
mqttPublisher.updatePluginStatus(
    pluginId = "easytouch",
    connected = isConnected,
    authenticated = isAuthenticated,
    dataHealthy = isReceivingData
)

// OneControlDevicePlugin.kt
mqttPublisher.updatePluginStatus(
    pluginId = "onecontrol",
    connected = isConnected,
    authenticated = isPaired,
    dataHealthy = isReceivingData
)
```

### MQTT Diagnostic Sensors (Per-Plugin)

Each plugin publishes its own diagnostic sensors to Home Assistant:

```kotlin
// Published under each plugin's base topic
// Example: homeassistant/sensor/gopower_XX_XX_XX/diag/connected/config

Topic Pattern:
  $baseTopic/diag/connected      - BLE connection status
  $baseTopic/diag/authenticated  - Authentication status
  $baseTopic/diag/data_healthy   - Data health status

// OneControlDevicePlugin.kt example:
private fun publishDiagnosticSensors() {
    val baseTopic = "onecontrol/${device.address}"
    
    // Connected sensor
    publishBinarySensor(
        topic = "$topicPrefix/binary_sensor/$nodeId/diag_connected/config",
        name = "Connected",
        stateTopic = "$topicPrefix/$baseTopic/diag/connected",
        deviceClass = "connectivity"
    )
    
    // Authenticated sensor
    publishBinarySensor(
        topic = "$topicPrefix/binary_sensor/$nodeId/diag_authenticated/config",
        name = "Authenticated",
        stateTopic = "$topicPrefix/$baseTopic/diag/authenticated",
        deviceClass = "connectivity"
    )
    
    // Data healthy sensor
    publishBinarySensor(
        topic = "$topicPrefix/binary_sensor/$nodeId/diag_data_healthy/config",
        name = "Data Healthy",
        stateTopic = "$topicPrefix/$baseTopic/diag/data_healthy",
        deviceClass = "connectivity"
    )
}
```

### SettingsViewModel UI Per-Plugin Status (v2.3.2+)

**Location:** `app/src/main/java/com/blemqttbridge/ui/viewmodel/SettingsViewModel.kt`

The UI shows **per-plugin** status indicators - each plugin card displays its own connection and health status independently:

```kotlin
class SettingsViewModel : ViewModel() {
    // Per-plugin status map (replaces aggregated booleans)
    private val _pluginStatuses = MutableStateFlow<Map<String, BaseBleService.Companion.PluginStatus>>(emptyMap())
    val pluginStatuses: StateFlow<Map<String, BaseBleService.Companion.PluginStatus>> = _pluginStatuses
    
    init {
        viewModelScope.launch {
            BaseBleService.pluginStatuses.collect { statuses ->
                if (_serviceRunningStatus.value) {
                    // Pass through per-plugin statuses
                    _pluginStatuses.value = statuses
                } else {
                    // Service not running - clear all indicators
                    _pluginStatuses.value = emptyMap()
                }
            }
        }
    }
}
```

### SettingsScreen Per-Plugin Indicators

**Location:** `app/src/main/java/com/blemqttbridge/ui/screens/SettingsScreen.kt`

Each plugin card uses a helper function to retrieve its specific status:

```kotlin
// Collect per-plugin statuses
val pluginStatuses by viewModel.pluginStatuses.collectAsState()

// Helper to get status for a specific plugin
fun getPluginStatus(pluginId: String): BaseBleService.Companion.PluginStatus? {
    return if (serviceRunning) pluginStatuses[pluginId] else null
}

// OneControl plugin card
val oneControlStatus = getPluginStatus("onecontrol")
StatusIndicator(label = "Connected", isActive = oneControlStatus?.connected == true)
StatusIndicator(label = "Paired", isActive = oneControlStatus?.authenticated == true)
StatusIndicator(label = "Data Healthy", isActive = oneControlStatus?.dataHealthy == true)

// EasyTouch plugin card  
val easyTouchStatus = getPluginStatus("easytouch")
StatusIndicator(label = "Connected", isActive = easyTouchStatus?.connected == true)
StatusIndicator(label = "Authenticated", isActive = easyTouchStatus?.authenticated == true)
StatusIndicator(label = "Data Healthy", isActive = easyTouchStatus?.dataHealthy == true)

// GoPower plugin card
val goPowerStatus = getPluginStatus("gopower")
StatusIndicator(label = "Connected", isActive = goPowerStatus?.connected == true)
StatusIndicator(label = "Authenticated", isActive = goPowerStatus?.authenticated == true)
StatusIndicator(label = "Data Healthy", isActive = goPowerStatus?.dataHealthy == true)
```

**Plugin IDs:**
- `"onecontrol"` - OneControl RV gateway
- `"easytouch"` - EasyTouch/Micro-Air thermostat
- `"gopower"` - GoPower solar charge controller

### BLE Scanner Conditional Initialization

The BLE Scanner plugin (PoC utility for device discovery) is now **conditionally initialized** based on whether it's enabled in app settings:

```kotlin
// BaseBleService.kt
private suspend fun initializeMultiplePlugins(...) {
    // Only initialize BLE Scanner if enabled
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
```

**Why this matters:** Previously, BLE Scanner would always publish Home Assistant discovery even when disabled in the app, causing it to reappear in HA after being removed. Now it only initializes (and publishes discovery) when explicitly enabled.

### System Diagnostic Sensors

The MQTT bridge device itself publishes comprehensive system diagnostics:

**Location:** `app/src/main/java/com/blemqttbridge/plugins/output/MqttOutputPlugin.kt`

```kotlin
private fun publishSystemDiagnostics() {
    // Battery sensors
    publishDiagnosticSensor("battery", "Battery Level", getBatteryLevel())
    publishDiagnosticSensor("battery_status", "Battery Status", getBatteryStatus())
    publishDiagnosticSensor("battery_temp", "Battery Temperature", getBatteryTemperature())
    
    // Memory sensors
    publishDiagnosticSensor("ram_used", "RAM Used %", getMemoryUsedPercent())
    publishDiagnosticSensor("ram_available", "RAM Available MB", getMemoryAvailableMB())
    
    // CPU sensor
    publishDiagnosticSensor("cpu_usage", "CPU Usage %", getCpuUsage())
    
    // Storage sensors
    publishDiagnosticSensor("storage_available", "Storage Available GB", getStorageAvailableGB())
    publishDiagnosticSensor("storage_used", "Storage Used %", getStorageUsedPercent())
    
    // Network sensors
    publishDiagnosticSensor("wifi_ssid", "WiFi Network", getWifiSSID())
    publishDiagnosticSensor("wifi_rssi", "WiFi Signal dBm", getWifiRSSI())
    
    // Device info sensors
    publishDiagnosticSensor("device_name", "Device Name", Build.MODEL)
    publishDiagnosticSensor("device_manufacturer", "Device Manufacturer", Build.MANUFACTURER)
    publishDiagnosticSensor("android_version", "Android Version", Build.VERSION.RELEASE)
    publishDiagnosticSensor("device_uptime", "Device Uptime Hours", getDeviceUptimeHours())
}
```

All system diagnostic sensors are published under the **"BLE MQTT Bridge"** device in Home Assistant with `entity_category: "diagnostic"`.

### Settings Screen

**Location:** `app/src/main/java/com/blemqttbridge/ui/screens/SettingsScreen.kt`

- Toggle enables/disables service
- When toggle ON: settings fields locked (grayed out)
- When toggle OFF: settings editable
- Confirmation dialog for destructive actions (removing BLE Scanner)
- Status indicators show per-plugin status (each plugin card has independent indicators)

---

## 11. Debug Logging & Performance

### Overview

The app implements trace-aware conditional debug logging to optimize production performance while preserving full debugging capabilities when needed. This system reduces CPU overhead, battery consumption, and logcat buffer pressure in production builds.

### DebugLog Utility

**Location:** `app/src/main/java/com/blemqttbridge/util/DebugLog.kt`

The `DebugLog` utility provides conditional debug logging that respects BLE trace capture mode:

```kotlin
object DebugLog {
    /**
     * Check if debug logging should be enabled.
     * Returns true if in debug build OR trace capture is active.
     */
    @PublishedApi
    internal fun isDebugEnabled(): Boolean {
        return BuildConfig.DEBUG || BaseBleService.traceActive.value
    }
    
    /**
     * Log at DEBUG level - only outputs if debug enabled or trace active.
     */
    fun d(tag: String, message: String) {
        if (isDebugEnabled()) {
            Log.d(tag, message)
        }
    }
    
    /**
     * Lazy evaluation variant - for expensive log operations.
     */
    inline fun d(tag: String, message: () -> String) {
        if (isDebugEnabled()) {
            Log.d(tag, message())
        }
    }
    
    // INFO, WARN, ERROR logs always output (not gated)
    fun i(tag: String, message: String) = Log.i(tag, message)
    fun w(tag: String, message: String) = Log.w(tag, message)
    fun e(tag: String, message: String) = Log.e(tag, message)
}
```

### Usage Guidelines

#### When to Use DebugLog

Use `DebugLog.d()` for:
- Verbose protocol-level logging (parsing, packet details)
- High-frequency polling operations
- State transitions and intermediate values
- Connection flow details
- Any debug information not critical for production troubleshooting

#### When to Use Standard Log

Use standard `Log.i/w/e()` for:
- Connection/disconnection events
- Authentication success/failure
- Discovery publication
- Command handling
- Error conditions
- Any information needed for production troubleshooting

#### Example Conversion

```kotlin
// BEFORE (always executes)
Log.d(TAG, "Polling status...")
Log.d(TAG, "Received chunk: $chunk")
Log.d(TAG, "Device matched by MAC: $deviceAddress")

// AFTER (only when trace active or debug build)
DebugLog.d(TAG, "Polling status...")
DebugLog.d(TAG, "Received chunk: $chunk")
DebugLog.d(TAG, "Device matched by MAC: $deviceAddress")

// Lazy evaluation for expensive operations
DebugLog.d(TAG) { "Complex parsing result: ${expensiveOperation()}" }
```

### BLE Trace Capture

**Location:** `BaseBleService.traceActive` StateFlow

The trace capture feature allows users to enable full debug logging on-demand:

```kotlin
companion object {
    const val ACTION_START_TRACE = "com.blemqttbridge.START_TRACE"
    const val ACTION_STOP_TRACE = "com.blemqttbridge.STOP_TRACE"
    
    private val _traceActive = MutableStateFlow(false)
    val traceActive: StateFlow<Boolean> = _traceActive
}
```

When users enable trace capture from the Settings UI:
1. `traceActive` StateFlow updates to `true`
2. All `DebugLog.d()` calls begin outputting
3. Users can capture detailed logs for troubleshooting
4. Disabling trace returns to production logging

### Performance Impact

#### Before Optimization
- ~100 debug log statements executed unconditionally
- String interpolation happened before log level check
- Emoji UTF-8 encoding overhead in 80+ log statements
- Polling plugins (EasyTouch: 4s, GoPower: 1s) generated constant debug logs

#### After Optimization
- Debug logs conditionally execute only when needed
- String interpolation skipped when logging disabled
- Clean ASCII-only log messages
- Lazy evaluation support for expensive operations

#### Estimated Savings
- **CPU:** Eliminated string interpolation for ~100 debug logs in production
- **Battery:** Reduced logcat write operations by ~60-90% in production
- **Memory:** Lower logcat buffer pressure and churn

### Emoji Removal

All emoji characters have been removed from log statements for performance:

```kotlin
// BEFORE
Log.i(TAG, "✅ Connected to ${device.address}")
Log.d(TAG, "📡 Polling status...")
Log.i(TAG, "🔄 Starting status polling loop")

// AFTER
Log.i(TAG, "Connected to ${device.address}")
DebugLog.d(TAG, "Polling status...")
Log.i(TAG, "Starting status polling loop")
```

**Rationale:**
- UTF-8 encoding overhead for multi-byte emoji characters
- Font rendering issues in some logcat viewers
- Reduced log entry sizes
- Better compatibility with automated log parsing tools

### Affected Files

- `EasyTouchDevicePlugin.kt`: 30+ debug logs converted
- `GoPowerDevicePlugin.kt`: 15+ debug logs converted
- `OneControlDevicePlugin.kt`: DebugLog import added for future use
- All emojis removed from plugin log statements (80+ occurrences)

### Best Practices

1. **Use DebugLog for high-frequency operations**: Polling loops, packet parsing, state checks
2. **Keep INFO/WARN/ERROR always visible**: Connection events, errors, command handling
3. **Lazy evaluation for expensive logs**: Use `DebugLog.d(TAG) { ... }` when log message construction is costly
4. **No emojis**: Use plain ASCII text in all log statements
5. **Respect trace mode**: Design plugins assuming debug logs are conditionally enabled

---

## 12. Common Pitfalls & Solutions

### 1. MQTT Publish Exceptions

**Problem:** App crashes when MQTT is disconnected

**Solution:** Never use `resumeWithException()` in publish functions:

```kotlin
// WRONG:
continuation.resumeWithException(exception ?: Exception("Publish failed"))

// RIGHT:
Log.e(TAG, "Publish failed", exception)
continuation.resume(Unit)  // Graceful degradation
```

### 2. Stale Status Indicators

**Problem:** UI shows "connected" after service stops

**Solution:** Gate all status updates by `serviceRunning`:

```kotlin
if (_serviceRunningStatus.value) {
    _bleConnectedStatus.value = connected
}
```

### 3. BLE GATT Error 133

**Problem:** Connections fail with status 133 after bonding changes

**Solution:** 
- Call `refreshGattCache(gatt)` before service discovery
- Close existing GATT before reconnecting
- Don't call `createBond()` explicitly

### 4. Wrong Byte Order

**Problem:** Authentication fails

**Solution:** OneControl uses BIG-ENDIAN for auth key:

```kotlin
// Challenge parsing: BIG-ENDIAN
val seedBigEndian = ((data[0].toInt() and 0xFF) shl 24) or
                   ((data[1].toInt() and 0xFF) shl 16) or
                   ((data[2].toInt() and 0xFF) shl 8) or
                   ((data[3].toInt() and 0xFF) shl 0)
```

### 5. Missing COBS Encoding

**Problem:** Commands don't work

**Solution:** All commands must be COBS-encoded with CRC:

```kotlin
val rawCommand = MyRvLinkCommandBuilder.buildActionSwitch(...)
val encoded = CobsDecoder.encode(rawCommand, prependStartFrame = true, useCrc = true)
writeChar.value = encoded  // NOT rawCommand!
```

### 6. Notification Subscription Race Condition

**Problem:** Notifications not received

**Solution:** Use delays between descriptor writes:

```kotlin
// Small delay before writing descriptor
handler.postDelayed({
    descriptor.value = BluetoothGattDescriptor.ENABLE_NOTIFICATION_VALUE
    gatt.writeDescriptor(descriptor)
}, 100)
```

### 7. Dimmable Bouncing

**Problem:** Slider bounces back during adjustment

**Solution:** Use pending guard and debouncing:

```kotlin
// Track pending commands
pendingDimmable[key] = targetBrightness to System.currentTimeMillis()

// Ignore mismatching status updates during window
if (age <= DIMMER_PENDING_WINDOW_MS && brightness != desired) {
    return  // Don't publish stale status
}
```

### 8. BLE Trace Logging

**Added in v2.4.7:** All plugins now implement BLE event logging via `mqttPublisher.logBleEvent()`:

```kotlin
// In GATT callbacks - all plugins
override fun onConnectionStateChange(gatt: BluetoothGatt, status: Int, newState: Int) {
    val stateStr = when (newState) {
        BluetoothProfile.STATE_CONNECTED -> "CONNECTED"
        BluetoothProfile.STATE_DISCONNECTED -> "DISCONNECTED"
        else -> "UNKNOWN($newState)"
    }
    mqttPublisher.logBleEvent("STATE_CHANGE: $stateStr (status=$status)")
}

override fun onCharacteristicChanged(gatt: BluetoothGatt, characteristic: BluetoothGattCharacteristic) {
    val hex = data.joinToString(" ") { "%02X".format(it) }
    mqttPublisher.logBleEvent("NOTIFY ${characteristic.uuid}: $hex")
}

override fun onCharacteristicWrite(gatt: BluetoothGatt, characteristic: BluetoothGattCharacteristic, status: Int) {
    val hex = characteristic.value?.joinToString(" ") { "%02X".format(it) } ?: "(null)"
    mqttPublisher.logBleEvent("WRITE ${characteristic.uuid}: $hex (status=$status)")
}
```

**Purpose:** Users can send trace files showing BLE traffic from their specific hardware variants (multi-zone thermostats, different heat modes, etc.) for debugging and adding support for new device features.

**Trace File Location:** `/sdcard/Download/trace_YYYYMMDD_HHMMSS.log`

**Example Trace Output:**
```
19:03:23.085 TRACE START ts=20260106_190323
19:03:23.180 NOTIFY 00000034-0200-a58e-e411-afe28044e62c: 00 C5 06 08 08 80 FF 40 01 6D 00
19:03:24.593 WRITE 00000033-0200-a58e-e411-afe28044e62c: 00 41 06 42 01 08 02 FF E8 00 (status=0)
19:03:26.399 WRITE 0000fff2-0000-1000-8000-00805f9b34fb: 20 (status=0)
19:03:26.656 WRITE 0000ee01-0000-1000-8000-00805f9b34fb: 7B 22 54 79 70 65 22 3A... (status=0)
```

---

## 13. Background Operation

**The app runs as a foreground service, not a foreground app.** This means:
- ✅ Continues running when you switch to other apps (e.g., Fully Kiosk Browser)
- ✅ Maintains BLE connections and MQTT publishing in the background
- ✅ Shows a persistent notification (required by Android)
- ✅ Survives screen-off and deep sleep (with proper configuration)
- ✅ No need to keep the app visible - perfect for kiosk setups

### Battery Optimization & Background Execution

**v2.3.6+ includliability

### Battery Optimization & Background Execution

**v2.3.6 introduces comprehensive service hardening features** to prevent Android from killing the app during idle periods:

#### Defense Layers

1. **Battery Optimization Exemption**
   - Prevents Doze mode from stopping the service
   - Configure via Settings (⚙️) → Battery Optimization Exemption
   - **Critical for overnight operation**

2. **Bluetooth State Monitoring**
   - Automatically detects when OS disables Bluetooth (common on Samsung/TCL devices)
   - Graceful cleanup when BT turns off
   - Auto-reconnect when BT turns back on

3. **WorkManager Watchdog**
   - Checks service health every 15 minutes
   - Auto-restarts service if killed by OS
   - Survives device reboots

4. **Adaptive Scanning**
   - Uses BLE scan filters for screen-off operation
   - Maintains connections during deep sleep

5. **Doze Mode Keepalive (v2.3.6+)**
   - **Prevents BLE connections from dropping during deep sleep**
   - Sends periodic keepalive pings every 30 minutes
   - Uses `AlarmManager.setExactAndAllowWhileIdle()` to wake device during Doze
   - **Enabled by default** - ideal for mains-powered devices
   - **Toggle in Settings:** System Settings (⚙️) → Doze Mode Prevention → Keepalive Pings
   
   **When to enable:**
   - ✅ Device is plugged in/mains-powered (recommended)
   - ✅ You need 24/7 BLE connectivity without manual intervention
   - ✅ RV/boat/camper installations with shore power
   
   **When to disable:**
   - ❌ Battery-powered devices where power savings are critical
   - ❌ You only use the app occasionally (connections auto-reconnect on device wake)

6. **Android TV Power Fix (v2.4.6+)**
   - **Prevents service from being killed when TV enters standby**
   - HDMI-CEC sends "standby" command to streaming devices when TV powers off
   - This setting disables the device's response to CEC standby commands
   - **Requires ADB permission grant (one-time setup)**
   - Auto-applies on service start once permission is granted
   
   **Setup (via ADB):**
   ```bash
   adb shell pm grant com.blemqttbridge android.permission.WRITE_SECURE_SETTINGS
   ```
   
   **Or manually disable CEC:**
   ```bash
   adb shell settings put global hdmi_control_auto_device_off_enabled 0
   ```
   
   **UI Access:** Settings → System Settings → Android TV Power Fix (only visible on Android TV devices)

#### Recommended Settings

For maximum reliability on aggressive battery management devices (Samsung, Xiaomi, OnePlus, etc.):

1. **Battery Optimization:** Set to "Active - Service protected"
2. **Keepalive Pings:** Enable for mains-powered setups (Settings → Doze Mode Prevention)
3. **Auto-start:** Enable in device settings if available
4. **Background restrictions:** Disable for this app
5. **Data saver:** Add app to exception list

---

## Appendix: File Quick Reference

| File | Purpose |
|------|---------|
| `BaseBleService.kt` | Foreground service, plugin orchestration, per-plugin status tracking |
| `PluginRegistry.kt` | Plugin factory management |
| `BleDevicePlugin.kt` | Plugin interface definition |
| `MqttPublisher.kt` | MQTT abstraction for plugins, status reporting interface |
| `ServiceStateManager.kt` | Plugin enable/disable state management |
| **OneControl Plugin** | |
| `OneControlDevicePlugin.kt` | Main OneControl implementation, **guard checks for entity republishing** |
| `protocol/Constants.kt` | UUIDs, encryption constants |
| `protocol/TeaEncryption.kt` | Authentication encryption |
| `protocol/CobsDecoder.kt` | Frame encoding |
| `protocol/CobsByteDecoder.kt` | Frame decoding |
| `protocol/MyRvLinkCommandBuilder.kt` | Command builders |
| `protocol/HomeAssistantMqttDiscovery.kt` | HA discovery payloads |
| **EasyTouch Plugin** | |
| `EasyTouchDevicePlugin.kt` | EasyTouch thermostat implementation |
| `protocol/EasyTouchConstants.kt` | UUIDs, status indices, mode mappings |
| **GoPower Plugin** | |
| `GoPowerDevicePlugin.kt` | GoPower solar controller implementation |
| `protocol/GoPowerConstants.kt` | Service/characteristic UUIDs, field indices |
| `protocol/GoPowerGattCallback.kt` | BLE notification handler, data parsing |
| **BLE Scanner Plugin** | |
| `BleScannerPlugin.kt` | Device discovery utility, **multi-gateway device ID suffix** |
| **Output & UI** | |
| `MqttOutputPlugin.kt` | Paho MQTT client wrapper, system diagnostics, **multi-gateway device ID suffix** |
| `SettingsViewModel.kt` | UI state management, status aggregation |
| `SettingsScreen.kt` | Main settings UI |

---

*Document version: 2.4.8*  
*Last updated: January 7, 2026 - Multi-gateway support, OneControl guard check fix*  
*See [GitHub Releases](https://github.com/phurth/ble-plugin-bridge/releases) for complete version history*
