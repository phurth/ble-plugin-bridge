# Claude.md - BLE MQTT Plugin Bridge Project Context

## ⚠️ CRITICAL: Agent Instructions

**DO NOT UNINSTALL THE APP** (`adb uninstall com.blemqttbridge`)  
Uninstalling wipes all saved state:
- Plugin configurations and instances (stored in SharedPreferences)
- Granted permissions
- App data and user settings

**Always reinstall with `-r` flag:** `adb install -r <apk>`  
This preserves all data and permissions while updating the binary.

**Exception:** Only uninstall if explicitly instructed: "uninstall the app" or "wipe config"

---

## Project Overview
**BLE MQTT Plugin Bridge** - An Android application that bridges BLE (Bluetooth Low Energy) devices to MQTT, supporting multiple device plugins (GoWatt/GoPower, EasyTouch, OneControl). The app runs as a foreground service, manages MQTT connections, and handles BLE device discovery/communication.

**Repository:** https://github.com/phurth/ble-plugin-bridge  
**Current Version:** 2.6.0-pre1 (prerelease)  
**Min SDK:** Android 8.0 (API 26)  
**Target SDK:** Android 14 (API 34)

## Recent Changes (as of Jan 26, 2026)

- **Service independence + Peplink polling fixes (branch: service-independence)**
  - Web/MQTT/BLE services fully decoupled; web server can run solo
  - Polling (HTTP) plugins now report health to the UI via BaseBleService
  - Web status API reads dedicated polling status flow
  - Polling auto-start retries up to 3x if MQTT isn’t ready yet
  - Debug log export now includes polling plugin statuses
  - Peplink plugin: status surfaces correctly in UI; auto-start works after retries
  - Peplink branch merged into service-independence; remote branch peplink-plugin closed

- **v2.5.13:** Service startup fix + enhanced state visibility
  - Fixed: app would skip BLE auto-start when launched via MainActivity
  - Added StateFlows: `bleScanningActive`, `bluetoothAvailable`; UI 3-state indicator
  - See: [RELEASE_NOTES_v2.5.13.md](RELEASE_NOTES_v2.5.13.md)

- **v2.5.6.1:** Patch release fixing compilation bug in v2.5.6
  - Extra closing brace in MqttOutputPlugin.kt prevented debug builds

- **v2.5.6:** Multi-instance plugin support + comprehensive testing
  - Multi-instance data model and HTTP endpoints; EasyTouch supports multi-instance

- **v2.5.3:** Enabled debug logging in release builds (`FORCE_DEBUG_LOG` flag added)

## Project Structure

### Core Application
- **[app/src/main/java/com/blemqttbridge/core/](app/src/main/java/com/blemqttbridge/core/)**
  - `BaseBleService.kt` - Main foreground service, BLE manager, MQTT coordinator
  - `PluginRegistry.kt` - Plugin discovery and lifecycle management
  - `ServiceStateManager.kt` - Service state persistence
  - `RemoteControlManager.kt` - Remote control command handling

### Plugins (Device Handlers)
- **GoWatt/GoPower** - `[app/src/main/java/com/blemqttbridge/plugins/gopower/](app/src/main/java/com/blemqttbridge/plugins/gopower/)`
- **EasyTouch** - `[app/src/main/java/com/blemqttbridge/plugins/easytouch/](app/src/main/java/com/blemqttbridge/plugins/easytouch/)`
- **OneControl** - `[app/src/main/java/com/blemqttbridge/plugins/onecontrol/](app/src/main/java/com/blemqttbridge/plugins/onecontrol/)`
- **BLE Scanner** - `[app/src/main/java/com/blemqttbridge/plugins/blescanner/](app/src/main/java/com/blemqttbridge/plugins/blescanner/)` - Device discovery plugin
- **MQTT Output** - `[app/src/main/java/com/blemqttbridge/plugins/output/](app/src/main/java/com/blemqttbridge/plugins/output/)`

### Web Interface (v2.5.5+)
- **[app/src/main/java/com/blemqttbridge/web/](app/src/main/java/com/blemqttbridge/web/)**
  - `WebServerManager.kt` - Embedded web server (NanoHTTPD), REST API, HTML/JS UI
  - `WebServerService.kt` - Foreground service hosting web server
  - **Port:** 8088 (hardcoded)
  - **Features:** Plugin management, MQTT config editing, plugin configuration
  - **Access:** http://<device_ip>:8088 (no authentication currently)

