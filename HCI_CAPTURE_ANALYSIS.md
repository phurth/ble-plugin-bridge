# HCI Capture Analysis - Official OneControl App BLE Communication

**Source**: `bt_snooper.json` - Wireshark HCI snoop capture from official OneControl Android app  
**Date**: 2025-11-26  
**Gateway**: LCI Remote (Data Service gateway, UUID `00000030-0200-a58e-e411-afe28044e62c`)

## Executive Summary

**Key Finding**: The gateway sends **proactive notifications** immediately after CCCD is enabled. No periodic keepalive/heartbeat commands are required from the client. The gateway maintains the connection by sending continuous status updates and events via BLE notifications.

**Statistics**:
- **186 notifications** received from gateway (handle 0x0039)
- **28 writes** sent by app (handle 0x0037)
- **Ratio**: ~6.6 notifications per write
- Gateway sends data **proactively**, not just in response to commands

---

## 1. Connection Sequence

### 1.1 Timeline of Key Events

| Event | Frame | Time (s) | Delay from Previous |
|-------|-------|----------|---------------------|
| CCCD write to 0x003a (enable notifications) | 2708 | 450.451 | - |
| **First notification arrives** | 2724 | 450.520 | **69ms** |
| First write to 0x0037 (GetDevices) | 2815 | 451.508 | 1057ms after CCCD |

### 1.2 Critical Finding

**The gateway sends the first notification 69ms after CCCD is enabled, BEFORE any command is sent.**

This proves:
- Gateway sends `GatewayInformation` and other events **proactively**
- No initial command is required to "wake up" the gateway
- The gateway maintains the connection by sending periodic status updates

---

## 2. Notification Pattern

### 2.1 Notification Characteristics

- **Handle**: `0x0039` (Data Read characteristic, UUID `00000034-0200-a58e-e411-afe28044e62c`)
- **Opcode**: `0x1b` (Handle Value Notification)
- **Total Count**: 186 notifications in the capture
- **Format**: COBS-encoded MyRvLink events (frames delimited by `0x00`)

### 2.2 Example Notification Data

First notification (frame 2724):
```
00:06:03:08:0e:ff:fc:b7:00
```

This is a COBS-encoded frame:
- Starts with `0x00` (frame delimiter)
- Contains MyRvLink event data
- Ends with `0x00` (frame delimiter)

### 2.3 Notification Frequency

Notifications arrive continuously throughout the capture:
- Initial burst after CCCD enable
- Continuous stream of status updates
- Device events (relay status, light status, etc.)
- Responses to GetDevices commands

**No gaps longer than a few seconds** - the gateway actively maintains communication.

---

## 3. Command Pattern

### 3.1 Write Characteristics

- **Handle**: `0x0037` (Data Write characteristic, UUID `00000033-0200-a58e-e411-afe28044e62c`)
- **Opcode**: `0x52` (Write Without Response) or `0x12` (Write Request)
- **Total Count**: 28 writes in the capture
- **Format**: COBS-encoded MyRvLink commands

### 3.2 Example Command Data

First write (frame 2815):
```
00:40:42:01:60:01:a8:00
```

This is a COBS-encoded GetDevices command:
- Starts with `0x00` (frame delimiter)
- Contains MyRvLink command data
- Ends with `0x00` (frame delimiter)

### 3.3 Command Timing

- **First command**: Sent ~1 second after CCCD enable
- **Subsequent commands**: Sent periodically (likely every 5-10 seconds based on decompiled code)
- **Purpose**: Request device information, not keepalive

---

## 4. No Keepalive/Heartbeat Required

### 4.1 Evidence

1. **186 notifications vs 28 writes**: Gateway sends 6.6x more data than app requests
2. **Proactive notifications**: First notification arrives 69ms after CCCD enable, before any command
3. **Continuous stream**: Notifications arrive continuously without gaps
4. **No periodic empty commands**: All writes contain actual MyRvLink commands (GetDevices, etc.)

### 4.2 How Connection Stays Alive

The gateway maintains the connection by:
1. **Sending periodic status updates** via notifications
2. **Sending device events** (relay changes, light changes, etc.)
3. **Responding to commands** with data notifications
4. **Active bidirectional communication** - not idle

The official app does **NOT** send periodic "ping" or empty keepalive commands.

---

## 5. Protocol Flow

### 5.1 Initialization Sequence

```
1. Connect BLE
2. Discover services
3. Enable notifications on 0x0039 (CCCD write to 0x003a)
   └─ Write 0x0001 to handle 0x003a
4. Gateway immediately starts sending notifications (69ms later)
   └─ GatewayInformation event arrives
   └─ Device status events arrive
5. App sends first GetDevices command (~1s after CCCD)
6. Gateway responds with GetDevices response
7. Continuous bidirectional communication
```

