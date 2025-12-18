# BLE to MQTT Bridge - Plugin Architecture Design

**Date**: December 18, 2025  
**Project**: Multi-device BLE to Home Assistant MQTT bridge with plugin architecture  
**Status**: Design phase

---

## Executive Summary

This document describes the architecture for refactoring the existing OneControl BLE bridge into a plugin-based system that supports multiple BLE devices (OneControl, Micro-Air EasyTouch, and future devices) with configurable output destinations (MQTT, HA REST API, webhooks).

**Key Goals:**
- ✅ Support multiple BLE device protocols via plugins
- ✅ Maintain 100% OneControl compatibility (zero regression)
- ✅ Optimize for low-memory environments (runs alongside Fully Kiosk)
- ✅ Enable multiple output destinations (MQTT primary, REST/webhook future)

---

## Design Principles

### 1. **Zero Regression Tolerance for OneControl**
- Protocol code frozen and moved unchanged
- Thin wrapper pattern only
- Extensive regression testing with real packet captures
- Parallel deployment during migration
- Immediate rollback capability

### 2. **Memory Efficiency First**
- Single plugin loaded at runtime (not all plugins in memory)
- Aggressive cleanup on `onTrimMemory` events
- Graceful degradation under memory pressure
- Tested with Fully Kiosk running simultaneously

### 3. **Separation of Concerns**
- **Input plugins**: BLE device protocols (OneControl, EasyTouch, etc.)
- **Output plugins**: Destination systems (MQTT, REST, webhook)
- **Core**: Shared BLE connection, lifecycle, settings

---

## Architecture Overview

```
BleToMqttBridge/
├── core/
│   ├── interfaces/
│   │   ├── BlePluginInterface.kt         ← Device protocol abstraction
│   │   └── OutputPluginInterface.kt      ← Output destination abstraction
│   ├── service/
│   │   ├── BaseBleService.kt             ← Main service (memory-aware)
│   │   ├── PluginRegistry.kt             ← Plugin discovery/loading
│   │   └── MemoryManager.kt              ← Memory pressure handling
│   └── models/
│       ├── DeviceState.kt                ← Common state representation
│       └── Command.kt                    ← Common command representation
├── plugins/
│   ├── device/                           ← Input side (BLE devices)
│   │   ├── onecontrol/
│   │   │   ├── OneControlPlugin.kt       ← THIN wrapper only
│   │   │   ├── OneControlSettings.kt     ← Plugin-specific settings
│   │   │   └── protocol/                 ← FROZEN - exact copies
│   │   │       ├── CobsDecoder.kt
│   │   │       ├── CobsByteDecoder.kt
│   │   │       ├── TeaEncryption.kt
│   │   │       ├── CanMessageParser.kt
│   │   │       ├── MyRvLinkEventDecoders.kt
│   │   │       ├── MyRvLinkEventFactory.kt
│   │   │       ├── MyRvLinkCommandEncoder.kt
│   │   │       ├── MyRvLinkCommandBuilder.kt
│   │   │       └── DeviceStatusParser.kt
│   │   └── easytouch/
│   │       ├── EasyTouchPlugin.kt        ← New plugin
│   │       ├── EasyTouchSettings.kt
│   │       └── protocol/
│   │           ├── AuthenticationHandler.kt
│   │           └── JsonMessageParser.kt
│   └── output/                           ← Output side
│       ├── MqttOutputPlugin.kt           ← Primary (current)
│       └── HomeAssistantRestPlugin.kt    ← Future
├── ui/
│   ├── MainActivity.kt                   ← Plugin selection + settings
│   ├── PluginSelectorFragment.kt        ← Choose device plugin
│   └── SettingsFragment.kt              ← Per-plugin configuration
├── util/
│   ├── Crc8.kt                          ← Shared utilities
│   └── logging/
└── tests/
    ├── OneControlRegressionTest.kt       ← Real packet replay
    └── MemoryPressureTest.kt            ← Low memory simulation
```

---

## Plugin Interfaces

### BlePluginInterface (Device Input)

