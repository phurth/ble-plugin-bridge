# OneControl BLE - MQTT Topic Structure

## Base Topic

**`onecontrol-ble`** - All topics are published under this base

---

## Device Naming

Devices are identified by **friendly names** (e.g., `living_room_ceiling_light`) automatically derived from FUNCTION_NAME metadata:

1. **GetDevicesMetadata** command retrieves `FunctionName` (numeric ID) and `FunctionInstance` for each device
2. **FunctionNameMapper** maps numeric IDs to friendly names (e.g., 41 = "Living Room Ceiling Light")
3. Names are converted to MQTT-safe topics: lowercase with underscores

**Example Mappings:**

| Function ID | Instance | Friendly Name              | MQTT Topic                   |
|-------------|----------|----------------------------|------------------------------|
| 41          | 0        | Living Room Ceiling Light  | `living_room_ceiling_light`  |
| 74          | 0        | Front Slide                | `front_slide`                |
| 49          | 0        | Awning Light               | `awning_light`               |
| 7           | 2        | Light                      | `light_2`                    |

**Fallback:** If metadata is unavailable, hex address format (`0xTTDD`) is used temporarily.

---

## System Topics

### Battery Voltage
```
onecontrol-ble/system/battery_voltage
```
**Type:** Float  
**Example:** `13.50`  
**Update Frequency:** ~1 second  
**Description:** RV battery voltage in volts

### External Temperature
```
onecontrol-ble/system/external_temperature
```
**Type:** Float  
**Example:** `25.5`  
**Update Frequency:** ~1 second  
**Description:** External temperature in °C (if sensor equipped)

### System State (JSON)
```
onecontrol-ble/system/state
```
**Type:** JSON Object  
**Example:**
```json
{
  "gateway_version": 6,
  "device_count": 14,
  "device_table_id": "0x8",
  "battery_voltage": 13.5,
  "external_temperature": 25.5,
  "datetime": "RTC(...)",
  "device_addresses": ["0x0908", "0x0a08", "0x0208", ...]
}
```
**Update Frequency:** ~1 second  
**Description:** Complete system state snapshot

---

## Device Topics

### Pattern
```
onecontrol-ble/device/<DEVICE_NAME>/<ATTRIBUTE>
```

Where:
- `<DEVICE_NAME>` = Friendly device name (e.g., `living_room_ceiling_light`) or hex address fallback (`0x0908`)
- `<ATTRIBUTE>` = Specific attribute (state, brightness, position, type)

---

### Dimmable Lights

**State:**
```
onecontrol-ble/device/living_room_ceiling_light/state
```
**Values:** `ON` | `OFF`

**Brightness:**
```
onecontrol-ble/device/living_room_ceiling_light/brightness
```
**Values:** `0` - `100` (percentage)

**Type:**
```
onecontrol-ble/device/living_room_ceiling_light/type
```
**Value:** `dimmable_light`

**Full State (JSON):**
```
onecontrol-ble/device/living_room_ceiling_light/state
```
**Example:**
```json
{
  "type": "dimmable_light",
  "address": "0x0908",
  "state": "ON",
  "brightness": 75
}
```

---

### Latching Relays (ON/OFF Switches)

**State:**
```
onecontrol-ble/device/water_pump/state
```
**Values:** `ON` | `OFF`

**Type:**
```
onecontrol-ble/device/water_pump/type
```
**Value:** `relay` or `latching_relay`

**Full State (JSON):**
```
onecontrol-ble/device/water_pump/state
```
**Example:**
```json
{
  "type": "latching_relay",
  "address": "0x0408",
  "state": "ON"
}
```

---

### H-Bridge Relays (Motors, Slides, Awnings)

**Position:**
```
onecontrol-ble/device/front_slide/position
```
**Values:** `0` - `255` (position/status code)

**Type:**
```
onecontrol-ble/device/front_slide/type
```
**Value:** `hbridge_relay`

**Full State (JSON):**
```
onecontrol-ble/device/front_slide/state
```
**Example:**
```json
{
  "type": "hbridge_relay",
  "address": "0x0208",
  "position": 128,
  "status": 192
}
```

---

### RGB Lights

**Type:**
```
onecontrol-ble/device/0x0108/type
```
**Value:** `rgb_light`