### UI & ViewModels
- **[app/src/main/java/com/blemqttbridge/ui/](app/src/main/java/com/blemqttbridge/ui/)**
  - `SystemSettingsScreen.kt` - Debug tools, BLE trace, device management
  - `SettingsViewModel.kt` - Settings state management
  - Compose-based Material 3 UI

### Utilities
- **[app/src/main/java/com/blemqttbridge/util/DebugLog.kt](app/src/main/java/com/blemqttbridge/util/DebugLog.kt)** - Conditional debug logging (respects BuildConfig.DEBUG, FORCE_DEBUG_LOG, or trace active)
- **[app/src/main/java/com/blemqttbridge/core/interfaces/](app/src/main/java/com/blemqttbridge/core/interfaces/)** - Plugin APIs (MqttPublisher, DevicePlugin, etc.)

## Build & Deployment

### Quick Deploy to Test Device
**Recommended Method:** Use the automated deployment script:
```bash
./scripts/install-dev.sh
```

This script:
- Builds debug APK (`./gradlew assembleDebug`)
- Installs with `-r` flag (preserves all data and permissions)
- Force-stops the app to ensure clean restart
- Launches the app (`com.blemqttbridge/.MainActivity`)
- Verifies app started successfully

**Prerequisites:**
- Device connected: `adb connect 10.115.19.214:5555`
- Script is executable: `chmod +x scripts/install-dev.sh`

### Manual Build Commands
```bash
# Debug build
./gradlew assembleDebug

# Release build
./gradlew assembleRelease

# Run tests
./gradlew test
```

### Manual Deployment to Test Device
```bash
# Build and install (preserves data)
./gradlew assembleDebug
adb -s 10.115.19.214:5555 install -r app/build/outputs/apk/debug/app-debug.apk

# Launch app
adb -s 10.115.19.214:5555 shell am start -n com.blemqttbridge/.MainActivity
```

### APK Output
- Debug: `app/build/outputs/apk/debug/app-debug.apk`
- Release: `app/build/outputs/apk/release/app-release.apk` (12MB)

### Release Process
1. Update version in [app/build.gradle.kts](app/build.gradle.kts) (versionCode + versionName)
2. Install on the test device and relaunch
3. Commit & push to main and to remote
4. Close any related issues - if you don't see any, stop and ask
5. Build release APK: `./gradlew assembleRelease`
6. Create GitHub release: `gh release create vX.X.X app/build/outputs/apk/release/app-release.apk`

## Diagnostic Features

### Debug Logging
- **DebugLog.d()** / **DebugLog.v()** - Only output when:
  - BuildConfig.DEBUG = true (debug build), OR
  - BuildConfig.FORCE_DEBUG_LOG = true (release builds since v2.5.3), OR
  - BLE trace is active
- **DebugLog.i/w/e()** - Always output (warnings/errors)

### BLE Trace Logging
- **UI:** System Settings → "Start BLE Trace & Send"
- **File Location:** `/sdcard/Android/data/com.blemqttbridge/files/traces/`
- **Features:** Captures all BLE events (connections, reads, writes, notifications)
- **Limits:** 5 min timeout or 5MB max size
- **Export:** Share dialog opens when trace stops

### Debug Log Export
- **UI:** System Settings → "Export Debug Log"
- **Content:** Ring buffer of last 500 lines
- **File Location:** `/sdcard/Android/data/com.blemqttbridge/files/`

## Test Device Setup

### Test Device
- **IP:** 10.115.19.214
- **Wireless ADB:** Port 5555
- **Status:** Connected via wireless ADB pairing
- **droidVNC:** Backup connectivity to test device
- **SSH Access:** `ssh -p 8022 u0_a134@10.115.19.214` (Termux with openssh + termux-services)
  - Auth: Ed25519 key-based authentication
  - Service: Managed by runit (sv-enabled sshd, auto-starts on reboot)
  - Purpose: Remote deployment and debugging when away from RV

### ADB Commands
```bash
# Kill & restart ADB server
adb kill-server && adb start-server

# Pair (requires pairing code from device)
adb pair 10.115.19.214:<pairing_port>

# Connect
adb connect 10.115.19.214:5555

# List devices
adb devices -l

# Force stop app & restart
adb shell am force-stop com.blemqttbridge
adb shell monkey -p com.blemqttbridge 1

# Force stop droidVNC (if needed)
adb shell am force-stop net.christianbeier.droidvnc_ng
```

