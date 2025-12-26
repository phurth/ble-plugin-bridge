# Development Scripts

Utility scripts for testing and configuring the BLE MQTT Bridge during development.

## Available Scripts

### configure-mqtt.sh

Configure MQTT broker credentials remotely via ADB without device interaction.

**Usage:**
```bash
./scripts/configure-mqtt.sh <broker_url> [username] [password]
```

**Examples:**
```bash
# Local Home Assistant
./scripts/configure-mqtt.sh tcp://192.168.1.100:1883 mqtt mypassword

# Using hostname
./scripts/configure-mqtt.sh tcp://homeassistant.local:1883 mqtt mypassword

# Public broker (no auth)
./scripts/configure-mqtt.sh tcp://broker.hivemq.com:1883
```

**Requirements:**
- ADB installed and in PATH
- Android device connected via USB or WiFi ADB
- BLE MQTT Bridge app installed on device

---

### test-adb-control.sh

Test remote ADB control commands for the BLE Bridge service.

**Usage:**
```bash
./scripts/test-adb-control.sh
```

**Tests:**
- Service status check
- Plugin enumeration
- Service start/stop
- Plugin enable/disable
- MQTT connection status

**Output:**
Streams logcat filtered for control commands and displays test results.

---

### test-onecontrol-v2.sh

Test OneControl BLE plugin functionality.

**Usage:**
```bash
./scripts/test-onecontrol-v2.sh
```

**Tests:**
- OneControl device connection
- Authentication flow
- Device enumeration
- Command sending
- Status monitoring

---

## Requirements

All scripts require:
- macOS or Linux
- ADB (Android Debug Bridge) installed
- Android device with USB debugging enabled
- BLE MQTT Bridge app installed

## Tips

**Enable WiFi ADB (no USB cable needed):**
```bash
# Connect device via USB first
adb tcpip 5555
# Disconnect USB, then:
adb connect <device-ip>:5555
```

**View all app logs:**
```bash
adb logcat -s BaseBleService:I OneControlDevice:I MqttOutputPlugin:I GoPowerDevice:I EasyTouchDevice:I
```

**Clear app data (reset to defaults):**
```bash
adb shell pm clear com.blemqttbridge
```
