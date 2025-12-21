# MQTT Setup Guide - Completely Remote Operation

This guide shows how to configure and operate the BLE bridge completely remotely with **ZERO device interaction** after initial setup.

## Quick Start (3 Steps to Hands-Free Operation)

```bash
# Step 1: Configure MQTT broker credentials (if not using defaults)
# Default is already set: tcp://10.115.19.131:1883 with mqtt/mqtt
./configure-mqtt.sh tcp://YOUR_BROKER:1883 mqtt yourpassword

# Step 2: Enable auto-start on boot (optional but recommended)
adb shell am broadcast --receiver-foreground \
  -a com.blemqttbridge.SET_AUTO_START \
  --ez enabled true

# Step 3: Start service via UI (touch device once)
# - Open app, tap "Start Service", press Home
# Note: After initial start, everything else is remote via MQTT
```

**Latest Update (Dec 20, 2025):** MQTT stability fixes applied
- Clean sessions enabled (fixes persistence issues)
- Keep-alive increased to 120 seconds
- Automatic resubscription after reconnect
- Better connection loss handling

**That's it!** From now on, control everything via MQTT from anywhere:

```bash
# All control via MQTT
mosquitto_pub -h YOUR_BROKER -u mqtt -P yourpassword \
  -t "homeassistant/bridge/control" \
  -m '{"command":"reload_plugin","plugin_id":"onecontrol"}'
```

## MQTT Broker Configuration

### Option 1: Using the Configuration Script (Recommended)

```bash
# Basic configuration
./configure-mqtt.sh tcp://192.168.1.100:1883

# With custom credentials
./configure-mqtt.sh tcp://homeassistant.local:1883 myuser mypass

# Public broker (testing only - no auth)
./configure-mqtt.sh tcp://broker.hivemq.com:1883 "" ""
```

**What it does:**
- Validates broker URL format
- Stores credentials securely in app preferences
- Shows verification command
- Works even when service isn't running

### Option 2: Manual ADB Command

```bash
adb shell am broadcast --receiver-foreground \
  -a com.blemqttbridge.CONFIGURE_MQTT \
  --es broker_url "tcp://192.168.1.100:1883" \
  --es username "mqtt" \
  --es password "mypassword"

# Monitor confirmation
adb logcat | grep "MqttConfig:"
```

**Response:**
```
MqttConfig: ✅ SUCCESS: MQTT configuration saved
MqttConfig:    Broker: tcp://192.168.1.100:1883
MqttConfig:    Username: mqtt
MqttConfig:    Password: ***
```

### Broker URL Format

| Protocol | Format | Use Case |
|----------|--------|----------|
| TCP | `tcp://host:1883` | Standard unencrypted MQTT |
| SSL/TLS | `ssl://host:8883` | Encrypted MQTT (recommended) |

**Examples:**
- `tcp://192.168.1.100:1883` - Local broker
- `tcp://homeassistant.local:1883` - Home Assistant default
- `tcp://broker.hivemq.com:1883` - Public test broker
- `ssl://mqtt.myserver.com:8883` - Secure connection

## Auto-Start on Boot

Enable auto-start to make operation completely hands-free:

```bash
# Enable auto-start (default: enabled)
adb shell am broadcast --receiver-foreground \
  -a com.blemqttbridge.SET_AUTO_START \
  --ez enabled true

# Disable auto-start
adb shell am broadcast --receiver-foreground \
  -a com.blemqttbridge.SET_AUTO_START \
  --ez enabled false
```

**With auto-start enabled:**
1. Device boots (or reboots)
2. Service starts automatically
3. Connects to MQTT with stored credentials
4. Remote control immediately available

**Without auto-start:**
- Must start service manually via ADB or UI after each boot
- Useful for testing/development

## Complete Hands-Free Setup Workflow

This is the **recommended setup** for production deployment:

