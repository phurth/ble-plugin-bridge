# BLE Notification Fix - December 22, 2025

## Executive Summary

After extensive debugging, we successfully fixed the BLE notification issue where the plugin app would connect and authenticate but receive **zero `onCharacteristicChanged` callbacks**. The fix was informed by analyzing decompiled DLLs from the official OneControl manufacturer's app.

## The Problem

### Symptoms
- Plugin app connects to gateway successfully
- MTU exchange completes (185 bytes)
- CCCD writes succeed for DATA_READ (00000034)
- GetDevices commands are sent successfully
- **BUT: Zero `onCharacteristicChanged` callbacks received**
- At BT stack level: `bta_gattc_proc_other_indication` count was 0 (vs 47 in legacy app)

### Root Cause
A **race condition** in the authentication flow caused notifications to be enabled **before** the gateway had fully processed the KEY write authentication.

## Solution

### Key Discovery from Decompiled DLLs

The official app's `BleDeviceUnlockManager.cs` revealed the exact authentication sequence:

```csharp
// From ids.portable_ble (2.1.0.0)/BleDeviceUnlockManager.cs

public static readonly Guid SeedCharacteristicGuidDefault = Guid.Parse("00000012-0200-a58e-e411-afe28044e62c");
public static readonly Guid KeyCharacteristicGuidDefault = Guid.Parse("00000013-0200-a58e-e411-afe28044e62c");

public static uint Encrypt(uint cypher, uint seed)
{
    uint num = 2654435769u;  // TEA delta constant
    for (int i = 0; i < 32; i++)
    {
        seed += ((cypher << 4) + 1131376761) ^ (cypher + num) ^ ((cypher >> 5) + 1919510376);
        cypher += ((seed << 4) + 1948272964) ^ (seed + num) ^ ((seed >> 5) + 1400073827);
        num += 2654435769u;
    }
    return seed;
}
```

### Gateway-Specific Cypher Values

From `MyRvLinkBleGatewayScanResult.cs`:
```csharp
public const uint RvLinkKeySeedCypher = 612643285u;  // 0x248431D5
```

Different gateway types use different cyphers:

| Gateway Type | Cypher Value | Hex |
|--------------|--------------|-----|
| **MyRvLink** (LCIRemote) | `612643285` | `0x248431D5` |
| **IdsCanAccessory** | `2645682455` | `0x9DB1E917` |
| **X180T Gateway** | `3357376288` | `0xC8261B20` |
| **SureShade** | `1360062733` | `0x51081D0D` |

### Correct Authentication Flow

From `BleManager.cs` and `AccessoryConnectionManager.cs`:

```
1. Connect to device
2. Request MTU (185 bytes)
3. Read SEED characteristic (00000012) → get 4-byte challenge
4. Encrypt(cypher, seed) → calculate KEY response
5. Write KEY to characteristic (00000013)
6. Read SEED again → verify returns "Unlocked" string
7. THEN enable notifications via CCCD writes
8. Start sending commands
```

### The Bug

**Before (broken):**
```kotlin
override fun onCharacteristicWrite(gatt, characteristic, status) {
    if (characteristic.uuid == KEY_CHARACTERISTIC_UUID) {
        // BUG: Called enableDataNotifications immediately after KEY write
        enableDataNotifications(gatt)  // ← Race condition!
    }
}

fun handleUnlockStatusRead(gatt, data) {
    if (data.size == 4) {  // Challenge bytes
        // Calculate and write KEY
        gatt.writeCharacteristic(keyChar)
        
        // Also scheduled a re-read to verify...
        handler.postDelayed({
            gatt.readCharacteristic(unlockStatusChar)  // Verify "Unlocked"
        }, 500)
    } else if (data.contains("Unlocked")) {
        enableDataNotifications(gatt)  // ← Also called here!
    }
}
```

This caused `enableDataNotifications` to be called **twice**:
1. Immediately in `onCharacteristicWrite` after KEY write
2. After verification in `handleUnlockStatusRead`

The gateway hadn't finished processing the KEY when notifications were enabled the first time.

**After (fixed):**
```kotlin
override fun onCharacteristicWrite(gatt, characteristic, status) {
    if (characteristic.uuid == KEY_CHARACTERISTIC_UUID) {
        // DON'T call enableDataNotifications here
        // Let the verification read trigger it
        Log.i(TAG, "KEY write complete - waiting for UNLOCK_STATUS verify...")
    }
}
```

## Byte Order Analysis

### Official App Uses BIG-ENDIAN

From `ArrayExtension.cs`:
```csharp
public static uint GetValueUInt32(this byte[] buffer, int startOffset, Endian endian = Endian.Big)
{
    // Default is BIG-ENDIAN
    b = buffer[startOffset];
    b2 = buffer[1 + startOffset];
    b3 = buffer[2 + startOffset];
    b4 = buffer[3 + startOffset];
    return (uint)((b << 24) | (b2 << 16) | (b3 << 8) | b4);
}
```

### Verified with Test Case

From legacy app logs:
- Challenge bytes: `21 81 80 48`
- Expected KEY: `0B F0 42 A9`