```kotlin
/**
 * Interface for BLE device protocol implementations.
 * Each plugin handles one specific device type (e.g., OneControl, EasyTouch).
 */
interface BlePluginInterface {
    
    // ========== Metadata ==========
    
    /** Unique plugin identifier (e.g., "onecontrol", "easytouch") */
    fun getPluginId(): String
    
    /** Human-readable name for UI display */
    fun getPluginName(): String
    
    /** Manufacturer/device type description */
    fun getDeviceType(): String
    
    
    // ========== BLE Configuration ==========
    
    /** Primary BLE service UUID to connect to */
    fun getServiceUuid(): UUID
    
    /** Map of characteristic UUIDs by function (e.g., "auth", "data", "command") */
    fun getCharacteristicUuids(): Map<String, UUID>
    
    /** Whether device requires BLE bonding/pairing */
    fun requiresBonding(): Boolean
    
    /** Whether device requires notification subscription (vs unsolicited notifications) */
    fun requiresNotificationSubscription(): Boolean
    
    
    // ========== Connection Lifecycle ==========
    
    /**
     * Called when BLE scan discovers a potential device.
     * Return true if this device should be connected to.
     */
    suspend fun onDeviceDiscovered(
        device: BluetoothDevice,
        rssi: Int,
        scanRecord: ScanRecord?
    ): Boolean
    
    /**
     * Perform authentication/unlock sequence after GATT connection established.
     * @return Result.success(Unit) if auth succeeded, Result.failure if auth failed
     */
    suspend fun authenticate(gatt: BluetoothGatt): Result<Unit>
    
    /**
     * Called when BLE connection is fully established and authenticated.
     * Plugin can perform initial setup (e.g., subscribe to notifications).
     */
    suspend fun onConnected(gatt: BluetoothGatt)
    
    /**
     * Called when BLE connection is lost.
     * Plugin should clean up any connection-specific state.
     */
    suspend fun onDisconnected()
    
    
    // ========== Data Handling ==========
    
    /**
     * Parse incoming BLE notification data into standardized device states.
     * @param characteristicUuid The UUID of the characteristic that sent the notification
     * @param data Raw notification bytes
     * @return List of device state updates (can be multiple devices per notification)
     */
    fun parseNotification(characteristicUuid: UUID, data: ByteArray): List<DeviceState>
    
    /**
     * Build command bytes to send to BLE device.
     * @param deviceId Identifier for the specific device/entity
     * @param command Command to execute (e.g., turn on, set brightness)
     * @return Bytes to write to BLE characteristic
     */
    fun buildCommand(deviceId: String, command: Command): ByteArray?
    
    /**
     * Return the characteristic UUID to write commands to.
     */
    fun getCommandCharacteristicUuid(): UUID
    
    
    // ========== Home Assistant Discovery ==========
    
    /**
     * Generate Home Assistant MQTT discovery payloads for all devices.
     * Called once after initial connection to publish device/entity configurations.
     * @param topicPrefix MQTT topic prefix (e.g., "homeassistant")
     * @return List of discovery configurations
     */
    fun getDiscoveryPayloads(topicPrefix: String): List<DiscoveryPayload>
    
    
    // ========== Settings UI ==========
    
    /**
     * Provide a settings fragment for plugin-specific configuration.
     * Return null if plugin has no additional settings beyond MAC address.
     */
    fun getSettingsFragment(): Fragment?
}

/**
 * Common device state representation across all plugins.
 */
data class DeviceState(
    val pluginId: String,           // Which plugin generated this state
    val deviceId: String,            // Unique device identifier within plugin
    val deviceType: DeviceType,      // SWITCH, LIGHT, COVER, SENSOR, etc.
    val attributes: Map<String, Any> // State attributes (state, brightness, position, etc.)
)

enum class DeviceType {
    SWITCH,
    LIGHT,
    DIMMABLE_LIGHT,
    RGB_LIGHT,
    COVER,
    SENSOR,
    BINARY_SENSOR,
    CLIMATE
}

/**
 * Common command representation for device control.
 */
data class Command(
    val action: CommandAction,
    val parameters: Map<String, Any> = emptyMap()
)

enum class CommandAction {
    TURN_ON,
    TURN_OFF,
    SET_BRIGHTNESS,
    SET_RGB_COLOR,
    OPEN_COVER,
    CLOSE_COVER,
    STOP_COVER,
    SET_COVER_POSITION,
    SET_TEMPERATURE,
    SET_HVAC_MODE
}

/**
 * Home Assistant MQTT discovery payload.
 */
data class DiscoveryPayload(
    val topic: String,              // Full MQTT discovery topic
    val payload: String,            // JSON discovery payload
    val retain: Boolean = true      // Typically retained for discovery
)
```

