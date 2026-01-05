# Changelog

All notable changes to the BLE-MQTT Plugin Bridge project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.4.6] - 2026-01-05

### Changed
- **Version bump:** Stable release with Android TV Power Fix feature
- **Documentation:** Updated README and INTERNALS with Android TV Power Fix section

## [2.4.5] - 2026-01-04

### Added
- **Android TV Power Fix**: New feature to prevent service from being killed when TV enters standby
  - Added `AndroidTvHelper` utility class for HDMI-CEC settings management
  - New "Android TV Power Fix" section in System Settings (only visible on Android TV devices)
  - Auto-detects Android TV via Leanback feature
  - Displays HDMI-CEC auto device off status
  - Provides ADB command with copy button to grant WRITE_SECURE_SETTINGS permission
  - When permission is granted, service automatically disables CEC auto-off on startup
  - Toggle to manually control CEC auto-off setting once permission is granted

### Technical Details
- Root cause: HDMI-CEC "auto device off" (`hdmi_control_auto_device_off_enabled=1`) causes streaming devices to sleep when TV sends CEC standby command, killing the foreground service
- Solution: Disable CEC auto device off to keep service running when TV powers off
- Permission: Requires `WRITE_SECURE_SETTINGS` which must be granted via ADB
- ADB command: `adb shell pm grant com.blemqttbridge android.permission.WRITE_SECURE_SETTINGS`

## [2.4.3] - 2026-01-04

### Added
- **Android TV Launcher Support**: App now appears in Android TV / Google TV launcher
  - Added `LEANBACK_LAUNCHER` category to MainActivity intent filter
  - Added banner attribute for TV launcher display
  - Tested on Onn 4K Pro Streaming Device

### Fixed
- **BLE Unavailable Crash**: App no longer crashes on devices without BLE support
  - Root cause: `BluetoothAdapter.getBluetoothLeScanner()` returns null on some devices (e.g., Emteria Android on Raspberry Pi)
  - Solution: Added null-safety checks and graceful error handling
  - Result: App shows "BLE not available" message instead of crash loop

### Tested Devices
- **Onn 4K Pro Streaming Device** (Android 14): Full BLE support confirmed, ethernet + WiFi
- **Raspberry Pi 4 + Emteria** (Android 14): No BLE support (driver limitation), app handles gracefully

## [2.4.2] - 2026-01-04

### Fixed
- **Fire Tablet Compatibility**: Added `tools:targetApi="s"` annotations to Android 12+ BLE permissions
  - Root cause: `usesPermissionFlags="neverForLocation"` attribute on BLUETOOTH_SCAN was unparseable by older Android/Fire OS
  - Solution: Added `tools:targetApi="s"` to indicate these attributes only apply to API 31+
  - Also added `xmlns:tools` namespace declaration to manifest
  - Result: App now installs on Amazon Fire tablets running older Fire OS versions

### Added
- **Location Feature Declaration**: Explicitly declared `android.hardware.location` and `android.hardware.location.gps` as optional features

## [2.4.1] - 2026-01-03

### Added
- **GoPower Energy Sensor**: New "Energy" sensor shows daily energy production in Wh (calculated as Ah × battery voltage)
- **GoPower Reboot Command**: Added unlock sequence (`&G++0900`) before reboot command per official app protocol
- **GoPower Reboot Button**: Button entity in Home Assistant to trigger controller reboot

### Changed
- **GoPower Protocol**: Updated to use correct field mappings based on official app analysis
  - Field 0: PV Voltage (÷10 for volts)
  - Field 2: PV Current (÷100 for amps)
  - Field 7: Battery Voltage (÷10 for volts)
  - Field 12: Battery SOC percentage
  - Field 14: Temperature (°C)
  - Field 15: Model ID
  - Field 19: Ah Today (÷100 for Ah)
  - Field 26: Firmware version
- **GoPower Sensors**: Simplified to essential sensors only (PV voltage/current/power, battery voltage/SOC, temperature, energy)

