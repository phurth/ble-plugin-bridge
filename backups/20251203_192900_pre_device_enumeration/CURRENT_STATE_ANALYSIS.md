# Current State Analysis - Notification Regression

## Date: 2025-12-03

## Critical Finding
**Notifications were working at some point during development**, which means:
- The Android BLE stack CAN receive notifications from this gateway
- The issue is a **regression** we introduced, not a fundamental incompatibility
- The fix exists somewhere in our code history

## Current Symptoms

### What Works ✅
1. **BLE Connection**: Establishes successfully to gateway `24:DC:C3:ED:1E:0A`
2. **Service Discovery**: Correctly identifies Data Service (`00000030-0200-a58e-e411-afe28044e62c`)
3. **Characteristic Discovery**: Finds all characteristics including Data Read (`00000034`)
4. **MTU Negotiation**: Successfully negotiates to 185 bytes
5. **Bonding**: Device is bonded (bondState=12)
6. **CCCD Write**: Descriptor write succeeds (status=GATT_SUCCESS)
7. **setCharacteristicNotification**: Returns `true`
8. **Stream Reader**: Starts successfully and waits for notifications

### What Doesn't Work ❌
1. **`onCharacteristicChanged()` Never Called**: Neither Android 13+ nor legacy signature
2. **No Notifications Received**: `notificationQueue` remains empty
3. **Gateway Disconnects**: After ~8 seconds (idle timeout)
4. **No Data Flow**: Stream reader times out waiting for data

## Current Initialization Sequence

### 1. Connection Phase
```
connectToDevice()
  └─ device.connectGatt(autoConnect=false, TRANSPORT_LE)
```

### 2. Service Discovery Phase
```
onConnectionStateChange(STATE_CONNECTED)
  └─ gatt.requestMtu(185)
  └─ gatt.discoverServices() [after 500ms delay]
```

### 3. Service Mapping Phase
```
onServicesDiscovered()
  └─ Identify Data Service (00000030)
  └─ Store dataReadChar (00000034)
  └─ Store dataWriteChar (00000033)
  └─ Call onReady()
```

### 4. Ready Phase
```
onReady()
  └─ Set connectionState = READY
  └─ Reset subscription tracking
  └─ Attempt read of 00000012 (unlock status) - non-blocking
  └─ Call enableDataNotifications() [after 100ms delay]
```

### 5. Notification Subscription Phase
```
enableDataNotifications()
  └─ gatt.setCharacteristicNotification(dataReadChar, true) → returns TRUE
  └─ Increment notificationSubscriptionsPending
  └─ [After 100ms delay]:
      └─ Get CCCD descriptor (00002902)
      └─ descriptor.value = ENABLE_NOTIFICATION_VALUE (0x0001)
      └─ gatt.writeDescriptor(descriptor) → returns TRUE
```

### 6. Descriptor Write Callback
```
onDescriptorWrite(status=GATT_SUCCESS)
  └─ Decrement notificationSubscriptionsPending
  └─ Mark allNotificationsSubscribed = true
  └─ IMMEDIATELY call startActiveStreamReading()
```

### 7. Stream Reading Phase
```
startActiveStreamReading()
  └─ Create thread "OneControlStreamReader"
  └─ Wait on notificationQueue.wait(8000ms)
  └─ Timeout after 8 seconds (no notifications arrive)
```

### 8. Notification Callback (NEVER CALLED)
```
onCharacteristicChanged() [SHOULD BE CALLED BUT ISN'T]
  └─ notificationQueue.offer(data)
  └─ streamReadLock.notify()
```

## Code Architecture

### Notification Flow
```
[Gateway] --BLE Notification--> [Android BLE Stack]
                                       |
                                       v
                              onCharacteristicChanged()  ❌ NEVER CALLED
                                       |
                                       v
                         handleCharacteristicNotification()
                                       |
                                       v
                              notificationQueue.offer()
                                       |
                                       v
                         streamReadLock.notify()
                                       |
                                       v
                         startActiveStreamReading() thread wakes
                                       |
                                       v
                         notificationQueue.poll()
                                       |
                                       v
                         cobsByteDecoder.decodeByte()
                                       |
                                       v
                         processDecodedFrame()
```

