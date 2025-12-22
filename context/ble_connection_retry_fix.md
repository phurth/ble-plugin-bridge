# BLE Connection Retry Fix - Root Cause Analysis

**Date:** December 20, 2025  
**Status:** ✅ RESOLVED  
**Device:** OneControl Gateway (24:DC:C3:ED:1E:0A)

## Problem Statement

The `android_ble_plugin_bridge` app was unable to maintain a stable connection to the OneControl BLE gateway:

1. **Initial behavior:** Gateway disconnected after 6-10 seconds with `GATT_CONN_TERMINATE_PEER_USER` (status 19)
2. **Earlier behavior:** Connection failed immediately with `GATT_ERROR(133)` 
3. **Critical symptom:** NO notifications received from gateway before disconnect
4. **Comparison:** Legacy app (`android_ble_bridge`) connected successfully and received continuous notifications

## Investigation Process

### Controlled Comparison Setup

Testing was performed on the same device (phone 10.115.19.172) with the same gateway bond to eliminate variables:

- **Device:** Same phone with valid bond to gateway
- **Gateway:** Same OneControl gateway (24:DC:C3:ED:1E:0A)
- **Bond State:** Valid (BOND_BONDED)
- **Result:** Legacy app worked perfectly, plugin bridge failed consistently

### Key Observations

1. **Plugin Bridge Behavior:**
   - Connected to gateway
   - Authentication succeeded ("Unlocked" response received)
   - CCCD subscriptions succeeded (00000011, 00000014, 00000034)
   - GetDevices command sent (10 bytes)
   - **NO notifications received** (no `📨` logs)
   - Gateway disconnected after ~6 seconds

2. **Legacy App Behavior:**
   - Connected to gateway
   - Authentication succeeded
   - CCCD subscriptions succeeded
   - GetDevices command sent
   - **Continuous notifications received immediately** (`📨 onCharacteristicChanged`)
   - Connection remained stable

### Bluetooth Stack Analysis

Examining Bluetooth stack logs revealed the true error:

```
bt_stack: btm_ble_connected Security Manager: handle:8 enc_mode:0000  bda: 24:dc:c3:ed:1e:0a
bt_stack: gatt_connect_cback: from 24:dc:c3:ed:1e:0a connected: 0, conn_id: 0x0001reason: 0x003e
```

**Critical Discovery:**
- `enc_mode:0000` - No encryption negotiated
- `reason: 0x003e` (62 decimal) - **Connection Failed to be Established / Synchronization Timeout**
- Android reported this as status 133 to the app

### Legacy App Deep Dive

Fresh start of legacy app revealed it **also experienced the same failure** on first attempt:

```
22:01:00 - First connection attempt
bt_stack: gatt_connect_cback: from 24:dc:c3:ed:1e:0a connected: 0, reason: 0x003e
OneControlBleService: ❌ BLE Disconnected - GATT error (0x85)
OneControlBleService: 🔄 Scheduling reconnect attempt 1/3 in 5000ms...

22:01:06 - Retry after 5 seconds
bt_stack: gatt_connect_cback: from 24:dc:c3:ed:1e:0a connected: 1, reason: 000000
OneControlBleService: ✅ Connected successfully
📨 Notifications flowing immediately
```

**Breakthrough:** The legacy app has **automatic retry logic** that the plugin bridge was missing!

## Root Cause

The BLE connection to OneControl gateways fails on the first attempt due to **encryption negotiation timing**:

1. First `connectGatt()` attempt initiates connection
2. BLE stack attempts to establish L2CAP connection
3. Encryption negotiation hasn't completed (`enc_mode:0000`)
4. Connection times out with error 0x003e (Connection Failed to Establish)
5. Android translates this to status 133 or 62 for the app

**Why retry works:**
- 5-second delay allows BLE stack and gateway to stabilize
- Second connection attempt succeeds with proper encryption
- Notifications flow normally

This is **not** a bug in either the gateway or the app - it's a characteristic of BLE encryption negotiation with bonded devices that requires retry logic.

## Solution Implemented

Added automatic retry logic to `BaseBleService.kt`:

```kotlin
// In onConnectionStateChanged, STATE_DISCONNECTED case:
val shouldRetry = (status == 62 || status == 133) && 
                 device.bondState == BluetoothDevice.BOND_BONDED &&
                 pluginId != null

if (shouldRetry) {
    Log.w(TAG, "⚠️ Connection failed with $status - will retry in 5 seconds...")
    updateNotification("Connection failed - retrying...")
    
    gatt.close()
    connectedDevices.remove(device.address)
    pollingJobs[device.address]?.cancel()
    pollingJobs.remove(device.address)
    
    // Retry after 5 seconds (matches legacy app behavior)
    serviceScope.launch {
        delay(5000)
        Log.i(TAG, "🔄 Retrying connection to ${device.address}...")
        connectToDevice(device, pluginId!!)
    }
    return
}
```

### Additional Improvements

1. **MTU Request:** Added `gatt.requestMtu(185)` after service discovery (matches legacy app)
2. **CCCD Write Delay:** Added 100ms delay after `setCharacteristicNotification()` before descriptor write
3. **Connection Timing:** Reduced stabilization delay from 1500ms to 500ms (MTU negotiation time)

## Results

After implementing the retry logic:

✅ **First connection attempt:** Failed with status 62 (as expected)  
✅ **Retry after 5 seconds:** Connected successfully  
✅ **Notifications:** Flowing continuously (`📨 onCharacteristicChanged`)  
✅ **Stability:** Connection remained stable with continuous data flow  
✅ **MQTT:** Publishing device states successfully (with some queue overflow issues to address separately)

