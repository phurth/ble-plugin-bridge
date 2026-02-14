# RPC over MQTT — Design Plan

## Overview

Add JSON-RPC 2.0 request/response support over MQTT, enabling confirmed command execution and on-demand queries. Currently all MQTT commands are fire-and-forget — the caller publishes a payload and never gets a result back. RPC over MQTT closes that loop.

## Motivation

| Problem today | With RPC |
|---------------|----------|
| BLE write fails silently; HA shows stale state | Caller gets `{"error": {"code": -1, "message": "BLE write failed: GATT status 5"}}` |
| No way to request fresh telemetry on demand | `{"method": "device.read_telemetry"}` → response with current values |
| Plugin diagnostics require ADB or web UI | `{"method": "diag.status"}` → JSON response via MQTT |
| Service control requires physical access | `{"method": "service.restart_plugin", "params": {"id": "hughes_gen2"}}` |
| BLE scan only app-initiated | `{"method": "ble.scan", "params": {"duration": 10}}` → found devices |

## Protocol

Standard **JSON-RPC 2.0** over MQTT (same approach as Shelly Gen2+).

### Topics

```
{prefix}/{baseTopic}/rpc        ← request (HA/caller publishes here)
{prefix}/{baseTopic}/rpc/response  ← response (app publishes here)
```

Global (non-device) RPC:
```
{prefix}/bridge/rpc
{prefix}/bridge/rpc/response
```

### Request Format

```json
{
  "id": 1,
  "method": "Switch.Set",
  "params": { "state": "ON" }
}
```

- `id` — integer or string, echoed in response for correlation
- `method` — dot-separated namespace: `{domain}.{action}`
- `params` — optional object

### Response Format

**Success:**
```json
{
  "id": 1,
  "result": {
    "success": true,
    "ble_status": 0
  }
}
```

**Error:**
```json
{
  "id": 1,
  "error": {
    "code": -32000,
    "message": "BLE write failed",
    "data": { "gatt_status": 5 }
  }
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| -32700 | Parse error (invalid JSON) |
| -32600 | Invalid request (missing method/id) |
| -32601 | Method not found |
| -32602 | Invalid params |
| -32000 | Device not connected |
| -32001 | BLE write failed |
| -32002 | Timeout waiting for device response |
| -32003 | Plugin not loaded |

## Architecture

### New Components

```
core/
  rpc/
    RpcDispatcher.kt        — Parses JSON-RPC, routes to handlers, publishes response
    RpcMethod.kt             — Interface for registerable RPC methods
    RpcError.kt              — Standard error codes and builder
    RpcResponse.kt           — Response builder (result or error)