```bash
#!/bin/bash
# One-time setup for completely hands-free operation

# 1. Install app (only needed once)
adb install -r app/build/outputs/apk/debug/app-debug.apk

# 2. Configure MQTT broker
./configure-mqtt.sh tcp://192.168.1.100:1883 mqtt mypassword

# 3. Enable auto-start
adb shell am broadcast --receiver-foreground \
  -a com.blemqttbridge.SET_AUTO_START \
  --ez enabled true

# 4. Start service for first time
adb shell am broadcast --receiver-foreground \
  -a com.blemqttbridge.CONTROL_COMMAND \
  --es command "start_service"

# 5. Verify MQTT connection
sleep 5
mosquitto_sub -h 192.168.1.100 -u mqtt -P mypassword \
  -t "homeassistant/bridge/status" -C 1

echo "✅ Setup complete! Device is now fully hands-free."
echo "   Reboot test: adb reboot"
echo "   After reboot, service will start automatically."
```

## Using Your MQTT Broker

### Home Assistant Built-in Broker

If you're using Home Assistant's built-in MQTT broker:

```bash
# Default Home Assistant MQTT settings
./configure-mqtt.sh tcp://homeassistant.local:1883 homeassistant YOUR_HA_TOKEN

# Or use IP if mDNS doesn't work
./configure-mqtt.sh tcp://192.168.1.50:1883 homeassistant YOUR_HA_TOKEN
```

Find your Home Assistant MQTT credentials:
1. Open Home Assistant
2. Settings → Devices & Services → MQTT
3. Configure → Re-configure
4. Note username and password

### Mosquitto on Raspberry Pi

```bash
# Standard Mosquitto configuration
./configure-mqtt.sh tcp://raspberrypi.local:1883 mqtt mqtt

# Or with custom user
./configure-mqtt.sh tcp://192.168.1.10:1883 myuser mypass
```

### Public Test Brokers (Development Only)

```bash
# HiveMQ public broker (no auth)
./configure-mqtt.sh tcp://broker.hivemq.com:1883 "" ""

# Eclipse public broker
./configure-mqtt.sh tcp://mqtt.eclipseprojects.io:1883 "" ""
```

**Warning:** Public brokers are for testing only. Anyone can see your data!

## Remote Control via MQTT

Once configured and running, control from **any device** on your network:

### Start/Stop Service

```bash
# Start service
mosquitto_pub -h YOUR_BROKER -u mqtt -P pass \
  -t "homeassistant/bridge/control" \
  -m '{"command":"start_service"}'

# Stop service
mosquitto_pub -h YOUR_BROKER -u mqtt -P pass \
  -t "homeassistant/bridge/control" \
  -m '{"command":"stop_service"}'

# Restart service
mosquitto_pub -h YOUR_BROKER -u mqtt -P pass \
  -t "homeassistant/bridge/control" \
  -m '{"command":"restart_service"}'
```

### Plugin Management

```bash
# Reload plugin (for testing changes)
mosquitto_pub -h YOUR_BROKER -u mqtt -P pass \
  -t "homeassistant/bridge/control" \
  -m '{"command":"reload_plugin","plugin_id":"onecontrol"}'

# Load plugin
mosquitto_pub -h YOUR_BROKER -u mqtt -P pass \
  -t "homeassistant/bridge/control" \
  -m '{"command":"load_plugin","plugin_id":"onecontrol"}'

# Unload plugin
mosquitto_pub -h YOUR_BROKER -u mqtt -P pass \
  -t "homeassistant/bridge/control" \
  -m '{"command":"unload_plugin","plugin_id":"onecontrol"}'
```

### Status Queries

```bash
# Get service status
mosquitto_pub -h YOUR_BROKER -u mqtt -P pass \
  -t "homeassistant/bridge/control" \
  -m '{"command":"service_status"}'

# List plugins
mosquitto_pub -h YOUR_BROKER -u mqtt -P pass \
  -t "homeassistant/bridge/control" \
  -m '{"command":"list_plugins"}'

# Monitor responses
mosquitto_sub -h YOUR_BROKER -u mqtt -P pass \
  -t "homeassistant/bridge/status"
```

## Troubleshooting

### Verify Configuration

```bash
# Check if configuration was saved
adb logcat -d | grep "MqttConfig:" | tail -5

# Should see:
# MqttConfig: ✅ SUCCESS: MQTT configuration saved
# MqttConfig:    Broker: tcp://YOUR_BROKER:1883
```

### Test MQTT Connection

