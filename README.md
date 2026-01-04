# BLE-MQTT Bridge (Android)

Android foreground service that bridges BLE (Bluetooth Low Energy) devices to MQTT, enabling Home Assistant integration via a plugin-based architecture.

**Current Version:** v2.4.3 - Android TV launcher support, graceful BLE unavailable handling

## 🚀 Quick Start

### Prerequisites

- **Android Device:** Android 8.0+ with BLE support  
  - **Recommended:** Android 12+ for best battery optimization support
- **MQTT Broker:** Accessible from the Android device (e.g., Mosquitto on Home Assistant)
- **Home Assistant:** MQTT integration enabled

### Installation

1. Download the latest APK from [GitHub Releases](https://github.com/phurth/ble-plugin-bridge/releases)
2. Enable "Install unknown apps" for your browser/file manager
3. Install the APK
4. **On first launch:** The app will automatically request required permissions (Location, Bluetooth, Notifications)
5. **Important:** Configure battery optimization exemption via the System Settings screen (⚙️ icon in top-right)

### Initial Configuration

1. Open the app - all toggles will be OFF by default
2. **Grant permissions:** On first launch, the app will request:
   - **Location** (required for BLE scanning)
   - **Bluetooth Scan/Connect** (Android 12+)
   - **Notifications** (for foreground service)
3. Tap the **⚙️ Settings icon** (top-right) to:
   - Enable **Battery Optimization Exemption** (critical for reliable operation)
   - Verify all permissions are granted
4. Return to main screen and configure **MQTT broker settings** (expand "Broker Settings"):
   - Host, Port, Username, Password
   - Topic Prefix: `homeassistant` (recommended for auto-discovery)
5. Configure your **device plugin settings** (see plugin sections below)
6. Enable toggles in order: **MQTT → Plugin → BLE Service**

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
   - The gateway should appear with a name starting with "LCI..."
   - Tap to pair - enter your gateway PIN if prompted (this is unlikely, it should just pair)
   - Wait for "Paired" status to appear

3. **Configure the plugin:**
   - In the BLE-MQTT Bridge app, expand "Gateway Settings" under the OneControl plugin
   - Enter the **Gateway MAC Address** (found in Bluetooth settings after pairing)
   - Enter your **Gateway PIN** (found on a sticker on your OneControl board)

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

### EasyTouch Thermostat (Micro-Air)

The EasyTouch plugin connects to Micro-Air EasyTouch BLE thermostats commonly found in RVs.

#### ⚠️ Firmware Requirement

**This plugin requires EasyTouch firmware version 1.0.6.0 or newer.** The plugin uses the unified JSON protocol introduced in firmware 1.0.6.0. Older firmware versions use model-specific protocols that are not supported.

To check your firmware version, connect to the thermostat and look for the "Firmware Version" diagnostic sensor in Home Assistant, or check the official EasyTouch RV app.

#### Acknowledgments

Special thanks to **[k3vmcd](https://github.com/k3vmcd)** and their [ha-micro-air-easytouch](https://github.com/k3vmcd/ha-micro-air-easytouch) HACS integration. Their project inspired the plugin architecture and their work decoding the thermostat's BLE protocol was essential to this implementation.

#### Configuration

1. Expand "EasyTouch Settings" in the app
2. Enter the **Thermostat MAC Address** (found in Bluetooth settings or the official app)
3. Enter your **Thermostat Password** (this is the password you use to login in the Micro-Air app)
4. Toggle the main service off and on

#### Features

| Feature | Description |
|---------|-------------|
| **Multi-Zone Support** | Up to 4 climate zones |
| **Capability Discovery** | Only shows modes your device supports |
| **Auto Mode** | High/low setpoint UI when in Auto |
| **Temperature Limits** | Min/max from actual device config |

#### Supported Modes

Modes are discovered dynamically from device. Common modes:
- Off, Heat, Cool, Auto, Fan Only
- Dry (only if device supports it)

#### Troubleshooting

- **First command fails:** Normal BLE timing issue - retry built in
- **Wrong modes showing:** Delete the HA climate entity and let it rediscover
- **Connection drops:** Check thermostat is in range and not connected to another device

---

### GoPower Solar Controller

The GoPower plugin connects to GoPower solar charge controllers (e.g., GP-PWM-30-SB) commonly found in RVs. This is a **read-only** plugin - it monitors solar system status but does not send commands.

#### Configuration

1. Expand "GoPower Settings" in the app
2. Enter the **Controller MAC Address** (found in Bluetooth settings)
3. Toggle the main service off and on

**Note:** GoPower controllers do not require pairing or authentication.

#### Features

| Feature | Description |
|---------|-------------|
| **Read-Only Protocol** | Monitors status only, no control commands |
| **No Authentication** | Connects without pairing or password |
| **Real-Time Data** | ~1 second update rate |
| **Device Diagnostics** | Model, serial number, firmware version |

#### Sensors

| Sensor | Description | Unit |
|--------|-------------|------|
| PV Voltage | Solar panel voltage | V |
| PV Current | Solar panel current | A |
| PV Power | Solar input power | W |
| Battery Voltage | Battery voltage | V |
| Battery Percentage | State of charge | % |
| Controller Temperature | Internal temperature | °C |
| Energy | Daily energy production (Ah × voltage) | Wh |
| Device Model | Controller model number | text |
| Device Firmware | Firmware version | text |

#### Troubleshooting

- **No data received:** Ensure controller is in BLE range (within ~30 feet)
- **Connection drops:** Verify no other device is connected to the controller
- **Reboot button doesn't work:** The unlock sequence is sent but controller may not respond to BLE reboot commands

---

### BLE Scanner Plugin

A utility plugin that scans for nearby BLE devices and publishes results to MQTT. This is not needed for anything else to function, but was added as a proof of concept for supporting multiple BLE connected plugins and might be useful, so I left it in.

#### Use Cases

- Discovering MAC addresses of BLE devices
- Monitoring BLE device presence
- Debugging BLE connectivity issues

#### Configuration

Enable via the **BLE Scanner** toggle. Results are published as sensor attributes in Home Assistant.

**Note:** Starting in v2.3.1, BLE Scanner only initializes and publishes to Home Assistant when enabled in the app.

---ckground Operation

**The app runs as a foreground service, not a foreground app.** This means:
- ✅ Continues running when you switch to other apps (e.g., Fully Kiosk Browser)
- ✅ Maintains BLE connections and MQTT publishing in the background
- ✅ Shows a persistent notification (required by Android)
- ✅ Survives screen-off and deep sleep (with proper configuration)
- ✅ No need to keep the app visible - perfect for kiosk setups

### Battery Optimization & Background Execution

**v2.3.6+ includliability

### Battery Optimization & Background Execution

**v2.3.6 introduces comprehensive service hardening features** to prevent Android from killing the app during idle periods:

#### Defense Layers

1. **Battery Optimization Exemption**
   - Prevents Doze mode from stopping the service
   - Configure via Settings (⚙️) → Battery Optimization Exemption
   - **Critical for overnight operation**

2. **Bluetooth State Monitoring**
   - Automatically detects when OS disables Bluetooth (common on Samsung/TCL devices)
   - Graceful cleanup when BT turns off
   - Auto-reconnect when BT turns back on

3. **WorkManager Watchdog**
   - Checks service health every 15 minutes
   - Auto-restarts service if killed by OS
   - Survives device reboots

4. **Adaptive Scanning**
   - Uses BLE scan filters for screen-off operation
   - Maintains connections during deep sleep

5. **Doze Mode Keepalive (v2.3.6+)**
   - **Prevents BLE connections from dropping during deep sleep**
   - Sends periodic keepalive pings every 30 minutes
   - Uses `AlarmManager.setExactAndAllowWhileIdle()` to wake device during Doze
   - **Enabled by default** - ideal for mains-powered devices
   - **Toggle in Settings:** System Settings (⚙️) → Doze Mode Prevention → Keepalive Pings
   
   **When to enable:**
   - ✅ Device is plugged in/mains-powered (recommended)
   - ✅ You need 24/7 BLE connectivity without manual intervention
   - ✅ RV/boat/camper installations with shore power
   
   **When to disable:**
   - ❌ Battery-powered devices where power savings are critical
   - ❌ You only use the app occasionally (connections auto-reconnect on device wake)

#### Recommended Settings

For maximum reliability on aggressive battery management devices (Samsung, Xiaomi, OnePlus, etc.):

1. **Battery Optimization:** Set to "Active - Service protected"
2. **Keepalive Pings:** Enable for mains-powered setups (Settings → Doze Mode Prevention)
3. **Auto-start:** Enable in device settings if available
4. **Background restrictions:** Disable for this app
5. **Data saver:** Add app to exception list

See [docs/SERVICE_HARDENING.md](docs/SERVICE_HARDENING.md) for detailed hardening strategy.

---

## 🏗️ Architecture

```
BaseBleService (foreground service)
  ├─> MqttOutputPlugin (MQTT connection & publishing)
  ├─> OneControlDevicePlugin (RV automation)
  ├─> EasyTouchDevicePlugin (climate control)
  ├─> GoPowerDevicePlugin (solar monitoring)
  └─> BleScannerPlugin (device discovery - optional)
```

Each BLE device plugin:
- Owns its `BluetoothGattCallback` completely
- Handles authentication, framing, and protocol specifics
- Publishes state via the `MqttPublisher` interface
- Reports status independently (per-plugin health indicators)

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

MIT License - see [LICENSE](LICENSE) for details.

EasyTouch thermostat protocol implementation was informed by the [ha-micro-air-easytouch](https://github.com/k3vmcd/ha-micro-air-easytouch) project by k3vmcd.