```

### RpcDispatcher

- Subscribes to `{prefix}/+/rpc` (per-device) and `{prefix}/bridge/rpc` (global)
- Parses incoming JSON-RPC request
- Looks up method in registry → delegates to handler
- Publishes JSON-RPC response to `../rpc/response`
- Timeout handling: if handler doesn't respond within N seconds, send timeout error

### RpcMethod Interface

```kotlin
interface RpcMethod {
    val methodName: String   // e.g., "Switch.Set"
    suspend fun execute(params: JSONObject?): RpcResponse
}
```

### Integration Points

1. **MqttPublisher** — Add `publishRpcResponse(topic, payload)` (or reuse `publishState`)
2. **BleDevicePlugin** — Add optional `fun getRpcMethods(): List<RpcMethod>` (default empty)
3. **BaseBleService** — Initialize `RpcDispatcher`, register global methods, subscribe on connect
4. **Plugin callbacks** — Modify `handleCommand()` return path to propagate GATT write status back through RPC response

### Per-Plugin Methods

#### All BLE Plugins (built-in)
| Method | Description |
|--------|-------------|
| `Device.GetStatus` | Return current connection state, last telemetry timestamp |
| `Device.Ping` | Confirm device is reachable (read a characteristic) |

#### Hughes Gen2
| Method | Description |
|--------|-------------|
| `Switch.Set` | `{"state": "ON"}` — relay control with confirmed GATT write |
| `Switch.GetStatus` | Return current relay state |
| `Energy.Reset` | Reset kWh counter, confirmed |
| `Device.ReadTelemetry` | Force a DLReport read |
| `Device.GetErrors` | Request error history |

#### OneControl
| Method | Description |
|--------|-------------|
| `Light.Set` | `{"id": 3, "state": "ON", "brightness": 128}` — confirmed |
| `HVAC.SetMode` | `{"zone": 1, "mode": "cool", "setpoint": 72}` |
| `Slide.Set` | `{"id": 1, "action": "extend"}` |

#### Global (bridge-level)
| Method | Description |
|--------|-------------|
| `Bridge.GetStatus` | Service state, connected devices, MQTT status |
| `Bridge.RestartPlugin` | `{"plugin_id": "hughes_gen2"}` |
| `Bridge.Scan` | `{"duration": 10}` → list of discovered BLE devices |
| `Bridge.GetLog` | Last N log entries |

## Implementation Phases

### Phase 1 — Core framework (estimated: 1 session)
- `RpcDispatcher`, `RpcMethod`, `RpcError`, `RpcResponse`
- Global topic subscription (`bridge/rpc`)
- `Bridge.GetStatus` as proof-of-concept method
- Response publishing

### Phase 2 — Plugin integration (estimated: 1-2 sessions)
- Add `getRpcMethods()` to `BleDevicePlugin` interface (default empty)
- Per-device topic subscription (`{baseTopic}/rpc`)
- Modify `handleCommand()` to return structured result (not just `Result<Unit>`)
- Wire up `Device.GetStatus` and `Device.Ping` as built-in methods

### Phase 3 — Plugin-specific methods (per plugin, ~1 session each)
- Hughes Gen2: `Switch.Set`, `Energy.Reset`, `Device.ReadTelemetry`
- OneControl: `Light.Set`, `HVAC.SetMode` (leverage existing command handlers)
- Other plugins as needed

### Phase 4 — HA integration (optional)
- Custom HA integration component that uses RPC for confirmed commands
- Or: Node-RED flows that use RPC for automation with error handling

## Migration / Backwards Compatibility

- **Zero breaking changes** — existing `command/#` topics continue to work as-is
- RPC is an **additional** channel, not a replacement
- Plugins that don't implement `getRpcMethods()` simply have no RPC methods available
- The `handleCommand()` path remains for simple fire-and-forget HA switch/light commands

## Configuration

```json
{
  "rpc_enabled": true,
  "rpc_timeout_ms": 10000,
  "rpc_log_requests": true
}
```

Controlled via web UI settings or MQTT config topic.

## Security Considerations

- RPC inherits MQTT broker authentication (username/password, TLS)
- No additional auth layer needed for local-only deployments
- Methods that modify service state (restart, scan) should be gated behind a config flag
- Rate limiting: max N requests/second per topic to prevent flood

---

## Optimistic State & Suppression Window

RPC alone confirms that a BLE write succeeded, but does **not** prevent state bounce. The bounce problem is a state ownership conflict:

1. HA sends `set_temperature 72` → app writes BLE → GATT ACK ✓
2. Before the device processes it, the next telemetry cycle publishes the **old** value (70)
3. HA sees 70, thinks the command failed, UI snaps back to 70
4. User or HA retry sends 72 again → oscillation loop

### Solution: Optimistic State + Telemetry Suppression

When a confirmed write succeeds:

1. **Optimistic publish** — Immediately publish the *commanded* value as state on the relevant MQTT topic (e.g., publish `72` to `target_temp` before the device confirms via telemetry)
2. **Suppression window** — For N seconds after a successful write, suppress incoming telemetry updates for the affected field(s). Let the device "catch up" without overwriting the optimistic state.
3. **Window expiry** — After the suppression window, resume normal telemetry. If the device settled on the commanded value, no visible change. If the device rejected it (e.g., out of range), the corrected value naturally replaces the optimistic one.

### Architecture

```
core/
  state/
    OptimisticStateManager.kt  — Tracks suppressed fields, publishes optimistic values
```

```kotlin
class OptimisticStateManager {
    // field key → expiry timestamp
    private val suppressions = ConcurrentHashMap<String, Long>()

    /**
     * Called when a command write is confirmed successful.
     * Publishes the commanded value immediately and begins suppression.
     */
    fun onCommandConfirmed(
        topic: String,
        commandedValue: String,
        suppressionMs: Long = 5000,
        publisher: MqttPublisher
    ) {
        publisher.publishState(topic, commandedValue, retained = false)
        suppressions[topic] = System.currentTimeMillis() + suppressionMs
    }

    /**
     * Called before publishing telemetry. Returns true if the field
     * is currently suppressed (a recent command is still settling).
     */
    fun isSuppressed(topic: String): Boolean {
        val expiry = suppressions[topic] ?: return false
        if (System.currentTimeMillis() > expiry) {
            suppressions.remove(topic)
            return false
        }
        return true
    }
}
```

### Per-Field Suppression Map