### Removed
- **GoPower Serial Number**: Removed unreliable serial number sensor (field value didn't match official app display)
- **GoPower Fault Sensors**: Removed fault_code and fault_description sensors - discovered these show historical faults, not current active faults
- **GoPower Load Sensors**: Removed load current/power/status as not all models support load output

### Technical Details
- Protocol uses semicolon-delimited ASCII strings with 32 fields
- Polling via 0x20 command at 1-second intervals
- Reboot requires unlock sequence with 200ms delay before reboot command

## [2.3.9] - 2025-12-30

### Fixed
- **Dimmable Light Panel Updates**: Fixed OneControl dimmable lights not updating in Home Assistant when operated from RV panel
  - Root cause: Overly-aggressive "spurious status guard" blocked all brightness=0 updates if last known was > 0
  - The guard was meant to filter gateway glitches but also blocked legitimate panel OFF commands
  - Solution: Removed redundant spurious filter - existing "pending guard" already handles spurious updates during HA commands
  - Result: Panel-operated lights now sync bidirectionally (HA ↔ Panel)

### Technical Details
- Spurious guard was added in v0.0.4 to filter incorrect gateway updates
- It became redundant after pending guard was implemented for command debouncing
- Pending guard (12-second window) prevents UI bouncing during HA-initiated commands
- When no command is pending, all status updates are from panel or real gateway state

## [2.3.8] - 2025-12-30

### Fixed
- **Text Input Cursor Jumping**: Fixed critical UI bug where typing in OutlinedTextField caused cursor to jump to start after each keystroke
  - Root cause: `collectAsState()` from DataStore flow caused full recomposition on every keystroke
  - Solution: Changed to `remember { mutableStateOf() }` for immediate local updates with async persistence
  - Affected fields: MQTT broker settings, plugin MAC addresses and PINs
- **Permission Crashes**: Fixed FOREGROUND_SERVICE_CONNECTED_DEVICE crash
  - Added `android:usesPermissionFlags="neverForLocation"` to BLUETOOTH_SCAN permission
  - Required for Android 12+ foreground service with BLE
- **Keepalive Alarm Crashes**: Fixed SCHEDULE_EXACT_ALARM permission crash
  - Added SCHEDULE_EXACT_ALARM and USE_EXACT_ALARM permissions
  - Required for exact alarms during Doze mode
- **OneControl DataHealthy Bouncing**: Fixed status indicator toggling every 2-3 seconds
  - Root cause: Event-driven plugin treated event silence (normal) as unhealthy data
  - Solution: Changed `computeDataHealthy()` to return `isConnected && isAuthenticated` (removed time-based check)
  - Result: Indicator stays solid green when connected, regardless of event frequency

### Added
- **Automatic Permission Requests**: App now requests all required permissions on first launch
  - Requests: ACCESS_FINE_LOCATION, BLUETOOTH_SCAN, BLUETOOTH_CONNECT, POST_NOTIFICATIONS
  - Added `checkAndRequestPermissionsOnStartup()` in MainActivity.onCreate()
  - Eliminates need for manual permission granting before using app

### Changed
- Improved first-time user experience with automatic permission flow
- Better text input responsiveness in settings

## [2.3.5] - 2024-12-26

### Added
- **Plugin removal with app kill**: Removing a plugin now kills the app to ensure clean disconnection
- **Discovery cleanup on removal**: Clears Home Assistant discovery topics when a plugin is removed
  - Publishes empty payloads to config topics to remove entities from HA
  - Tracks published discovery topics for cleanup
- Confirmation dialog now informs user that app will close

### Fixed
- Plugin removal no longer leaves orphaned entities in Home Assistant
- Plugin removal properly disconnects BLE and stops heartbeats

## [0.0.4] - 2024-12-22

### Fixed
- **Critical:** Brightness and mode bytes were swapped in DimmableLightStatus parsing
  - Corrected to: `data[3]=mode`, `data[4]=brightness` (matching MyRvLink protocol spec)
  - Previously: brightness showed mode value (e.g., "brightness=1 mode=150")
- Brightness slider unable to reach 100%
  - Removed incorrect brightness=255 "restore" logic that prevented max brightness
  - All brightness values (1-255) now treated as literal brightness levels
- Added spurious status guard to filter incorrect gateway brightness=0 updates
  - Gateway sometimes sends stale `brightness=0 mode=0` status even when light is on
  - Now filtered using `lastKnownDimmableBrightness` tracking

### Improved
- Dimmable light restore-on-ON behavior now works correctly
  - Uses `lastKnownDimmableBrightness` tracking instead of brightness=255 special case
  - Light restores to previous brightness when turned ON after being OFF
- Enhanced logging for debugging brightness issues

## [0.0.3] - 2024-12-21

### Added
- MQTT command subscription and routing
  - Subscribe to `homeassistant/onecontrol/{MAC}/command/#` topics
  - Route commands to plugin's `handleCommand()` method
- Dimmable light control with debouncing
  - 200ms debounce window to coalesce rapid slider changes
  - Prevents UI bouncing during brightness adjustment
- Pending status guard for dimmable lights
  - 12-second window to suppress conflicting gateway status updates
  - Prevents slider from jumping back during command execution
- Restore-on-ON behavior for dimmable lights
  - Tracks last known brightness value
  - Restores previous brightness when light turned ON

### Fixed
- Dimmable light command format (8-byte payload)
- Command routing from MQTT to BLE

## [0.0.2] - 2024-12-20

### Added
- Switch (relay) control via MQTT commands
  - ON/OFF commands work correctly
  - Optimistic state updates
- Enhanced BLE notification handling
  - Fixed notification callback registration
  - Proper stream reading queue

### Fixed
- Switch state parsing (use low nibble of status byte)
- Temperature sensor 0x7FFF handling (invalid/unavailable marker)
- BLE notification callbacks not firing

## [0.0.1] - 2024-12-19

### Added
- Initial plugin architecture
  - `BleDevicePlugin` interface
  - `OneControlDevicePlugin` implementation
  - Plugin owns GATT callback (no forwarding layer)
- OneControl gateway connection
  - BLE scanning and device matching
  - Authentication via Data Service (UNLOCK_STATUS/KEY characteristics)
  - MTU negotiation (185 bytes)
- COBS frame decoding
  - Stream reading from notification queue
  - CRC8 validation
  - Event processing
- MQTT integration
  - Device status publishing
  - Home Assistant auto-discovery
- Device support
  - Switches (relay status)
  - Dimmable lights (status only)
  - Temperature sensors
  - Voltage sensors
  - Tank level sensors
  - Cover/awning status

### Technical
- Android foreground service with wake lock
- Background notification handling
- Heartbeat mechanism (GetDevices every 5s)
- Event-driven architecture

## [Unreleased]

### Planned
- Cover/awning control (OPEN/CLOSE/STOP commands)
- HVAC control
- RGB light support
- Generator control
- Multiple gateway support
- Custom device naming in Home Assistant
- State persistence across restarts

---

## Version History Legend

- **Added** - New features
- **Changed** - Changes in existing functionality
- **Deprecated** - Soon-to-be removed features
- **Removed** - Removed features
- **Fixed** - Bug fixes
- **Security** - Vulnerability fixes
