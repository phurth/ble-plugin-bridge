# Keepalive Investigation Results

## Key Finding: No Explicit Keepalive Messages

After investigating the decompiled WeRV app code, **the official app does NOT send explicit keepalive messages**. 

## How the Official App Stays Connected

1. **Receives Notifications**: The gateway sends periodic status updates, device events, and CAN messages via BLE notifications
2. **Sends Real Commands**: When needed, the app sends actual MyRvLink commands (GetDevices, GetDevicesMetadata, device control commands, etc.)
3. **Bidirectional Communication**: The connection stays alive through active two-way communication

## MyRvLink Command Format

Commands are NOT raw CAN messages. They use a high-level MyRvLink protocol:

**Format**: `[ClientCommandId (2 bytes)][CommandType (1 byte)][Command-specific data...]`

**Example - GetDevices Command**:
```
[ClientCommandId (2 bytes)][0x01][DeviceTableId][StartDeviceId][MaxDeviceRequestCount]
```

Where:
- ClientCommandId: Sequential command ID (ushort, little-endian)
- CommandType: 0x01 = GetDevices
- DeviceTableId: Gateway device table ID
- StartDeviceId: Starting device ID (usually 0)
- MaxDeviceRequestCount: Max devices to request (usually 255)

## Current Issue

We're sending raw CAN messages (`[0x00][0x00][0x00]`) which the gateway likely rejects because:
1. They're not valid MyRvLink commands
2. Empty CAN messages (length 0) may be invalid
3. The gateway expects MyRvLink protocol, not raw CAN

## Solution

Instead of sending empty CAN messages, we should:
1. Send a proper MyRvLink GetDevices command periodically (every 5-10 seconds)
2. Or wait for the gateway to send notifications (which should keep connection alive)
3. Or implement proper MyRvLink command protocol

## Next Steps

1. Implement MyRvLink command encoding
2. Send GetDevices command as keepalive (with proper ClientCommandId)
3. Handle GetDevices response to maintain connection
4. Monitor if gateway sends notifications that keep connection alive

