# Remote Control API Documentation

The BLE MQTT Bridge now supports remote control via MQTT commands for service management and plugin control.

## Overview

Remote control is enabled automatically when the service starts with MQTT output plugin configured. Commands are sent via MQTT to a control topic and status responses are published to a status topic.

**Control Topic:** `homeassistant/bridge/control` (or `{topic_prefix}/bridge/control`)  
**Status Topic:** `homeassistant/bridge/status` (or `{topic_prefix}/bridge/status`)

## Command Format

Commands are sent as JSON payloads to the control topic:

```json
{
  "command": "command_name",
  "param1": "value1",
  "param2": "value2"
}
```

## Available Commands

### Service Control

#### Start Service
Starts the BLE bridge service with specified plugins.

```json
{
  "command": "start_service",
  "ble_plugin": "onecontrol",
  "output_plugin": "mqtt"
}
```

**Parameters:**
- `ble_plugin` (optional): BLE plugin ID to use (default: "onecontrol")
- `output_plugin` (optional): Output plugin ID to use (default: "mqtt")

**Response:**
```
Service starting with plugin: onecontrol
```

---

#### Stop Service
Stops the BLE bridge service and disconnects all devices.

```json
{
  "command": "stop_service"
}
```

**Response:**
```
Service stopping
```

---

#### Restart Service
Restarts the service with new configuration.

```json
{
  "command": "restart_service",
  "ble_plugin": "onecontrol",
  "output_plugin": "mqtt"
}
```

**Response:**
```
Service stopping
Service starting with plugin: onecontrol
```

---

### Plugin Management

#### Load Plugin
Loads a BLE plugin with optional configuration.

```json
{
  "command": "load_plugin",
  "plugin_id": "onecontrol",
  "config": {
    "gateway_mac": "24:DC:C3:ED:1E:0A",
    "gateway_pin": "1234"
  }
}
```

**Parameters:**
- `plugin_id` (required): The plugin ID to load
- `config` (optional): Plugin configuration as key-value pairs. If omitted, loads from SharedPreferences.

**Response:**
```
Plugin loaded: onecontrol v1.0.0
```

---

#### Unload Plugin
Unloads a BLE plugin and frees its resources.

```json
{
  "command": "unload_plugin",
  "plugin_id": "onecontrol"
}
```

**Parameters:**
- `plugin_id` (required): The plugin ID to unload

**Response:**
```
Plugin unloaded: onecontrol
```

---

#### Reload Plugin
Unloads and reloads a plugin (useful for applying config changes).

```json
{
  "command": "reload_plugin",
  "plugin_id": "onecontrol"
}
```

**Parameters:**
- `plugin_id` (required): The plugin ID to reload

**Response:**
```
Plugin reloaded: onecontrol v1.0.0
```

---

### Status Queries

#### List Plugins
Lists all registered and currently loaded plugins.

```json
{
  "command": "list_plugins"
}
```

**Response (JSON):**
```json
{
  "registered_plugins": "onecontrol, mock_battery",
  "loaded_plugins": "onecontrol",
  "loaded_count": 1
}
```

---

#### Service Status
Returns current service and connection status.

```json
{
  "command": "service_status"
}
```

**Response (JSON):**
```json
{
  "service_running": true,
  "loaded_plugins": "onecontrol",
  "mqtt_connected": true
}
```

---

## Usage Examples

### Using mosquitto_pub CLI

Start the service:
```bash
mosquitto_pub -h mqtt.example.com -t "homeassistant/bridge/control" -m '{"command":"start_service","ble_plugin":"onecontrol"}'
```

Load a plugin with custom config:
```bash
mosquitto_pub -h mqtt.example.com -t "homeassistant/bridge/control" -m '{
  "command": "load_plugin",
  "plugin_id": "onecontrol",
  "config": {
    "gateway_mac": "24:DC:C3:ED:1E:0A"
  }
}'
```

Check service status:
```bash
mosquitto_pub -h mqtt.example.com -t "homeassistant/bridge/control" -m '{"command":"service_status"}'

# Listen to status responses
mosquitto_sub -h mqtt.example.com -t "homeassistant/bridge/status" -v
```

Stop the service:
```bash
mosquitto_pub -h mqtt.example.com -t "homeassistant/bridge/control" -m '{"command":"stop_service"}'
```

### Using Python with paho-mqtt

```python
import json
import paho.mqtt.client as mqtt

# Connect to MQTT broker
client = mqtt.Client()
client.connect("mqtt.example.com", 1883, 60)

# Send command
command = {
    "command": "load_plugin",
    "plugin_id": "onecontrol",
    "config": {
        "gateway_mac": "24:DC:C3:ED:1E:0A"
    }
}
client.publish("homeassistant/bridge/control", json.dumps(command))

# Subscribe to status
def on_message(client, userdata, msg):
    print(f"Status: {msg.payload.decode()}")

client.on_message = on_message
client.subscribe("homeassistant/bridge/status")
client.loop_forever()
```

### Using Node-RED

Create a flow with an inject node connected to an MQTT out node:

**Inject Node:**
- Topic: `homeassistant/bridge/control`
- Payload (JSON):
  ```json
  {
    "command": "start_service",
    "ble_plugin": "onecontrol"
  }
  ```

**MQTT Out Node:**
- Server: Your MQTT broker
- Topic: `homeassistant/bridge/control`

Add an MQTT In node to receive status updates:
- Topic: `homeassistant/bridge/status`

---

## Error Handling

If a command fails, an error message is published to the status topic:

```
Error: plugin_id required
Error: Failed to load plugin 'unknown_plugin'
Error: Unknown command 'invalid_command'
```

## Security Considerations

1. **MQTT Authentication:** Ensure your MQTT broker requires authentication
2. **ACL Rules:** Restrict which clients can publish to the control topic
3. **TLS/SSL:** Use encrypted connections for production deployments
4. **Topic Namespacing:** Use unique topic prefixes to avoid conflicts

## Implementation Notes

- Remote control is initialized automatically when the service starts with MQTT configured
- Commands are processed asynchronously in the service's coroutine scope
- All commands log to Android logcat for debugging (tag: `RemoteControlManager`)
- Plugin configs can be stored in SharedPreferences and referenced by plugin_id

## Troubleshooting

**No response to commands:**
- Check MQTT broker connection
- Verify topic prefix matches your configuration
- Check logcat for `RemoteControlManager` errors

**Plugin load fails:**
- Ensure plugin_id is registered
- Check plugin configuration is valid
- Review logcat for plugin initialization errors

**Service doesn't start:**
- Verify Bluetooth permissions are granted
- Check that Bluetooth is enabled on the device
- Ensure foreground service permissions are available
