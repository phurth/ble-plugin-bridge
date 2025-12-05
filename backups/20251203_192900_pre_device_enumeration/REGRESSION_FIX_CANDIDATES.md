# Regression Fix Candidates - Prioritized Action List

## Date: 2025-12-03

This document lists specific code changes to try, ranked by likelihood of fixing the notification regression.

## Top Priority Fixes (Try These First)

### Fix #1: Remove Unlock Status Read [HIGHEST PRIORITY]
**Suspicion Level**: 🔥🔥🔥 VERY HIGH

**Problem**: We're issuing a read of characteristic `00000012` that never gets a callback. This may be blocking the GATT operation queue, preventing the notification subscription from actually taking effect.

**Code to Remove**:
```kotlin
// In onReady() for Data Service gateways:
// REMOVE THIS BLOCK:
Log.i(TAG, "📖 Attempting to read unlock status (per HCI capture, may fail)...")
authService?.let { service ->
    val unlockStatusChar = service.getCharacteristic(UUID.fromString("00000012-0200-a58e-e411-afe28044e62c"))
    if (unlockStatusChar != null && bluetoothGatt != null) {
        Log.i(TAG, "📖 Issuing read for characteristic 00000012 (unlock status)...")
        bluetoothGatt!!.readCharacteristic(unlockStatusChar)
    }
}
```

**Rationale**: 
- This read was added to match the HCI capture
- HCI shows this read getting ERROR responses, not success
- It never completes in our app (no callback)
- May be blocking GATT queue

---

### Fix #2: Add Small Delay Before Stream Reader Start [HIGH PRIORITY]
**Suspicion Level**: 🔥🔥 HIGH

**Problem**: We start the stream reader immediately after CCCD write completes, but the Android BLE stack may need time to fully process the notification subscription.

**Code to Change**:
```kotlin
// In onDescriptorWrite() for Data Service gateways:
// CHANGE THIS:
startActiveStreamReading()

// TO THIS:
handler.postDelayed({
    startActiveStreamReading()
}, 50)  // 50ms delay to let BLE stack process CCCD
```

**Rationale**:
- HCI shows 47ms between CCCD write and write response
- HCI shows 69ms between CCCD write and first notification
- Android BLE stack needs time to register the notification callback
- Small delay might let stack complete registration

---

### Fix #3: Verify Characteristic Reference Before Subscription [MEDIUM-HIGH PRIORITY]
**Suspicion Level**: 🔥 MEDIUM-HIGH

**Problem**: The `dataReadChar` reference may become stale between service discovery and notification subscription.

**Code to Add**:
```kotlin
// In enableDataNotifications():
// ADD THIS BEFORE setCharacteristicNotification:
val readChar = canReadChar ?: dataReadChar
if (readChar == null) {
    Log.e(TAG, "❌ Read characteristic is null!")
    return
}

// Verify the characteristic is still valid by re-fetching it
val service = readChar.service
val freshChar = service.getCharacteristic(readChar.uuid)
if (freshChar == null) {
    Log.e(TAG, "❌ Failed to re-fetch characteristic!")
    return
}

Log.i(TAG, "📝 Using fresh characteristic reference: ${freshChar.uuid}")
// USE freshChar instead of readChar for setCharacteristicNotification
```

**Rationale**:
- BLE characteristic references can become stale
- Re-fetching ensures we have a valid reference
- Official app using Plugin.BLE might handle this differently

---

## Medium Priority Fixes

### Fix #4: Simplify onReady() Guard [MEDIUM PRIORITY]
**Suspicion Level**: 🔥 MEDIUM

**Problem**: The guard preventing multiple `onReady()` calls might prevent proper reinitialization on reconnect.

**Code to Change**:
```kotlin
// In onReady():
// CHANGE THIS:
if (connectionState == ConnectionState.READY) {
    Log.d(TAG, "onReady() called but already in READY state, ignoring")
    return
}

// TO THIS:
if (connectionState == ConnectionState.READY && allNotificationsSubscribed) {
    Log.d(TAG, "onReady() called but already fully initialized, ignoring")
    return
}
```

**Rationale**:
- Current guard is too aggressive
- Prevents re-enabling notifications after disconnect/reconnect
- Should only skip if BOTH ready AND subscribed

---

### Fix #5: Force Notification Callback Registration [MEDIUM PRIORITY]
**Suspicion Level**: 🔥 MEDIUM

