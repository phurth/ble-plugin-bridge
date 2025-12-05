# Debugging Approach - BLE Connection Issues

## Current Status
- ✅ Connection established
- ✅ Bonding/pairing working
- ✅ Service discovery working
- ✅ Authentication (TEA) working
- ✅ Commands being sent successfully (`writeResult=true`)
- ✅ Heartbeat running
- ❌ **ZERO notifications received** - `onCharacteristicChanged` never called
- ❌ Gateway disconnects after ~5-6 seconds despite active communication

## Critical Finding
**Our gateway does NOT have the CAN Service (`00000000`)** that the official app expects. Instead, it only has:
- Data Service: `00000030-0200-a58e-e411-afe28044e62c`
  - Write: `00000033`
  - Read: `00000034`

The official app's `BleCommunicationsAdapter` specifically looks for:
- CAN Service: `00000000-0200-A58E-E411-AFE28044E62C`
  - Write: `00000001`
  - Read: `00000002`

## Possible Issues

### 1. Gateway Variant Mismatch
- Our gateway might be a different hardware/firmware variant
- May require different initialization sequence
- May use different command format

### 2. Notification Subscription Not Working
- Descriptor writes complete, but notifications not actually enabled
- Android BLE stack issue
- Gateway not accepting our subscription

### 3. Command Format Issue
- Gateway rejecting our commands silently
- Wrong COBS encoding
- Wrong command structure
- Missing required fields

### 4. Timing/Sequence Issue
- Wrong order of operations
- Missing delays
- Race conditions

## Next Steps to Try

### Option 1: BLE Packet Capture
Use a BLE sniffer (nRF Connect, Wireshark with BLE adapter) to capture:
- What the official app actually sends
- What the gateway responds with
- Compare to what we're sending

### Option 2: Try Different Service/Characteristic
Since our gateway uses Data Service, maybe we need to:
- Try different characteristics
- Try different service UUIDs
- Check if there's a hidden/unadvertised service

### Option 3: Verify Notification Subscription
- Add explicit verification that notifications are enabled
- Check if descriptor value is actually set correctly
- Try reading the descriptor back to verify

### Option 4: Try Raw BLE Commands
- Skip COBS encoding temporarily
- Send raw bytes to see if gateway responds
- Test if gateway accepts any data at all

### Option 5: Check Android BLE Logs
Enable verbose BLE logging:
```bash
adb shell setprop log.tag.BluetoothGatt VERBOSE
adb shell setprop log.tag.BluetoothAdapter VERBOSE
```

### Option 6: Contact Gateway Manufacturer
- This might be a known issue
- May require firmware update
- May need specific app version

## What We Know Works
1. BLE connection (GATT)
2. Service/characteristic discovery
3. Bonding/pairing
4. Authentication (TEA encryption)
5. Characteristic writes (confirmed by `onCharacteristicWrite` callback)

## What Doesn't Work
1. Receiving notifications (zero data received)
2. Maintaining connection (disconnects after ~5-6 seconds)
3. Getting any response from gateway

## Hypothesis
The gateway might be:
- Rejecting our commands due to wrong format
- Not sending data because we're not properly subscribed
- Disconnecting because it detects we're not the official app
- Requiring a different initialization sequence for Data Service variant

