# OneControl BLE → MQTT Bridge (Android)

Android foreground service that connects to a Lippert OneControl BLE gateway, authenticates/unlocks it, and bridges device states and commands over MQTT for Home Assistant.

## What it does
- Maintains a stable BLE connection to the OneControl gateway (scan/reconnect, bonding, auth/unlock).
- Subscribes to gateway notifications, decodes MyRvLink events, and tracks device states.
- Publishes Home Assistant MQTT discovery and retained state for:
  - Relay switches
  - Dimmable lights
  - Covers (h-bridge)
  - System voltage sensor
  - Tank sensors (with retained placeholders when empty)
- Accepts MQTT commands from HA for switches, dimmable lights, and covers.
- Provides diagnostic binary sensors for service running, paired, BLE connected, data healthy (recent frames seen), and MQTT connected.
- Kiosk-friendly UI checklist showing connection/health status (minimal logging to UI).

## MQTT topics (prefix `onecontrol/ble`)
- State: `device/<tableId>/<deviceId>/...` (e.g., state, brightness, position)
- Commands: `command/(switch|dimmable|cover)/<tableId>/<deviceId>`
- Diagnostics: `diag/(service_running|paired|ble_connected|data_healthy|mqtt_connected)`

## Configuration notes
- Gateway MAC/PIN/cypher and MQTT broker/creds are currently constants in `OneControlBleService.kt` (make configurable in the future).
- HCI captures and backups are ignored from git; root README is tracked, other markdown docs are ignored.

## Build & run (dev)
1. Open in Android Studio.
2. Set MQTT/gateway constants as needed.
3. Build & deploy to device; service runs in foreground.
4. Watch logcat tag `OneControlBleService` for connection and command flow.

## System requirements
- Android device with BLE and internet connectivity.
- Minimum Android version: API 26 (Android 8.0) per `minSdk` in `app/build.gradle.kts`.
- Target/compile SDK: 34.
- Foreground service permissions/notifications enabled.
- Stable MQTT broker reachable from the device.

## Known/pending
- Covers: discovery published; command handling present but may need real-world validation.
- Tank sensors: retained placeholders; revisit parsing when live data available.
# OneControl BLE Bridge

Android foreground service that bridges a Lippert OneControl BLE gateway to MQTT for Home Assistant. It maintains a BLE connection, authenticates/unlocks, discovers devices, publishes HA discovery/state, and accepts MQTT commands for switches, dimmable lights, and covers.

## Quick start (dev)
1. Open in Android Studio (Java/Kotlin, min SDK per `build.gradle`).
2. Set local MQTT broker/credentials in `OneControlBleService` constants.
3. Build & run on device (foreground service).
4. Watch logcat tag `OneControlBleService` for connection/status.

## MQTT topics
- Prefix: `onecontrol/ble`
- Discovery/state: published automatically for known devices.
- Commands: `onecontrol/ble/command/(switch|dimmable|cover)/<tableId>/<deviceId>`

## Notes
- BLE gateway MAC/PIN/cypher are constants in `OneControlBleService` (make configurable later).
- Data health now tracks recent incoming frames (15s window).
- Diagnostic binary sensors are published for service, pairing, BLE, data, MQTT connectivity.

