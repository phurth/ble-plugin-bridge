# Remote Control Testing Guide

This guide walks through testing the newly implemented remote control functionality.

## Prerequisites

1. ✅ App installed with remote control support
2. ✅ MQTT broker running and accessible
3. ✅ App configured with MQTT settings
4. ✅ MQTT client tools installed (mosquitto_pub/mosquitto_sub)

## Test Setup

### 1. Configure MQTT in the App

Launch the app and configure MQTT settings:
- Broker URL: tcp://YOUR_BROKER:1883
- Username/Password (if required)
- Topic Prefix: homeassistant (or your preference)

### 2. Start the Service via UI

First start to establish MQTT connection:
1. Open the app
2. Tap "Start Service" button
3. Wait for service to start and connect to MQTT

### 3. Monitor Status Topic

In a terminal, start monitoring status responses:

```bash
mosquitto_sub -h YOUR_BROKER -u YOUR_USER -P YOUR_PASS \
  -t "homeassistant/bridge/status" -v
```

Keep this running to see responses from all commands.

## Test Cases

### Test 1: Service Status Query

**Command:**
```bash
mosquitto_pub -h YOUR_BROKER -u YOUR_USER -P YOUR_PASS \
  -t "homeassistant/bridge/control" \
  -m '{"command":"service_status"}'
```

**Expected Response:**
```json
{
  "service_running": true,
  "loaded_plugins": "onecontrol",
  "mqtt_connected": true
}
```

**Verify:**
- service_running should be `true`
- mqtt_connected should be `true`
- loaded_plugins may be empty if no devices connected yet

---

### Test 2: List Available Plugins

**Command:**
```bash
mosquitto_pub -h YOUR_BROKER -u YOUR_USER -P YOUR_PASS \
  -t "homeassistant/bridge/control" \
  -m '{"command":"list_plugins"}'
```

**Expected Response:**
```json
{
  "registered_plugins": "onecontrol, mock_battery",
  "loaded_plugins": "onecontrol",
  "loaded_count": 1
}
```

**Verify:**
- registered_plugins shows all available plugin IDs
- loaded_plugins shows currently loaded plugins
- loaded_count matches number of loaded plugins

---

### Test 3: Load Plugin

**Command:**
```bash
mosquitto_pub -h YOUR_BROKER -u YOUR_USER -P YOUR_PASS \
  -t "homeassistant/bridge/control" \
  -m '{
    "command": "load_plugin",
    "plugin_id": "onecontrol",
    "config": {
      "gateway_mac": "24:DC:C3:ED:1E:0A"
    }
  }'
```

**Expected Response:**
```
Plugin loaded: onecontrol v1.0.0
```

**Verify:**
- Plugin loads successfully
- Version number is displayed
- No error messages

**Check Logs:**
```bash
adb logcat | grep -i "RemoteControlManager\|PluginRegistry"
```

Look for:
```
I RemoteControlManager: Loading plugin: onecontrol
I PluginRegistry: Loading BLE plugin: onecontrol
I PluginRegistry: BLE plugin loaded successfully: onecontrol v1.0.0
```

---

### Test 4: Unload Plugin

**Command:**
```bash
mosquitto_pub -h YOUR_BROKER -u YOUR_USER -P YOUR_PASS \
  -t "homeassistant/bridge/control" \
  -m '{"command":"unload_plugin","plugin_id":"onecontrol"}'
```

**Expected Response:**
```
Plugin unloaded: onecontrol
```

**Verify:**
- Plugin unloads cleanly
- Check with list_plugins that it's no longer loaded

---

### Test 5: Reload Plugin

**Command:**
```bash
mosquitto_pub -h YOUR_BROKER -u YOUR_USER -P YOUR_PASS \
  -t "homeassistant/bridge/control" \
  -m '{"command":"reload_plugin","plugin_id":"onecontrol"}'
```

**Expected Response:**
```
Plugin reloaded: onecontrol v1.0.0
```

**Verify:**
- Plugin unloads then reloads
- Configuration is preserved (or reloaded from SharedPreferences)

---

### Test 6: Stop Service

**Command:**
```bash
mosquitto_pub -h YOUR_BROKER -u YOUR_USER -P YOUR_PASS \
  -t "homeassistant/bridge/control" \
  -m '{"command":"stop_service"}'
```

**Expected Response:**
```
Service stopping
```

**Verify:**
- Service stops
- App UI shows "Service stopped"
- No more status responses (service is offline)

---

### Test 7: Start Service Remotely

**Command:**
```bash
mosquitto_pub -h YOUR_BROKER -u YOUR_USER -P YOUR_PASS \
  -t "homeassistant/bridge/control" \
  -m '{"command":"start_service","ble_plugin":"onecontrol"}'
```

**Expected Response:**
```
Service starting with plugin: onecontrol
```

**Verify:**
- Service starts
- App UI shows "Service running"
- Status topic starts receiving responses

