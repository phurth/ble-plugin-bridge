# Remote Control Quick Start Guide

This guide shows practical examples of remotely controlling the BLE MQTT Bridge service.

## Prerequisites

1. MQTT broker running and accessible (Default: `tcp://10.115.19.131:1883`)
2. BLE MQTT Bridge app installed with latest stability fixes
3. Service started via UI (one-time: tap "Start Service" button)
4. MQTT client tool (mosquitto_pub, MQTT Explorer, or similar)

**Note:** Latest build includes MQTT stability fixes - clean sessions, longer keep-alive, automatic reconnection.

## Basic Usage

### 1. Start the Service Remotely

```bash
# Basic start with default plugin
mosquitto_pub -h YOUR_BROKER -u YOUR_USER -P YOUR_PASS \
  -t "homeassistant/bridge/control" \
  -m '{"command":"start_service"}'

# Start with specific plugin
mosquitto_pub -h YOUR_BROKER -u YOUR_USER -P YOUR_PASS \
  -t "homeassistant/bridge/control" \
  -m '{"command":"start_service","ble_plugin":"onecontrol"}'
```

### 2. Monitor Status

In a separate terminal, monitor status responses:

```bash
mosquitto_sub -h YOUR_BROKER -u YOUR_USER -P YOUR_PASS \
  -t "homeassistant/bridge/status" -v
```

### 3. Query Service Status

```bash
mosquitto_pub -h YOUR_BROKER -u YOUR_USER -P YOUR_PASS \
  -t "homeassistant/bridge/control" \
  -m '{"command":"service_status"}'
```

Expected response:
```
homeassistant/bridge/status {"service_running":true,"loaded_plugins":"onecontrol","mqtt_connected":true}
```

### 4. Stop the Service

```bash
mosquitto_pub -h YOUR_BROKER -u YOUR_USER -P YOUR_PASS \
  -t "homeassistant/bridge/control" \
  -m '{"command":"stop_service"}'
```

## Advanced Examples

### Load Plugin with Custom Configuration

```bash
mosquitto_pub -h YOUR_BROKER -u YOUR_USER -P YOUR_PASS \
  -t "homeassistant/bridge/control" \
  -m '{
    "command": "load_plugin",
    "plugin_id": "onecontrol",
    "config": {
      "gateway_mac": "24:DC:C3:ED:1E:0A",
      "gateway_pin": "1234"
    }
  }'
```

### Reload Plugin to Apply Configuration Changes

```bash
# First, unload the plugin
mosquitto_pub -h YOUR_BROKER -u YOUR_USER -P YOUR_PASS \
  -t "homeassistant/bridge/control" \
  -m '{"command":"unload_plugin","plugin_id":"onecontrol"}'

# Then load it again (picks up new config from SharedPreferences)
mosquitto_pub -h YOUR_BROKER -u YOUR_USER -P YOUR_PASS \
  -t "homeassistant/bridge/control" \
  -m '{"command":"load_plugin","plugin_id":"onecontrol"}'

# Or use reload command (does both in one step)
mosquitto_pub -h YOUR_BROKER -u YOUR_USER -P YOUR_PASS \
  -t "homeassistant/bridge/control" \
  -m '{"command":"reload_plugin","plugin_id":"onecontrol"}'
```

### List Available and Loaded Plugins

```bash
mosquitto_pub -h YOUR_BROKER -u YOUR_USER -P YOUR_PASS \
  -t "homeassistant/bridge/control" \
  -m '{"command":"list_plugins"}'
```

## Using with Home Assistant

### Automation Example

Create an automation to start the bridge service when Home Assistant starts:

```yaml
automation:
  - alias: "Start BLE Bridge on Startup"
    trigger:
      - platform: homeassistant
        event: start
    action:
      - service: mqtt.publish
        data:
          topic: "homeassistant/bridge/control"
          payload: '{"command":"start_service","ble_plugin":"onecontrol"}'
```

### Manual Control via Developer Tools

Go to Developer Tools → Services and use:

**Service:** `mqtt.publish`

**Service Data:**
```yaml
topic: homeassistant/bridge/control
payload: '{"command":"service_status"}'
```

### Status Sensor

Monitor service status with a sensor:

```yaml
mqtt:
  sensor:
    - name: "BLE Bridge Status"
      state_topic: "homeassistant/bridge/status"
      value_template: >
        {% if 'service_running' in value %}
          {{ value | from_json | default({}) | tojson }}
        {% else %}
          {{ value }}
        {% endif %}
```

## Using with Node-RED

### Flow Example

1. **Start Service Flow:**
   - `Inject` node (timestamp) → 
   - `Function` node (set command) →
   - `MQTT Out` node

   Function node code:
   ```javascript
   msg.payload = {
       command: "start_service",
       ble_plugin: "onecontrol"
   };
   return msg;
   ```

2. **Status Monitor Flow:**
   - `MQTT In` node (subscribe to status) →
   - `Debug` node

## Shell Script for Remote Management

Create a helper script `ble-bridge-control.sh`:

