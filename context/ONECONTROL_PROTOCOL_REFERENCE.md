# OneControl BLE Protocol - Consolidated Reference

**Purpose:** Single source of truth for OneControl gateway BLE communication.  
**Last Updated:** December 21, 2025

---

## Table of Contents
1. [Gateway Identification](#gateway-identification)
2. [Service & Characteristic UUIDs](#service--characteristic-uuids)
3. [GATT Handle Mapping](#gatt-handle-mapping)
4. [Authentication Protocol](#authentication-protocol)
5. [Notification Subscription](#notification-subscription)
6. [Working Sequence Timeline](#working-sequence-timeline)
7. [MyRvLink Protocol](#myrvlink-protocol)
8. [Code Architecture Comparison](#code-architecture-comparison)
9. [Known Issues & Symptoms](#known-issues--symptoms)

---

## Gateway Identification

| Property | Value |
|----------|-------|
| MAC Address | `24:DC:C3:ED:1E:0A` |
| Device Name | `LCIRemotebzB9CztDZ` |
| Bond State | BOND_BONDED (required) |
| MTU | 185 bytes (negotiated) |

---

## Service & Characteristic UUIDs

### Auth Service: `00000010-0200-a58e-e411-afe28044e62c`

| Characteristic | UUID | Properties | Purpose |
|----------------|------|------------|---------|
| SEED | `00000011-...` | READ, NOTIFY | CAN events (optional subscription) |
| UNLOCK_STATUS | `00000012-...` | READ | Challenge/seed & "Unlocked" status |
| KEY | `00000013-...` | WRITE | TEA auth key (WRITE_TYPE_NO_RESPONSE) |
| AUTH_STATUS | `00000014-...` | READ, NOTIFY | CAN events (optional subscription) |

### Data Service: `00000030-0200-a58e-e411-afe28044e62c`

| Characteristic | UUID | Properties | Purpose |
|----------------|------|------------|---------|
| DATA_WRITE | `00000033-...` | WRITE | MyRvLink commands (WRITE_TYPE_DEFAULT) |
| DATA_READ | `00000034-...` | READ, NOTIFY | MyRvLink events (PRIMARY data channel) |

### CCCD Descriptor
- UUID: `00002902-0000-1000-8000-00805f9b34fb`
- Value: `0x0001` to enable notifications

---

## GATT Handle Mapping

From HCI capture `hci_capture_12_04_2025.json`:

| Handle | Characteristic | Description |
|--------|---------------|-------------|
| 0x002d | 00000012 | UNLOCK_STATUS (challenge read) |
| 0x002f | 00000013 | KEY (auth key write) |
| 0x0037 | 00000033 | DATA_WRITE (commands) |
| 0x0039 | 00000034 | DATA_READ (notifications) |
| 0x003a | CCCD for 0x0039 | Enable notifications on DATA_READ |

---

## Authentication Protocol

### TEA Encryption Algorithm

```kotlin
private fun calculateAuthKey(seed: Long): ByteArray {
    val cypher = 612643285L  // 0x2483FFD5 (MyRvLink RvLinkKeySeedCypher)
    
    var v0 = seed and 0xFFFFFFFFL
    var v1 = cypher and 0xFFFFFFFFL
    var sum = 0L
    val delta = 0x9E3779B9L
    
    // 32 rounds of TEA encryption
    repeat(32) {
        sum = (sum + delta) and 0xFFFFFFFFL
        v0 = (v0 + (((v1 shl 4) + 0xA341316CL) xor (v1 + sum) xor ((v1 ushr 5) + 0xC8013EA4L))) and 0xFFFFFFFFL
        v1 = (v1 + (((v0 shl 4) + 0x2143694EL) xor (v0 + sum) xor ((v0 ushr 5) + 0x4851D263L))) and 0xFFFFFFFFL
    }
    
    // Return as BIG-ENDIAN byte array
    return byteArrayOf(
        ((v0 shr 24) and 0xFF).toByte(),
        ((v0 shr 16) and 0xFF).toByte(),
        ((v0 shr 8) and 0xFF).toByte(),
        (v0 and 0xFF).toByte()
    )
}
```

### Authentication Flow
1. **Read** UNLOCK_STATUS (00000012) → Returns 4-byte challenge (e.g., `CD 59 A5 7B`)
2. **Calculate** KEY using TEA encryption
3. **Write** KEY to 00000013 with `WRITE_TYPE_NO_RESPONSE`
4. **Wait** 500ms
5. **Read** UNLOCK_STATUS again → Should return "Unlocked" (ASCII: `55 6E 6C 6F 63 6B 65 64`)
6. **Wait** 200ms
7. **Enable** notifications

---

## Notification Subscription

### Required Order (from HCI capture)
1. **00000034** (DATA_READ) - Primary data channel
2. **00000011** (SEED) - Optional but recommended
3. **00000014** (AUTH_STATUS) - Optional but recommended

### CCCD Write Sequence
```
setCharacteristicNotification(char, true)  // Local enable
wait 100ms
descriptor.value = ENABLE_NOTIFICATION_VALUE (0x0001)
gatt.writeDescriptor(descriptor)
wait for onDescriptorWrite callback
```

### Critical Timing
- First notification arrives ~50ms after CCCD write success
- Gateway disconnects (status 19) if no notifications flow within ~8 seconds

---

## Working Sequence Timeline

From HCI capture of legacy app:

```
218.878s: READ UNLOCK_STATUS → challenge: CD:59:A5:7B
219.018s: WRITE KEY → 30:76:F2:F7
219.625s: READ UNLOCK_STATUS → "Unlocked"
222.972s: WRITE CCCD to 0x003a → enable notifications
223.022s: FIRST NOTIFICATION from 0x0039 (50ms later!)
223.075s: Second notification...
[2019 total notifications in session]
```

---

## MyRvLink Protocol

### Frame Structure
- **COBS encoding** with CRC8 (init: 0x55)
- **Delimiter:** 0x00

### Command Format
```
[clientCommandId_lo][clientCommandId_hi][CommandType][deviceTableId][deviceId][payload...][CRC8]
→ COBS encode → append 0x00
```

### Event Types (gateway → app)
| Event | Byte | Description |
|-------|------|-------------|
| GatewayInformation | 0x01 | Gateway info (contains deviceTableId) |
| DeviceLockStatus | 0x04 | Lock status |
| RelayBasicLatchingStatusType2 | 0x06 | Relay/switch status |
| RvStatus | 0x07 | RV system status |
| DimmableLightStatus | 0x08 | Dimmable light state |
| HvacStatus | 0x0B | HVAC/climate zone |
| RelayHBridgeMomentaryStatusType2 | 0x0E | Slide/awning position |
| HourMeterStatus | 0x0F | Hour meter |
| Leveler4DeviceStatus | 0x10 | Leveling system |
| DeviceSessionStatus | 0x1A | Session status |
| TankSensorStatusV2 | 0x1B | Tank levels |
| RealTimeClock | 0x20 | Time sync |

### Command Types (app → gateway)
| Command | Byte | Description |
|---------|------|-------------|
| ActionSwitch | 0x42 | Toggle relay/switch |
| ActionDimmable | 0x43 | Set light brightness |
| GetDevices | 0x01 | Request device list (heartbeat) |

### GetDevices Command (Heartbeat)
```kotlin
val payload = byteArrayOf(
    (commandId and 0xFF).toByte(),
    ((commandId shr 8) and 0xFF).toByte(),
    0x01,  // CommandType: ActionSendDevices / CmdReadAllDevicesInTable
    deviceTableId,  // Usually 0x08
    0xFF   // DeviceId: all devices
)
// + CRC8 + COBS encode + 0x00 delimiter
```

---

## Code Architecture Comparison

### Legacy App (OneControlBleService.kt)

```kotlin
class OneControlBleService : Service() {
    // GATT stored immediately from connectGatt() return
    private var bluetoothGatt: BluetoothGatt? = null
    
    // Callback as anonymous inner object (has implicit this@Service reference)
    private val gattCallback = object : BluetoothGattCallback() {
        override fun onCharacteristicChanged(...) {
            // Uses bluetoothGatt field directly
        }
    }
    
    fun connect() {
        bluetoothGatt = device.connectGatt(this, false, gattCallback, TRANSPORT_LE)
    }
    
    fun enableDataNotifications() {
        // Uses bluetoothGatt?.setCharacteristicNotification()
        // Uses bluetoothGatt?.writeDescriptor()
        // Fires off multiple CCCD writes with delays (parallel)
    }
}
```

### Plugin App (OneControlDevicePlugin.kt)

```kotlin
class OneControlGattCallback(...) : BluetoothGattCallback() {
    // Separate top-level class (no implicit Service reference)
    private var currentGatt: BluetoothGatt? = null  // Set in onConnectionStateChange
    
    override fun onConnectionStateChange(gatt, status, newState) {
        currentGatt = gatt  // Set AFTER connection
    }
    
    fun enableDataNotifications(gatt: BluetoothGatt) {
        // Uses gatt parameter for operations
        // BUT uses currentGatt?.getService() for auth service lookup (inconsistent!)
        // Uses sequential queue (waits for each onDescriptorWrite)
    }
}
```

### Key Differences

| Aspect | Legacy App | Plugin App |
|--------|-----------|------------|
| Callback Type | Anonymous inner object | Separate top-level class |
| GATT Storage | Immediate from connectGatt() | In onConnectionStateChange |
| CCCD Writes | Parallel with delays | Sequential with queue |
| Service Access | bluetoothGatt field | Mix of gatt param and currentGatt |
| Service Context | `this` (Service) | Passed context |

---

## Known Issues & Symptoms

### Symptom: No Notifications Received
- Authentication succeeds ("Unlocked")
- CCCD writes succeed (status=0)
- `onCharacteristicChanged` NEVER fires
- Gateway disconnects after ~8 seconds (status 19)

### Verified Working
- Handle 57 (0x39) = DATA_READ characteristic
- `onRegisterForNotifications()` returns `registered=1, handle=57`
- GATT operations complete successfully

### Suspected Causes
1. **Callback architecture** - Top-level class vs anonymous inner object
2. **Sequential vs parallel CCCD writes** - Timing difference
3. **GATT reference inconsistency** - Mix of `gatt` param and `currentGatt` field

---

## Test Commands

### Check GATT Handle Map
```bash
adb shell dumpsys bluetooth_manager | grep -A50 "GATT Client Map"
```

### Clear BT State
```bash
adb shell am force-stop com.blemqttbridge
adb shell am force-stop com.onecontrol.blebridge
# Wait 30 seconds for gateway to reset
```

### Monitor Notifications
```bash
adb logcat | grep -E "📨📨📨|onCharacteristicChanged"
```

---

## File Locations

| File | Path |
|------|------|
| Legacy Service | `android_ble_bridge/app/.../OneControlBleService.kt` |
| Plugin Callback | `android_ble_plugin_bridge/app/.../OneControlDevicePlugin.kt` |
| HCI Capture | `android_ble_bridge/hci_capture_12_04_2025.json` |
| Auth Algorithm | `android_ble_bridge/docs/AUTHENTICATION_ALGORITHM.md` |
| Technical Spec | `android_ble_bridge/docs/technical_spec.md` |
