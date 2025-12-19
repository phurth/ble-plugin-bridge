# Phase 4 Testing Guide

## What Was Implemented

### 1. GattOperations Infrastructure (BaseBleService)
**File:** `app/src/main/java/com/blemqttbridge/core/BaseBleService.kt`

- **GattOperationsImpl** inner class providing async GATT operations:
  ```kotlin
  suspend fun readCharacteristic(uuid: String): Result<ByteArray>
  suspend fun writeCharacteristic(uuid: String, value: ByteArray): Result<Unit>
  suspend fun enableNotifications(uuid: String): Result<Unit>
  suspend fun disableNotifications(uuid: String): Result<Unit>
  ```

- **Callback Handling:**
  - `onCharacteristicRead()` - Completes pending read operations
  - `onCharacteristicWrite()` - Completes pending write operations
  - Tracked via `pendingReads` and `pendingWrites` maps

- **Service Discovery Flow:**
  - After GATT service discovery completes
  - Creates `GattOperationsImpl` instance
  - Calls `plugin.onServicesDiscovered(device, gattOps)`
  - Plugin performs authentication asynchronously

### 2. OneControl TEA Authentication (OneControlPlugin)
**File:** `app/src/main/java/com/blemqttbridge/plugins/device/onecontrol/OneControlPlugin.kt`

**Authentication Flow:**
```kotlin
override suspend fun onServicesDiscovered(
    device: BluetoothDevice,
    gattOperations: GattOperations
): Result<Unit> {
    // 1. Read SEED from gateway (4 bytes, little-endian)
    val seedBytes = gattOperations.readCharacteristic(SEED_CHAR_UUID)
    val seed = bytesToLong(seedBytes)  // 0x12345678
    
    // 2. Encrypt with TEA cipher
    val key = TeaEncryption.encrypt(0x8100080D, seed)
    
    // 3. Write KEY back (4 bytes, little-endian)
    gattOperations.writeCharacteristic(KEY_CHAR_UUID, longToBytes(key))
    
    // 4. Subscribe to CAN notifications
    gattOperations.enableNotifications(CAN_READ_CHAR_UUID)
    
    return Result.success(Unit)
}
```

**State Tracking:**
- `connectedDevices: Set<String>` - All connected devices
- `authenticatedDevices: Set<String>` - Successfully authenticated
- `gattOperations: GattOperations?` - Current GATT ops instance

## Testing Procedure

### Step 1: Enable OneControl Plugin
```bash
# Launch app
adb shell am start -n com.blemqttbridge/.ui.ServiceStatusActivity

# In app UI:
# 1. Press "Enable OneControl" button
# 2. Verify status shows: "✅ Enabled OneControl plugin"
```

### Step 2: Start Service and Monitor Logs
```bash
# Open separate terminal for logs
adb logcat -c  # Clear logs
adb logcat -s BaseBleService OneControlPlugin | grep -E "🔐|✅|❌|Seed|Key|Auth|CAN"

# In app UI:
# Press "Start Service" button
```

### Step 3: Expected Log Sequence
```
BaseBleService: BLE scan started
BaseBleService: Found matching device: 24:DC:C3:ED:1E:0A -> plugin: onecontrol
BaseBleService: Connecting to 24:DC:C3:ED:1E:0A (plugin: onecontrol)
BaseBleService: Connected to 24:DC:C3:ED:1E:0A (plugin: onecontrol)
OneControlPlugin: 🔗 Device connected: 24:DC:C3:ED:1E:0A
BaseBleService: Services discovered for 24:DC:C3:ED:1E:0A
OneControlPlugin: 🔐 Starting TEA authentication for 24:DC:C3:ED:1E:0A...
OneControlPlugin: Reading SEED from 00000011-0200-a58e-e411-afe28044e62c
BaseBleService: Read success for 00000011-0200-a58e-e411-afe28044e62c: 4 bytes
OneControlPlugin: Seed: 0x12345678  # Example value
OneControlPlugin: Key: 0x9abcdef0   # Example encrypted value
OneControlPlugin: Writing KEY to 00000013-0200-a58e-e411-afe28044e62c
BaseBleService: Write success for 00000013-0200-a58e-e411-afe28044e62c
OneControlPlugin: ✅ Authentication complete!
OneControlPlugin: Subscribing to CAN notifications: 00000002-0200-a58e-e411-afe28044e62c
BaseBleService: Enabled notifications for 00000002-0200-a58e-e411-afe28044e62c
OneControlPlugin: ✅ Subscribed to CAN notifications
BaseBleService: Published 1 discovery payloads for 24:DC:C3:ED:1E:0A
```