### Key Variables

#### Connection State
- `bluetoothGatt: BluetoothGatt?` - Current GATT instance
- `currentDevice: BluetoothDevice?` - Connected device
- `isConnected: Boolean` - Connection flag
- `connectionState: ConnectionState` - State machine

#### Service/Characteristic References
- `dataService: BluetoothGattService?` - Data Service instance
- `dataReadChar: BluetoothGattCharacteristic?` - Notification characteristic
- `dataWriteChar: BluetoothGattCharacteristic?` - Write characteristic

#### Notification Subscription
- `notificationSubscriptionsPending: Int` - Count of pending CCCD writes
- `allNotificationsSubscribed: Boolean` - Flag for completion

#### Stream Reading
- `notificationQueue: ConcurrentLinkedQueue<ByteArray>` - Queues incoming data
- `streamReadLock: Object` - Synchronization for wait/notify
- `isStreamReadingActive: Boolean` - Thread active flag
- `shouldStopStreamReading: Boolean` - Stop signal

## Recent Changes That Could Have Broken Notifications

### Change 1: Removed Active Read Fallback (HIGH SUSPICION)
**When**: Recent session
**What**: Removed `bluetoothGatt.readCharacteristic()` fallback from stream reader
**Why**: Was injecting 244 bytes of zeros into COBS decoder
**Impact**: Now relies 100% on notifications, no fallback

```kotlin
// REMOVED CODE:
if (!hasData) {
    bluetoothGatt?.readCharacteristic(dataReadChar)
}
```

### Change 2: Added Unlock Status Read
**When**: Recent session  
**What**: Added read of characteristic `00000012` before enabling notifications
**Why**: To match HCI capture sequence
**Impact**: May interfere with GATT operation queue

```kotlin
// ADDED CODE in onReady():
bluetoothGatt!!.readCharacteristic(unlockStatusChar)
```

### Change 3: Multiple onReady() Guard
**When**: Recent session
**What**: Added guard to prevent `onReady()` from being called multiple times
**Why**: To fix infinite loop issue
**Impact**: Could prevent proper initialization on reconnect

```kotlin
// ADDED CODE:
if (connectionState == ConnectionState.READY) {
    return
}
```

### Change 4: Removed Timing Delays
**When**: Recent session
**What**: Removed 200ms delays before starting stream reader
**Why**: To catch early notifications (69ms after CCCD)
**Impact**: Stream reader now starts immediately

### Change 5: Android 13+ Callback Signature
**When**: Most recent session
**What**: Added new `onCharacteristicChanged` signature with ByteArray parameter
**Why**: Android 13 requires this signature
**Impact**: Both signatures present, should work for all Android versions

## Potential Root Causes

### Theory 1: GATT Operation Queue Collision
The unlock status read (`00000012`) may not complete before we try to enable notifications, causing the Android BLE stack to drop the notification subscription silently.

**Evidence**: The read never gets a callback (neither success nor error)

### Theory 2: Characteristic Reference Invalidation
The `dataReadChar` reference stored during service discovery may become invalid or stale by the time we try to enable notifications.

**Evidence**: None concrete, but BLE characteristic references can become stale

### Theory 3: Descriptor Write Confirmation Timing
The descriptor write succeeds, but we start the stream reader before the BLE stack has fully processed the CCCD update.

**Evidence**: HCI shows 47ms between CCCD write and response, we start immediately

### Theory 4: Connection State Mismatch
The `allNotificationsSubscribed` flag or `notificationSubscriptionsPending` counter may be in the wrong state, preventing proper startup.

**Evidence**: Logs show correct values, but race conditions possible

### Theory 5: GATT Callback Registration Issue
The `gattCallback` object may not be properly registered or may have become detached from the `bluetoothGatt` instance.

**Evidence**: All other callbacks work (MTU, service discovery, descriptor write)

## What We Know From HCI Capture