**Problem**: The notification callback may not be properly registered with the BLE stack.

**Code to Add**:
```kotlin
// In enableDataNotifications(), AFTER setCharacteristicNotification:
// ADD THIS:
// Force immediate callback registration by reading the characteristic
handler.postDelayed({
    try {
        readChar?.let { ch ->
            Log.i(TAG, "📝 Forcing characteristic read to establish callback")
            bluetoothGatt?.readCharacteristic(ch)
        }
    } catch (e: Exception) {
        Log.w(TAG, "📝 Force read failed: ${e.message}")
    }
}, 50)  // After descriptor write has time to complete
```

**Rationale**:
- Some BLE stacks require a read to "prime" the notification callback
- Official app may do this implicitly
- Low risk to try

---

## Low Priority Fixes (Try If Above Don't Work)

### Fix #6: Reset GATT Connection [LOW PRIORITY]
**Suspicion Level**: 🔥 LOW

**Problem**: The GATT connection may be in a bad state.

**Code to Add**:
```kotlin
// In onServicesDiscovered(), BEFORE calling onReady():
// ADD THIS:
// Ensure GATT connection is fully established
Thread.sleep(100)
```

---

### Fix #7: Explicit CCCD Read-Back Verification [LOW PRIORITY]
**Suspicion Level**: 🔥 LOW

**Problem**: The CCCD write may not have actually taken effect.

**Code to Add**:
```kotlin
// In onDescriptorWrite(), AFTER success:
// ADD THIS:
handler.postDelayed({
    val descriptor = char.getDescriptor(UUID.fromString("00002902-0000-1000-8000-00805f9b34fb"))
    if (descriptor != null) {
        bluetoothGatt?.readDescriptor(descriptor)
    }
}, 100)

// And add handler in onDescriptorRead:
override fun onDescriptorRead(gatt: BluetoothGatt, descriptor: BluetoothGattDescriptor, status: Int) {
    if (status == BluetoothGatt.GATT_SUCCESS) {
        val value = descriptor.value
        Log.i(TAG, "📖 CCCD read back: ${value?.joinToString(" ") { "%02X".format(it) }}")
    }
}
```

---

## Testing Strategy

### Phase 1: Quick Fixes (< 5 minutes each)
1. Try Fix #1 (remove unlock read) - REBUILD & TEST
2. If #1 fails, try Fix #2 (add 50ms delay) - REBUILD & TEST  
3. If #2 fails, try BOTH #1 AND #2 together - REBUILD & TEST

### Phase 2: Characteristic Verification (10 minutes)
4. Try Fix #3 (re-fetch characteristic) - REBUILD & TEST
5. Try Fix #4 (simplify guard) - REBUILD & TEST

### Phase 3: Advanced (15 minutes)
6. Try Fix #5 (force callback registration) - REBUILD & TEST
7. Try combinations of above fixes

### Phase 4: Wait for HCI Capture
8. Compare our app's HCI capture to official app
9. Identify exact packet-level differences
10. Implement specific fixes based on capture comparison

## Success Criteria

When the fix works, you will see in the logs:
```
📨📨📨 onCharacteristicChanged (Android 13+) CALLED for 00000034-0200-a58e-e411-afe28044e62c: X bytes
📨 Notification received from 00000034-0200-a58e-e411-afe28044e62c: X bytes
📥 Processing queued notification: X bytes
✅ Decoded COBS frame: Y bytes
```

## Known Working State (To Restore)

At some point earlier in development, notifications were working. If we can identify the exact git commit or code state when it worked, we can:
1. Check out that version
2. Compare it line-by-line to current version
3. Identify the exact breaking change
4. Apply the fix

**ACTION ITEM**: If you have git history or backup copies of earlier working code, that would be extremely valuable.

## Summary

**Most Likely Fix**: Remove the unlock status read (Fix #1) or add a small delay before starting stream reader (Fix #2).

**Recommended First Attempt**: Try Fix #1 + Fix #2 together:
- Remove unlock status read
- Add 50ms delay before `startActiveStreamReading()`
- Rebuild and test

This combination addresses the two most likely causes:
1. GATT queue blocking from incomplete read
2. Insufficient time for BLE stack to process CCCD

If this doesn't work, the new HCI capture will give us definitive answers.

