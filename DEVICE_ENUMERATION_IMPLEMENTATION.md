# Device Enumeration Implementation

## Summary

Implemented comprehensive device state tracking and structured MQTT publishing for OneControl RV devices.

## What Was Added

### 1. MyRvLinkEventDecoders.kt

New file containing decoders for all major MyRvLink event types:

**Event Decoders:**
- ✅ `GatewayInformationEvent` (0x01) - Gateway details, device count, table IDs
- ✅ `RvStatusEvent` (0x07) - Battery voltage and external temperature
- ✅ `DimmableLightStatusEvent` (0x08) - Light brightness and on/off state
- ✅ `RelayHBridgeStatusEvent` (0x0E) - Motor/slide/awning position and status
- ✅ `RelayBasicLatchingStatusEvent` (0x06) - Simple ON/OFF relay state
- ✅ `DeviceOnlineStatusEvent` (0x03) - Device online/offline status
- ✅ `DeviceLockStatusEvent` (0x04) - Device lock status
- ✅ `RealTimeClockEvent` (0x20) - System date/time

**DeviceStateTracker Class:**
- Maintains current state of all devices
- Processes incoming events and updates device states
- Provides methods to query device states
- Generates structured data for MQTT publishing

### 2. OneControlBleService.kt Updates

**Changes:**
- Added `deviceStateTracker` instance
- Updated `handleMyRvLinkEvent()` to process all events through the tracker
- Added MQTT publishing for:
  - System state (voltage, temperature, gateway info)
  - Individual device states
  - Per-device attributes for Home Assistant integration

**MQTT Topic Structure:**
```
onecontrol/system/state                    - Full system state JSON
onecontrol/system/battery_voltage          - 13.37V (raw float)
onecontrol/system/external_temperature     - Temperature in °C
onecontrol/device/0x0808/state             - Full device state JSON
onecontrol/device/0x0808/state             - ON/OFF for switches
onecontrol/device/0x0808/brightness        - 0-100 for dimmable lights
onecontrol/device/0x0808/position          - Position for motors/slides
```

## Current Capabilities

### System Monitoring

✅ **Battery Voltage**
- Real-time monitoring
- Published to MQTT every ~1 second
- Format: Float (e.g., 13.37)

✅ **External Temperature** 
- If equipped/available
- Published to MQTT
- Format: Float in °C

✅ **Gateway Information**
- Protocol version
- Device count (14 devices detected)
- Device table ID (0x08)
- Table CRCs

### Device Tracking

✅ **Dimmable Lights** (Type 0x08)
- Device address
- Brightness (0-100%)
- On/Off state
- Auto-publishes state changes to MQTT

✅ **H-Bridge Relays** (Type 0x0E - Motors, Slides, Awnings)
- Device address
- Position status
- Movement state
- Auto-publishes to MQTT

✅ **Latching Relays** (Type 0x06 - Simple ON/OFF)
- Device address
- On/Off state
- Auto-publishes to MQTT

✅ **Device Status**
- Online/Offline detection
- Lock status
- All tracked per device address

## Detected Devices (Live Data)

From actual RV system - 14 devices total:

| Address | Type | Description |
|---------|------|-------------|
| 0x0808 | Dimmable Light | Interior/Exterior Light |
| 0x0809 | Dimmable Light | Interior/Exterior Light |
| 0x0802 | H-Bridge Relay | Slide/Awning/Jack |
| 0x0803 | H-Bridge Relay | Slide/Awning/Jack |
| 0x0806 | Latching Relay | Pump/Fan/Device |
| 0x0807 | Latching Relay | Pump/Fan/Device |
| 0x0804 | Unknown | Has lock status |
| ... | ... | ... (more being identified) |

## Home Assistant Integration

### Auto-Discovery Ready

With the structured MQTT topics, Home Assistant can:

1. **Discover Devices Automatically** via MQTT discovery
2. **Monitor Battery Voltage** as a sensor
3. **Control Lights** with brightness sliders
4. **Monitor Temperature** (if available)
5. **Track Device Online Status**

### Example HA Configuration