### OutputPluginInterface (Destination Output)

```kotlin
/**
 * Interface for output destination implementations (MQTT, REST, webhook, etc.).
 * Handles publishing device states and receiving commands.
 */
interface OutputPluginInterface {
    
    /** Output plugin identifier (e.g., "mqtt", "ha_rest") */
    fun getOutputId(): String
    
    /** Human-readable name for UI */
    fun getOutputName(): String
    
    /**
     * Initialize the output plugin with user configuration.
     * @param config Plugin-specific configuration (e.g., broker URL, API key)
     * @return Result.success if initialization succeeded
     */
    suspend fun initialize(config: Map<String, String>): Result<Unit>
    
    /**
     * Publish a device state update.
     * @param topic Topic/path for the state (plugin interprets format)
     * @param payload Serialized state data (typically JSON)
     * @param retained Whether to retain the message (if supported)
     */
    suspend fun publishState(topic: String, payload: String, retained: Boolean = false)
    
    /**
     * Publish a Home Assistant discovery payload.
     * @param topic Discovery topic
     * @param payload JSON discovery configuration
     */
    suspend fun publishDiscovery(topic: String, payload: String)
    
    /**
     * Subscribe to command topics.
     * @param topicPattern Topic pattern to subscribe to
     * @param callback Called when command received; returns parsed Command
     */
    suspend fun subscribeToCommands(
        topicPattern: String,
        callback: (topic: String, payload: String) -> Unit
    )
    
    /**
     * Publish availability status.
     */
    suspend fun publishAvailability(online: Boolean)
    
    /**
     * Disconnect and clean up resources.
     */
    fun disconnect()
    
    /**
     * Check if output is currently connected.
     */
    fun isConnected(): Boolean
}
```

---

## Core Service Architecture

### BaseBleService

**Purpose**: Main Android foreground service managing BLE connection and plugin lifecycle.

**Key responsibilities:**
- Single plugin instance loaded at runtime (memory efficient)
- BLE scanning, connection, and GATT operations
- MQTT/output connection management
- Memory pressure monitoring and cleanup
- Watchdog for connection health

**Memory Management:**

```kotlin
class BaseBleService : Service() {
    
    private var devicePlugin: BlePluginInterface? = null  // Only ONE loaded
    private var outputPlugin: OutputPluginInterface? = null
    
    override fun onCreate() {
        super.onCreate()
        
        // Load only the selected plugins (not all)
        val selectedDevice = prefs.getString("selected_device_plugin", "onecontrol")
        val selectedOutput = prefs.getString("selected_output_plugin", "mqtt")
        
        devicePlugin = PluginRegistry.loadDevicePlugin(selectedDevice)
        outputPlugin = PluginRegistry.loadOutputPlugin(selectedOutput)
        
        // Monitor memory pressure
        registerComponentCallbacks(MemoryManager(this))
    }
    
    override fun onTrimMemory(level: Int) {
        super.onTrimMemory(level)
        when (level) {
            TRIM_MEMORY_RUNNING_CRITICAL -> {
                Log.w(TAG, "Memory critical - aggressive cleanup")
                disableVerboseLogging()
                clearCachedDiscoveries()
                devicePlugin?.onMemoryPressure()
            }
            TRIM_MEMORY_UI_HIDDEN -> {
                // User switched to another app (e.g., Fully Kiosk)
                // Release UI resources but keep BLE/MQTT running
                releaseUiResources()
            }
        }
    }
}
```

### PluginRegistry

**Purpose**: Lazy loading and discovery of plugins.

