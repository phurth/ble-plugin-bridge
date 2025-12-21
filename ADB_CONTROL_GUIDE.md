# ADB Control Guide

Complete guide for controlling the BLE bridge service and plugins via ADB without any physical device interaction.

## Overview

The `ControlCommandReceiver` provides full remote control capabilities via ADB broadcast commands. All responses are logged to logcat with the `ControlCmd:` prefix for easy filtering.

## Prerequisites

```bash
# Ensure device is connected
adb devices

# Monitor responses (run in separate terminal)
adb logcat | grep "ControlCmd:"
```

**Important - Android Background Execution Restrictions**:  
Android 8.0+ blocks implicit broadcasts to apps not currently running. For ADB control to work with broadcast receivers, the service must be running first.

**Recommended Development Workflow**:

```bash
# Step 1: Start service via UI (one time)
# - Open app on device
# - Tap "Start Service" button
# - Press home button (app can go to background)

# Step 2: Now use ADB for all control (no device interaction needed)
adb shell am broadcast --receiver-foreground \
  -a com.blemqttbridge.CONTROL_COMMAND \
  --es command "service_status"

adb shell am broadcast --receiver-foreground \
  -a com.blemqttbridge.CONTROL_COMMAND \
  --es command "reload_plugin" \
  --es plugin_id "onecontrol"

# Monitor everything via ADB
adb logcat | grep -i "ble\|onecontrol\|ControlCmd"
```

**Why this restriction exists**: Android prevents background apps from receiving broadcasts to save battery and improve security. Once your service is running, the app is no longer considered "background" and broadcasts work normally.

**Alternative for completely hands-off operation**: Use **MQTT remote control** instead (see [REMOTE_CONTROL_API.md](REMOTE_CONTROL_API.md)). MQTT control works from any network location without any Android restrictions.

## Quick Start (Development Workflow)

The simplest approach for development is to launch the app UI once, then use ADB commands:

```bash
# 1. Launch app (only needed once per device boot or if app was force-stopped)
adb shell am start -n com.blemqttbridge/.ui.ServiceStatusActivity

# 2. Now you can control everything via ADB without touching the device
adb shell am broadcast --receiver-foreground \
  -a com.blemqttbridge.CONTROL_COMMAND \
  --es command "start_service"

# 3. Monitor logs
adb logcat | grep -i "ble\|onecontrol\|ControlCmd"

# 4. Stop when done
adb shell am broadcast --receiver-foreground \
  -a com.blemqttbridge.CONTROL_COMMAND \
  --es command "stop_service"
```

**Why this works**: Once the app UI has been launched, Android allows the app's broadcast receivers to process commands even after you navigate away or press home. The app doesn't need to stay visible.

## Service Control

### Start Service
```bash
# Start with default plugins (OneControl BLE + MQTT output)
adb shell am broadcast --receiver-foreground \
  -a com.blemqttbridge.CONTROL_COMMAND \
  --es command "start_service"

# Start with specific plugins
adb shell am broadcast --receiver-foreground \
  -a com.blemqttbridge.CONTROL_COMMAND \
  --es command "start_service" \
  --es ble_plugin "onecontrol" \
  --es output_plugin "mqtt"
```

**Response:**
```
ControlCmd: 🚀 Starting service with BLE plugin: onecontrol, output: mqtt
ControlCmd: ✅ SUCCESS: Service start command sent
```

### Stop Service
```bash
adb shell am broadcast --receiver-foreground -a com.blemqttbridge.CONTROL_COMMAND \
  --es command "stop_service"
```

**Response:**
```
ControlCmd: 🛑 Stopping service
ControlCmd: ✅ SUCCESS: Service stop command sent
```

### Restart Service
```bash
# Restart with default plugins
adb shell am broadcast --receiver-foreground -a com.blemqttbridge.CONTROL_COMMAND \
  --es command "restart_service"

# Restart with specific plugins
adb shell am broadcast --receiver-foreground -a com.blemqttbridge.CONTROL_COMMAND \
  --es command "restart_service" \
  --es ble_plugin "onecontrol" \
  --es output_plugin "mqtt"
```