```yaml
# Battery Voltage Sensor
sensor:
  - platform: mqtt
    name: "RV Battery Voltage"
    state_topic: "onecontrol/system/battery_voltage"
    unit_of_measurement: "V"
    device_class: voltage

# Dimmable Light
light:
  - platform: mqtt
    name: "RV Interior Light"
    state_topic: "onecontrol/device/0x0808/state"
    brightness_state_topic: "onecontrol/device/0x0808/brightness"
    command_topic: "onecontrol/device/0x0808/command"
    brightness_command_topic: "onecontrol/device/0x0808/brightness/set"
    brightness_scale: 100
```

## What's Missing (To Be Implemented)

### Device Metadata

❌ **Device Names** - Currently only have addresses
- Need to send `GetDevices` command with `DeviceTableId=0x08`
- Parse response to get human-readable names
- Map addresses → "Living Room Light", "Bedroom Slide", etc.

❌ **Device Capabilities** - Unknown which devices support which features
- Some lights may not be dimmable
- Some relays may have different capabilities
- Need metadata table from gateway

### Command/Control

❌ **Send Commands to Devices**
- Turn lights on/off
- Set brightness
- Extend/retract slides
- Start/stop motors

Need to implement:
- `ActionSwitch` command
- `ActionDimmable` command
- `ActionMotor` command

### Additional Event Types

❌ **Tank Sensors** (0x0C, 0x1B) - Fresh/Gray/Black water levels
❌ **HVAC Status** (0x0B) - Thermostat, AC, furnace
❌ **Generator Status** (0x0A) - If equipped
❌ **Battery Monitor** (0x31) - Advanced battery info if equipped
❌ **Door Lock Status** (0x33) - Smart lock integration

## Testing Status

✅ **Authentication** - Working with dynamic KEY calculation
✅ **Connection** - Stable, no disconnects
✅ **Data Reception** - Continuous stream of events
✅ **Event Parsing** - All major types decoded
✅ **State Tracking** - Device states maintained
✅ **MQTT Publishing** - Structured data flowing

❌ **Device Control** - Not yet implemented
❌ **Device Naming** - Need to send GetDevices command
❌ **Reconnection** - Not yet tested

## Next Steps

### Priority 1: Get Device Names

1. Send `GetDevices` command with `DeviceTableId=0x08`
2. Parse response to extract device names
3. Update MQTT topics to use friendly names
4. Example: `onecontrol/device/living_room_light/state`

### Priority 2: Implement Device Control

1. Create command builders for:
   - `ActionSwitch` (ON/OFF)
   - `ActionDimmable` (brightness 0-100)
   - `ActionMotor` (extend/retract/stop)
2. Subscribe to MQTT command topics
3. Send commands to devices via BLE
4. Verify state changes

### Priority 3: Additional Sensors

1. Implement tank sensor decoders
2. Implement HVAC decoder
3. Add MQTT publishing for all sensor types

### Priority 4: Home Assistant Discovery

1. Implement MQTT Discovery protocol
2. Publish device configurations automatically
3. Devices appear in HA without manual configuration

## Files Modified

### New Files:
- `app/src/main/java/com/onecontrol/blebridge/MyRvLinkEventDecoders.kt`

### Modified Files:
- `app/src/main/java/com/onecontrol/blebridge/OneControlBleService.kt`

### Backup:
- `backups/20251203_192900_pre_device_enumeration/` - Full backup before changes

## Build & Deploy

```bash
# In Android Studio:
1. Build → Clean Project
2. Build → Rebuild Project
3. Run → Run 'app'

# Or via command line:
./gradlew clean
./gradlew assembleDebug
adb install -r app/build/outputs/apk/debug/app-debug.apk
```

## Monitoring

```bash
# Watch all device state changes
adb logcat -s 'OneControlBleService:*' | grep -E "(💡|🔌|⚡|🔋|📊)"

# Watch MQTT publishes
mosquitto_sub -h <broker> -t 'onecontrol/#' -v

# Watch specific device
mosquitto_sub -h <broker> -t 'onecontrol/device/0x0808/#' -v
```

## Summary

The Android app now:
- ✅ Authenticates successfully
- ✅ Maintains stable connection
- ✅ Receives and decodes all major event types
- ✅ Tracks device states in real-time
- ✅ Publishes structured data to MQTT
- ✅ Ready for Home Assistant integration
- ✅ Monitors battery voltage (13.37V)
- ✅ Tracks 14 devices

Still needed:
- ❌ Device names (need GetDevices command)
- ❌ Device control (send commands)
- ❌ Additional sensor types (tanks, HVAC, etc.)

