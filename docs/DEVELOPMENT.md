# BLE MQTT Plugin Bridge - Development Guide

## ⚠️ CRITICAL: Agent Instructions

**DO NOT UNINSTALL THE APP** (`adb uninstall com.blemqttbridge`)  
Uninstalling wipes all saved state:
- Plugin configurations and instances (stored in SharedPreferences)
- Granted permissions
- App data and user settings

**Always reinstall with `-r` flag:** `adb install -r <apk>`  and relaunch the app after install
This preserves all data and permissions while updating the binary.

**Exception:** Only uninstall if explicitly instructed: "uninstall the app" or "wipe config"

---

## Project Overview
**BLE MQTT Plugin Bridge** - An Android application that bridges BLE (Bluetooth Low Energy) devices and HTTP polling devices to MQTT with Home Assistant integration. Supports multiple device plugins including solar controllers, thermostats, RV systems, tank sensors, routers, and more. The app runs as a foreground service, manages MQTT connections, and handles BLE device discovery/communication and HTTP device polling.

**Repository:** https://github.com/phurth/ble-plugin-bridge  
**Current Version:** 2.6.0-pre1 (prerelease branch: prerelease-2.6)  
**Min SDK:** Android 8.0 (API 26)  
**Target SDK:** Android 14 (API 34)

## Recent Changes (Jan 2026)
- **Peplink plugin fixes (Jan 29):** Fixed authentication state tracking, added diagnostic binary sensors (API Connected, Authenticated, Data Healthy), added availability topic publishing
- **Service independence:** Web/MQTT/BLE services fully decoupled; polling (HTTP) plugins report health to UI
- **Multi-instance support:** EasyTouch, Mopeka, and Peplink support multiple instances

## Project Structure

### Core Application
- **[app/src/main/java/com/blemqttbridge/core/](app/src/main/java/com/blemqttbridge/core/)**
  - `BaseBleService.kt` - Main foreground service, BLE manager, MQTT coordinator
  - `PluginRegistry.kt` - Plugin discovery and lifecycle management
  - `ServiceStateManager.kt` - Service state persistence
  - `RemoteControlManager.kt` - Remote control command handling

### Plugins (Device Handlers)

**BLE Plugins (Bluetooth Low Energy):**
- **OneControl** (`onecontrol/`) - RV control system (Lippert OneControl)
- **EasyTouch** (`easytouch/`) - RV thermostat (Micro-Air EasyTouch) - Multi-instance support
- **GoPower** (`gopower/`) - Solar charge controller (Go Power! GP-PWM-30)
- **Mopeka** (`mopeka/`) - Tank level sensors (propane, fresh water, gray/black water) - Multi-instance support
- **Hughes Watchdog** (`hughes/`) - Hughes Autoformer surge protector with voltage/current monitoring
- **BLE Scanner** (`blescanner/`) - Generic BLE device discovery and advertising data capture

**HTTP Polling Plugins:**
- **Peplink** (`peplink/`) - Peplink router monitoring (WAN status, cellular, bandwidth, VPN, GPS) - Multi-instance support

**Infrastructure Plugins:**
- **MQTT Output** (`output/`) - MQTT publishing and Home Assistant discovery management

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
- **droidVNC:** Backup remote desktop connectivity

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
- **IDE:** Android Studio (or VS Code with Kotlin extensions)
- **JDK:** Java 17+

## MQTT Broker (Test Environment)
- **Address:** 10.115.19.131:1883
- **Credentials:** mqtt/mqtt
- **Test Subscribe:** `mosquitto_sub -h 10.115.19.131 -p 1883 -u mqtt -P mqtt -t "homeassistant/#"`
- **Test Publish:** `mosquitto_pub -h 10.115.19.131 -p 1883 -u mqtt -P mqtt -t "test/topic" -m "message"`

## Next Steps for New Sessions
1. Check `git log` for recent commits
2. Review [docs/INTERNALS.md](docs/INTERNALS.md) for architecture details
3. Check [app/build.gradle.kts](app/build.gradle.kts) for build config and version
4. Review [app/src/main/java/com/blemqttbridge/core/BaseBleService.kt](app/src/main/java/com/blemqttbridge/core/BaseBleService.kt) for service logic
5. For plugin-specific work, check respective plugin directories and docs/
6. For UI work, check [app/src/main/java/com/blemqttbridge/ui/](app/src/main/java/com/blemqttbridge/ui/)

---
**Last Updated:** January 29, 2026