Python verification:
```python
def encrypt(cypher, seed):
    mask = 0xFFFFFFFF
    num = 2654435769
    for i in range(32):
        seed = (seed + (((cypher << 4) + 1131376761) ^ (cypher + num) ^ ((cypher >> 5) + 1919510376))) & mask
        cypher = (cypher + (((seed << 4) + 1948272964) ^ (seed + num) ^ ((seed >> 5) + 1400073827))) & mask
        num = (num + 2654435769) & mask
    return seed

# Challenge: 21 81 80 48 (big-endian)
seed = 0x21818048  # 562135112
cypher = 612643285

result = encrypt(cypher, seed)
# Result: 0x0BF042A9 → bytes: 0B F0 42 A9 ✓
```

## Files Modified

### OneControlDevicePlugin.kt

Location: `/android_ble_plugin_bridge/app/src/main/java/com/blemqttbridge/plugins/onecontrol/OneControlDevicePlugin.kt`

**Change:** Modified `onCharacteristicWrite` to NOT call `enableDataNotifications` after KEY write:

```kotlin
override fun onCharacteristicWrite(gatt: BluetoothGatt, characteristic: BluetoothGattCharacteristic, status: Int) {
    if (status == BluetoothGatt.GATT_SUCCESS) {
        if (characteristic.uuid == KEY_CHARACTERISTIC_UUID) {
            // After KEY write, handleUnlockStatusRead will re-read UNLOCK_STATUS to verify
            // Don't call enableDataNotifications here - let the verify step do it
            Log.i(TAG, "✅ KEY write complete - waiting for UNLOCK_STATUS verify read...")
        }
    } else {
        // If KEY write fails, skip verification and try to enable notifications anyway
        if (characteristic.uuid == KEY_CHARACTERISTIC_UUID) {
            Log.w(TAG, "⚠️ KEY write failed, skipping verification, attempting notifications...")
            handler.postDelayed({
                enableDataNotifications(gatt)
            }, 100)
        }
    }
}
```

## Decompiled DLL Source Files

Key files analyzed from `/android_ble_plugin_bridge/context/official_app_dlls_decompiled/`:

| File | Purpose |
|------|---------|
| `ids.portable_ble/BleDeviceUnlockManager.cs` | TEA encryption algorithm, KEY/SEED exchange |
| `ids.portable_ble/BleManager.cs` | Connection flow, KeySeedCypher handling |
| `ids.portable_ble/BleStream.cs` | MTU negotiation (185 bytes default) |
| `ids.portable.common/ArrayExtension.cs` | Byte order (BIG-ENDIAN default) |
| `OneControl.Direct.MyRvLinkBle/MyRvLinkBleGatewayScanResult.cs` | MyRvLink cypher value |
| `OneControl.Direct.MyRvLinkBle/DirectConnectionMyRvLinkBle.cs` | Data service UUIDs |
| `OneControl.Direct.IdsCanAccessoryBle/AccessoryConnectionManager.cs` | Accessory cypher value |

## Verification

After the fix, logs show successful notification reception:

```
08:51:41.713 📨📨📨 onCharacteristicChanged (legacy): 00000034-..., 9 bytes
08:51:41.715 ✅ Decoded COBS frame: 5 bytes
08:51:41.716 📦 Processing decoded frame: 5 bytes - 02 02 00 82 02
...
08:52:14.659 📦 DimmableLightStatus event
08:52:14.659 📦 Dimmable 8:9 brightness=0
...
08:52:14.835 📦 DeviceOnlineStatus event
08:52:14.835 📦 Device 8:14 online=true
```

## Lessons Learned

1. **Decompiled source code is invaluable** - The official app's DLLs provided the exact algorithm and sequence
2. **Race conditions in BLE are subtle** - The gateway needs time to process authentication before notifications work
3. **Byte order matters** - Official app uses BIG-ENDIAN for KEY/SEED exchange
4. **Different devices = different cyphers** - Must use correct cypher for gateway type
5. **Verification step is critical** - Don't enable notifications until gateway confirms "Unlocked"

## Technical Details

### Service UUIDs
- **Auth Service**: `00000010-0200-a58e-e411-afe28044e62c`
- **Data Service**: `00000030-0200-a58e-e411-afe28044e62c`

### Characteristic UUIDs (Auth Service)
- `00000011` - SEED (READ, NOTIFY)
- `00000012` - UNLOCK_STATUS (READ) - returns challenge or "Unlocked"
- `00000013` - KEY (WRITE, WRITE_NO_RESPONSE)
- `00000014` - AUTH_STATUS (READ, NOTIFY)

### Characteristic UUIDs (Data Service)
- `00000033` - DATA_WRITE (WRITE)
- `00000034` - DATA_READ (NOTIFY) - main data channel

### TEA Algorithm Constants
```
Delta:     0x9E3779B9 (2654435769)
Key[0]:    0x4365F889 (1131376761)
Key[1]:    0x72615A68 (1919510376)
Key[2]:    0x74124DC4 (1948272964)
Key[3]:    0x5367E703 (1400073827)
```