### 5.2 Ongoing Communication

```
Gateway → App: Continuous notifications
  - GatewayInformation (periodic)
  - Device status updates
  - Device events (relay changes, etc.)
  - Command responses

App → Gateway: Periodic commands
  - GetDevices (every 5-10s, based on decompiled code)
  - GetDevicesMetadata (when needed)
  - Device control commands (when user interacts)
```

---

## 6. Key Differences from Our Implementation

### 6.1 What We're Doing Wrong

1. **Waiting for GatewayInformation before sending commands**
   - **Reality**: Gateway sends GatewayInformation proactively 69ms after CCCD enable
   - **Fix**: Start stream reader immediately, process GatewayInformation when it arrives

2. **Sending periodic heartbeat/keepalive commands**
   - **Reality**: Gateway maintains connection with proactive notifications
   - **Fix**: Remove heartbeat, rely on gateway's notifications

3. **Expecting commands to trigger initial data**
   - **Reality**: Gateway sends data immediately after CCCD enable
   - **Fix**: Enable notifications, then wait for data (don't send commands immediately)

### 6.2 What We Should Do

1. **Enable notifications on Data Read characteristic (0x0034)**
2. **Start COBS stream reader immediately**
3. **Wait for GatewayInformation to arrive via notification** (should be ~69ms)
4. **Process all incoming notifications** (gateway sends continuously)
5. **Send GetDevices commands only when needed** (not for keepalive)
6. **No periodic heartbeat required** - gateway keeps connection alive

---

## 7. Data Format

### 7.1 Notifications (Gateway → App)

All notifications are COBS-encoded MyRvLink events:
- Frame delimiter: `0x00` at start and end
- Payload: MyRvLink event data (EventType + event-specific data)
- CRC8: Appended for integrity (if enabled)

Example decoded (after COBS):
```
[EventType][EventData...]
```

### 7.2 Commands (App → Gateway)

All commands are COBS-encoded MyRvLink commands:
- Frame delimiter: `0x00` at start and end
- Payload: MyRvLink command data (ClientCommandId + CommandType + command-specific data)
- CRC8: Appended for integrity (if enabled)

Example decoded (after COBS):
```
[ClientCommandId (2 bytes LE)][CommandType][CommandData...]
```

---

## 8. Connection Stability

### 8.1 Why Connection Stays Alive

The gateway's **proactive notification stream** keeps the BLE link active:
- Notifications arrive every few seconds
- BLE stack sees continuous activity
- No idle timeout occurs

### 8.2 Why Our Connection Disconnects

Our implementation:
1. Enables notifications ✓
2. Waits for GatewayInformation (but doesn't receive it) ✗
3. Sends periodic heartbeat commands (unnecessary) ✗
4. Connection idles and disconnects ✗

**Root cause**: We're not receiving notifications, so the connection appears idle to the BLE stack.

---

## 9. Recommendations

### 9.1 Immediate Fixes

1. **Remove heartbeat/keepalive mechanism**
   - Gateway maintains connection with proactive notifications
   - Heartbeat is unnecessary and may interfere

2. **Start stream reader immediately after CCCD enable**
   - Don't wait for GatewayInformation
   - Process notifications as they arrive

3. **Verify notification reception**
   - Add logging to confirm notifications are arriving
   - If not arriving, investigate CCCD write/notification subscription

4. **Send GetDevices only when needed**
   - Not for keepalive
   - Only to refresh device list or when GatewayInformation indicates new devices

### 9.2 Long-term Improvements

1. **Match official app's timing**
   - Send first GetDevices ~1 second after CCCD enable
   - Then send periodically (every 5-10 seconds) for device refresh

2. **Process all notification types**
   - GatewayInformation
   - Device status updates
   - Device events
   - Command responses

3. **Handle notification gaps gracefully**
   - If notifications stop, investigate (but don't send keepalive)
   - Gateway should maintain connection, so gaps indicate a problem

---

## 10. Conclusion

The HCI capture definitively shows:

1. **Gateway sends proactive notifications** - No keepalive needed
2. **First notification arrives 69ms after CCCD enable** - Before any command
3. **186 notifications vs 28 writes** - Gateway is the active party
4. **Connection maintained by gateway's notifications** - Not by client commands

**Our implementation should**:
- Enable notifications
- Start stream reader
- Process all incoming notifications
- Send commands only when needed (not for keepalive)
- Trust the gateway to maintain the connection

The disconnect issue is likely due to:
- Notifications not being received (CCCD issue?)
- Or notifications being received but not processed correctly
- Or BLE stack issue preventing notification delivery

**Next step**: Verify that notifications are actually arriving in our implementation, and if not, fix the CCCD subscription process.