---

### Test 8: Restart Service

**Command:**
```bash
mosquitto_pub -h YOUR_BROKER -u YOUR_USER -P YOUR_PASS \
  -t "homeassistant/bridge/control" \
  -m '{"command":"restart_service","ble_plugin":"onecontrol"}'
```

**Expected Response:**
```
Service stopping
Service starting with plugin: onecontrol
```

**Verify:**
- Service stops then restarts
- Brief gap in status responses
- Service comes back online

---

### Test 9: Invalid Command

**Command:**
```bash
mosquitto_pub -h YOUR_BROKER -u YOUR_USER -P YOUR_PASS \
  -t "homeassistant/bridge/control" \
  -m '{"command":"invalid_command"}'
```

**Expected Response:**
```
Error: Unknown command 'invalid_command'
```

**Verify:**
- Error is reported
- Service continues running normally

---

### Test 10: Missing Parameter

**Command:**
```bash
mosquitto_pub -h YOUR_BROKER -u YOUR_USER -P YOUR_PASS \
  -t "homeassistant/bridge/control" \
  -m '{"command":"load_plugin"}'
```

**Expected Response:**
```
Error: plugin_id required
```

**Verify:**
- Error is reported
- Service continues running normally

---

## Integration Tests

### Home Assistant Test

Create a test automation:

```yaml
automation:
  - alias: "Test BLE Bridge Control"
    trigger:
      - platform: state
        entity_id: input_boolean.test_ble_bridge
        to: 'on'
    action:
      - service: mqtt.publish
        data:
          topic: "homeassistant/bridge/control"
          payload: '{"command":"service_status"}'
```

Toggle the input_boolean and verify command is sent.

### Python Test Script

```python
import paho.mqtt.client as mqtt
import json
import time

def test_remote_control(broker, username, password):
    client = mqtt.Client()
    client.username_pw_set(username, password)
    client.connect(broker, 1883, 60)
    
    # Subscribe to status
    responses = []
    def on_message(client, userdata, msg):
        responses.append(msg.payload.decode())
        print(f"Response: {msg.payload.decode()}")
    
    client.on_message = on_message
    client.subscribe("homeassistant/bridge/status")
    client.loop_start()
    
    # Test 1: Service status
    print("\nTest 1: Service Status")
    client.publish("homeassistant/bridge/control", 
                   json.dumps({"command": "service_status"}))
    time.sleep(2)
    
    # Test 2: List plugins
    print("\nTest 2: List Plugins")
    client.publish("homeassistant/bridge/control",
                   json.dumps({"command": "list_plugins"}))
    time.sleep(2)
    
    # Test 3: Load plugin
    print("\nTest 3: Load Plugin")
    client.publish("homeassistant/bridge/control",
                   json.dumps({
                       "command": "load_plugin",
                       "plugin_id": "onecontrol"
                   }))
    time.sleep(2)
    
    client.loop_stop()
    print(f"\n{len(responses)} responses received")
    return len(responses) >= 3

# Run test
success = test_remote_control("mqtt.example.com", "user", "pass")
print("✅ All tests passed!" if success else "❌ Some tests failed")
```

## Troubleshooting

### No Responses Received

**Check MQTT Connection:**
```bash
# Verify broker is reachable
mosquitto_sub -h YOUR_BROKER -t "#" -v

# Check app logs
adb logcat | grep -i "mqtt\|RemoteControl"
```

**Common Causes:**
- MQTT broker not running
- Incorrect broker URL/credentials
- Topic prefix mismatch
- Service not started

### Commands Not Executing

**Check Logs:**
```bash
adb logcat | grep -i "RemoteControlManager"
```

**Look for:**
- Command received confirmation
- Parsing errors
- Execution errors

**Common Causes:**
- Invalid JSON format
- Missing required parameters
- Plugin not registered
- Permission issues

### Service Won't Start Remotely

**Verify:**
1. Bluetooth is enabled on device
2. All permissions are granted
3. Service is not already running

**Check Logs:**
```bash
adb logcat | grep -i "BaseBleService"
```

## Success Criteria

✅ All commands execute successfully  
✅ Status responses are received for each command  
✅ Service starts/stops remotely  
✅ Plugins load/unload dynamically  
✅ Error handling works (invalid commands, missing params)  
✅ Logs show proper command processing  
✅ No crashes or exceptions

## Next Steps

After successful testing:
1. Document any issues found
2. Test in production environment
3. Create automation workflows
4. Set up monitoring/alerting
5. Integrate with your home automation system

## Support

If you encounter issues:
1. Check [REMOTE_CONTROL_API.md](REMOTE_CONTROL_API.md) for command reference
2. Review [REMOTE_CONTROL_QUICKSTART.md](REMOTE_CONTROL_QUICKSTART.md) for examples
3. Check logcat for detailed error messages
4. Verify MQTT broker is working with other clients
