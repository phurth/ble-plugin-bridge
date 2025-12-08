# OneControl BLE → MQTT Bridge (Android)

Android foreground service that connects to a Lippert OneControl BLE gateway, authenticates/unlocks it, and bridges device states and commands over MQTT for Home Assistant.

## Initial setup/onboarding
- Onboarding can be rough. The app may crash while requesting permissions. Accept the permissions request and it should launch cleanly.
- Your OneControl unit must be in pairing mode (if yours is already paired with the Android device, it should work without re-pairing needed), then the app will pop up a pairing request.
- After pairing, the app should connect to the OneControl unit and data should (in theory) begiin flowing to MQTT.

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

## To Do
- Investigate adding support for the effects OneControl makes available for some dimmable lights
- Covers (slides & awnings): discovery published; command handling present but completely untested
- Tank sensors: retained placeholders; revisit parsing when live data available.

## MQTT topics (prefix `onecontrol/ble`)
- State: `device/<tableId>/<deviceId>/...` (e.g., state, brightness, position)
- Commands: `command/(switch|dimmable|cover)/<tableId>/<deviceId>`
- Diagnostics: `diag/(service_running|paired|ble_connected|data_healthy|mqtt_connected)`

## Build & run (dev)
1. Open in Android Studio.
2. Build & deploy to device; service runs in foreground.
3. Complete configuration in the app (gateway MAC, MQTT info, etc.).
4. Set the "reliability" options in the app.
5. Watch logcat tag `OneControlBleService` for connection and command flow.

## System requirements
- Android device with BLE and internet connectivity.
- Minimum Android version: API 26 (Android 8.0) per `minSdk` in `app/build.gradle.kts`.
- Target/compile SDK: 34.
- Foreground service permissions/notifications enabled.
- Stable MQTT broker reachable from the device.

## Notes
- Diagnostic binary sensors are published for service, pairing, BLE, data, MQTT connectivity.