```kotlin
object PluginRegistry {
    
    private val devicePlugins = mapOf(
        "onecontrol" to OneControlPlugin::class.java,
        "easytouch" to EasyTouchPlugin::class.java
    )
    
    private val outputPlugins = mapOf(
        "mqtt" to MqttOutputPlugin::class.java,
        "ha_rest" to HomeAssistantRestPlugin::class.java
    )
    
    /**
     * Load a device plugin by ID. Only one instance created at a time.
     */
    fun loadDevicePlugin(pluginId: String): BlePluginInterface {
        val clazz = devicePlugins[pluginId]
            ?: throw IllegalArgumentException("Unknown device plugin: $pluginId")
        return clazz.getDeclaredConstructor().newInstance()
    }
    
    /**
     * Load an output plugin by ID.
     */
    fun loadOutputPlugin(pluginId: String): OutputPluginInterface {
        val clazz = outputPlugins[pluginId]
            ?: throw IllegalArgumentException("Unknown output plugin: $pluginId")
        return clazz.getDeclaredConstructor().newInstance()
    }
    
    /**
     * List all available device plugins for UI selection.
     */
    fun listDevicePlugins(): List<PluginInfo> {
        return devicePlugins.map { (id, clazz) ->
            val instance = clazz.getDeclaredConstructor().newInstance()
            PluginInfo(id, instance.getPluginName(), instance.getDeviceType())
        }
    }
}

data class PluginInfo(
    val id: String,
    val name: String,
    val description: String
)
```

---

## OneControl Migration Strategy

### Phase 1: Freeze & Archive

```bash
# In existing onecontrol-ble-mqtt-gateway repo
git tag onecontrol-stable-v1.0 -m "Last known working implementation before plugin migration"
git archive onecontrol-stable-v1.0 -o onecontrol-reference.zip
```

### Phase 2: Move Protocol Files (Zero Changes)

**Action**: Copy files to new location **byte-for-byte identical**.

```
plugins/device/onecontrol/protocol/
├── CobsDecoder.kt          ← EXACT copy from stable tag
├── CobsByteDecoder.kt      ← EXACT copy
├── TeaEncryption.kt        ← EXACT copy
├── CanMessageParser.kt     ← EXACT copy
├── MyRvLinkEventDecoders.kt    ← EXACT copy
├── MyRvLinkEventFactory.kt     ← EXACT copy
├── MyRvLinkCommandEncoder.kt   ← EXACT copy
├── MyRvLinkCommandBuilder.kt   ← EXACT copy
└── DeviceStatusParser.kt   ← EXACT copy
```

**Verification:**
```bash
# After copying, verify files are identical
diff -r onecontrol-reference/app/src/main/java/com/onecontrol/blebridge/ \
        plugins/device/onecontrol/protocol/
```

### Phase 3: Create Thin Wrapper

**OneControlPlugin.kt** (new file, delegates to existing protocol):

```kotlin
class OneControlPlugin : BlePluginInterface {
    
    override fun getPluginId() = "onecontrol"
    override fun getPluginName() = "Lippert OneControl"
    override fun getDeviceType() = "OneControl RV Gateway"
    
    // BLE UUIDs (from Constants.kt)
    override fun getServiceUuid() = Constants.SERVICE_UUID
    override fun getCharacteristicUuids() = mapOf(
        "data" to Constants.DATA_CHARACTERISTIC_UUID,
        "command" to Constants.COMMAND_CHARACTERISTIC_UUID
    )
    override fun requiresBonding() = true
    override fun requiresNotificationSubscription() = true
    
    // Delegate to existing protocol (UNCHANGED)
    override fun parseNotification(uuid: UUID, data: ByteArray): List<DeviceState> {
        // Call existing MyRvLinkEventDecoders - NO CHANGES
        val events = MyRvLinkEventDecoders.decode(data)
        
        // Just wrap results in new DeviceState format
        return events.map { event ->
            DeviceState(
                pluginId = "onecontrol",
                deviceId = event.deviceId,
                deviceType = mapDeviceType(event),
                attributes = event.toAttributes()
            )
        }
    }
    
    override fun buildCommand(deviceId: String, command: Command): ByteArray? {
        // Call existing MyRvLinkCommandEncoder - NO CHANGES
        return MyRvLinkCommandEncoder.encode(deviceId, command)
    }
    
    // ... other methods delegate to existing protocol classes
}
```

**Critical**: Zero logic changes to protocol classes. Only thin wrapper/adapter.

### Phase 4: Regression Testing

**Test with real captured packets** from HCI trace:

```kotlin
@Test
fun testOneControlProtocol_RealPacketReplay() {
    val plugin = OneControlPlugin()
    
    // Load actual packet captures from working app
    val testPackets = loadPacketCaptures("onecontrol_real_traffic.pcap")
    
    testPackets.forEach { packet ->
        // Parse with new plugin
        val newResult = plugin.parseNotification(packet.uuid, packet.data)
        
        // Compare against known-good results
        val expectedResult = packet.expectedDeviceStates
        assertEquals(expectedResult, newResult)
    }
}

@Test
fun testOneControlCommands_BitwiseIdentical() {
    val plugin = OneControlPlugin()
    
    val testCases = loadCommandTestCases() // From working app logs
    
    testCases.forEach { test ->
        val generated = plugin.buildCommand(test.deviceId, test.command)
        assertArrayEquals(test.expectedBytes, generated)
    }
}
```

