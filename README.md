# BLE-MQTT Bridge (Android)

Android foreground service that bridges BLE (Bluetooth Low Energy) devices to MQTT, enabling Home Assistant integration via a plugin-based architecture. Available plugins:
- OneControl RV Control System
- Micro-Air EasyTouch RV Thermostat
- GoPower Solar Controller
- Mopeka Pro Check Fluid Sensors
- Hughes Power Watchdog EPO
- Basic BLE scanner plugin

Note: I'm able to build and test these plugins since they are components I have in my RV. There's a good chance your RV is different and adjustments will need to be made. Pleaase help other users by submitting an issue or feature request if you encounter problems.

## 🚀 Quick Start

### Prerequisites

- **Android Device:** Android 8.0+ with BLE support (Recommended: Android 12+ for best battery optimization support)
  - Tested with tablet, phone and Android TV devices.
- **MQTT Broker:** Accessible from the Android device (e.g., Mosquitto on Home Assistant)
- **Home Assistant:** MQTT integration enabled

### Installation

1. Download the latest APK from [GitHub Releases](https://github.com/phurth/ble-plugin-bridge/releases)
2. Enable "Install unknown apps" for your browser/file manager
3. Install the APK
4. **On first launch:** The app will automatically request required permissions (Location, Bluetooth, Notifications)
5. **Important:** If desired, configure battery optimization exemption via the System Settings screen (⚙️ icon in top-right)

### Initial Configuration

1. Open the app - main service and MQTT toggles will be OFF by default
2. **Grant permissions:** On first launch, the app will request:
   - **Location** (required for BLE scanning)
   - **Bluetooth Scan/Connect** (Android 12+)
   - **Notifications** (for foreground service - this helps keep the app from getting killed by the OS)
3. Tap the **⚙️ Settings icon** (top-right) to:
   - Enable **Battery Optimization Exemption** (critical for reliable operation on battery-powered devices, especially phones)
   - Verify all permissions are granted
4. Return to the main screen and if desired, enable authentication for the web configuration interface and/or change the port used for the web service.
**All further config will happen in the web UI**
5. Either click the URL to open the web UI on the Android device, or note the URL and open on any device on the same network.
6. In the web UI, configure **MQTT broker settings** (expand "Broker Settings"):
   - Host, Port, Username, Password
   - Topic Prefix: `homeassistant` (recommended for auto-discovery)
7. Configure your **device plugin settings** (see plugin sections below)
8. Enable toggles in order: **MQTT → Main Service**
9. Restarting the device is not necessary, but if things don't start showing up in Home Assistant, you may need to try toggling the main service, or stopping and relaunching the app to get things flowing.

> **Note:** Settings are locked while the service toggle is ON. Turn OFF to edit.

### Home Assistant Integration

Once connected, devices are automatically discovered via MQTT auto-discovery:

- **BLE MQTT Bridge {MAC}:** General diagnostic info for the app and Android device
- Each enabled plugin will be a separate device

The devices will have a mix of the following discovered entities which depend on your specific hardware:

- **Switches** - Binary relays
- **Lights** - Dimmable with brightness control
- **Covers** - Slides and awnings
- **Sensors** - Temperature, voltage, tank levels
- **Binary Sensors** - Diagnostic indicators
- **Climate** - Still a work in progress, but OneControl-connected HVAC should be fully supportable

---

## 📦 Plugins

### 🔌 OneControl Gateway (LCI/Lippert)

The OneControl plugin connects to LCI/Lippert OneControl BLE gateways found in RVs. This is an unofficial, community-developed integration not affiliated with or supported by LCI/Lippert, and is provided "as-is" without warranty—use at your own risk.

####Tested Gateways
The following OneControl gateways have been confirmed working:
- Unity X270D
- X1.5
- x4

#### Setup

##### Pairing Information

The OneControl gateway must be paired to the Android device. 

**⚠️ IMPORTANT - Disconnect/unpair from other devices:** If the gateway is currently paired with a different phone or tablet running the official OneControl app (i.e., *not* the device you plan to use with this app), make sure the LCI app is not running, and preferrably unpair it completely (Settings → Bluetooth → Forget Device on that device). The gateway can only connect to one device at a time.

**Pairing Steps:**

Newer Android OS versions do not show BLE devices in the Bluetooth pairing settings, so there are a couple of ways to accomplish this:

1. **Use an existing pairing:** 
  - If you previously connected to the OneControl gateway with the same device, you already have a pair bond (you can confirm this by checking devices in  Bluetooth settings). In this case, you're good to go.
  - Go ahead and configure the plugin in the web UI:
     - Make sure the BLE service toggle is off (you won't be able to make changes while it's running)
     - Add the plugin
     - Enter the **Gateway MAC Address** (found in Bluetooth settings after pairing or you can get this from the Advertisement Monitor in Home Aassistant)
     - Enter your **Gateway PIN/Password** (found on a sticker on your OneControl board)
     - Toggle the service on
     - Plugin status indicators should turn green: Connection → Data → Paired

2. **Let the app trigger pairing:**
     - Make sure the main service toggle is off (you won't be able to make changes while it's running)
     - Add the plugin
     - Enter the **Gateway MAC Address** (found in Bluetooth settings after pairing or you can get this from the Advertisement Monitor in Home Aassistant)
     - Enter your **Gateway PIN/Password** (found on a sticker on your OneControl board)
     - **IMPORTANT:** From here there are two pairing paths depeding upon they style supported by your OneControl hardware
     
       - **Path 1 - If you have a 'Connect' button:** Put the OneControl gateway in pairing mode by pressing the 'Connect' button (the exact process may vary for different gateways, but this is the same process you do to initially connect using the LCI app)
         - Toggle the service on
       - **Path 2 - If you DO NOT have a 'Connect' button:** Toggle the main service on and you should get a pairing request pop-up.
         - Enter the same PIN from the sticker on the gateway
     - The app should then complete pairing with your gateway. You can confirm this by checking Settings → Bluetooth. The Gateway should show as a paired device.
     - Plugin status indicators should turn green: Connection → Data → Paired


#### Supported Devices

| Device Type | HA Entity | Features |
|-------------|-----------|----------|
| Switches | `switch` | ON/OFF control |
| Dimmable Lights | `light` | Brightness 0-255 |
| Slides/Awnings | `cover` | Open/Close/Stop |
| Tank Sensors | `sensor` | Fill level % |
| System Voltage | `sensor` | Battery voltage |
| HVAC | `climate`,`sensor` | Status monitoring |

#### Troubleshooting

- **Connection fails:** Ensure the gateway shows as paired in Android Bluetooth settings
- **No devices appear:** The app sends a GetDevices command on connect - toggle the service off/on, or force-close and relaunch the app.

---

### 🔌 EasyTouch Thermostat (Micro-Air)

The EasyTouch plugin connects to the EasyTouch RV thermostat by Micro-Air. This is an unofficial, community-developed integration not affiliated with or supported by Micro-Air, and is provided "as-is" without warranty—use at your own risk.

#### ⚠️ Firmware Requirement

**This plugin requires EasyTouch firmware version 1.0.6.0 or newer.** The plugin uses the unified JSON protocol introduced in firmware 1.0.6.0. Older firmware versions use model-specific protocols that are not supported. To check your firmware version, check the official EasyTouch RV app.

#### Acknowledgments

Special thanks to **[k3vmcd](https://github.com/k3vmcd)** and his [ha-micro-air-easytouch](https://github.com/k3vmcd/ha-micro-air-easytouch) HACS integration. His project inspired this application's creation and his work decoding the thermostat's BLE protocol was essential to this implementation.

#### Configuration
  - Make sure the main service toggle is off (you won't be able to make changes while it's running)
  - Add the plugin
  - Enter the **Thermostat MAC Address** (you can get this from the Advertisement Monitor in Home Aassistant)
  - Enter your **Thermostat Password** (this is the password you use to login in the Micro-Air app)
- Toggle the service on
- Plugin status indicators should turn green: Connection → Data → Pair

#### Features

| Feature | Description |
|---------|-------------|
| **Multi-Zone Support** | Up to 4 climate zones ** Not yet fully tested** |
| **Capability Discovery** | Only shows modes your device supports |
| **Auto Mode** | High/low setpoint UI when in Auto |
| **Temperature Limits** | Min/max from actual device config |

#### Supported Modes

Modes are discovered dynamically from device. Common modes:
- Off, Heat, Cool, Auto, Fan Only
- Dry (only if device supports it)

#### Troubleshooting

- **Connection drops:** Check thermostat is in range and not connected to another device, toggle the service off/on, or force-close and relaunch the app.

---

### 🔌 GoPower Solar Controller

The GoPower plugin connects to GoPower solar charge controllers (e.g., GP-PWM-30-SB) commonly found in RVs. This is an unofficial, community-developed integration not affiliated with or supported by GoPower, and is provided "as-is" without warranty—use at your own risk.

#### Configuration

- Make sure the main service toggle is off (you won't be able to make changes while it's running)
- Add the plugin
- Enter the **Controller MAC Address** (found in Bluetooth settings after pairing or you can get this from the Advertisement Monitor in Home Aassistant) 
- Toggle the service on
- Plugin status indicators should turn green: Connection → Data

**Note:** GoPower controllers do not require pairing or authentication.

#### Features

| Feature | Description |
|---------|-------------|
| **Reboot** | Reboot the solar controller |
| **No Authentication** | Connects without pairing or password |
| **Real-Time Data** | ~1 second update rate |
| **Device Diagnostics** | Model, firmware version |

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
| Reboot Controller | Soft-reboots the controller | button |

#### Troubleshooting

- **No data received:** Ensure controller is in BLE range (within ~30 feet)
- **Connection drops:** Verify no other device is connected to the controller

---

### 🔌 Hughes Power Watchdog EPO

The Hugjes plugin connects to Hughes Power Watchdog EPO surge protectors (e.g., WD30EPO, PWD50EPO) commonly found in RVs. This is an unofficial, community-developed integration not affiliated with or supported by Hughes, and is provided "as-is" without warranty—use at your own risk.

#### Configuration

- Make sure the main service toggle is off (you won't be able to make changes while it's running)
- Add the plugin
- Enter the **Device MAC Address** (you can get this from the Advertisement Monitor in Home Aassistant) 
- Toggle the service on
- Plugin status indicators should turn green: Connection → Data

**Note:** PWD devices do not require pairing or authentication.

#### Features

| Feature | Description |
|---------|-------------|
| **No Authentication** | Connects without pairing or password |
| **Real-Time Data** | ~1 second update rate |


#### Troubleshooting

- **No data received:** Ensure device is in BLE range (within ~30 feet)
- **Connection drops:** Verify no other device is connected to the controller and that the device is in range

---

### 🔌 Mopeka Pro Check Fluid Sensors

The Mopeka plugin integrates Mopeka Pro Check/Pro Plus/Pro H2O Bluetooth tank level sensors. Unlike other plugins in this system, Mopeka uses **passive BLE advertisement scanning** - no GATT connection is required. The sensor broadcasts tank level, temperature, and battery status in manufacturer-specific advertisement data.

**Credits:**
- **sbrogan**: Original work decoding the Mopeka sensor BLE protocol (mopeka-iot-ble library)
- **jrhelbert**: Volumetric calculation formulas for accurate tank percentage ([HA Community Post](https://community.home-assistant.io/t/add-tank-percentage-to-mopeka-integration/531322/34))

**Supported Models:**
- Mopeka Pro Plus (M1015)
- Mopeka Pro Check (M1017)
- Mopeka Pro 200
- Mopeka Pro H2O (water sensors)
- Mopeka Pro H2O Plus
- Lippert BottleCheck
- TD40, TD200

#### Configuration

- Make sure the main service toggle is off (you won't be able to make changes while it's running)
- Add the plugin
- Enter the **MAC Address** (you can get this from the Advertisement Monitor in Home Aassistant)
- Enter the tank size and fluid type
- Toggle the service on
- Plugin status indicator should turn green

**Note:** Mopeka controllers do not require pairing or authentication.

#### Features

| Feature | Description |
|---------|-------------|
| **No Authentication** | Connects without pairing or password |
| **Real-Time Data** | ~14 second update rate |

#### Sensors

| Sensor | Description | Unit |
|--------|-------------|------|
| Battery Percentage | State of charge | % |
| Temperature | Sensor temperature | °C |
| Tank Level | Tank level percent | % |

#### Troubleshooting

- **No data received:** Ensure sensors are in BLE range (within ~30 feet)

---

### 🔌 BLE Scanner Plugin

A utility plugin that scans for nearby BLE devices and publishes results to MQTT. This is not needed for anything else to function, but was added as a proof of concept for supporting multiple BLE connected plugins and might be useful, so I left it in.

#### Use Cases

- Discovering MAC addresses of BLE devices
- Monitoring BLE device presence
- Debugging BLE connectivity issues

#### Configuration

- Make sure the main service toggle is off (you won't be able to make changes while it's running)
- Add the plugin
- Toggle the service on
- Scanning is triggered by a button in the MQTT device in Home Assistant
- Results are published as sensor attributes in Home Assistant

---

## 🏗️ Architecture

See [docs/INTERNALS.md](docs/INTERNALS.md) for detailed architecture documentation.

---

## 🛠️ Home Assistant Add-on: ADB Bridge

For HAOS users who want remote APK deployment without physical device access, this repository includes an optional add-on that maintains wireless ADB connectivity.

### Features
- **Auto-detection**: Automatically chooses wireless or bridge mode based on device network
- **Wireless ADB**: Keeps wireless ADB alive for WiFi-connected devices
- **Bridge Mode**: ADB over network for Ethernet-connected devices
- **Works with any Android device**: Not limited to this app

### Quick Start
1. Add this repository to Home Assistant: `https://github.com/phurth/ble-plugin-bridge`
2. Install "ADB Bridge" add-on
3. Connect Android device via USB to HAOS host
4. Enable USB debugging on device
5. Start add-on and deploy APKs remotely via `adb connect`

See [haos-addon/README.md](haos-addon/README.md) for detailed setup and usage.

---

## License

MIT License - see [LICENSE](LICENSE) for details.