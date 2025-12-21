# BLE Authentication Status

## Current Status: ✅ WORKING

**Last Verified**: January 2025  
**Gateway**: 24:DC:C3:ED:1E:0A (Data Service v2)  
**Target Device**: Lenovo TB300FU tablet

## Authentication Flow

```
1. Connect to gateway
2. Service discovery (1.5s stabilization)
3. Read UNLOCK_STATUS (00000012) → 4-byte challenge
4. Calculate TEA KEY using cypher 612643285
5. Write KEY to 00000013 (WRITE_NO_RESPONSE, BIG-ENDIAN)
6. Verify "Unlocked" status
7. Subscribe: 00000011, 00000014, 00000034
8. Send GetDevices
9. Heartbeat (5s interval)
```

## Critical Implementation Details

| Detail | Value | Notes |
|--------|-------|-------|
| TEA Cypher | `612643285` (0x2483FFD5) | Data Service gateway specific |
| Byte Order | BIG-ENDIAN | Critical for Data Service gateways |
| Write Type | `WRITE_NO_RESPONSE` | For KEY characteristic 00000013 |
| Stabilization | 1500ms | After service discovery |
| CCCD Delay | 150ms | Between notification subscriptions |

## BLE Characteristics

| UUID | Name | Direction | Purpose |
|------|------|-----------|---------|
| 00000010 | Auth Service | - | Service container |
| 00000011 | SEED | NOTIFY | Auth seed |
| 00000012 | UNLOCK_STATUS | READ | Challenge/status |
| 00000013 | KEY | WRITE | TEA response |
| 00000014 | AUTH_STATUS | NOTIFY | Auth updates |
| 00000030 | Data Service | - | Service container |
| 00000034 | DATA_READ | NOTIFY | CAN bus data |
| 00000035 | DATA_WRITE | WRITE | CAN commands |

## Gateway Types

### Data Service Gateway (v2) - Current
- Uses 00000030 service
- Challenge-response TEA authentication
- BIG-ENDIAN byte order
- No PIN unlock required

### CAN Service Gateway (v1) - Legacy
- Uses PIN unlock
- LITTLE-ENDIAN byte order
- Different authentication flow

## Troubleshooting

### status=133 (GATT_INTERNAL_ERROR)
- **Cause**: Corrupted BLE bonding state
- **Solution**: Re-pair gateway in Android Bluetooth settings
- **Note**: This persists across app reinstalls

### Authentication Fails Silently
- Check byte order (must be BIG-ENDIAN)
- Verify write type is NO_RESPONSE
- Ensure 1.5s stabilization delay after service discovery

### No Notifications Received
- Authentication MUST complete before subscribing
- Gateway silently ignores CCCD writes if not authenticated
- Verify "Unlocked" status before enabling notifications

## Successful Log Output

```
🔐 Data Service gateway detected - performing challenge-response authentication...
📥 UNLOCK_STATUS response: XX XX XX XX (4 bytes)
🔑 Challenge (big-endian): 0x...
🔑 Calculated KEY: 0x...
✅ KEY written successfully
🔓 Verify status: 'Unlocked' (8 bytes)
✅ Authentication verified - gateway unlocked!
✅ Subscribed to SEED (00000011) notifications
✅ Subscribed to Auth (00000014) notifications
✅ Subscribed to DATA (00000034) notifications
✅ Data Service gateway ready!
📤 Sent initial GetDevices command (10 bytes)
💓 Heartbeat started for 24:DC:C3:ED:1E:0A
```

## Code Locations

- **Authentication**: `OneControlPlugin.onServicesDiscovered()`
- **TEA Encryption**: `protocol/TeaEncryption.kt`
- **GATT Operations**: `BaseBleService.kt`
- **Constants**: `protocol/Constants.kt`