### Test Logs

```
22:03:14 - Plugin bridge started
22:03:15 - First connection attempt initiated
22:03:15 - Connection failed (status 62)
22:03:20 - Retry initiated
22:03:20 - Connection succeeded
22:04:10 - Notifications flowing: 📨📨📨 onCharacteristicChanged
         - 00 42 02 01 03 82 02 49 00 (9 bytes)
         - 00 07 07 0d 60 7f ff 01 53 00... (53 bytes)
         - Continuous data stream confirmed
```

## Key Learnings

1. **BLE encryption timing is variable** - First connection attempts may fail during encryption negotiation
2. **Retry logic is essential** - Production BLE apps should implement automatic retries for transient failures
3. **Status code mapping** - Android may report HCI error 0x003e as status 62 or 133 depending on context
4. **Comparison testing** - Having a working reference app was critical for identifying the missing retry logic
5. **Bluetooth stack logs** - Low-level `bt_stack` logs revealed the true error (`enc_mode:0000`, `reason: 0x003e`) that wasn't obvious from app-level logs

## Recommendations

1. **Keep retry logic** - Essential for reliable BLE connections to bonded devices
2. **Monitor first-attempt failures** - Log metrics to understand failure rates
3. **Consider exponential backoff** - For production, implement increasing delays (5s, 10s, 20s) for subsequent failures
4. **Add retry limits** - Legacy app has 3-attempt limit; consider similar constraint
5. **MTU negotiation** - Always request appropriate MTU (185 bytes for OneControl protocol)

## Related Issues

- **MQTT queue overflow:** Separate issue causing "Too many publishes in progress" errors - needs rate limiting
- **Status 19 disconnects:** Previously seen when notifications weren't working - resolved by fixing connection establishment

## Files Modified

- `android_ble_plugin_bridge/app/src/main/java/com/blemqttbridge/core/BaseBleService.kt`
  - Added retry logic for status 62/133 failures
  - Added MTU request (185 bytes)
  - Added 100ms delay before CCCD descriptor writes

## Testing

**Test Device:** Phone 10.115.19.172:5555  
**Gateway:** 24:DC:C3:ED:1E:0A (LCIRemotebzB9CztDZ)  
**Bond State:** Valid (BOND_BONDED)  
**Connection:** Stable after retry  
**Notifications:** Continuous data flow confirmed  
**Duration:** Monitored for 60+ seconds with no disconnects

---

# UPDATE: December 21, 2025 - Notification Delivery Issue

## Problem Persists

Despite the above fixes, **notifications are still NOT being delivered** to the plugin's callback. The symptom is:

1. Authentication succeeds (gateway returns "Unlocked")
2. CCCD writes succeed (status=0)
3. GetDevices commands sent successfully
4. **NO `onCharacteristicChanged` callbacks fire**
5. Gateway disconnects after ~8 seconds (status 19 - peer terminated)

## HCI Capture Analysis

Analyzed `/Users/petehurth/Downloads/Decom/android_ble_bridge/hci_capture_12_04_2025.json` (107MB, 2019 notifications captured):

### Working Sequence (Legacy App)
```
218.878s: Read challenge from 0x002d: cd:59:a5:7b
219.018s: KEY write to 0x002f: 30:76:f2:f7
219.625s: Read "Unlocked" from 0x002d
222.972s: CCCD write to 0x003a (enable notifications)
223.022s: FIRST NOTIFICATION from 0x0039 (50ms after CCCD!)
223.075s: Second notification...
[2019 notifications follow]
```

### Key Handle Mapping
- **Handle 0x0039**: Characteristic 00000034 (Data Read) - receives notifications
- **Handle 0x003a**: CCCD descriptor for 00000034 - write `0x0001` to enable
- **Handle 0x002d**: Characteristic 00000012 (UNLOCK_STATUS)
- **Handle 0x002f**: Characteristic 00000013 (KEY)

### Plugin Observations
From logcat during plugin connection:
```
BtGatt.GattService: got characteristic UUID=00000034 id: 57
onRegisterForNotifications() - address=null, status=0, registered=1, handle=57
```

Handle 57 decimal = 0x39 hex = **CORRECT handle!**

## Current Hypothesis

The plugin's notification subscription appears correct at the GATT level:
- Correct characteristic UUID (00000034)
- Correct handle (0x39 / 57)
- `setCharacteristicNotification()` returns true
- CCCD descriptor write returns status=0

**But `onCharacteristicChanged()` never fires!**

### Possible Causes

1. **Callback Architecture Difference**
   - Legacy: Anonymous inner object `private val gattCallback = object : BluetoothGattCallback() { ... }`
   - Plugin: Separate top-level class `class OneControlGattCallback(...) : BluetoothGattCallback()`
   - Could Android BLE dispatch differently to top-level classes vs anonymous objects?

2. **Context/Reference Issue**
   - Legacy callback has implicit reference to enclosing Service
   - Plugin callback is passed context explicitly
   - Could this affect how Android tracks notification registrations?

3. **GATT Object Identity**
   - Legacy stores `bluetoothGatt` field immediately after `connectGatt()`
   - Plugin uses `currentGatt` set in `onConnectionStateChange`
   - Could there be a subtle timing issue?

## Next Steps to Try

1. Convert plugin callback to anonymous inner object pattern (match legacy exactly)
2. Add logging to verify `gatt` object identity is consistent across all callbacks
3. Check if there's a difference in how the CCCD descriptor is obtained/written
4. Consider if the gateway is rejecting notifications for some protocol reason