### Step 4: Verify Persistent Connection
**Before authentication (old behavior):**
```
BaseBleService: Connected to 24:DC:C3:ED:1E:0A
BaseBleService: Services discovered for 24:DC:C3:ED:1E:0A
[Wait ~10 seconds]
BaseBleService: Disconnected from 24:DC:C3:ED:1E:0A  # ❌ Gateway times out
[Reconnect loop continues]
```

**After authentication (expected new behavior):**
```
OneControlPlugin: ✅ Authentication complete!
OneControlPlugin: ✅ Subscribed to CAN notifications
[Connection remains stable - no disconnects]
[CAN notifications start arriving]
```

### Step 5: Check for CAN Data
```bash
# Monitor for CAN notifications
adb logcat -s OneControlPlugin | grep "📨"

# Expected:
OneControlPlugin: 📨 Notification from 00000002-0200-a58e-e411-afe28044e62c: aa bb cc dd ...
OneControlPlugin: 📨 CAN data received: aa bb cc dd ...
```

## Success Criteria

- ✅ **Authentication completes successfully**
  - SEED read (4 bytes)
  - KEY written (encrypted with TEA)
  - No errors in logs

- ✅ **Connection persists**
  - No disconnect after 10 seconds
  - Connection remains stable for minutes

- ✅ **CAN notifications received**
  - Periodic data on CAN_READ characteristic
  - Data is COBS-encoded CAN messages

- ✅ **Clean error handling**
  - Timeouts logged clearly
  - Failed operations don't crash
  - Proper cleanup on disconnect

## Common Issues

### Issue: Authentication Timeout
```
OneControlPlugin: Reading SEED from ...
[5 second pause]
BaseBleService: Read failed for ...: status=133
OneControlPlugin: ❌ Authentication failed: GATT read failed
```
**Causes:**
- Gateway not responding (check Bluetooth bond status)
- Characteristic UUID mismatch
- GATT connection unstable

**Debug:**
```bash
# Check bonded devices
adb shell dumpsys bluetooth_manager | grep "24:DC:C3:ED:1E:0A"

# Verify characteristic exists
adb logcat | grep "Service UUID\|Characteristic"
```

### Issue: Write Fails
```
OneControlPlugin: Writing KEY to ...
BaseBleService: Write failed for ...: status=133
```
**Causes:**
- Characteristic doesn't support writes
- Gateway rejected encrypted key (wrong cypher)
- Connection dropped during write

**Debug:**
```bash
# Check characteristic properties
adb logcat | grep "properties=0x"
# Should include WRITE flag
```

### Issue: No CAN Data
```
OneControlPlugin: ✅ Subscribed to CAN notifications
[No further notifications]
```
**Causes:**
- Authentication succeeded but gateway isn't sending data
- No CAN bus activity on RV
- CCCD descriptor not written properly

**Debug:**
```bash
# Verify notification enabled
adb logcat | grep "setCharacteristicNotification\|writeDescriptor"

# Check for any characteristic changes
adb logcat | grep "onCharacteristicChanged"
```

## Next Steps (Phase 4 Continued)

### 1. CAN Message Parsing
**File:** `OneControlPlugin.kt` - `onCharacteristicNotification()`

```kotlin
override suspend fun onCharacteristicNotification(
    device: BluetoothDevice,
    characteristicUuid: String,
    value: ByteArray
): Map<String, String> {
    if (uuid != CAN_READ_CHAR_UUID) return emptyMap()
    
    // Decode COBS framing
    val decoded = CobsDecoder.decode(value)
    
    // Parse CAN message
    val canMessage = CanMessageParser.parse(decoded)
    
    // Extract device states
    val states = DeviceStatusParser.parseCanMessage(canMessage)
    
    // Return MQTT topic -> payload map
    return states.mapValues { (deviceId, state) ->
        buildMqttPayload(deviceId, state)
    }
}
```

**Protocol files ready to use:**
- `CobsDecoder.kt` - COBS frame decoding
- `CanMessageParser.kt` - Parse CAN bus messages
- `DeviceStatusParser.kt` - Extract device states

### 2. Command Handling
**File:** `OneControlPlugin.kt` - `handleCommand()`

