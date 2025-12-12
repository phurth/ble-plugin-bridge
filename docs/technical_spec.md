# Technical Specifications – OneControl BLE → MQTT Bridge (Android)

This document summarizes the current Android BLE bridge, the rationale for choosing BLE, and how the app communicates with the OneControl gateway and Home Assistant. Update this as new capabilities land (e.g., HVAC control).

## Why an Android App?
- The production OneControl hardware in scope exposes a BLE **Data Service** (`00000030-0200-a58e-e411-afe28044e62c`) and delivers the MyRvLink stream over BLE notifications. This doc targets that BLE Data Service hardware (no CAN control path available here).
- Earlier attempts:
  - **CAN bus**: We physically tapped the OneControl unity board’s CAN lines and could see state data, but the control path is ciphered. We never recovered that cipher (likely firmware-hardcoded), so writes/commands were blocked.
  - **HACS integration**: Home Assistant/BlueZ attempts were unstable around bonding/auth and notification flow; maintaining a robust BLE stack inside HA proved brittle compared to a dedicated client.
  - **ESPHome/ESP32:** Demonstrated TEA auth and a diagnostic sensor, but could not get notifications subscribed—similar pairing/connection stability issues to the HACS approach; full stream handling, MQTT discovery, and controls were not completed there.
  - **Android BLE to MQTT Gateway App:** An Android foreground service mirrors the official app’s behavior (bonding, key write, CCCD enable, COBS/MyRvLink decode), giving a reliable, stateful client with reconnection, permissions, and MQTT already available.

## Overall Architecture
- **Client**: Android app (foreground service) in Kotlin.
- **Transport**: BLE GATT to OneControl gateway, MTU raised (~185), LE Secure Connections bonding.
- **Protocol**: MyRvLink events/commands COBS-encoded with CRC8, delivered via BLE notifications.
- **Bridge**: Decode events → track device state → publish to MQTT with Home Assistant discovery; consume MQTT commands → encode MyRvLink commands → write over BLE.

## BLE / GATT
- Key observation: the gateway behaves like the official OneControl app expects—bonded LE link, KEY write to unlock, notifications only on the data characteristic. MTU ~185 materially reduces COBS fragmentation; no CAN tunneling is involved on this hardware.
- MTU/bonding: request MTU ≥ 185; require LE Secure Connections bonding. Notifications must be enabled (CCCD = 0x0001) on `00000034`; indications are not used.
- **Services**
  - Auth Service `00000010-0200-a58e-e411-afe28044e62c`
    - Challenge `00000012-...` (seed/unlock status)
    - KEY `00000013-...` (4-byte write, WRITE_NO_RESPONSE; enables “data mode”)
    - Status `00000014-...`
  - Data Service `00000030-0200-a58e-e411-afe28044e62c`
    - Write `00000033-...` (MyRvLink commands, WRITE_NO_RESPONSE)
    - Read/Notify `00000034-...` (MyRvLink events)
- **Startup sequence**
1) Connect and bond; negotiate MTU.  
2) Discover services/characteristics.  
3) Write KEY (`00000013`, 4 bytes) to enter data mode.  
4) Enable notifications on `00000034` (write CCCD `0x2902` = `0x0001`).  
5) Start stream reader consuming notifications only (no active reads).  
6) Begin heartbeat (`GetDevices`) to keep link alive and refresh state.

## MyRvLink framing
- Key observation: every BLE notification is a COBS frame ending with 0x00 and protected by CRC8 (init 0x55). Event byte is first, followed by type-specific payload; commands mirror this with a clientCommandId prefix before COBS/CRC8.
- **Encoding**: COBS with CRC8 (reset value 0x55), 0x00 frame delimiter.
- **Example (structure)**:
  - Raw message: `[payload bytes ...][crc8]`
  - COBS-encode the whole buffer (payload+crc), then append delimiter `0x00`.
  - Decoding: strip trailing `0x00`, COBS-decode, verify CRC8 (init 0x55).