*(RGB state parsing not yet implemented)*

---

## Status Topics

### Connection Status
```
onecontrol-ble/status
```
**Values:** `connected` | `ready` | `disconnected`

---

## Raw Data Topics (Debug)

### CAN Bus Raw Data
```
onecontrol-ble/can_rx
```
**Format:** Hex bytes (space-separated)  
**Example:** `00 42 01 06 0B 0E 08 D9 A6 5C`

### Data Service Raw Data
```
onecontrol-ble/data_rx
```
**Format:** Hex bytes (space-separated)  
**Example:** `00 C5 06 08 07 80 FF 40 01 2F 00`

---

## Command Topics (Future)

### Switch Command
```
onecontrol-ble/command/switch/<DEVICE_ID>
```
**Payload:** `ON` | `OFF`

### Dimmable Light Brightness
```
onecontrol-ble/command/dimmable/<DEVICE_ID>/brightness
```
**Payload:** `0` - `100`

---

## Home Assistant Configuration Examples

### Battery Voltage Sensor

```yaml
mqtt:
  sensor:
    - name: "RV Battery Voltage"
      state_topic: "onecontrol-ble/system/battery_voltage"
      unit_of_measurement: "V"
      device_class: voltage
      state_class: measurement
      icon: mdi:car-battery
```

### Dimmable Light

```yaml
mqtt:
  light:
    - name: "Living Room Ceiling Light"
      unique_id: "rv_living_room_ceiling_light"
      state_topic: "onecontrol-ble/device/living_room_ceiling_light/state"
      command_topic: "onecontrol-ble/device/living_room_ceiling_light/command"
      brightness_state_topic: "onecontrol-ble/device/living_room_ceiling_light/brightness"
      brightness_command_topic: "onecontrol-ble/device/living_room_ceiling_light/brightness/set"
      brightness_scale: 100
      payload_on: "ON"
      payload_off: "OFF"
      optimistic: false
```

### Relay Switch

```yaml
mqtt:
  switch:
    - name: "Water Pump"
      unique_id: "rv_water_pump"
      state_topic: "onecontrol-ble/device/water_pump/state"
      command_topic: "onecontrol-ble/device/water_pump/command"
      payload_on: "ON"
      payload_off: "OFF"
      icon: mdi:water-pump
```

### Device Count Sensor

```yaml
mqtt:
  sensor:
    - name: "RV Connected Devices"
      state_topic: "onecontrol-ble/system/state"
      value_template: "{{ value_json.device_count }}"
      icon: mdi:devices
```

---

## Wildcard Subscriptions

### All OneControl Topics
```
onecontrol-ble/#
```

### System Topics Only
```
onecontrol-ble/system/#
```

### All Devices
```
onecontrol-ble/device/#
```

### Specific Device
```
onecontrol-ble/device/living_room_ceiling_light/#
```

### All Device States
```
onecontrol-ble/device/+/state
```

### All Device Types
```
onecontrol-ble/device/+/type
```

---

## Device Address Format

**Format:** `0xABCD` (hexadecimal with 0x prefix)

**Composition:** 
- High byte: Device Table ID (usually `0x08`)
- Low byte: Device ID within table

**Examples:**
- `0x0908` = Table 8, Device 9
- `0x0a08` = Table 8, Device 10
- `0x0208` = Table 8, Device 2

**Note:** Device addresses are formed as little-endian:
- Raw bytes: `09 08`
- Interpreted as: `0x0809`
- Displayed as: `0x0908` (for consistency with system conventions)

---

## Topic Retention

**Currently:** No topics are retained (retained flag = false)

**Reason:** Real-time state that changes frequently. Subscribers get current state on connect from live broadcasts.

**Future:** May add retained flag to `onecontrol-ble/system/state` and device states for last-known-good values.

---

## Update Frequency

| Topic Type | Frequency |
|------------|-----------|
| Battery Voltage | ~1 second |
| System State | ~1 second |
| Device States | ~1-2 seconds per device (cycling) |
| Raw Data | As received from gateway |

---

## Quality of Service (QoS)

**All topics:** QoS 0 (at most once delivery)

**Reason:** Real-time data that's continuously updated. Missing one message is acceptable as the next update arrives within 1-2 seconds.