## Known Issues & Regressions
- **Currently tracking:** Regressions to be fixed (user mentioned Jan 12)
- **BuildConfig.DEBUG:** Release builds had logging disabled; fixed in v2.5.3

## Future Enhancements
### Web Interface Improvements
- **Configurable Port:** Make web server port configurable (currently hardcoded to 8088)
  - Add to AppSettings and ServiceStateManager
  - Add UI control in System Settings screen
  - Restart WebServerService when port changes
  - Validate port range (1024-65535)
- **Authentication:** Add optional HTTP authentication to web interface
  - Username/password configuration
  - Session management
  - Consider basic auth vs. digest auth vs. token-based
  - Store credentials securely (Android Keystore or encrypted DataStore)
  - Add "Enable Web Auth" toggle in settings
- **Additional Context:**
  - Current web UI is completely open (no auth) - accessible to anyone on network
  - Port 8088 may conflict with other services on some networks
  - These features would enable safer deployment in multi-user environments
  - Consider adding HTTPS support in future (self-signed certs or Let's Encrypt)

## Key Dependencies
- **Kotlin Coroutines** - 1.9.0
- **Compose UI** - androidx.compose:compose-bom:2024.02.00
- **MQTT** - Eclipse Paho (org.eclipse.paho:*)
- **DataStore** - Preferences persistence
- **Android Gradle Plugin** - 8.4 (uses Gradle 8.4)

## Important Documentation
- **[docs/](docs/)** - Architecture and implementation docs
  - `INTERNALS.md` - Internal architecture details
  - `CONDITIONAL_ONBOARDING_IMPLEMENTATION.md` - Onboarding flow
  - `PLANNING_CONNECTION_POOLING.md` - Connection pooling plans
  - **Plugin-specific documentation:** Each plugin has its own subdirectory:
    - `docs/easytouch_plugin_docs/` - EasyTouch thermostat plugin docs
    - `docs/gopower_plugin_docs/` - GoPower solar controller plugin docs
    - `docs/onecontrol_plugin_docs/` - OneControl RV system plugin docs
    - `docs/mopeka_plugin_docs/` - Mopeka tank sensor plugin docs
    - `docs/hughes_plugin_docs/` - Hughes Autoformer watchdog plugin docs
    - `docs/peplink_plugin_docs/` - Peplink router plugin docs
    - `docs/gmg_plugin_docs/` - GMG pellet grill plugin docs
- **[scripts/](scripts/)** - Deployment and testing utilities
  - `install-dev.sh` - Automated build and deploy to test device
  - `configure-mqtt.sh` - MQTT configuration helper
  - `test-adb-control.sh` - ADB connectivity testing

## Git Workflow
- **Main branch:** Production releases
- **Commits:** Use descriptive messages
- **Releases:** Created via GitHub CLI (`gh release create`)

## Development Environment
- **Language:** Kotlin
- **Target:** Android (minSdk 26, targetSdk 34)
- **Build System:** Gradle with Kotlin DSL
## MQTT Broker (Test Environment)
- **Address:** 10.115.19.131:1883
- **Credentials:** mqtt/mqtt
- **Test Subscribe:** `mosquitto_sub -h 10.115.19.131 -p 1883 -u mqtt -P mqtt -t "homeassistant/#"`
- **Test Publish:** `mosquitto_pub -h 10.115.19.131 -p 1883 -u mqtt -P mqtt -t "test/topic" -m "message"`

---
**Last Updated:** January 29

## Next Steps for New Sessions
1. Check `git log` for recent commits
2. Review [docs/INTERNALS.md](docs/INTERNALS.md) for architecture details
3. Check [app/build.gradle.kts](app/build.gradle.kts) for build config and version
4. Review [app/src/main/java/com/blemqttbridge/core/BaseBleService.kt](app/src/main/java/com/blemqttbridge/core/BaseBleService.kt) for service logic
5. For plugin-specific work, check respective plugin directories
6. For UI work, check [app/src/main/java/com/blemqttbridge/ui/](app/src/main/java/com/blemqttbridge/ui/)

---
**Last Updated:** January 18, 2026