### Official App Sequence (Working)
```
t=449.935s - Error Response to handle 0x0031
t=449.988s - Error Response to handle 0x0033
t=450.048s - Error Response to handle 0x0039
t=450.078s - Error Response to handle 0x003b
t=450.115s - Error Response to handle 0x003f
t=450.138s - Error Response to handle 0x0040
t=450.149s - Read Request to handle 0x002d
t=450.175s - Read Response from 0x002d ("Unlocked")
t=450.451s - Write Request to 0x003a (CCCD) value=0x0001
t=450.497s - Write Response from 0x003a
t=450.520s - NOTIFICATION from 0x0039 (69ms after CCCD write!)
t=450.768s - NOTIFICATION from 0x0039
[... continuous notifications ...]
```

### Our App Sequence (Broken)
```
t=0.000s - gatt.discoverServices()
t=0.500s - Services discovered
t=0.600s - onReady() called
t=0.600s - Attempt read of 0x002d (00000012) - NO CALLBACK
t=0.700s - enableDataNotifications()
t=0.700s - setCharacteristicNotification() returns TRUE
t=0.800s - writeDescriptor() called
t=0.850s - onDescriptorWrite(SUCCESS)
t=0.850s - startActiveStreamReading()
t=0.850s - Stream reader waiting for notifications...
[... 8 seconds pass ...]
t=8.850s - Timeout, no notifications
t=8.850s - Gateway disconnects
```

## Comparison: Working vs. Broken

| Aspect | Official App (Working) | Our App (Broken) |
|--------|----------------------|------------------|
| Pre-CCCD Operations | Multiple reads (mostly fail) | One read (no callback) |
| CCCD Write | Success | Success |
| First Notification | 69ms after CCCD | NEVER |
| Stream Reader Start | Unknown timing | Immediately after CCCD |
| Callback Signature | Plugin.BLE (C#) | Native Android (Kotlin) |
| GATT Queue Management | Plugin.BLE library | Native Android |

## Next Steps for Debugging

### High Priority
1. **Capture fresh HCI snoop of OUR app** - Compare packet-level differences
2. **Test reverting Change 1** - Re-add active read fallback temporarily
3. **Test reverting Change 2** - Remove unlock status read
4. **Add delay before stream reader** - Give BLE stack time to process CCCD

### Medium Priority
1. **Verify characteristic reference** - Log characteristic object identity
2. **Test with simpler notification test** - Single read/notify without stream
3. **Check Android BLE permissions** - Ensure BLUETOOTH_CONNECT granted

### Low Priority  
1. **Factory reset gateway** - Clear all bonding data
2. **Test on different Android device** - Rule out device-specific issues
3. **Compare Plugin.BLE implementation** - See if it does something special

## Files Modified in Recent Sessions

1. `OneControlBleService.kt` - Main service, extensive changes
2. `PROTOCOL_MYRVLINK_BLE.md` - Protocol documentation
3. `HCI_CAPTURE_ANALYSIS.md` - HCI capture analysis

## Key Logging Points

### Should See (Currently DO)
- ✅ "✅ Gateway ready! Connected"
- ✅ "📝 Enabling notifications for Data read"
- ✅ "📝 setCharacteristicNotification result: true"
- ✅ "✅ Descriptor write successful"
- ✅ "✅ All notification subscriptions complete"
- ✅ "🔄 Active stream reading started"
- ✅ "⏳ Queue empty, waiting up to 8 seconds"

### Should See (Currently DON'T)
- ❌ "📨📨📨 onCharacteristicChanged"
- ❌ "📨 Notification received from"
- ❌ "📥 Processing queued notification"
- ❌ "✅ Decoded COBS frame"

## Conclusion

The regression is **definitely fixable** since notifications worked before. The most likely culprits are:
1. The removal of the active read fallback breaking notification callback registration
2. The unlock status read interfering with GATT operations
3. Timing changes that cause the BLE stack to miss the notification subscription

Once we have the new HCI capture from the official app, we can compare it packet-by-packet against a capture of our app to identify the exact difference.