### Phase 5: Parallel Deployment

**Run both apps side-by-side** for 48+ hours:

- Old app: publishes to `onecontrol/ble/v1/...`
- New app: publishes to `onecontrol/ble/v2/...`
- Compare states in Home Assistant
- Monitor for any discrepancies

**Rollback plan:**
- If ANY issue detected: immediate revert to old APK
- Emergency flag: `USE_LEGACY_SERVICE = true` in new app code
- Keep old APK accessible for instant downgrade

---

## EasyTouch Plugin Implementation

### Protocol Details (from HACS integration)

```kotlin
class EasyTouchPlugin : BlePluginInterface {
    
    companion object {
        private const val SERVICE_UUID = "000000FF-0000-1000-8000-00805F9B34FB"
        private const val PASSWORD_CHAR_UUID = "0000DD01-0000-1000-8000-00805F9B34FB"
        private const val JSON_CMD_UUID = "0000EE01-0000-1000-8000-00805F9B34FB"
        private const val JSON_RETURN_UUID = "0000FF01-0000-1000-8000-00805F9B34FB"
    }
    
    override fun getPluginId() = "easytouch"
    override fun getPluginName() = "Micro-Air EasyTouch"
    override fun getDeviceType() = "RV Thermostat"
    
    override fun getServiceUuid() = UUID.fromString(SERVICE_UUID)
    override fun getCharacteristicUuids() = mapOf(
        "password" to UUID.fromString(PASSWORD_CHAR_UUID),
        "json_cmd" to UUID.fromString(JSON_CMD_UUID),
        "json_return" to UUID.fromString(JSON_RETURN_UUID)
    )
    
    override fun requiresBonding() = false
    override fun requiresNotificationSubscription() = false  // Unsolicited notifications
    
    override suspend fun authenticate(gatt: BluetoothGatt): Result<Unit> {
        // Write password to password characteristic
        val passwordChar = gatt.getService(getServiceUuid())
            ?.getCharacteristic(UUID.fromString(PASSWORD_CHAR_UUID))
            ?: return Result.failure(Exception("Password characteristic not found"))
        
        val password = getPasswordFromSettings()
        passwordChar.value = password.toByteArray()
        
        return if (gatt.writeCharacteristic(passwordChar)) {
            Result.success(Unit)
        } else {
            Result.failure(Exception("Password write failed"))
        }
    }
    
    override fun parseNotification(uuid: UUID, data: ByteArray): List<DeviceState> {
        // Parse JSON notification (from HACS integration logic)
        val json = JSONObject(String(data))
        
        // Extract Z_sts array (zone status)
        val zSts = json.getJSONObject("Z_sts").getJSONArray("0")
        
        // Parse temperature and mode
        val currentTemp = zSts.getDouble(12)  // facePlateTemperature
        val modeNum = zSts.getInt(10)
        val currentModeNum = zSts.getInt(15)
        
        return listOf(
            DeviceState(
                pluginId = "easytouch",
                deviceId = "thermostat",
                deviceType = DeviceType.CLIMATE,
                attributes = mapOf(
                    "current_temperature" to currentTemp,
                    "hvac_mode" to mapHvacMode(modeNum),
                    "target_temperature" to extractTargetTemp(zSts, modeNum)
                )
            )
        )
    }
}
```

---

## Memory Optimization Strategy

### Target Environment

**Typical deployment:**
- Low-end Android tablet ($50-100 range)
- 2GB RAM total
- Running Fully Kiosk Browser (300-500MB)
- System + other apps (500-700MB)
- **Available for our app: ~300-500MB**

### Optimization Techniques

**1. Single Plugin in Memory**
```kotlin
// Don't do this:
val allPlugins = listOf(OneControlPlugin(), EasyTouchPlugin(), ...)  // Wastes memory

// Do this:
val activePlugin = when (selectedType) {
    "onecontrol" -> OneControlPlugin()
    "easytouch" -> EasyTouchPlugin()
    else -> throw IllegalArgumentException()
}
```

