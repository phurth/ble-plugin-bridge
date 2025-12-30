# BLE Plugin Bridge - Internal Architecture Documentation

> **Purpose:** This document provides comprehensive technical documentation for the BLE Plugin Bridge Android application. It is designed to enable future LLM-assisted development, particularly for adding new entity types to the OneControl plugin or creating entirely new device plugins.

> **Last Updated:** December 30, 2025 - v2.3.8  
> **Major Changes (v2.3.8):**  
> - **Text Input Fix:** Fixed cursor jumping on keystroke by using local mutableStateOf() instead of collectAsState()
> - **Permission Fixes:** Added SCHEDULE_EXACT_ALARM, USE_EXACT_ALARM, and BLUETOOTH_SCAN neverForLocation flag
> - **Automatic Permissions:** Added checkAndRequestPermissionsOnStartup() in MainActivity.onCreate()
> - **OneControl DataHealthy:** Fixed bouncing indicator by removing time-based check for event-driven plugin
> 
> **Previous Changes (v2.3.7):**  
> - **Keepalive Scheduling Fix:** Fixed critical bug where keepalive would not schedule if service started from UI instead of explicit intent
> - **Multi-path Scheduling:** Keepalive now schedules in onCreate() as backup, onStartCommand with ACTION_START_SCAN, and null action handler
> - **Android TV Compatibility:** Added optional touchscreen and leanback features for Android TV box support
> - **Improved Logging:** Better startup path tracking and keepalive status visibility
>
> **Changes (v2.3.6):**  
> - **Service Hardening:** Added battery optimization exemption, WorkManager watchdog, and Bluetooth state monitoring
> - **UI Reorganization:** Split settings into main screen (plugins) and system settings screen (permissions/diagnostics)
> - **Battery Optimization Helper:** Utility class for checking and requesting battery exemptions
> - **Bluetooth State Recovery:** BroadcastReceiver monitors BT on/off events, graceful cleanup and auto-reconnect
> - **WorkManager Watchdog:** Periodic health checks (15min) with auto-restart if service killed by OS
> - **Lifecycle-aware State Refresh:** Permission/battery states auto-refresh when returning from system settings
> - **Improved UI:** Settings gear icon, collapsible plugin settings sections with icons
> - **Smart Fallback:** Permanently denied permissions → auto-open app settings page

---

## Table of Contents

