# OneControl BLE Bridge - Project Status

**Last Updated:** December 3, 2025  
**Backup:** `android_ble_bridge_backup_20251203_212016.tar.gz`

---

## 🎯 **Project Overview**

Android app that bridges OneControl RV gateway (BLE) to Home Assistant via MQTT. Provides real-time device status monitoring and control for RV systems (lights, switches, slides, tanks, etc.).

---

## ✅ **Completed Features**

### **1. BLE Connection & Authentication**
- ✅ BLE scanning and connection to OneControl gateway
- ✅ Challenge-response authentication using TEA encryption algorithm
- ✅ Big-endian byte order handling for challenge/KEY
- ✅ Automatic reconnection on disconnect
- ✅ Heartbeat/keepalive mechanism (alternates GetDevices/GetDevicesMetadata)

**Key Files:**
- `OneControlBleService.kt` - Main service handling BLE connection
- `AUTHENTICATION_ALGORITHM.md` - Complete authentication documentation

**Authentication Algorithm:**
- Source: `ids.portable_ble/BleDeviceUnlockManager.Encrypt()`
- Cypher: `0x2483FFD5` (RvLinkKeySeedCypher)
- Byte Order: **BIG-ENDIAN** (critical!)

### **2. MyRvLink Protocol Implementation**
- ✅ COBS encoding/decoding with CRC8
- ✅ MyRvLink event decoding (GatewayInformation, RvStatus, DimmableLight, Relay, HBridge, TankSensor)
- ✅ Device state tracking via `DeviceStateTracker`
- ✅ Stream reading with notification queue (mimics official app)

**Key Files:**
- `MyRvLinkEventFactory.kt` - Event type detection and decoding
- `MyRvLinkEventDecoders.kt` - Event-specific decoders
- `CobsDecoder.kt` - COBS frame decoding

**Supported Events:**
- `GatewayInformation` (0x01) - Gateway protocol version, device count, table IDs
- `RvStatus` (0x07) - Battery voltage, external temperature
- `DimmableLightStatus` (0x08) - Light state and brightness
- `RelayBasicLatchingStatusType2` (0x06) - Switch/relay state
- `RelayHBridgeMomentaryStatusType2` (0x0E) - Motor/slide/awning position
- `TankSensorStatusV2` (0x1B) - Tank level percentages
- `RealTimeClock` (0x20) - Gateway time sync

### **3. MQTT Integration**
- ✅ MQTT client connection and publishing
- ✅ Structured topic hierarchy: `onecontrol-ble/{system|device}/{entity}/{attribute}`
- ✅ Real-time state updates for all devices
- ✅ System status publishing (voltage, temperature)

**Topic Structure:**
```
onecontrol-ble/
├── system/
│   ├── battery_voltage          # Float (V)
│   ├── external_temperature     # Float (°C) - only if sensor available
│   └── state                    # JSON system state
└── device/
    └── {device_address}/        # Hex format: 0x0908
        ├── state                # ON/OFF or position
        ├── brightness           # 0-100 (dimmable lights)
        ├── level                 # 0-100% (tank sensors)
        └── type                  # Device type string
```

### **4. Home Assistant MQTT Discovery**
- ✅ Automatic entity creation via MQTT Discovery
- ✅ Single device grouping: "OneControl BLE {MAC}"
- ✅ Generic device names: "Dimmable Light 0908", "Switch 0803", etc.
- ✅ Proper device info (manufacturer, model, firmware version)
- ✅ Component types: `light`, `switch`, `cover`, `sensor`

**Discovery Components:**
- Lights (dimmable) - brightness control
- Switches (relays) - ON/OFF control
- Covers (motors) - position control (slides/awning)
- Sensors - battery voltage, tank levels

**Key Files:**
- `HomeAssistantMqttDiscovery.kt` - Discovery config generation

### **5. Device Detection & Enumeration**
- ✅ Automatic device discovery from status events
- ✅ Device address calculation: `(DeviceTableId << 8) | DeviceId`
- ✅ Device type detection from event types
- ✅ Tank sensor name mapping (0x67=Fresh, 0x68=Grey, 0x69=Black)

**Device Types Detected:**
- Dimmable Lights (2 detected: 0x0809, 0x080A)
- Latching Relays/Switches (6+ detected: 0x0808, 0x0408, 0x0708, 0x0608, 0x0508, etc.)
- HBridge Motors (2 detected: 0x0308, 0x0208 - likely slides/awning)
- Tank Sensors (3 detected: 0x080B, 0x080C, 0x080D)

