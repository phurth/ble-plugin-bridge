# BLE-MQTT Bridge (Android)

Android foreground service that bridges BLE (Bluetooth Low Energy) devices to MQTT, enabling Home Assistant integration via a plugin-based architecture.

## 🚀 Quick Start

### Prerequisites

- **Android Device:** Android 8.0+ with BLE support
- **MQTT Broker:** Accessible from the Android device (e.g., Mosquitto on Home Assistant)
- **Home Assistant:** MQTT integration enabled

### Installation

1. Download the latest APK from [GitHub Releases](https://github.com/phurth/ble-plugin-bridge/releases)
2. Enable "Install unknown apps" for your browser/file manager
3. Install the APK and grant all requested permissions

### Initial Configuration

1. Open the app - all toggles will be OFF by default
2. Configure **MQTT broker settings** (expand "Broker Settings"):
   - Host, Port, Username, Password
   - Topic Prefix: `homeassistant` (recommended for auto-discovery)
3. Configure your **device plugin settings** (see plugin sections below)
4. Enable toggles in order: **MQTT → Plugin → BLE Service**

> **Note:** Settings are locked while their toggle is ON. Turn OFF to edit.

### Home Assistant Integration

Once connected, devices are automatically discovered via MQTT auto-discovery:

- **Switches** - Binary relays
- **Lights** - Dimmable with brightness control
- **Covers** - Slides and awnings
- **Sensors** - Temperature, voltage, tank levels
- **Binary Sensors** - Diagnostic indicators

---

## 📦 Plugins

### OneControl Gateway (LCI/Lippert)

The OneControl plugin connects to LCI/Lippert OneControl BLE gateways found in RVs.

#### ⚠️ IMPORTANT: Pair Before Running

**The OneControl gateway must be paired to your Android device via Bluetooth Settings BEFORE enabling the plugin.** The app will not initiate pairing automatically.

**Pairing Steps:**

1. **Unpair from other devices:** If the gateway is currently paired with a phone or tablet running the official OneControl app, unpair it first (Settings → Bluetooth → Forget Device on that device)

2. **Pair via Android Bluetooth Settings:**
   - Go to Android **Settings → Bluetooth**
   - Ensure Bluetooth is ON
   - The gateway should appear as "OneControl" or similar
   - Tap to pair - enter your gateway PIN if prompted
   - Wait for "Paired" status to appear

3. **Configure the plugin:**
   - In the BLE-MQTT Bridge app, expand "Gateway Settings"
   - Enter the **Gateway MAC Address** (found in Bluetooth settings after pairing)
   - Enter your **Gateway PIN** (same PIN used in the official OneControl app)

4. **Enable the plugin:**
   - Turn ON the **OneControl** toggle
   - Turn ON the **BLE Service** toggle
   - Status indicators should turn green: BLE → Data → Paired

#### Supported Devices

| Device Type | HA Entity | Features |
|-------------|-----------|----------|
| Switches | `switch` | ON/OFF control |
| Dimmable Lights | `light` | Brightness 0-255 |
| Covers/Slides/Awnings | `cover` | Open/Close/Stop |
| Tank Sensors | `sensor` | Fill level % |
| System Voltage | `sensor` | Battery voltage |
| System Temperature | `sensor` | Internal temp |
| HVAC | `sensor` | Status monitoring |

#### Troubleshooting

- **Connection fails:** Ensure the gateway is paired in Android Bluetooth settings first
- **No devices appear:** The app sends a GetDevices command on connect - check MQTT logs
- **Status 133 errors:** Try unpairing and re-pairing the gateway

---

### BLE Scanner Plugin

A utility plugin that scans for nearby BLE devices and publishes results to MQTT.

#### Use Cases

- Discovering MAC addresses of BLE devices
- Monitoring BLE device presence
- Debugging BLE connectivity issues

#### Configuration

Enable via the **BLE Scanner** toggle. Results are published as sensor attributes in Home Assistant.

---

## 🏗️ Architecture

```
BaseBleService (foreground service)
  ├─> MqttOutputPlugin (MQTT connection & publishing)
  ├─> OneControlDevicePlugin (RV automation)
  │    └─> Plugin-owned GATT callback for protocol handling
  └─> BleScannerPlugin (device discovery)
```

Each BLE device plugin:
- Owns its `BluetoothGattCallback` completely
- Handles authentication, framing, and protocol specifics
- Publishes state via the `MqttPublisher` interface

See [docs/INTERNALS.md](docs/INTERNALS.md) for detailed architecture documentation.

---

## 🔧 Development

### Building

```bash
./gradlew assembleDebug
adb install -r app/build/outputs/apk/debug/app-debug.apk
```

### Debugging

```bash
# Monitor logs
adb logcat -s BaseBleService:I OneControlDevice:I MqttOutputPlugin:I

# Check MQTT messages
mosquitto_sub -h <broker> -u mqtt -P mqtt -t "homeassistant/#" -v
```

### System Requirements

- **Android:** API 26+ (Android 8.0+)
- **Permissions:** Bluetooth, Location (for BLE scanning), Notifications
- **MQTT Broker:** Any MQTT 3.1.1 compatible broker

---

## To Do

- [ ] Additional BLE device plugins
- [ ] Cover position tracking
- [ ] HVAC control commands
- [ ] Light effects support

## License

This project is provided as-is for personal use. The OneControl protocol implementation is based on observation and reverse engineering of BLE communications.