**Response:**
```
ControlCmd: 🔄 Restarting service
ControlCmd: 🛑 Stopping service
ControlCmd: ✅ SUCCESS: Service stop command sent
(1 second delay)
ControlCmd: 🚀 Starting service with BLE plugin: onecontrol, output: mqtt
ControlCmd: ✅ SUCCESS: Service start command sent
```

## Plugin Management

### Load Plugin
```bash
adb shell am broadcast --receiver-foreground -a com.blemqttbridge.CONTROL_COMMAND \
  --es command "load_plugin" \
  --es plugin_id "onecontrol"
```

**Response:**
```
ControlCmd: 📥 Loading plugin: onecontrol
ControlCmd: ✅ SUCCESS: Plugin loaded - onecontrol v1.0.0
```

### Unload Plugin
```bash
adb shell am broadcast --receiver-foreground -a com.blemqttbridge.CONTROL_COMMAND \
  --es command "unload_plugin" \
  --es plugin_id "onecontrol"
```

**Response:**
```
ControlCmd: 📤 Unloading plugin: onecontrol
ControlCmd: ✅ SUCCESS: Plugin unloaded - onecontrol
```

### Reload Plugin
Unloads then reloads a plugin (useful for testing plugin changes):

```bash
adb shell am broadcast --receiver-foreground -a com.blemqttbridge.CONTROL_COMMAND \
  --es command "reload_plugin" \
  --es plugin_id "onecontrol"
```

**Response:**
```
ControlCmd: 🔄 Reloading plugin: onecontrol
ControlCmd:    Unloaded onecontrol
ControlCmd: ✅ SUCCESS: Plugin reloaded - onecontrol v1.0.0
```

## Status Queries

### List Plugins
```bash
adb shell am broadcast --receiver-foreground -a com.blemqttbridge.CONTROL_COMMAND \
  --es command "list_plugins"
```

**Response:**
```
ControlCmd: 📋 Listing plugins
ControlCmd:    Registered plugins: onecontrol
ControlCmd:    Loaded plugins: onecontrol
ControlCmd:    Loaded count: 1
ControlCmd: ✅ SUCCESS: Plugin list complete
```

### Service Status
```bash
adb shell am broadcast --receiver-foreground -a com.blemqttbridge.CONTROL_COMMAND \
  --es command "service_status"
```

**Response:**
```
ControlCmd: 📊 Service status
ControlCmd:    Service running: true
ControlCmd:    Loaded plugins: onecontrol
ControlCmd:    Plugin count: 1
ControlCmd: ✅ SUCCESS: Status query complete
```

## Development Workflows

### Quick Start Workflow
```bash
# 1. Start service
adb shell am broadcast --receiver-foreground -a com.blemqttbridge.CONTROL_COMMAND --es command "start_service"

# 2. Monitor all logs
adb logcat | grep -i 'ble\|bond\|onecontrol\|plugin\|ControlCmd'

# 3. Stop when done
adb shell am broadcast --receiver-foreground -a com.blemqttbridge.CONTROL_COMMAND --es command "stop_service"
```

### Plugin Development Workflow
```bash
# 1. Build and install new APK
cd /path/to/android_ble_plugin_bridge
./gradlew assembleDebug
adb install -r app/build/outputs/apk/debug/app-debug.apk

# 2. Start service (if not running)
adb shell am broadcast --receiver-foreground -a com.blemqttbridge.CONTROL_COMMAND --es command "start_service"

# 3. Reload plugin to test changes
adb shell am broadcast --receiver-foreground -a com.blemqttbridge.CONTROL_COMMAND \
  --es command "reload_plugin" \
  --es plugin_id "onecontrol"

# 4. Monitor behavior
adb logcat | grep "OneControl\|ControlCmd"
```

### Testing Authentication/Bonding
```bash
# 1. Start service
adb shell am broadcast --receiver-foreground -a com.blemqttbridge.CONTROL_COMMAND --es command "start_service"

# 2. Monitor bonding process
adb logcat | grep -i 'bond\|auth\|ControlCmd'

# 3. Restart to test again
adb shell am broadcast --receiver-foreground -a com.blemqttbridge.CONTROL_COMMAND --es command "restart_service"
```

### Automated Testing Script
Create `test-service.sh`:

```bash
#!/bin/bash

echo "=== Starting BLE Bridge Service Test ==="

# Start monitoring in background
adb logcat -c  # Clear logs
adb logcat | grep "ControlCmd:" &
LOGCAT_PID=$!

# Start service
echo "Starting service..."
adb shell am broadcast --receiver-foreground -a com.blemqttbridge.CONTROL_COMMAND --es command "start_service"
sleep 3

# Check status
echo "Checking status..."
adb shell am broadcast --receiver-foreground -a com.blemqttbridge.CONTROL_COMMAND --es command "service_status"
sleep 2

# List plugins
echo "Listing plugins..."
adb shell am broadcast --receiver-foreground -a com.blemqttbridge.CONTROL_COMMAND --es command "list_plugins"
sleep 2

# Stop service
echo "Stopping service..."
adb shell am broadcast --receiver-foreground -a com.blemqttbridge.CONTROL_COMMAND --es command "stop_service"
sleep 2

# Stop monitoring
kill $LOGCAT_PID

echo "=== Test Complete ==="
```

Usage:
```bash
chmod +x test-service.sh
./test-service.sh
```

## Error Handling

### Missing Parameters
```bash
# Missing command
adb shell am broadcast --receiver-foreground -a com.blemqttbridge.CONTROL_COMMAND
```
**Response:** `ControlCmd: ❌ ERROR: Missing 'command' parameter`

```bash
# Missing plugin_id for load command
adb shell am broadcast --receiver-foreground -a com.blemqttbridge.CONTROL_COMMAND --es command "load_plugin"
```
**Response:** `ControlCmd: ❌ ERROR: Missing 'plugin_id' parameter`

### Unknown Command
```bash
adb shell am broadcast --receiver-foreground -a com.blemqttbridge.CONTROL_COMMAND --es command "invalid_command"
```
**Response:** `ControlCmd: ❌ ERROR: Unknown command 'invalid_command'`

### Service Start Failure
If service fails to start, check logs for details:
```bash
adb logcat | grep -i 'error\|exception\|crash'
```

## Command Reference

| Command | Required Params | Optional Params | Description |
|---------|----------------|-----------------|-------------|
| `start_service` | - | `ble_plugin`, `output_plugin` | Start BLE service |
| `stop_service` | - | - | Stop BLE service |
| `restart_service` | - | `ble_plugin`, `output_plugin` | Restart service |
| `load_plugin` | `plugin_id` | - | Load BLE plugin |
| `unload_plugin` | `plugin_id` | - | Unload BLE plugin |
| `reload_plugin` | `plugin_id` | - | Reload plugin |
| `list_plugins` | - | - | List all plugins |
| `service_status` | - | - | Get service status |

## Parameter Reference

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `command` | String | *required* | Command to execute |
| `ble_plugin` | String | `onecontrol` | BLE plugin ID |
| `output_plugin` | String | `mqtt` | Output plugin ID |
| `plugin_id` | String | *required* | Plugin ID for plugin commands |

## Tips

1. **Monitor continuously**: Keep `adb logcat | grep "ControlCmd:"` running to see all responses
2. **Check service state**: Use `service_status` to verify service is running before testing
3. **Clear logs**: Use `adb logcat -c` before tests to avoid clutter
4. **Script repetitive tasks**: Use bash scripts for common workflows
5. **Combine filters**: Use `adb logcat | grep -i 'ControlCmd\|OneControl\|BLE'` for comprehensive monitoring

## Comparison: ADB vs MQTT Control

| Feature | ADB Control | MQTT Control |
|---------|-------------|--------------|
| Service start/stop | ✅ Yes | ✅ Yes |
| Plugin load/unload | ✅ Yes | ✅ Yes |
| Status queries | ✅ Yes | ✅ Yes |
| Remote access | ❌ USB/network ADB only | ✅ Network MQTT |
| Response format | Logcat (text) | MQTT (JSON) |
| Automation | Shell scripts | Any MQTT client |
| Use case | Development/debugging | Production/integration |

## Next Steps

- See [REMOTE_CONTROL_API.md](REMOTE_CONTROL_API.md) for MQTT control reference
- See [REMOTE_CONTROL_QUICKSTART.md](REMOTE_CONTROL_QUICKSTART.md) for MQTT examples
- See [TESTING_INSTRUCTIONS.md](TESTING_INSTRUCTIONS.md) for complete testing guide
