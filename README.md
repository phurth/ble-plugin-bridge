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