### **6. Documentation**
- ✅ `AUTHENTICATION_ALGORITHM.md` - Complete auth algorithm documentation
- ✅ `MQTT_TOPIC_STRUCTURE.md` - MQTT topic reference
- ✅ `DEVICE_CATALOG.md` - Observed device types and addresses
- ✅ `PROJECT_STATUS.md` - This file

---

## 🔧 **Current Implementation Details**

### **BLE Services & Characteristics**
- **Data Service:** `00000030-0200-a58e-e411-afe28044e62c`
  - Read: `00000034-0200-a58e-e411-afe28044e62c`
  - Write: `00000033-0200-a58e-e411-afe28044e62c`
- **Auth Service:** `00000010-0200-a58e-e411-afe28044e62c`
  - Challenge: `00000012-0200-a58e-e411-afe28044e62c`
  - KEY: `00000013-0200-a58e-e411-afe28044e62c`
  - Status: `00000014-0200-a58e-e411-afe28044e62c`

### **Connection Flow**
1. Scan for gateway by MAC address
2. Connect via BLE GATT
3. Read challenge from `00000012`
4. Calculate KEY using TEA algorithm
5. Write KEY to `00000013`
6. Verify unlock status from `00000014`
7. Enable notifications on Data Service read characteristic
8. Start stream reading loop
9. Send GetDevices/GetDevicesMetadata as heartbeat

### **MQTT Configuration**
- Broker: `tcp://10.115.19.131:1883`
- Client ID: `onecontrol_ble_bridge`
- Username/Password: `mqtt`/`mqtt`
- Topic Prefix: `onecontrol-ble`
- Gateway MAC: `24:DC:C3:ED:1E:0A` (hardcoded, should be configurable)

---

## ⚠️ **Known Limitations**