- **Events** (gateway → app): start with `EventType` byte, then type-specific payload. Examples supported:
  - `GatewayInformation` (0x01)
  - `RvStatus` (0x07)
  - `DimmableLightStatus` (0x08)
  - `RelayBasicLatchingStatusType2` (0x06)
  - `RelayHBridgeMomentaryStatusType2` (0x0E)
  - `TankSensorStatusV2` (0x1B)
  - `HourMeterStatus` (0x0F)
  - `Leveler4DeviceStatus` (0x10)
  - `DeviceSessionStatus` (0x1A)
  - `DeviceLockStatus` (0x04)
  - `HvacStatus` (0x0B) — payload matches decompiled LogicalDeviceClimateZoneStatus; control mapping in progress
  - `RealTimeClock` (0x20)
- **Commands** (app → gateway): clientCommandId (LE ushort) + CommandType + payload, then COBS+CRC8 and write to `00000033`. Implemented: `GetDevices`, `ActionSwitch`, `ActionDimmable`; covers and further device types follow same pattern.
- **Command frame structure (example)**:
  - `[clientCommandId_lo][clientCommandId_hi][CommandType][deviceTableId][deviceId][payload...]`
  - Encode this buffer with CRC8 (init 0x55), then COBS, then append `0x00`; write to `00000033` with WRITE_NO_RESPONSE.
- **Dimmable light command example (on/off + brightness)**:
  - Fields: `CommandType=0x43` (ActionDimmable), `deviceTableId`, `deviceId`, payload = single byte brightness 0–100.
  - Example turning light 0x02 on to 75% on table 0x11 with `clientCommandId=0x1234`:
    - Raw: `[0x34][0x12][0x43][0x11][0x02][0x4B]` (`0x4B` = 75)
    - On/off shortcut: use payload `0x64` (100) for on, `0x00` for off when brightness not provided.
    - Full dimmable command variant can carry 8 bytes (brightness, mode, onDuration, blink/swell timings, reserved) but the simple 1-byte form above matches observed behavior and current app usage.

## MQTT and Home Assistant
- Key observation: MQTT is the sole northbound interface; HA discovery is retained so entities persist across restarts. Topics are tableId/deviceId scoped to stay stable regardless of human-friendly names in HA.
- **Broker**: configurable (defaults previously set to `mqtt/mqtt@10.115.19.131:1883`).
- **Topic prefix**: `onecontrol/ble`
- **Discovery**:
  - Device grouping: single HA device “OneControl BLE Gateway” with MAC in connections and `sw_version` set to app version.
  - Entities: dimmable lights, relays/switches, covers (slides/awnings), sensors (system voltage, tanks), diagnostics (service_running, paired, ble_connected, data_healthy, mqtt_connected).
- **State topics**: `onecontrol/ble/device/{tableId}/{deviceId}/...` (state, brightness, position, level, type).
- **Command topics**: `onecontrol/ble/command/(switch|dimmable|cover)/{tableId}/{deviceId}` (and brightness/position where applicable).
- **Retain**: discovery retained; state typically retained for sensors (e.g., tanks) to survive restarts.
- **Discovery/state example (dimmable light)**:
  - Discovery topic: `homeassistant/light/onecontrol_ble/11_02/config`
  - Discovery payload:
    ```json
    {"name":"Dimmable 11:2","uniq_id":"onecontrol_11_2","cmd_t":"onecontrol/ble/command/dimmable/17/2","stat_t":"onecontrol/ble/device/17/2/state","bri_cmd_t":"onecontrol/ble/command/dimmable/17/2/brightness","bri_stat_t":"onecontrol/ble/device/17/2/brightness","dev":{"name":"OneControl BLE Gateway","ids":["onecontrol_ble_gateway"],"sw":"1.0.x"}}
    ```
  - State topics retain last values so HA recovers after restart.

## Authentication / Unlock
- Key observation: unlocking requires a 4-byte KEY write to `00000013` after bonding/MTU. The KEY value differs by gateway/firmware; we currently use a captured value and can extend to derive session keys (TEA-based per decompiled code) if newer gateways demand it. Challenge/status chars (`00000012/00000014`) report success.
- Data Service gateways require a 4-byte KEY write to `00000013` before notifications flow. Values observed vary per session. Current implementation writes a captured value (bundled in app config); extendable to compute session-specific keys (TEA-derived per decompiled OneControl code) if a gateway rejects the static key. Without a successful KEY write, notifications stay silent.
- Challenge/status reads on `00000012/00000014` are used to confirm “Unlocked”.

