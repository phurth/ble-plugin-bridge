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