1. [High-Level Architecture](#1-high-level-architecture)
2. [Service Layer](#2-service-layer)
3. [Plugin System](#3-plugin-system)
4. [OneControl Protocol Deep Dive](#4-onecontrol-protocol-deep-dive)
5. [EasyTouch Thermostat Protocol](#5-easytouch-thermostat-protocol)
6. [GoPower Solar Controller Protocol](#6-gopower-solar-controller-protocol)
7. [MQTT Integration](#7-mqtt-integration)
8. [Home Assistant Discovery](#8-home-assistant-discovery)
9. [State Management & Status Indicators](#9-state-management--status-indicators)
10. [Debug Logging & Performance](#10-debug-logging--performance)
11. [Adding New Entity Types](#11-adding-new-entity-types)
12. [Creating New Plugins](#12-creating-new-plugins)
13. [Common Pitfalls](#13-common-pitfalls)

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

**File:** `OneControlDevicePlugin.kt`, method `startAuthentication()`

```
1. onServicesDiscovered() 
   ↓
2. Request MTU = 185
   ↓
3. onMtuChanged() → startAuthentication()
   ↓
4. Read UNLOCK_STATUS (00000012) 
   ↓
5. handleUnlockStatusRead() - receives 4-byte challenge
   ↓
6. calculateAuthKey(challenge) - BIG-ENDIAN TEA encryption
   ↓
7. Write KEY to 00000013 (WRITE_TYPE_NO_RESPONSE)
   ↓
8. Re-read UNLOCK_STATUS - should now return "Unlocked"
   ↓
9. enableDataNotifications() - subscribe to 00000034
   ↓
10. onAllNotificationsSubscribed() → startActiveStreamReading()
```

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

### Event Parsing Example: Relay Status

```kotlin
private fun handleRelayStatus(data: ByteArray) {
    if (data.size < 5) return
    
    val tableId = data[1].toInt() and 0xFF    // Device table ID
    val deviceId = data[2].toInt() and 0xFF   // Device ID within table
    val statusByte = data[3].toInt() and 0xFF
    val rawOutputState = statusByte and 0x0F  // State in LOW nibble
    val isOn = rawOutputState == 0x01         // 0x01 = ON, 0x00 = OFF
    
    // Publish to MQTT
    val stateTopic = "onecontrol/${device.address}/device/$tableId/$deviceId/state"
    mqttPublisher.publishState(stateTopic, if (isOn) "ON" else "OFF", true)
    
    // Publish HA discovery if first time seeing this device
    val keyHex = "%02x%02x".format(tableId, deviceId)
    val discoveryKey = "switch_$keyHex"
    if (haDiscoveryPublished.add(discoveryKey)) {
        val discovery = HomeAssistantMqttDiscovery.getSwitchDiscovery(...)
        mqttPublisher.publishDiscovery(discoveryTopic, discovery.toString())
    }
}
```

### Event Parsing Example: Dimmable Light

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
| Connection Type | BLE notifications only (read-only) |
| Pairing | Not required (no bonding needed) |
| Authentication | None |
| Service UUID | `0000fff0-0000-1000-8000-00805f9b34fb` |
| Notify UUID | `0000fff1-0000-1000-8000-00805f9b34fb` |
| Data Format | Fixed 20-byte comma-delimited ASCII string |
| Update Rate | ~1 second (automatic from controller) |
| Encoding | ASCII text, fields separated by commas |

### Notification Data Format

The controller sends a 20-byte notification containing comma-separated integer values:

```
Format: "f0,f1,f2,f3,f4,f5,f6,f7,f8,f9,f10,f11,f12,f13,f14,f15"
Example: "123,456,789,0,1,2,3,4,5,6,7,8,9,10,ABC,16"

Field Mapping (0-indexed):
Field 0:  PV Voltage (integer, divide by 10 for actual voltage)
Field 1:  Battery Voltage (integer, divide by 10 for actual voltage)
Field 2:  Charge Current (integer, divide by 100 for actual amperage)
Field 3:  Controller Temperature (integer, degrees Celsius)
Field 4:  Battery Percentage (integer, 0-100%)
Field 5:  Load Current (integer, divide by 100 for actual amperage)
Field 6:  Load Status (integer, 0=off, 1=on)
Field 7:  Unknown / Reserved
Field 8:  Firmware Version (integer, e.g., 5 = v5)
Field 9:  Unknown / Reserved
Field 10: Unknown / Reserved
Field 11: Unknown / Reserved
Field 12: Unknown / Reserved
Field 13: Unknown / Reserved
Field 14: Serial Number (hex string, needs conversion to decimal)
Field 15: Unknown / Reserved
```

### Key Implementation Details

#### Serial Number Hex Parsing

Field 14 contains the device serial number as a **hexadecimal string** that must be converted to decimal:

```kotlin
// GoPowerConstants.kt
const val FIELD_SERIAL = 14
const val FIELD_FIRMWARE = 8

// GoPowerDevicePlugin.kt
private fun parseStatusNotification(data: ByteArray) {
    val dataString = String(data, Charsets.UTF_8).trim()
    val fields = dataString.split(",")
    
    if (fields.size >= FIELD_SERIAL + 1) {
        val serialHex = fields[FIELD_SERIAL].trim()
        try {
            // Convert hex string to decimal (e.g., "177" hex -> "375" decimal)
            deviceSerial = serialHex.toInt(16).toString()
        } catch (e: NumberFormatException) {
            Log.w(TAG, "Failed to parse serial number: $serialHex")
            deviceSerial = "unknown"
        }
    }
}
```

**Important:** The official GoPower app displays serial numbers in **decimal**, not hex. Field 14's raw value (e.g., `"177"`) must be interpreted as hexadecimal (`0x177 = 375 decimal`) to match the official app.

#### Model Number Detection

The GoPower controller does not report its model number via BLE. The plugin derives the model from the serial number prefix:

```kotlin
// Serial numbers starting with "3" are GP-PWM-30-SB model
deviceModel = when {
    deviceSerial.startsWith("3") -> "GP-PWM-30-SB"
    else -> "GP-PWM-30-SB"  // Default assumption
}
```

This heuristic may need adjustment as more models are supported.

#### Data Parsing and Validation

```kotlin
private fun parseStatusNotification(data: ByteArray) {
    val dataString = String(data, Charsets.UTF_8).trim()
    val fields = dataString.split(",")
    
    // Validate field count
    if (fields.size < 15) {
        Log.w(TAG, "Incomplete data: ${fields.size} fields")
        return
    }
    
    try {
        // Parse sensors with scaling
        val pvVoltage = fields[0].toIntOrNull()?.let { it / 10.0f } ?: 0f
        val batteryVoltage = fields[1].toIntOrNull()?.let { it / 10.0f } ?: 0f
        val chargeCurrent = fields[2].toIntOrNull()?.let { it / 100.0f } ?: 0f
        val batteryPercent = fields[4].toIntOrNull() ?: 0
        val loadCurrent = fields[5].toIntOrNull()?.let { it / 100.0f } ?: 0f
        val temperature = fields[3].toIntOrNull() ?: 0
        val loadOn = fields[6].toIntOrNull() == 1
        
        // Calculate derived values
        val chargeWatts = pvVoltage * chargeCurrent
        val loadWatts = batteryVoltage * loadCurrent
        
        // Publish to MQTT
        publishSensorData(pvVoltage, batteryVoltage, chargeCurrent, ...)
        
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
| Sensor | `battery_voltage` | Battery voltage | V | voltage |
| Sensor | `charge_current` | Charge current | A | current |
| Sensor | `charge_watts` | Charge power (calculated) | W | power |
| Sensor | `battery_percent` | Battery state of charge | % | battery |
| Sensor | `load_current` | Load current | A | current |
| Sensor | `load_watts` | Load power (calculated) | W | power |
| Sensor | `controller_temp` | Controller temperature | °C | temperature |
| Binary Sensor | `load_status` | Load output status | - | power |
| Text Sensor | `device_model` | Device model number | - | diagnostic |
| Text Sensor | `device_serial` | Device serial number (decimal) | - | diagnostic |
| Text Sensor | `device_firmware` | Firmware version | - | diagnostic |

### Plugin Structure

```
plugins/gopower/
├── GoPowerDevicePlugin.kt          # Main plugin implementation
├── protocol/
│   ├── GoPowerConstants.kt         # UUIDs and field indices
│   └── GoPowerGattCallback.kt      # BLE notification handler
```

### Configuration Requirements

Unlike OneControl, GoPower does not require pairing or authentication:

1. Configure the controller MAC address in app settings
2. Enable the GoPower plugin
3. Enable the BLE Service

The controller will immediately start sending status notifications.

### Key Differences from Other Plugins

| Feature | OneControl | EasyTouch | GoPower |
|---------|-----------|-----------|---------|
| Pairing Required | Yes (bonding) | No | No |
| Authentication | TEA encryption | Password auth | None |
| Commands | Full bidirectional | Climate control | **None (read-only)** |
| Notification Type | Event-driven | JSON status | Fixed ASCII string |
| Data Format | COBS-encoded binary | JSON | Comma-delimited ASCII |
| Heartbeat Needed | Yes (GetDevices) | No | No |
| Multi-device | Yes (gateway) | Yes (zones) | No (single controller) |

### Diagnostic Sensors

The GoPower plugin adds three diagnostic text sensors for device identification and troubleshooting:

```kotlin
// Published on connect and when data is first received
publishDiagnosticSensor(
    objectId = "device_model",
    name = "Device Model",
    value = deviceModel  // "GP-PWM-30-SB"
)

publishDiagnosticSensor(
    objectId = "device_serial", 
    name = "Device Serial",
    value = deviceSerial  // Decimal format (e.g., "375")
)

publishDiagnosticSensor(
    objectId = "device_firmware",
    name = "Device Firmware",
    value = deviceFirmware  // Integer version (e.g., "5")
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
| Serial shows "unknown" | Field 14 parse error | Check notification format matches spec |
| Wrong serial number | Decimal/hex confusion | Ensure using `toInt(16)` conversion |
| Sensors not appearing | Discovery not published | Check MQTT logs, verify baseTopic |
| Load status incorrect | Field 6 not 0 or 1 | Validate notification data format |

### Future Enhancements

Potential additions if GoPower releases command specifications:
- Load output control (ON/OFF)
- Charging parameters configuration
- Battery type selection
- Temperature compensation settings

Currently, the GoPower protocol is **read-only** - the controller does not accept write commands via BLE.

---

## 7. MQTT Integration

### MqttOutputPlugin

**Location:** `app/src/main/java/com/blemqttbridge/plugins/output/MqttOutputPlugin.kt`

Uses Eclipse Paho MQTT client with:
- Automatic reconnect
- LWT (Last Will and Testament) for availability
- QoS 1 for reliability

#### Topic Structure

```
homeassistant/                              # prefix (configurable)
├── availability                            # online/offline LWT
├── onecontrol/{MAC}/                       # device namespace
│   ├── status                              # ready/offline
│   ├── gateway                             # gateway info JSON
│   ├── system/
│   │   ├── voltage                         # system voltage
│   │   └── temperature                     # system temperature
│   ├── device/{tableId}/{deviceId}/
│   │   ├── state                           # ON/OFF
│   │   ├── brightness                      # 0-255 (dimmables)
│   │   ├── level                           # 0-100 (tanks)
│   │   └── hvac                            # HVAC JSON
│   ├── command/{type}/{tableId}/{deviceId} # commands from HA
│   │   └── brightness                      # brightness subcommand
│   └── events/                             # generic events
└── diag/                                   # diagnostic sensors
    ├── device_paired
    ├── ble_connected
    └── data_healthy
```

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

### Step-by-Step: Adding Cover Support (Legacy Example)

1. **Add event handler in `processDecodedFrame()`:**

```kotlin
0x0D, 0x0E -> {
    Log.i(TAG, "📦 RelayHBridgeStatus event (cover)")
    handleHBridgeStatus(decodedFrame)
}
```

2. **Implement the handler:**

```kotlin
private fun handleHBridgeStatus(data: ByteArray) {
    if (data.size < 4) return
    
    val tableId = data[1].toInt() and 0xFF
    val deviceId = data[2].toInt() and 0xFF
    val status = data[3].toInt() and 0xFF
    
    // Map status to HA cover state
    val haState = when (status) {
        0xC2 -> "opening"
        0xC3 -> "closing"
        0xC0 -> "stopped"
        else -> "unknown"
    }
    
    // Publish state
    val baseTopic = "onecontrol/${device.address}"
    val stateTopic = "$baseTopic/device/$tableId/$deviceId/state"
    mqttPublisher.publishState(stateTopic, haState, true)
    
    // Publish discovery
    val keyHex = "%02x%02x".format(tableId, deviceId)
    val discoveryKey = "cover_$keyHex"
    if (haDiscoveryPublished.add(discoveryKey)) {
        val discovery = HomeAssistantMqttDiscovery.getCoverDiscovery(...)
        mqttPublisher.publishDiscovery(discoveryTopic, discovery.toString())
    }
}
```

3. **Add discovery method in `HomeAssistantMqttDiscovery.kt`:**

```kotlin
fun getCoverDiscovery(
    gatewayMac: String,
    deviceAddr: Int,
    deviceName: String,
    stateTopic: String,
    commandTopic: String,
    positionTopic: String,
    appVersion: String?
): JSONObject {
    return JSONObject().apply {
        put("unique_id", "onecontrol_ble_${gatewayMac}_cover_${deviceAddr.toString(16)}")
        put("name", deviceName)
        put("device", getDeviceInfo(gatewayMac, appVersion))
        put("state_topic", stateTopic)
        put("command_topic", commandTopic)
        put("position_topic", positionTopic)
        put("payload_open", "OPEN")
        put("payload_close", "CLOSE")
        put("payload_stop", "STOP")
        put("icon", "mdi:window-shutter")
    }
}
```

4. **Add command builder in `MyRvLinkCommandBuilder.kt`:**

```kotlin
fun buildActionCover(
    clientCommandId: UShort,
    deviceTableId: Byte,
    deviceId: Byte,
    action: CoverAction  // OPEN, CLOSE, STOP
): ByteArray {
    val actionByte = when (action) {
        CoverAction.OPEN -> 0x01.toByte()
        CoverAction.CLOSE -> 0x02.toByte()
        CoverAction.STOP -> 0x00.toByte()
    }
    
    return byteArrayOf(
        (clientCommandId.toInt() and 0xFF).toByte(),
        ((clientCommandId.toInt() shr 8) and 0xFF).toByte(),
        0x4?.toByte(),  // CommandType: ActionCover (needs verification)
        deviceTableId,
        deviceId,
        actionByte
    )
}
```

5. **Add command handler:**

```kotlin
// In handleCommand():
"cover" -> controlCover(tableId.toByte(), deviceId.toByte(), payload)

// New method:
private fun controlCover(tableId: Byte, deviceId: Byte, payload: String): Result<Unit> {
    val action = when (payload.uppercase()) {
        "OPEN" -> CoverAction.OPEN
        "CLOSE" -> CoverAction.CLOSE
        "STOP" -> CoverAction.STOP
        else -> return Result.failure(Exception("Invalid cover action"))
    }
    
    val commandId = getNextCommandId()
    val command = MyRvLinkCommandBuilder.buildActionCover(commandId, tableId, deviceId, action)
    val encoded = CobsDecoder.encode(command, prependStartFrame = true, useCrc = true)
    
    dataWriteChar?.let { char ->
        char.value = encoded
        char.writeType = BluetoothGattCharacteristic.WRITE_TYPE_NO_RESPONSE
        currentGatt?.writeCharacteristic(char)
    }
    
    return Result.success(Unit)
}
```

---

## 12. Creating New Plugins

### Plugin Template

```kotlin
class MyDevicePlugin : BleDevicePlugin {
    override val pluginId = "my_device"
    override val displayName = "My Device"
    
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

## 9. State Management & Status Indicators

### Per-Plugin Status Architecture (v2.3.1+)

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

## 10. Debug Logging & Performance

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

## 11. Adding New Entity Types

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
| `OneControlDevicePlugin.kt` | Main OneControl implementation |
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
| `BleScannerPlugin.kt` | Device discovery utility (conditional initialization) |
| **Output & UI** | |
| `MqttOutputPlugin.kt` | Paho MQTT client wrapper, system diagnostics |
| `SettingsViewModel.kt` | UI state management, status aggregation |
| `SettingsScreen.kt` | Main settings UI |

---

*Document version: 2.3.1*
*Last updated: December 26, 2025 - Added GoPower plugin, per-plugin status tracking, system diagnostics*