**2. Lazy Initialization**
```kotlin
class OneControlPlugin : BlePluginInterface {
    // Don't eagerly load heavy resources
    private val eventDecoder by lazy { MyRvLinkEventDecoders() }
    private val commandEncoder by lazy { MyRvLinkCommandEncoder() }
}
```

**3. Memory Pressure Handling**
```kotlin
override fun onTrimMemory(level: Int) {
    when (level) {
        TRIM_MEMORY_RUNNING_CRITICAL -> {
            // Clear caches
            discoveryCache.clear()
            logBuffer.clear()
            
            // Disable verbose logging
            Log.setLevel(Log.WARN)
            
            // Reduce reconnect frequency
            reconnectDelay = max(reconnectDelay * 2, 30_000L)
        }
        TRIM_MEMORY_RUNNING_LOW -> {
            // Pre-emptive cleanup
            System.gc()
        }
    }
}
```

**4. Efficient MQTT Client**
```kotlin
// Use lightweight Paho client config
val mqttOptions = MqttConnectOptions().apply {
    isAutomaticReconnect = true
    maxInflight = 10  // Limit in-flight messages
    connectionTimeout = 30
    keepAliveInterval = 60
    isCleanSession = false  // Reuse session on reconnect
}
```

**5. Foreground Service Priority**
```kotlin
override fun onCreate() {
    // Request high priority to avoid OOM killer
    startForeground(
        NOTIFICATION_ID,
        buildNotification(),
        ServiceInfo.FOREGROUND_SERVICE_TYPE_CONNECTED_DEVICE
    )
}
```

### Memory Testing

**Load test scenario:**
1. Start Fully Kiosk Browser with live dashboard
2. Start BLE bridge service
3. Simulate memory pressure: `adb shell am send-trim-memory <package> RUNNING_CRITICAL`
4. Monitor for 24+ hours
5. Check logcat for OOM events or service restarts

**Success criteria:**
- Service survives 24+ hours under memory pressure
- Zero OOM kills
- BLE/MQTT reconnects successfully after `onTrimMemory` events
- UI remains responsive when switching from Fully to bridge app

---

## MQTT Topic Structure

### Per-Plugin Namespacing

**Old (OneControl-specific):**
```
onecontrol/ble/device/1/123/state
onecontrol/ble/command/switch/1/123
```

**New (plugin-aware):**
```
{base_topic}/{plugin_id}/device/{deviceId}/state
{base_topic}/{plugin_id}/command/{deviceType}/{deviceId}
```

**Examples:**
```
homeassistant/onecontrol/device/relay_0x0101/state
homeassistant/easytouch/device/thermostat/state

homeassistant/onecontrol/command/switch/relay_0x0101
homeassistant/easytouch/command/climate/thermostat
```

**Configuration:**
- `base_topic`: User-configurable (default: `homeassistant`)
- `plugin_id`: Automatic from selected plugin
- Backward compatibility: Optional `legacy_topic_mode` for OneControl

---

## Implementation Phases

### Phase 1: Output Abstraction (Safe - 2 days)
**Goal**: Refactor MQTT into plugin without touching BLE.

- [ ] Create `OutputPluginInterface.kt`
- [ ] Extract MQTT logic into `MqttOutputPlugin.kt`
- [ ] Update service to use output plugin
- [ ] Test with existing OneControl app (no plugin system yet)
- [ ] **Risk**: Low - only output layer changed

### Phase 2: Core Plugin Infrastructure (2 days)
**Goal**: Build plugin registry and service architecture.

- [ ] Create `BlePluginInterface.kt`
- [ ] Implement `PluginRegistry` with lazy loading
- [ ] Create `BaseBleService` with plugin hooks
- [ ] Implement `MemoryManager`
- [ ] Test with mock plugin (simple battery sensor)
- [ ] **Risk**: Low - no production code changed yet

### Phase 3: OneControl Migration (CRITICAL - 3 days)
**Goal**: Move OneControl to plugin with zero regression.

- [ ] Tag stable version in old repo
- [ ] Copy protocol files unchanged to `plugins/device/onecontrol/protocol/`
- [ ] Verify byte-for-byte identical with diff
- [ ] Create `OneControlPlugin.kt` thin wrapper
- [ ] Write regression tests with real packet captures
- [ ] Run tests - all must pass
- [ ] Deploy parallel (old + new app, different MQTT topics)
- [ ] Monitor for 48+ hours
- [ ] Compare output between old/new
- [ ] **Risk**: Medium - extensive validation required

