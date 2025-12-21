# BLE Authentication Session Summary - January 2025

## Overview

This document captures the debugging session that resolved BLE authentication issues with the OneControl gateway and prepared the codebase for MQTT integration.

## Key Accomplishments

### ✅ BLE Authentication Fixed
- **Status**: COMPLETE
- **Issue**: `status=133` GATT_INTERNAL_ERROR on characteristic reads
- **Root Cause**: Corrupted BLE bonding/GATT cache state on tablet (persisted across app reinstalls)
- **Solution**: Re-pairing the gateway on the tablet cleared the corrupted state

### ✅ UI Flow Fixed for Fresh Install
- **Status**: COMPLETE
- **Issue**: On fresh install, no plugins enabled by default, service wouldn't auto-start
- **Solution**: Modified `ServiceStateManager.getEnabledBlePlugins()` to default to "onecontrol" on first run

### ✅ Full Authentication Cycle Working
Verified complete flow:
1. Connect to gateway
2. Service discovery with 1.5s stabilization delay
3. Read UNLOCK_STATUS (00000012) for challenge (4 bytes)
4. Calculate TEA KEY using cypher `612643285` (0x2483FFD5)
5. Write KEY to 00000013 (BIG-ENDIAN, WRITE_NO_RESPONSE)
6. Verify "Unlocked" status
7. Subscribe to notifications (SEED, Auth, DATA)
8. Send GetDevices command
9. Heartbeat running every 5 seconds

## Device Configuration

### Target Deployment
- **Device**: Lenovo TB300FU tablet
- **ADB**: `10.115.19.204:5555`
- **Android**: 14 (API 34)

### Gateway
- **MAC**: `24:DC:C3:ED:1E:0A`
- **PIN**: `090336`
- **TEA Cypher**: `612643285` (0x2483FFD5)
- **Type**: Data Service Gateway (00000030)

### MQTT Broker
- **URL**: `tcp://10.115.19.131:1883`
- **Credentials**: `mqtt/mqtt`

## Key BLE UUIDs

| Characteristic | UUID | Purpose |
|----------------|------|---------|
| Auth Service | 00000010 | Authentication service |
| SEED | 00000011 | Auth seed notifications |
| UNLOCK_STATUS | 00000012 | Challenge/Unlock status |
| KEY | 00000013 | TEA authentication key |
| AUTH_STATUS | 00000014 | Auth notifications |
| Data Service | 00000030 | Data service |
| DATA_READ | 00000034 | CAN data notifications |
| DATA_WRITE | 00000035 | CAN command writes |

## Authentication Protocol

### Data Service Gateway (v2)
1. **Read UNLOCK_STATUS** (00000012) - Returns 4-byte challenge (BIG-ENDIAN)
2. **Calculate KEY**: `TEA_encrypt(challenge, cypher_612643285)`
3. **Write KEY** to 00000013 - **CRITICAL**: Use `WRITE_TYPE_NO_RESPONSE`
4. **Verify**: Read UNLOCK_STATUS again - Should return "Unlocked"
5. **Subscribe**: Enable notifications on 00000011, 00000014, 00000034
6. **Send GetDevices**: Wake up gateway with initial command

### Critical Implementation Details
- Byte order: **BIG-ENDIAN** for Data Service gateways
- Write type: `WRITE_TYPE_NO_RESPONSE` for KEY characteristic
- Stabilization delay: 1.5s after service discovery before GATT operations
- Notification timing: 150ms delay between CCCD subscriptions

## Code Changes Made

### ServiceStateManager.kt
```kotlin
fun getEnabledBlePlugins(): Set<String> {
    if (!prefs.contains(KEY_ENABLED_BLE_PLUGINS)) {
        // Default to onecontrol on first run
        return setOf("onecontrol")
    }
    return prefs.getStringSet(KEY_ENABLED_BLE_PLUGINS, emptySet()) ?: emptySet()
}
```

### BaseBleService.kt
- GATT cache refresh on connection
- 1.5s stabilization delay after service discovery
- Bonded device direct connection (skip scanning)
- Auto-reconnect on disconnection

### OneControlPlugin.kt
- Full Data Service gateway authentication
- Challenge-response TEA encryption
- BIG-ENDIAN byte order handling
- Notification subscription for all 3 characteristics
- COBS decoder for CAN message framing

## Next Steps: MQTT Integration

The BLE layer is now fully working. Next phase is wiring device data to MQTT:

### Data Flow
```
Gateway → DATA (00000034) notifications → COBS decode → CAN parse → DeviceStateTracker → MQTT publish
```

### Key Files
- `OneControlPlugin.kt` - Receives DATA notifications, processes COBS/CAN
- `DeviceStateTracker` - Maintains device states (in MyRvLinkEventDecoders.kt)
- `MqttOutputPlugin.kt` - Publishes to MQTT broker
- `HomeAssistantMqttDiscovery.kt` - HA discovery payloads

### Tasks
1. Connect DeviceStateTracker state changes to MQTT output
2. Generate proper Home Assistant discovery payloads for each device type
3. Handle commands from MQTT → CAN writes to gateway
4. Test end-to-end with real devices

## Debugging Notes

### status=133 Resolution
- Both legacy app AND new app failed with same error on tablet
- Fresh phone worked perfectly - proved code was correct
- Re-pairing gateway on tablet resolved the issue
- Root cause: OS-level BLE bonding cache corruption

### Logcat Filter
```bash
adb logcat -d | grep -iE "OneControl|BaseBle|auth|unlock|challenge|KEY|bond"
```

### Successful Authentication Log
```
🔐 Data Service gateway detected - performing challenge-response authentication...
📥 UNLOCK_STATUS response: XX XX XX XX (4 bytes)
🔑 Challenge (big-endian): 0x...
🔑 Calculated KEY: 0x...
✅ KEY written successfully
🔓 Verify status: 'Unlocked' (8 bytes)
✅ Authentication verified - gateway unlocked!
✅ Subscribed to SEED/Auth/DATA notifications
✅ Data Service gateway ready!
📤 Sent initial GetDevices command (10 bytes)
💓 Heartbeat started for 24:DC:C3:ED:1E:0A
```

## Session Timeline

1. Started with status=133 GATT errors on tablet
2. Implemented multiple fixes (MQTT stability, auto-start, scan filters)
3. Tested on fresh phone - SUCCESS (code proven correct)
4. Re-paired gateway on tablet - FIXED
5. Fixed UI flow for fresh install (default plugin enabled)
6. Verified complete authentication cycle
7. Ready for MQTT integration phase
