# Release Notes v2.3.1

**Release Date:** December 26, 2025

## 🎉 New Features

### GoPower Solar Controller Plugin

Added full support for GoPower solar charge controllers (e.g., GP-PWM-30-SB) commonly found in RVs:

- **Read-only monitoring** - No authentication or pairing required
- **Real-time data** at ~1 second update rate
- **Comprehensive sensors**:
  - Solar panel voltage (V)
  - Battery voltage (V) and state of charge (%)
  - Charge current (A) and power (W)
  - Load current (A) and power (W)
  - Controller temperature (°C)
  - Load output status (ON/OFF)
- **Device diagnostics**:
  - Model number (derived from serial prefix)
  - Serial number (hex converted to decimal to match official app)
  - Firmware version

Simply enter the controller MAC address, enable the plugin, and all sensors appear automatically in Home Assistant.

### Per-Plugin Status Tracking

Refactored status/health indicators from global to **per-plugin architecture**:

- Each plugin (EasyTouch, GoPower, OneControl) now reports status independently
- Separate MQTT diagnostic sensors for each plugin:
  - `connected` - BLE connection status
  - `authenticated` - Authentication status (if applicable)
  - `data_healthy` - Receiving valid data
- App UI shows aggregate status (green if ANY plugin is healthy)
- Enables monitoring multiple plugins simultaneously

**Example:** You can now see that EasyTouch is connected and healthy while GoPower is disconnected, instead of just seeing one global status.

### System Diagnostic Sensors

The BLE MQTT Bridge device itself now publishes comprehensive system health metrics:

**Battery Monitoring:**
- Battery level (%)
- Charging status (charging/discharging/full)
- Battery temperature (°C)

**Resource Usage:**
- RAM used (%)
- RAM available (MB)
- CPU usage (%)
- Storage available (GB)
- Storage used (%)

**Network:**
- WiFi SSID (current network name)
- WiFi signal strength (dBm)

**Device Information:**
- Device name (e.g., "Pixel 6")
- Manufacturer
- Android version and API level
- Device uptime (hours)

All sensors appear under the "BLE MQTT Bridge" device in Home Assistant with diagnostic entity category.

## 🐛 Bug Fixes

### BLE Scanner Conditional Initialization

Fixed issue where BLE Scanner plugin would always publish Home Assistant discovery even when disabled:

- BLE Scanner now only initializes when explicitly enabled in app settings
- Prevents unwanted device entities reappearing in Home Assistant after removal
- Matches expected behavior of other optional plugins

## 📚 Documentation

### INTERNALS.md Updates

- Added comprehensive GoPower Protocol Deep Dive section
  - Complete protocol specification with field mapping
  - Hex-to-decimal serial number conversion details
  - Model number detection heuristics
  - Comparison with OneControl and EasyTouch protocols
- Updated State Management & Status Indicators section
  - Per-plugin PluginStatus architecture
  - MQTT diagnostic sensor publishing patterns
  - SettingsViewModel aggregation logic
  - BLE Scanner conditional initialization
- Updated file reference appendix with new plugin files
- Version bump to 2.3.1

### README.md Updates

- Added GoPower Solar Controller plugin section
  - Configuration instructions
  - Feature list and sensor table
  - Troubleshooting guide
- Updated architecture diagram to include GoPower plugin
- Added note about BLE Scanner conditional initialization
- Updated system requirements

## 🔄 API Changes

### MqttPublisher Interface

Added new method for per-plugin status reporting:

```kotlin
fun updatePluginStatus(
    pluginId: String,
    connected: Boolean,
    authenticated: Boolean,
    dataHealthy: Boolean
)
```

Deprecated old global methods (kept for backwards compatibility):
- `updateBleStatus()`
- `updateDiagnosticStatus()`

### BaseBleService State Management

Replaced global StateFlows with per-plugin tracking:

**Before:**
```kotlin
val bleConnected: StateFlow<Boolean>
val dataHealthy: StateFlow<Boolean>
val devicePaired: StateFlow<Boolean>
```

**After:**
```kotlin
val pluginStatuses: StateFlow<Map<String, PluginStatus>>
```

Where `PluginStatus` is:
```kotlin
data class PluginStatus(
    val pluginId: String,
    val connected: Boolean,
    val authenticated: Boolean,
    val dataHealthy: Boolean
)
```

## 🔧 Technical Details

### GoPower Protocol Implementation

**File Structure:**
```
plugins/gopower/
├── GoPowerDevicePlugin.kt          # Main plugin
└── protocol/
    ├── GoPowerConstants.kt         # UUIDs and field indices
    └── GoPowerGattCallback.kt      # BLE notification handler
```

**Key Implementation Notes:**
- Service UUID: `0000fff0-0000-1000-8000-00805f9b34fb`
- Notify UUID: `0000fff1-0000-1000-8000-00805f9b34fb`
- Data format: 20-byte comma-delimited ASCII string
- Serial number in field 14 is hexadecimal (requires `toInt(16)` conversion)
- Model number derived from serial prefix

### System Diagnostics Implementation

System diagnostic sensors are published from `MqttOutputPlugin.kt`:

- Sensors update on MQTT connection (typically once at startup)
- All use appropriate Home Assistant device classes (battery, voltage, temperature, etc.)
- Marked with `entity_category: "diagnostic"` for proper UI grouping
- Graceful fallback to default values if sensor data unavailable

**Android APIs Used:**
- `BatteryManager` - Battery level, status, temperature
- `ActivityManager.MemoryInfo` - RAM usage
- `/proc/stat` - CPU usage
- `StatFs` - Storage statistics
- `WifiManager` - Network information
- `Build` - Device information
- `SystemClock.elapsedRealtime()` - Uptime

## 📦 Installation

1. Download `app-debug.apk` from this release
2. If upgrading from previous version, you can install directly over the old version
3. Grant any new permissions if prompted
4. Configure GoPower plugin if using (Settings → GoPower → Enter MAC address)
5. Enable desired plugins and restart BLE Service

**Note:** Settings are preserved across upgrades.

## 🔗 Compatibility

- **Minimum Android Version:** Android 8.0 (API 26)
- **Home Assistant:** Any version with MQTT integration
- **MQTT Broker:** Any MQTT 3.1.1 compatible broker
- **Supported Devices:**
  - OneControl BLE Gateways (LCI/Lippert)
  - EasyTouch Thermostats (Micro-Air, firmware 1.0.6.0+)
  - GoPower Solar Controllers (GP-PWM-30-SB and compatible models)

## ⚠️ Breaking Changes

None. All changes are backwards compatible with existing configurations.

The deprecated `updateBleStatus()` and `updateDiagnosticStatus()` methods in `MqttPublisher` are still functional but plugins should migrate to `updatePluginStatus()` for proper per-plugin tracking.

## 🙏 Acknowledgments

- GoPower protocol reverse-engineered through BLE packet analysis
- Per-plugin status architecture inspired by need to monitor multiple RV systems simultaneously
- System diagnostic sensors modeled after common Android system monitoring tools

## 📝 Full Changelog

See commit history for complete list of changes:
```
git log v2.3.0..v2.3.1
```

## 🐛 Known Issues

None at this time. Please report issues on GitHub.

---

**Previous Release:** [v2.3.0](https://github.com/phurth/ble-plugin-bridge/releases/tag/v2.3.0)
