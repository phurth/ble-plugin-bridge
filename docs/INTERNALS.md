# BLE Plugin Bridge - Internal Architecture Documentation

> **Purpose:** This document provides comprehensive technical documentation for the BLE Plugin Bridge Android application. It is designed to enable future LLM-assisted development, particularly for adding new entity types to the OneControl plugin or creating entirely new device plugins.

---

## Table of Contents

1. [High-Level Architecture](#1-high-level-architecture)
2. [Service Layer](#2-service-layer)
3. [Plugin System](#3-plugin-system)
4. [OneControl Protocol Deep Dive](#4-onecontrol-protocol-deep-dive)
5. [EasyTouch Thermostat Protocol](#5-easytouch-thermostat-protocol)
6. [MQTT Integration](#6-mqtt-integration)
7. [Home Assistant Discovery](#7-home-assistant-discovery)
8. [Adding New Entity Types](#8-adding-new-entity-types)
9. [Creating New Plugins](#9-creating-new-plugins)
10. [State Management & UI](#10-state-management--ui)
11. [Common Pitfalls](#11-common-pitfalls)

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

The OneControl BLE Gateway uses a proprietary protocol for RV device control. Understanding this is critical for adding new entity support.

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

**Reference:** Official Android app `U_Thermostat.java` (decompiled)

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
// PRM[1] bit flags (from decompiled bytecode analysis)
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

```kotlin
private fun publishDiscovery(zone: Int, totalZones: Int) {
    val zoneName = if (totalZones > 1) "Zone $zone" else "Climate"
    val uniqueId = "easytouch_${serialNumber}_zone$zone"
    
    val discovery = JSONObject().apply {
        put("name", zoneName)
        put("object_id", uniqueId)  // Prevents entity ID duplication
        put("unique_id", uniqueId)
        put("modes", JSONArray(listOf("off", "heat", "cool", "heat_cool", "fan_only")))
        put("fan_modes", JSONArray(listOf("auto", "low", "medium", "high")))
        put("temperature_unit", "F")
        put("min_temp", 50)
        put("max_temp", 99)
        // ... state/command topics
    }
}
```

#### State Publishing

```kotlin
private fun publishZoneState(zone: Int) {
    val state = JSONObject().apply {
        put("current_temperature", ambientTemp)
        put("temperature", setpointTemp)
        put("mode", getHvacMode(mode, systemPower))
        put("fan_mode", getFanMode(fanSpeed))
        put("action", if (isRunning) getAction(mode) else "idle")
    }
    
    mqttPublisher.publishState(stateTopic, state.toString(), true)
}
```

### Command Handling

```kotlin
override fun handleCommand(commandTopic: String, payload: String): Result<Unit> {
    return when {
        commandTopic.endsWith("/temperature/set") -> {
            val temp = payload.toIntOrNull() ?: return Result.failure(...)
            writeCommand("""{"Type":"Set Setpoint Zone 1","SetpointTemp":$temp}""")
            Result.success(Unit)
        }
        commandTopic.endsWith("/mode/set") -> {
            val modeValue = when (payload.lowercase()) {
                "off" -> 0
                "fan_only" -> 1
                "cool" -> 2
                "heat" -> 4
                "heat_cool", "auto" -> 11
                else -> return Result.failure(...)
            }
            writeCommand("""{"Type":"Set Hvac Mode","Mode":$modeValue}""")
            Result.success(Unit)
        }
        // ... fan_mode handling
    }
}
```

### Key Differences from OneControl

| Aspect | OneControl | EasyTouch |
|--------|------------|-----------|
| Protocol | Binary + COBS + CRC + TEA encryption | Plain JSON strings |
| Authentication | 32-round TEA encryption | Simple password JSON |
| Status Updates | Event-driven (notifications) | Polling required (4s interval) |
| Service UUID | Custom (0x0010, 0x0030) | Nordic UART (NUS) |
| Entity Types | Switches, lights, covers, HVAC, tanks | Climate only |
| Multi-device | Gateway manages many devices | Single thermostat (up to 4 zones) |

---

## 6. MQTT Integration

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

## 7. Home Assistant Discovery

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

## 8. Adding New Entity Types

### Step-by-Step: Adding Cover Support

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
        0x4?.toByte(),  // CommandType: ActionCover (determine from decompiled code)
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

## 9. Creating New Plugins

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

## 10. State Management & UI

### SettingsViewModel

**Location:** `app/src/main/java/com/blemqttbridge/ui/viewmodel/SettingsViewModel.kt`

Collects StateFlows from `BaseBleService` companion object:

```kotlin
class SettingsViewModel : ViewModel() {
    private val _serviceRunningStatus = MutableStateFlow(false)
    private val _bleConnectedStatus = MutableStateFlow(false)
    private val _dataHealthyStatus = MutableStateFlow(false)
    private val _mqttConnectedStatus = MutableStateFlow(false)
    
    init {
        viewModelScope.launch {
            // CRITICAL: Gate all status flows by serviceRunning
            BaseBleService.serviceRunning.collect { running ->
                _serviceRunningStatus.value = running
                if (!running) {
                    // Clear status indicators when service stops
                    _bleConnectedStatus.value = false
                    _dataHealthyStatus.value = false
                    _mqttConnectedStatus.value = false
                }
            }
        }
        
        viewModelScope.launch {
            BaseBleService.bleConnected.collect { connected ->
                // Only update if service is running
                if (_serviceRunningStatus.value) {
                    _bleConnectedStatus.value = connected
                }
            }
        }
        
        // ... similar for other flows
    }
}
```

### Settings Screen

**Location:** `app/src/main/java/com/blemqttbridge/ui/screens/SettingsScreen.kt`

- Toggle enables/disables service
- When toggle ON: settings fields locked (grayed out)
- When toggle OFF: settings editable
- Confirmation dialog for destructive actions (removing BLE Scanner)

---

## 11. Common Pitfalls

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
| `BaseBleService.kt` | Foreground service, plugin orchestration |
| `PluginRegistry.kt` | Plugin factory management |
| `BleDevicePlugin.kt` | Plugin interface definition |
| `MqttPublisher.kt` | MQTT abstraction for plugins |
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
| **Output & UI** | |
| `MqttOutputPlugin.kt` | Paho MQTT client wrapper |
| `SettingsViewModel.kt` | UI state management |
| `SettingsScreen.kt` | Main settings UI |

---

*Document version: 2.1.0*
*Last updated: January 2025 - Added EasyTouch Thermostat Protocol section*