```kotlin
override suspend fun handleCommand(
    device: BluetoothDevice,
    commandTopic: String,
    payload: String
): Result<Unit> {
    // Parse command topic
    // Build CAN command with MyRvLinkCommandBuilder
    // Encode with MyRvLinkCommandEncoder
    // Write to CAN_WRITE_CHAR via gattOperations
    
    gattOperations?.writeCharacteristic(
        CAN_WRITE_CHAR_UUID,
        encodedCommand
    )
}
```

**Protocol files ready to use:**
- `MyRvLinkCommandBuilder.kt` - Build commands
- `MyRvLinkCommandEncoder.kt` - Encode for transmission

### 3. Home Assistant Discovery
**File:** `OneControlPlugin.kt` - `getDiscoveryPayloads()`

Use `HomeAssistantMqttDiscovery.kt` to generate discovery payloads for all device types:
- Lights (on/off, brightness, color)
- Awnings (extend/retract, position)
- Leveling system (status, commands)
- Tank sensors (levels, percentages)

## Monitoring Commands

### Live Authentication Monitoring
```bash
adb logcat -c && adb logcat -s BaseBleService OneControlPlugin \
  | grep --line-buffered -E "🔐|✅|❌|Seed|Key|Auth|CAN" \
  | while read line; do echo "$(date '+%H:%M:%S') $line"; done
```

### Connection State Tracking
```bash
adb logcat | grep -E "Connected to|Disconnected from|Services discovered"
```

### GATT Operation Debugging
```bash
adb logcat -s BaseBleService | grep -E "Read|Write|Notification|Descriptor"
```

### Full Debug Session
```bash
# Terminal 1: App logs
adb logcat -s BlePluginBridgeApp BaseBleService PluginRegistry

# Terminal 2: Plugin logs
adb logcat -s OneControlPlugin

# Terminal 3: GATT operations
adb logcat | grep -E "onCharacteristic|onDescriptor|gatt"
```

## Build and Deploy
```bash
# Build
cd /Users/petehurth/Downloads/Decom/android_ble_plugin_bridge
./gradlew assembleDebug

# Install
adb install -r app/build/outputs/apk/debug/app-debug.apk

# Launch
adb shell am start -n com.blemqttbridge/.ui.ServiceStatusActivity
```

## Testing Checklist

- [ ] App installs without errors
- [ ] Service starts successfully
- [ ] Gateway found and connected
- [ ] Service discovery completes
- [ ] onServicesDiscovered() called
- [ ] SEED read successful (4 bytes)
- [ ] TEA encryption applied
- [ ] KEY write successful
- [ ] Authentication log shows ✅
- [ ] CAN notification subscription successful
- [ ] Connection persists beyond 10 seconds
- [ ] CAN data notifications arriving
- [ ] No crashes or exceptions
- [ ] Clean disconnect on Stop Service
- [ ] State persists across app restarts

## Architecture Reference

```
┌─────────────────────────────────────────────┐
│          BaseBleService                     │
│  ┌───────────────────────────────────────┐  │
│  │   Scan → Connect → DiscoverServices  │  │
│  └───────────────┬──────────────────────┘  │
│                  │                          │
│                  ▼                          │
│  ┌───────────────────────────────────────┐  │
│  │    GattOperationsImpl                │  │
│  │  - readCharacteristic()             │  │
│  │  - writeCharacteristic()            │  │
│  │  - enableNotifications()            │  │
│  └───────────────┬──────────────────────┘  │
│                  │                          │
└──────────────────┼──────────────────────────┘
                   │ gattOperations
                   ▼
┌─────────────────────────────────────────────┐
│        OneControlPlugin                     │
│  ┌───────────────────────────────────────┐  │
│  │  onServicesDiscovered(gattOps)       │  │
│  │  1. Read SEED                         │  │
│  │  2. TeaEncryption.encrypt()          │  │
│  │  3. Write KEY                         │  │
│  │  4. Subscribe CAN                     │  │
│  └───────────────────────────────────────┘  │
│                                             │
│  ┌───────────────────────────────────────┐  │
│  │  onCharacteristicNotification()      │  │
│  │  - CobsDecoder.decode()              │  │
│  │  - CanMessageParser.parse()          │  │
│  │  - DeviceStatusParser.extract()      │  │
│  └───────────────────────────────────────┘  │
└─────────────────────────────────────────────┘
```