| Plugin | Command | Suppressed Fields | Window |
|--------|---------|-------------------|--------|
| OneControl HVAC | SetMode | `mode`, `action` | 5s |
| OneControl HVAC | SetSetpoint | `target_temp`, `target_temp_high`, `target_temp_low` | 8s |
| OneControl HVAC | SetFanMode | `fan_mode` | 5s |
| OneControl Light | SetBrightness | `brightness`, `state` | 3s |
| OneControl Light | SetRGB | `rgb_color`, `effect` | 3s |
| EasyTouch | SetSetpoint | `target_temp` | 5s |
| EasyTouch | SetMode | `mode` | 5s |
| Hughes Gen2 | SetOpen | `relay` | 3s |

### Guards That Can Be Removed

Once optimistic state + suppression is in place, the following defensive logic can be simplified or removed:

| Current Guard | File | Why It Exists | Removable? |
|--------------|------|---------------|------------|
| `setpointDebounceJob` / 2s debounce | OneControlDevicePlugin | Prevents rapid-fire setpoint writes | Yes — RPC confirms, suppress handles bounce |
| `requireStatusBeforeCommands` | OneControlDevicePlugin | Won't send command until telemetry received | Partially — still useful for initial state, but no longer needed for bounce prevention |
| `antiBounceSuppression` map | OneControlDevicePlugin | Suppresses telemetry after write for N seconds | Yes — replaced by `OptimisticStateManager` (centralized, reusable) |
| `lastCommandedSetpoint` tracking | OneControlDevicePlugin | Detects and discards bounce-back values | Yes — suppression window handles this |
| Temperature defaults rejection | OneControlDevicePlugin | Rejects 0°F commands from uninitialized state | Keep — this is a validity guard, not bounce-related |
| HVAC capability metadata fallback | OneControlDevicePlugin | Guesses heat/cool if metadata missing | Keep — orthogonal to bounce; needs metadata refresh fix |

### Integration with RPC

The optimistic state layer works **with or without** the full RPC framework:

- **Phase 0 (immediate, no RPC needed):** Add `OptimisticStateManager` to existing `handleCommand()` path. When `Result.success`, call `onCommandConfirmed()`. Check `isSuppressed()` before every telemetry publish. This alone fixes the bounce problem with zero HA-side changes.
- **Phase 2+ (with RPC):** RPC response includes the optimistic state in the result payload. HA integration can use this for immediate UI update without waiting for state topic.

### Priority Recommendation

**Phase 0 should be implemented immediately** — it's a standalone ~100-line change that directly fixes the HVAC setpoint bounce and light dimmer bounce problems without requiring the full RPC framework. It can be done in a single session and deployed to the test user.

---

## Metadata Refresh via RPC

### Problem

OneControl device metadata (HVAC capabilities, zone names, device types) is only fetched once during the initial BLE handshake. If the metadata exchange fails or is incomplete (e.g., due to BLE instability during first connection), the plugin operates with missing or stale metadata until the next full reconnect cycle.

### Solution: `OneControl.RefreshMetadata` RPC Method

Add an RPC method that triggers a metadata re-read over BLE:

```json
{
  "id": 42,
  "method": "OneControl.RefreshMetadata",
  "params": {}
}
```

Response (success):
```json
{
  "id": 42,
  "result": {
    "zones_found": 3,
    "devices_found": 24,
    "hvac_capabilities": ["heat", "cool", "auto"],
    "duration_ms": 1200
  }
}
```

### HA Integration: Button Entity

Expose a **button entity** via MQTT discovery that triggers the metadata refresh:

```json
{
  "name": "Refresh Metadata",
  "command_topic": "{prefix}/onecontrol_{mac}/rpc",
  "payload_press": "{\"id\":1,\"method\":\"OneControl.RefreshMetadata\"}",
  "unique_id": "onecontrol_{mac}_refresh_metadata",
  "device": { ... },
  "entity_category": "diagnostic",
  "icon": "mdi:refresh"
}
```

This gives the user a **Refresh Metadata** button in HA's device page under the diagnostic section. Press it → app re-reads metadata over BLE → republishes updated discovery payloads.

### Pre-RPC Alternative

Even before the full RPC framework exists, this can be implemented as a simple MQTT command:

- Subscribe to `{baseTopic}/command/refresh_metadata`
- On receive, trigger the metadata read sequence in the GATT callback
- Publish result to `{baseTopic}/metadata_status`
- Still expose as an HA button entity using `command_topic` directly

This is feasible **right now** with the existing command infrastructure — no RPC framework required.
