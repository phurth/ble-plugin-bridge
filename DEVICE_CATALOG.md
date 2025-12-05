# OneControl Device Catalog - Live Data Analysis

## Gateway Information

From decoded MyRvLink `GatewayInformation` event (EventType 0x01):

```
Raw: 01 06 00 0E 08 D9 A6 5C F2 A3 AC 5E 2A

Decoded:
- Protocol Version: 0x06 (v6)
- Options: 0x00 (Production Mode)
- Device Count: 14 devices
- DeviceTableId: 0x08 ⚠️ IMPORTANT - use this in all device commands!
- DeviceTableCrc: 0xD9A65CF2
- DeviceMetadataTableCrc: 0xA3AC5E2A
```

## System Status

### RV Status (EventType 0x07)

Broadcasts system voltage and external temperature.

**Example Message:** `07 0D 60 7F FF 01`

```
Decoded:
- Battery Voltage: 13.375V (0x0D60 = 3424/256)
- External Temperature: Invalid/Not Available (0x7FFF)
- Features: 0x01 (Voltage Available, Temperature Not Available)
```

**Structure:**
- Byte 0: Event Type (0x07)
- Byte 1-2: Battery Voltage (unsigned 8.8 fixed point, big-endian)
  - Formula: value / 256.0
  - 0xFFFF = invalid/unavailable
- Byte 3-4: External Temperature °C (signed 8.8 fixed point, big-endian)
  - Formula: value / 256.0
  - 0x7FFF = invalid/unavailable
- Byte 5: Feature flags
  - Bit 0 (0x01): Voltage Available
  - Bit 1 (0x02): Temperature Available

## Devices Detected

From live data stream, the following device event types are being broadcast:

### 1. Dimmable Lights (EventType 0x08)

**Example Messages:**
- `08 08 09 00 00 00 00 00 00 00 00` - 11 bytes
- Additional instances observed with varying data

**Purpose:** Controls and reports status of dimmable lights (interior, exterior, porch, etc.)

**Structure (from decompiled code):**
- Byte 0: Event Type (0x08)
- Byte 1-2: Device Address (little-endian)
  - Example: `08 08` = Device 0x0808
- Byte 3: Status byte
- Byte 4: Brightness level (0-100%)
- Byte 5+: Additional state information

### 2. Relay Devices - H-Bridge Momentary Type 2 (EventType 0x0E)

**Example Messages:**
- `0E 08 02 C0 FF 00 00 00 00` - 9 bytes
- `0E 08 03 C0 FF 00 00 00 00` - 9 bytes

**Purpose:** Controls motors, slides, awnings, leveling jacks - devices that need momentary contact in forward/reverse directions.

**Pattern:** Multiple devices with addresses `08 02`, `08 03`, etc.

### 3. Unknown Event Types

Several other event types are being received but not yet cataloged:

- **Type 0x03**: `03 08 0E FF FC` - 5 bytes
- **Type 0x04**: `04 00 0A 00 D6 FF 08 0E 00 01` - 10 bytes
- **Type 0x06**: `06 08 07 80 FF 00 00 00 00` - 9 bytes
- **Type 0x1A**: `1A 08 0E 00 00` - 5 bytes
- **Type 0x20**: `20 30 C3 4A 46 13 78 00 01` - 9 bytes

These map to the following event types from the protocol:
- 0x03 = DeviceOnlineStatus
- 0x04 = DeviceLockStatus
- 0x06 = RelayBasicLatchingStatusType2
- 0x1A (26) = DeviceSessionStatus
- 0x20 (32) = RealTimeClock

## Message Frequency

From ~20 seconds of observation:

**High Frequency (< 1 second interval):**
- RvStatus (0x07) - Battery voltage broadcast
- Multiple dimmable light status (0x08)
- Relay status messages (0x0E)

**Medium Frequency (1-2 second interval):**
- GatewayInformation (0x01)
- DeviceOnlineStatus (0x03)
- DeviceLockStatus (0x04)

**Patterns:**
- GatewayInformation repeats periodically with same data
- RvStatus voltage reading is consistent: ~13.37V
- Device status messages repeat in a cycle, suggesting status polling/broadcast

## Device Address Patterns

From the data observed:

**Address Format:** `XX YY` (little-endian uint16)

**Observed Addresses:**
- `08 08` (0x0808) - Dimmable light device
- `08 07` (0x0807) - Relay device
- `08 06` (0x0806) - Relay device
- `08 04` (0x0804) - Device with lock status
- `08 02` (0x0802) - H-Bridge relay
- `08 03` (0x0803) - H-Bridge relay
- `08 09` (0x0809) - Dimmable light or other device
- `0D 60` (0x600D) - May be voltage reading, not address
- `30 C3` (0xC330) - RealTimeClock device address

**Pattern:** Most devices have addresses in the `0x08XX` range, suggesting they're on CAN bus segment 8.

## Next Steps for Complete Device Enumeration

### 1. Parse All Event Types

Implement decoders for:
- ✅ GatewayInformation (0x01) - DeviceTableId obtained
- ✅ RvStatus (0x07) - Battery voltage
- 🔲 DeviceOnlineStatus (0x03) - Which devices are active
- 🔲 DeviceLockStatus (0x04) - Device lock states
- 🔲 RelayBasicLatchingStatusType2 (0x06) - ON/OFF relays
- 🔲 DimmableLightStatus (0x08) - Light brightness levels
- 🔲 RelayHBridgeMomentaryStatusType2 (0x0E) - Motor/slide positions
- 🔲 DeviceSessionStatus (0x1A) - Device connection state
- 🔲 RealTimeClock (0x20) - System time

### 2. Send GetDevices Command

With DeviceTableId = 0x08, send proper `GetDevices` command to enumerate all 14 devices.

### 3. Map Devices to Functions

Using device metadata and status messages, create mapping:
- Device Address → Device Type → Human-Readable Name
- Example: 0x0808 → DimmableLight → "Interior Lights - Living Room"

### 4. MQTT Topic Structure

Publish each device to its own MQTT topic:

```
onecontrol/system/voltage = 13.375V
onecontrol/system/temperature = --
onecontrol/device/0808/type = dimmable_light
onecontrol/device/0808/brightness = 50
onecontrol/device/0808/state = ON
onecontrol/device/0802/type = relay_hbridge
onecontrol/device/0802/position = extended
```

## Voltage Monitoring

**Current Reading:** 13.375V

**Status:** ✅ Healthy (12V system nominal range: 12.0V - 14.4V)

**Use Cases:**
- Low voltage alerts
- Battery charging status
- Power consumption tracking
- Solar charging monitoring (if equipped)

## Command/Control Capability

To control devices, send MyRvLink commands:

**Required Information:**
- DeviceTableId: 0x08 (from GatewayInformation)
- Device Address: From event messages (e.g., 0x0808)
- Command Type: Based on device type (ActionSwitch, ActionDimmable, etc.)

**Examples:**
- Turn on light 0x0808: Send `ActionDimmable` command
- Extend slide 0x0802: Send `ActionMotor` command with direction
- Read tank levels: Parse `TankSensorStatus` events

## Data Export

All data is currently published to MQTT topic `onecontrol/ble/data_rx` as raw hex.

Once event parsing is implemented, structured JSON will be published to individual topics for Home Assistant autodiscovery.

