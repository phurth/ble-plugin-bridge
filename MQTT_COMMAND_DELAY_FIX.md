# MQTT Command Processing Delay Fix (30-second issue)

## Problem
MQTT commands from Home Assistant were taking ~30 seconds to execute on OneControl (and likely other BLE plugins).

## Root Cause
**Handler Queue Bottleneck**: Commands were being processed on the main thread via `handler.post()`, which queued them behind other operations like:
- Heartbeat tasks (every 5 seconds)
- Watchdog checks (every 60 seconds)  
- Stream reading and frame processing
- BLE characteristic operations
- Diagnostic state publishing

When the main handler queue filled up with these periodic tasks, new MQTT commands had to wait for all queued tasks to complete before being processed, resulting in ~30 second delays.

### Technical Flow (Before)
```
MQTT Message (HA) 
  → messageArrived() on MQTT thread
    → callback invoked on MQTT thread
      → handler.post { handleCommand() }  ← QUEUES to main thread
        → Command waits in queue behind other tasks
        → 30+ second delay before execution
```

## Solution
**Direct Command Processing**: Execute command handling directly on the MQTT callback thread instead of posting to the main handler. This bypasses the queue entirely since:
1. BLE write operations can be called from any thread (Android BLE API is thread-safe)
2. Command handling is non-blocking (returns immediately after queuing BLE write)
3. No dependency on main thread state

### Technical Flow (After)
```
MQTT Message (HA)
  → messageArrived() on MQTT thread
    → callback invoked on MQTT thread
      → handleCommand() executed immediately  ← No queue
        → BLE write queued (thread-safe)
        → Returns immediately
        → Command response time: < 100ms
```

## Files Modified
1. **OneControlDevicePlugin.kt** (line 2950-2952)
   - Removed `handler.post()` wrapper
   - Commands now execute directly on MQTT callback thread
   
2. **EasyTouchDevicePlugin.kt** (line 1898-1900)  
   - Removed `mainHandler.post()` wrapper
   - Commands now execute directly on MQTT callback thread
   
3. **GoPowerDevicePlugin.kt** (line 713-715)
   - Removed `mainHandler.post()` wrapper
   - Commands now execute directly on MQTT callback thread

## Testing
To verify the fix:
1. Send MQTT command from Home Assistant: `mosquitto_pub -h localhost -t onecontrol/[MAC]/command/switch/8/1 -m ON`
2. Monitor app logs: `adb logcat | grep "Command processed"`
3. Measure response time (should be < 1 second instead of 30 seconds)

Expected log output:
```
📤 Command processed: onecontrol/XX:XX:XX:XX:XX:XX/command/switch/8/1 = ON, success=true
```

## Architecture Notes
The original design used `handler.post()` to ensure thread safety, but:
- Android's BluetoothGatt API is already thread-safe
- The BLE write operation is non-blocking (returns immediately after queueing)
- No UI operations occur in command handling
- Therefore, main thread was unnecessary

Future improvements could include:
- Using Kotlin coroutines with Dispatchers.IO for command processing
- Separate MQTT dispatcher thread to avoid blocking any handler
- Command queue with priority levels (urgent commands processed first)