## Heartbeat / Keepalive
- Periodic `GetDevices` (default 5s) keeps the gateway active and refreshes state; mirrors official app traces. If the heartbeat stops, some gateways slow or stop emitting notifications.
- On reconnect, discovery is republished and retained state is sent for sensors (e.g., tanks) to keep HA consistent. Placeholder tanks were removed—only observed tank IDs are published to avoid duplicates.

## Device Model & Addressing
- Key observation: devices are addressed by `(tableId << 8) | deviceId`. Types are inferred from event types; the bridge maintains last-known status per address and republishes discovery/state so HA remains consistent even after reconnects.
- Device address = `(deviceTableId << 8) | deviceId`.
- Types inferred from incoming event types. Generic naming used; HA users can rename entities.
- Supported device classes:
  - Dimmable lights (brightness 0–100)
  - Relay switches (on/off)
  - H-bridge covers (position/extend-retract) – discovery published; commands present but untested
  - Tank sensors (levels)
  - System/battery voltage, RTC, diagnostics
- HVAC: status decode in progress; control TBD; setpoint/command byte map is captured from decompiled schema and queued for implementation.
- Tanks: raw levels normalize 0x00/0x21/0x42/0x64 to 0/33/66/100%. Only observed tank IDs are discovered/published.
- HVAC (status payload summary): per zone, 11 bytes after the event header: `[deviceId][command][lowTripF][highTripF][status][indoorTempLo][indoorTempHi][outdoorTempLo][outdoorTempHi][dtcLo][dtcHi]`. `command` packs heat mode (bits 0–2), heat source (bits 4–5), fan mode (bits 6–7). `status` encodes zone mode (off/idle/cool/heat variants) and failure bits. Setpoints are Fahrenheit trip points. Command encoding follows the same bit packing when sending control.

## App Components (Android)
- Key observation: the foreground service owns BLE, parsing, state tracking, and MQTT. Event decoding and command building are in dedicated helpers to mirror the decompiled schema; discovery/state publication stays in one place to avoid divergence.
- `OneControlBleService.kt`: GATT lifecycle, notification stream, COBS/MyRvLink decode, device state tracking, MQTT publish/subscribe, command writes.
- `MyRvLinkEventFactory.kt` / `MyRvLinkEventDecoders.kt`: Event decoding.
- `CobsDecoder.kt`: COBS + CRC8 decode.
- `MyRvLinkCommandBuilder.kt`: Command encoding (GetDevices, ActionSwitch, ActionDimmable).
- `DeviceStateTracker.kt`: Maintains latest device states, address mapping `(tableId << 8) | deviceId`.
- `HomeAssistantMqttDiscovery.kt`: HA discovery payloads and device info (includes MAC in connections, app version in `sw_version`).
- UI (`MainActivity.kt` + `activity_main.xml`): Basic controls, diagnostics, version display, log/trace export hooks.

## Logging / Diagnostics
- Key observation: MQTT diagnostics mirror service health (BLE/MQTT/data heartbeat) so HA can surface alerts; logcat remains the detailed source during development, and optional trace export captures raw MyRvLink frames for deep dives.
- MQTT diagnostics binary sensors: service_running, paired, ble_connected, data_healthy (recent frames), mqtt_connected.
- In-app: minimal UI status; detailed logs via logcat tag `OneControlBleService`.
- Optional debug log export/trace capture (raw BLE trace bounded by size/time).

## Known Limitations / Future Work
- HVAC control not yet implemented; status decode underway.
- CAN-path devices not supported on this hardware (Data Service only).
- ESPHome POC remains a separate, incomplete path; Android app is the maintained bridge.
- Lighting effects/RGB control and cover command validation need live testing.
- Investigation of extending the app to include other manufacturers BLE devices (e.g., GoPower solar charge controller, Victron, Mopeka, and others).