```bash
# Start service and monitor MQTT connection
adb shell am broadcast --receiver-foreground \
  -a com.blemqttbridge.CONTROL_COMMAND \
  --es command "start_service"

# Watch for MQTT connection logs
adb logcat | grep -i "mqtt"

# Should see:
# MqttOutputPlugin: Connecting to broker: tcp://YOUR_BROKER:1883
# MqttOutputPlugin: Connected to MQTT broker
```

### Common Issues

**Problem:** "Background execution not allowed"  
**Solution:** Start service first via UI or wait for boot (if auto-start enabled)

**Problem:** MQTT connection fails  
**Solution:** 
- Verify broker is reachable: `ping YOUR_BROKER`
- Check credentials are correct
- Ensure broker allows connections from Android device IP
- Check firewall rules (port 1883 TCP)

**Problem:** Service doesn't start on boot  
**Solution:**
- Verify auto-start is enabled (see logs: `adb logcat | grep "BootReceiver:"`)
- Check Android battery optimization isn't killing the app
- Ensure RECEIVE_BOOT_COMPLETED permission granted

### Reset to Default Configuration

```bash
# Use default broker settings (from AppConfig.kt)
adb shell am broadcast --receiver-foreground \
  -a com.blemqttbridge.CONFIGURE_MQTT \
  --es broker_url "tcp://10.115.19.131:1883" \
  --es username "mqtt" \
  --es password "mqtt"
```

## Security Considerations

### Production Deployment

For production use:

1. **Use SSL/TLS**: Configure broker with `ssl://` instead of `tcp://`
2. **Strong passwords**: Avoid default "mqtt/mqtt" credentials
3. **Network isolation**: Keep MQTT broker on private network
4. **Firewall rules**: Limit connections to known devices
5. **Regular updates**: Keep broker and app updated

### Credentials Storage

- Credentials stored in Android SharedPreferences
- File location: `/data/data/com.blemqttbridge/shared_prefs/ble_bridge_config.xml`
- Only accessible to the app (Android sandbox)
- Not accessible via ADB on production devices (secure storage)

## Advanced Scenarios

### Multiple Devices

Configure each device with same broker, different client IDs:

```bash
# Device 1 (auto-generated unique client ID)
./configure-mqtt.sh tcp://BROKER:1883 mqtt pass

# Client ID is auto-generated with timestamp for uniqueness
```

### Home Assistant Automation

```yaml
# automation.yaml
- alias: "Reload BLE Plugin on Code Deploy"
  trigger:
    - platform: webhook
      webhook_id: ble_code_updated
  action:
    - service: mqtt.publish
      data:
        topic: "homeassistant/bridge/control"
        payload: '{"command":"reload_plugin","plugin_id":"onecontrol"}'
```

### Remote Monitoring

```bash
# Continuous status monitoring
while true; do
  mosquitto_pub -h BROKER -u mqtt -P pass \
    -t "homeassistant/bridge/control" \
    -m '{"command":"service_status"}'
  sleep 60
done

# In another terminal
mosquitto_sub -h BROKER -u mqtt -P pass -t "homeassistant/bridge/#"
```

## Summary

| Feature | ADB | MQTT |
|---------|-----|------|
| Configuration | ✅ Yes | ✅ Yes (after configured) |
| Start service | ✅ Yes | ✅ Yes |
| Stop service | ✅ Yes | ✅ Yes |
| Plugin reload | ✅ Yes | ✅ Yes |
| Remote access | ⚠️ USB/Network ADB | ✅ Anywhere |
| Boot auto-start | ✅ Yes | ✅ Yes |
| Zero device touch | ⚠️ After first start | ✅ Completely |
| Best for | Development | Production |

**Recommendation:** 
- Development: Use ADB for initial setup, MQTT for iterations
- Production: Configure once via ADB, then MQTT-only operation

## Next Steps

1. Configure your MQTT broker credentials
2. Enable auto-start
3. Start service
4. Test remote control via MQTT
5. Enjoy completely hands-free operation! 🎉

See also:
- [REMOTE_CONTROL_API.md](REMOTE_CONTROL_API.md) - Complete MQTT API reference
- [ADB_CONTROL_GUIDE.md](ADB_CONTROL_GUIDE.md) - ADB command reference
- [REMOTE_CONTROL_QUICKSTART.md](REMOTE_CONTROL_QUICKSTART.md) - Quick examples