### **1. Device Names**
- ❌ GetDevicesMetadata command **NOT supported** over BLE (gateway doesn't respond)
- ✅ Using generic names based on device type: "Dimmable Light 0908", "Switch 0803"
- ✅ Users can rename entities in Home Assistant UI
- ✅ `FunctionNameMapper.kt` ready for future use if metadata becomes available

### **2. External Temperature Sensor**
- ❌ Gateway doesn't have temperature sensor (`temperatureAvailable = false`)
- ✅ Discovery config only published if sensor is available
- ✅ Entity automatically removed if sensor unavailable

### **3. Device Control**
- ❌ **NOT YET IMPLEMENTED** - Next phase
- ✅ Status monitoring fully working
- ⏳ Control commands need to be implemented (ActionSwitch, ActionDimmable, etc.)

### **4. Configuration**
- ⚠️ Gateway MAC, MQTT broker, credentials are hardcoded
- ⚠️ Should be moved to SharedPreferences or config file

---

## 📊 **Current Device Inventory**

Based on live data from gateway:

| Device Address | Type | Name | Status |
|----------------|------|------|--------|
| 0x0809 | Dimmable Light | Dimmable Light 0809 | ✅ Detected |
| 0x080A | Dimmable Light | Dimmable Light 080A | ✅ Detected |
| 0x0808 | Latching Relay | Switch 0808 | ✅ Detected |
| 0x0408 | Latching Relay | Switch 0408 | ✅ Detected |
| 0x0708 | Latching Relay | Switch 0708 | ✅ Detected |
| 0x0608 | Latching Relay | Switch 0608 | ✅ Detected |
| 0x0508 | Latching Relay | Switch 0508 | ✅ Detected |
| 0x0308 | HBridge Motor | Motor 0308 | ✅ Detected |
| 0x0208 | HBridge Motor | Motor 0208 | ✅ Detected |
| 0x080B | Tank Sensor | Tank 080B | ✅ Detected |
| 0x080C | Tank Sensor | Tank 080C | ✅ Detected |
| 0x080D | Tank Sensor | Tank 080D | ✅ Detected |
| System | Battery Voltage | Battery Voltage | ✅ Working |
| System | External Temp | External Temperature | ❌ Not Available |

**Expected Devices (from user requirements):**
- ✅ System voltage
- ❌ Outside temp (gateway doesn't have sensor)
- ✅ Interior light (dimmable) - likely 0x0809 or 0x080A
- ✅ Awning light (dimmable) - likely 0x0809 or 0x080A
- ✅ Step light (switch) - one of the relay addresses
- ⚠️ Awning extend/retract (motor) - likely 0x0308 or 0x0208
- ⚠️ Slide extend/retract (motor) - likely 0x0308 or 0x0208
- ✅ Fresh water tank sensor - likely 0x080B, 0x080C, or 0x080D
- ✅ Grey water tank sensor - likely 0x080B, 0x080C, or 0x080D
- ✅ Black water tank sensor - likely 0x080B, 0x080C, or 0x080D
- ✅ Tank heater switch - one of the relay addresses
- ✅ Water heater switch (electric) - one of the relay addresses
- ✅ Water heater switch (gas) - one of the relay addresses
- ✅ Water pump switch - one of the relay addresses

---

## 🚀 **Next Steps: Device Control**

### **Implementation Plan**

1. **Research Control Commands**
   - Analyze decompiled code for `ActionSwitch`, `ActionDimmable`, `ActionHBridge`
   - Understand command structure and encoding
   - Identify required parameters (device address, action type, value)

2. **MQTT Command Subscriptions**
   - Subscribe to command topics: `onecontrol-ble/device/{address}/command`
   - Subscribe to brightness commands: `onecontrol-ble/device/{address}/brightness/set`
   - Subscribe to position commands: `onecontrol-ble/device/{address}/position/set`

3. **Command Encoding & Sending**
   - Encode MyRvLink control commands
   - Send via BLE write characteristic
   - Handle command responses (if any)

4. **State Synchronization**
   - Update local device state on command
   - Publish state updates to MQTT
   - Handle command failures/timeouts

### **Key Files to Create/Modify**
- `MyRvLinkCommandEncoder.kt` - Command encoding logic
- `OneControlBleService.kt` - Add MQTT command subscriptions and handlers
- `HomeAssistantMqttDiscovery.kt` - Ensure command topics are in discovery configs

---

## 📁 **Project Structure**

```
android_ble_bridge/
├── app/src/main/java/com/onecontrol/blebridge/
│   ├── OneControlBleService.kt          # Main BLE service
│   ├── MyRvLinkEventFactory.kt            # Event detection
│   ├── MyRvLinkEventDecoders.kt          # Event decoders
│   ├── DeviceStateTracker.kt             # Device state management
│   ├── HomeAssistantMqttDiscovery.kt     # HA discovery configs
│   ├── FunctionNameMapper.kt              # Device name mapping
│   ├── CobsDecoder.kt                    # COBS decoding
│   └── DeviceStatusParser.kt             # Legacy status parser
├── AUTHENTICATION_ALGORITHM.md           # Auth documentation
├── MQTT_TOPIC_STRUCTURE.md               # MQTT reference
├── DEVICE_CATALOG.md                     # Device inventory
└── PROJECT_STATUS.md                     # This file
```

---

## 🔍 **Testing Status**

### **✅ Verified Working**
- BLE connection and authentication
- Event reception and decoding
- MQTT publishing
- Home Assistant discovery
- Device state tracking
- System voltage monitoring
- Tank sensor readings

### **⏳ Pending Testing**
- Device control commands
- Command response handling
- Error recovery scenarios
- Long-term stability (24+ hours)

---

## 📝 **Notes**

- Gateway uses **MyRvLink Protocol v6**
- Device Table ID: `0x08`
- Total devices: 14 (from GatewayInformation event)
- Heartbeat interval: 5 seconds (alternates GetDevices/GetDevicesMetadata)
- MQTT QoS: 0 (at most once delivery)
- Discovery configs: Retained (survive HA restarts)

---

## 🐛 **Known Issues**

1. **"Unknown Device" in HA** - Fixed by removing `via_device` from discovery configs
2. **External Temperature showing "unknown"** - Fixed by only publishing discovery if sensor available
3. **Device names are generic** - Expected limitation, users can rename in HA

---

## 📚 **Reference Documentation**

- **Decompiled Code:** `OneControl.Direct.MyRvLink (2.0.0.0)/`
- **BLE Protocol:** `OneControl.Direct.MyRvLinkBle (2.0.0.0)/`
- **Authentication:** `ids.portable_ble (2.1.0.0)/BleDeviceUnlockManager.cs`
- **HCI Captures:** `android_ble_bridge/bt_snooper.json`, `2025-12-3-6-06.json`

---

**Status:** ✅ **READY FOR DEVICE CONTROL IMPLEMENTATION**