```bash
#!/bin/bash

BROKER="YOUR_BROKER"
USER="YOUR_USER"
PASS="YOUR_PASS"
CONTROL_TOPIC="homeassistant/bridge/control"

case "$1" in
  start)
    mosquitto_pub -h "$BROKER" -u "$USER" -P "$PASS" \
      -t "$CONTROL_TOPIC" \
      -m '{"command":"start_service"}'
    ;;
  stop)
    mosquitto_pub -h "$BROKER" -u "$USER" -P "$PASS" \
      -t "$CONTROL_TOPIC" \
      -m '{"command":"stop_service"}'
    ;;
  restart)
    mosquitto_pub -h "$BROKER" -u "$USER" -P "$PASS" \
      -t "$CONTROL_TOPIC" \
      -m '{"command":"restart_service"}'
    ;;
  status)
    mosquitto_pub -h "$BROKER" -u "$USER" -P "$PASS" \
      -t "$CONTROL_TOPIC" \
      -m '{"command":"service_status"}'
    ;;
  reload)
    if [ -z "$2" ]; then
      echo "Usage: $0 reload <plugin_id>"
      exit 1
    fi
    mosquitto_pub -h "$BROKER" -u "$USER" -P "$PASS" \
      -t "$CONTROL_TOPIC" \
      -m "{\"command\":\"reload_plugin\",\"plugin_id\":\"$2\"}"
    ;;
  *)
    echo "Usage: $0 {start|stop|restart|status|reload <plugin_id>}"
    exit 1
    ;;
esac
```

Make it executable:
```bash
chmod +x ble-bridge-control.sh
```

Usage:
```bash
./ble-bridge-control.sh start
./ble-bridge-control.sh status
./ble-bridge-control.sh reload onecontrol
./ble-bridge-control.sh stop
```

## Python Script for Remote Control

```python
#!/usr/bin/env python3
import json
import argparse
import paho.mqtt.client as mqtt
import time

class BLEBridgeControl:
    def __init__(self, broker, port=1883, username=None, password=None):
        self.client = mqtt.Client()
        if username and password:
            self.client.username_pw_set(username, password)
        self.client.connect(broker, port, 60)
        self.control_topic = "homeassistant/bridge/control"
        self.status_topic = "homeassistant/bridge/status"
        
    def send_command(self, command_data):
        """Send command and print response"""
        def on_message(client, userdata, msg):
            print(f"Response: {msg.payload.decode()}")
            client.disconnect()
        
        self.client.on_message = on_message
        self.client.subscribe(self.status_topic)
        self.client.loop_start()
        
        self.client.publish(self.control_topic, json.dumps(command_data))
        
        # Wait for response (timeout after 5 seconds)
        time.sleep(5)
        self.client.loop_stop()
    
    def start_service(self, ble_plugin="onecontrol"):
        self.send_command({
            "command": "start_service",
            "ble_plugin": ble_plugin
        })
    
    def stop_service(self):
        self.send_command({"command": "stop_service"})
    
    def restart_service(self):
        self.send_command({"command": "restart_service"})
    
    def service_status(self):
        self.send_command({"command": "service_status"})
    
    def load_plugin(self, plugin_id, config=None):
        cmd = {"command": "load_plugin", "plugin_id": plugin_id}
        if config:
            cmd["config"] = config
        self.send_command(cmd)
    
    def unload_plugin(self, plugin_id):
        self.send_command({
            "command": "unload_plugin",
            "plugin_id": plugin_id
        })
    
    def reload_plugin(self, plugin_id):
        self.send_command({
            "command": "reload_plugin",
            "plugin_id": plugin_id
        })
    
    def list_plugins(self):
        self.send_command({"command": "list_plugins"})

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="BLE Bridge Remote Control")
    parser.add_argument("--broker", required=True, help="MQTT broker address")
    parser.add_argument("--username", help="MQTT username")
    parser.add_argument("--password", help="MQTT password")
    
    subparsers = parser.add_subparsers(dest="command", help="Command to execute")
    
    subparsers.add_parser("start", help="Start service")
    subparsers.add_parser("stop", help="Stop service")
    subparsers.add_parser("restart", help="Restart service")
    subparsers.add_parser("status", help="Get status")
    
    reload_parser = subparsers.add_parser("reload", help="Reload plugin")
    reload_parser.add_argument("plugin_id", help="Plugin ID to reload")
    
    args = parser.parse_args()
    
    bridge = BLEBridgeControl(args.broker, username=args.username, password=args.password)
    
    if args.command == "start":
        bridge.start_service()
    elif args.command == "stop":
        bridge.stop_service()
    elif args.command == "restart":
        bridge.restart_service()
    elif args.command == "status":
        bridge.service_status()
    elif args.command == "reload":
        bridge.reload_plugin(args.plugin_id)
    else:
        parser.print_help()
```

Usage:
```bash
python3 bridge_control.py --broker mqtt.example.com --username user --password pass start
python3 bridge_control.py --broker mqtt.example.com --username user --password pass status
python3 bridge_control.py --broker mqtt.example.com --username user --password pass reload onecontrol
```

## Troubleshooting

### Commands Not Working

1. **Check MQTT Connection:**
   ```bash
   mosquitto_sub -h YOUR_BROKER -u YOUR_USER -P YOUR_PASS -t "#" -v
   ```

2. **Verify Topic Prefix:**
   Check app configuration for correct topic prefix (default: "homeassistant")

3. **Check Logs:**
   ```bash
   adb logcat | grep -i "RemoteControlManager\|BaseBleService"
   ```

### Service Won't Start

1. Ensure Bluetooth permissions are granted
2. Verify Bluetooth is enabled
3. Check that service isn't already running

### Plugin Load Fails

1. Verify plugin_id is correct (use `list_plugins` command)
2. Check plugin configuration is valid
3. Review logcat for initialization errors

## Next Steps

- See [REMOTE_CONTROL_API.md](REMOTE_CONTROL_API.md) for complete API reference
- Integrate with your automation system
- Create custom scripts for your workflow
