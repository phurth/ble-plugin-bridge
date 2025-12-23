# BLE-MQTT Bridge (Android)

Android foreground service that bridges BLE (Bluetooth Low Energy) devices to MQTT, enabling Home Assistant integration for OneControl RV automation systems and other BLE devices.

## 🚀 Quick Setup Guide

### Prerequisites

- **Android Device Requirements:**
  - Android 8.0 (API 26) or higher
  - Bluetooth Low Energy (BLE) support
  - Internet connectivity (WiFi or cellular)

- **Home Assistant:**
  - MQTT broker configured and accessible from the Android device
  - MQTT integration enabled in HA

### Installation

1. **Download the APK:**
   - Visit the [GitHub Releases](https://github.com/phurth/ble-plugin-bridge/releases) page
   - Download the latest APK file

2. **Install on Android Device:**
   - Enable "Install unknown apps" for your browser/file manager:
     - Go to Settings → Apps → [Browser/File Manager] → Install unknown apps → Allow
   - Open the downloaded APK file and install
   - Grant all requested permissions when prompted

3. **Initial Configuration:**
   - Open the app - all toggles will be OFF by default
   - Configure MQTT broker settings (expand "Broker Settings")
   - Configure OneControl gateway MAC address and PIN (expand "Gateway Settings")
   - Enable MQTT, then OneControl, then BLE Service toggles

### Configuration

**MQTT Settings:**
- **Host:** Your MQTT broker IP address (e.g., `192.168.1.100`)
- **Port:** MQTT broker port (default: `1883`)
- **Username/Password:** Your MQTT broker credentials
- **Topic Prefix:** `homeassistant` (recommended for auto-discovery)

**OneControl Settings:**
- **Gateway MAC Address:** Your OneControl gateway's Bluetooth MAC address
- **Gateway PIN:** Your OneControl PIN (found in the OneControl app)

> **Note:** Settings are locked while their respective toggle is ON. Turn the toggle OFF to edit settings.

### Pairing the Gateway

1. **Put Gateway in Pairing Mode:**
   - If the gateway is paired with another device, you may need to unpair it first
   - Follow your RV's OneControl documentation to enter pairing mode if needed

2. **Accept Pairing Request:**
   - The app will detect the gateway and show a pairing dialog
   - Accept the pairing request and enter the PIN if prompted

3. **Verify Connection:**
   - Status indicators will turn green: BLE → Data → Paired
   - Check your MQTT broker for topics under `homeassistant/`

### Home Assistant Setup

Once connected, devices are automatically discovered by Home Assistant via MQTT auto-discovery. You'll see:

- **Switches** - Binary relays and latching switches
- **Lights** - Dimmable lights with brightness control
- **Covers** - Awnings and slides
- **Sensors** - Temperature, voltage, tank levels
- **Binary Sensors** - Diagnostic status indicators

#### Optional: App Availability Monitoring

Add to `configuration.yaml`:
```yaml
mqtt:
  binary_sensor:
    - name: "BLE Bridge Availability"
      state_topic: "homeassistant/ble_bridge/availability"
      payload_on: "online"
      payload_off: "offline"
      device_class: connectivity
```

## 📦 Supported Devices

### OneControl Gateway (LCI/Lippert)
- **Switches** - Binary relays and latching switches
- **Dimmable Lights** - Full 0-255 brightness control
- **Covers** - Awnings, slides (bidirectional control)
- **Sensors** - Temperature, voltage, tank levels
- **HVAC** - Status monitoring

### BLE Scanner Plugin
- Scan for nearby BLE devices
- Results published to Home Assistant as sensor attributes
- Useful for discovering device MAC addresses

## 🏗️ Architecture

The app uses a plugin-based architecture where each BLE device type is handled by a dedicated plugin:

```
BaseBleService (foreground service)
  ├─> MqttOutputPlugin (MQTT connection & publishing)
  ├─> OneControlDevicePlugin (OneControl gateway)
  │    └─> BLE connection, authentication, command handling
  └─> BleScannerPlugin (BLE device discovery)
```

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

## System Requirements

- **Android:** API 26+ (Android 8.0+)
- **Permissions:** Bluetooth, Location (for BLE scanning), Notifications
- **MQTT Broker:** Any MQTT 3.1.1 compatible broker (Mosquitto recommended)

## To Do

- Additional BLE device plugins
- Cover position control improvements
- HVAC control commands
- Effects support for dimmable lights

## License

This project is provided as-is for personal use. The OneControl protocol implementation is based on observation and reverse engineering of BLE communications.