### Phase 4: EasyTouch Plugin (2-3 days)
**Goal**: Implement second plugin to validate architecture.

- [ ] Create `EasyTouchPlugin.kt`
- [ ] Port authentication from HACS integration
- [ ] Implement JSON parsing
- [ ] Create HA discovery payloads for climate entity
- [ ] Test with real thermostat
- [ ] **Risk**: Low - isolated from OneControl

### Phase 5: UI Improvements (1 day)
**Goal**: Plugin selection and per-plugin settings.

- [ ] Add plugin selector dropdown
- [ ] Dynamic settings UI based on selected plugin
- [ ] Output plugin selection
- [ ] **Risk**: Low - UI only

### Phase 6: Memory Hardening (1-2 days)
**Goal**: Optimize for low-memory environments.

- [ ] Implement aggressive `onTrimMemory` handling
- [ ] Add memory pressure simulation tests
- [ ] Test with Fully Kiosk running
- [ ] Load test on low-end tablet
- [ ] **Risk**: Low - improves existing

### Phase 7: Release (1 day)
**Goal**: Package and deploy.

- [ ] Final testing on multiple devices
- [ ] Create release APK
- [ ] Update documentation
- [ ] GitHub release with migration guide
- [ ] **Risk**: Low

---

## Testing Strategy

### Unit Tests
- Protocol parsing (OneControl, EasyTouch)
- Command encoding (byte-level validation)
- Plugin interface compliance

### Integration Tests
- Real packet replay (from HCI captures)
- MQTT round-trip (publish state → receive command)
- Memory pressure simulation

### End-to-End Tests
- Full connection flow (scan → connect → auth → data)
- Parallel deployment validation
- 24+ hour stability test
- Fully Kiosk coexistence test

### Regression Tests (OneControl)
```kotlin
class OneControlRegressionSuite {
    @Test fun testRelayOnCommand() { /* ... */ }
    @Test fun testDimmableLightBrightness() { /* ... */ }
    @Test fun testCoverPosition() { /* ... */ }
    @Test fun testSystemVoltage() { /* ... */ }
    @Test fun testTankSensors() { /* ... */ }
}
```

---

## Rollback & Emergency Procedures

### If OneControl Issues Appear

**Immediate actions:**
1. Stop new app deployment
2. Reinstall old APK on affected devices
3. Revert MQTT topics to old structure
4. Notify users of temporary rollback

**Emergency patch:**
```kotlin
// Add to BaseBleService.kt
companion object {
    const val USE_LEGACY_ONECONTROL = true  // EMERGENCY ROLLBACK FLAG
}

override fun onCreate() {
    if (USE_LEGACY_ONECONTROL && selectedPlugin == "onecontrol") {
        // Load old monolithic service code
        startLegacyOneControlService()
        return
    }
    // ... normal plugin flow
}
```

### Monitoring & Alerts

**Key metrics to track:**
- Service uptime (target: >99%)
- BLE reconnect count (target: <5 per 24h)
- MQTT connection stability
- Memory usage trend
- OOM kill events (target: 0)

---

## Future Enhancements

### Additional Device Plugins
- RV-C standard devices (generic CAN bus parser)
- Dometic thermostats
- Furrion appliances
- Generic BLE sensors (Ruuvi, Xiaomi)

### Additional Output Plugins
- Home Assistant REST API (no MQTT broker required)
- Webhook output (generic HTTP POST)
- Local WebSocket server (for dashboard apps)
- Cloud integrations (AWS IoT, Azure IoT)

### Advanced Features
- Multi-device support (connect to multiple BLE devices simultaneously)
- Plugin marketplace (download plugins from repository)
- OTA plugin updates (update plugins without reinstalling app)
- Cloud-based plugin registry

---

## Conclusion

This architecture provides:
- ✅ Clean separation between device protocols and output destinations
- ✅ Memory-efficient single-plugin-at-runtime design
- ✅ Zero-regression migration path for OneControl
- ✅ Extensibility for future devices and outputs
- ✅ Production-ready stability for low-memory environments

**Total estimated timeline: 10-14 days** from start to release.

**Next step**: Implement Phase 1 (Output Abstraction) to validate approach before touching OneControl code.
