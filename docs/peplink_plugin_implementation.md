# Peplink Plugin Implementation Summary

**Date:** January 26, 2026
**Branch:** `peplink-plugin`
**Test Device:** HGR507AP (USB ADB connection)
**Router:** Peplink MAX BR1 Pro at 192.168.1.1

## Overview

Implementing HTTP/REST polling plugin architecture for Peplink routers in the BLE-MQTT Bridge Android app. This is the first polling (non-BLE) plugin, requiring new infrastructure for HTTP-based device integrations.

## Current Status

**Phase 2 Implementation - Nearly Complete**

### ✅ What's Working
- Plugin can be added via web UI with OAuth credentials
- HTTP requests work (cleartext traffic allowed)
- Discovery payloads publish to Home Assistant
- Most entities showing actual values in Home Assistant:
  - WAN Priority values
  - IP addresses
  - Uptime values
- Remove button works for polling plugins

### ⚠️ Current Issues
1. **Polling doesn't auto-start** - Just implemented polling state persistence (untested)
2. **Some entities show "Unknown"** - Cellular signal and status entities not working
3. **Need to investigate** - Why cellular/status values aren't being published correctly

## Implementation Details

### Architecture

**Polling Plugin System:**
- Separate from BLE plugins (uses REST APIs instead of Bluetooth)
- Managed by `PluginRegistry` with `pollingPluginFactories` map
- Instances stored in SharedPreferences via `ServiceStateManager`
- Web UI controls starting/stopping polling independently from BLE service

**Key Classes:**
- `PeplinkPlugin.kt` - Main plugin implementation (implements `PollingDevicePlugin`)
- `PeplinkApiClient.kt` - OAuth2 + REST API client for Peplink
- `PeplinkDiscovery.kt` - Hardware discovery (detects WAN types)
- `WebServerManager.kt` - Web UI endpoints for polling control

### MQTT Integration

**Topic Structure:**
```
homeassistant/peplink/{instance_name}/wan/{conn_id}/{metric}
```

**Example Topics:**
```
homeassistant/peplink/pepwave_max_br1_pro/wan/1/status → "connected"
homeassistant/peplink/pepwave_max_br1_pro/wan/1/priority → "1"
homeassistant/peplink/pepwave_max_br1_pro/wan/1/cellular/signal → "-87"
```

**Home Assistant Discovery:**
- Binary Sensor: WAN connection status
- Sensors: Priority, IP, Uptime, Signal (cellular), Carrier (cellular), Network Type (cellular)
- Select: Priority control (1-4)
- Button: Cellular modem reset

## Issues Fixed During Implementation

### Issue 1: Remove Button Not Working for Polling Plugins
**Problem:** Remove button called `/api/instances/remove` for all plugins
**Fix:** Added `instanceToRemoveIsPolling` flag, route to `/api/polling/instances/remove` for polling plugins
**Files:** `web_ui_js.js`

### Issue 2: Missing http:// Scheme
**Problem:** User entered "192.168.1.1", OkHttp requires "http://192.168.1.1"
**Fix:** JavaScript auto-adds "http://" prefix if missing (done in previous session)

### Issue 3: Android Blocking HTTP Traffic
**Problem:** "CLEARTEXT communication to 192.168.1.1 not permitted by network security policy"
**Fix:** Created `network_security_config.xml` allowing cleartext for all connections
**Files:**
- `app/src/main/res/xml/network_security_config.xml` (new)
- `app/src/main/AndroidManifest.xml` (added networkSecurityConfig reference)

### Issue 4: App Crash - JSONObject Doesn't Accept Collections
**Problem:** `NoSuchMethodError: put(Ljava/lang/String;Ljava/util/Collection;)`
**Location:** `PeplinkPlugin.kt:233`
**Fix:** Wrap Lists in `JSONArray()` before putting in JSONObject
```kotlin
put("identifiers", JSONArray(listOf(instanceId)))  // Was: JSONObject().put("0", instanceId)
put("options", JSONArray(listOf("1", "2", "3", "4")))  // Was: listOf(...)
```

### Issue 5: outputPlugin is null
**Problem:** "❌ outputPlugin is null, cannot publish discovery/state"
**Root Cause:** WebServerManager held stale reference to BaseBleService from before service started
**Fix:** Modified WebServerManager to get service reference dynamically via `getService(): BaseBleService?`
**Files:**
- `WebServerManager.kt` - Removed service parameter, added dynamic getter
- `WebServerService.kt` - Stopped passing service parameter

### Issue 6: Service Won't Start with Only Polling Plugins
**Problem:** Service checked for BLE instances and returned early if none found
**Fix:** Check both BLE and polling instances; keep service running for MQTT if polling plugins exist
**File:** `BaseBleService.kt` lines 663-679

### Issue 7: Null Values Not Being Published
**Problem:** When WAN connections had null fields, nothing was published (using `?.let`)
**Fix:** Always publish values, use empty string or "0" for nulls
**File:** `PeplinkPlugin.kt` lines 377-387

### Issue 8: MQTT Topic Prefix Mismatch
**Problem:** Discovery configs didn't include "homeassistant/" prefix, but actual state did
**Example:**
- Discovery said: `state_topic = "peplink/pepwave_max_br1_pro/wan/1/status"`
- Actual state: `homeassistant/peplink/pepwave_max_br1_pro/wan/1/status"`
**Fix:** Get topic prefix from `mqttPublisher.topicPrefix`, create `fullBaseTopic` with prefix included
**File:** `PeplinkPlugin.kt` lines 176-184

### Issue 9: Polling State Doesn't Persist Through Restarts
**Problem:** "HTTP Polling Plugins" toggle doesn't retain state when app restarts
**Fix:** Added polling state persistence (JUST IMPLEMENTED - UNTESTED)
**Changes:**
- Added `POLLING_ENABLED` key to AppSettings (DataStore)
- Added `pollingEnabled` Flow and `setPollingEnabled()` setter
- Modified `handlePollingControlStart()` to save state when polling starts
- Modified `handlePollingControlStop()` to save state when polling stops
- Added `autoStartPolling()` to WebServerManager
- Modified `WebServerService.startWebServer()` to call autoStartPolling if enabled

**Files Modified:**
- `app/src/main/java/com/blemqttbridge/data/AppSettings.kt`
- `app/src/main/java/com/blemqttbridge/web/WebServerManager.kt`
- `app/src/main/java/com/blemqttbridge/web/WebServerService.kt`

## Outstanding Issues

### Issue 10: Cellular Signal and Status Still Show "Unknown"
**Symptoms:** Most entities work, but cellular signal and status entities show "Unknown" in Home Assistant
**Investigation Needed:**
1. Check if polling is actually running after the persistence fix
2. Verify cellular data is being retrieved from API
3. Check if status values are being published correctly
4. Compare working vs. non-working entities to find pattern

**Possible Causes:**
- Cellular data might not be populated from API response
- Status enum mapping might be incorrect
- Topic mismatch for these specific entities
- Cellular data only published when `connection.cellular?.let` block executes

**Debug Steps:**
1. Turn on "HTTP Polling Plugins" toggle in web UI
2. Check logcat for PeplinkPlugin polling activity
3. Check MQTT Explorer for published topics
4. Verify what data is coming from router API

## Testing Checklist

- [ ] Toggle "HTTP Polling Plugins" ON in web UI
- [ ] Verify polling starts (check logs for "Auto-started polling")
- [ ] Restart app completely
- [ ] Verify "HTTP Polling Plugins" toggle is still ON
- [ ] Verify polling auto-starts on app launch
- [ ] Check Home Assistant for cellular signal values
- [ ] Check Home Assistant for status values
- [ ] Test priority switching command
- [ ] Test cellular modem reset command

## File Locations

### Plugin Implementation
```
app/src/main/java/com/blemqttbridge/plugins/peplink/
├── PeplinkPlugin.kt          # Main plugin (polled every 30s)
├── PeplinkApiClient.kt       # OAuth2 + REST client
├── PeplinkDiscovery.kt       # Hardware detection
└── PeplinkDataModels.kt      # Data classes for API responses
```

### Core Infrastructure
```
app/src/main/java/com/blemqttbridge/core/
├── PluginRegistry.kt         # Plugin factory management
├── ServiceStateManager.kt    # Instance persistence (SharedPreferences)
└── interfaces/
    └── PollingDevicePlugin.kt  # Interface for polling plugins
```

### Web UI
```
app/src/main/java/com/blemqttbridge/web/
├── WebServerManager.kt       # REST API + polling control
└── WebServerService.kt       # Foreground service hosting web server

app/src/main/res/raw/
├── web_ui_html.html          # Web UI layout
├── web_ui_css.css            # Web UI styles
└── web_ui_js.js              # Web UI logic (polling controls)
```

### Configuration
```
app/src/main/res/xml/
└── network_security_config.xml  # Allows HTTP cleartext traffic

app/src/main/AndroidManifest.xml  # References network security config
```

### Data Layer
```
app/src/main/java/com/blemqttbridge/data/
└── AppSettings.kt            # DataStore preferences (includes pollingEnabled)
```

## Configuration Example

**OAuth Credentials** (from router's InControl2 settings):
```json
{
  "base_url": "http://192.168.1.1",
  "client_id": "06954c6dddfa68fea1928eb80164b380",
  "client_secret": "91750f790202125a04de4955b832d6ee",
  "polling_interval": "30",
  "instance_name": "pepwave_max_br1_pro"
}
```

## API Endpoints Used

**Peplink Router API:**
- `POST /api/auth/token` - OAuth2 token acquisition
- `GET /api/status.wan.connection?id=1+2+3+4+5+6+7+8+9+10` - WAN status query
- `PUT /api/config.wan.connection.X.priority` - Set WAN priority
- `POST /api/command.device.modem.reset` - Reset cellular modem

**Web UI Polling Control:**
- `GET /api/polling/status` - Get polling service status
- `GET /api/polling/instances` - Get all polling plugin instances
- `POST /api/polling/control/start` - Start all polling plugins
- `POST /api/polling/control/stop` - Stop all polling plugins
- `POST /api/polling/instances/add` - Add new polling plugin instance
- `POST /api/polling/instances/remove` - Remove polling plugin instance

## Key Code Patterns

### Publishing State with Null Safety
```kotlin
// Always publish values, use empty string or "0" for nulls
val priority = connection.priority?.toString() ?: ""
mqttPublisher.publishState("$baseTopic/wan/$connId/priority", priority)

val ip = connection.ip ?: ""
mqttPublisher.publishState("$baseTopic/wan/$connId/ip", ip)

val uptime = connection.uptime?.toString() ?: "0"
mqttPublisher.publishState("$baseTopic/wan/$connId/uptime", uptime)
```

### Discovery Payload with Topic Prefix
```kotlin
val topicPrefix = mqttPublisher.topicPrefix  // "homeassistant"
val baseTopic = getMqttBaseTopic()  // "peplink/pepwave_max_br1_pro"
val fullBaseTopic = "$topicPrefix/$baseTopic"  // "homeassistant/peplink/pepwave_max_br1_pro"

payloads.add(
    "homeassistant/binary_sensor/${uniqueId}_status/config" to JSONObject().apply {
        put("name", "${connection.name} Status")
        put("unique_id", "${uniqueId}_status")
        put("state_topic", "$fullBaseTopic/wan/$connId/status")  // Includes prefix!
        put("device", deviceInfo)
    }.toString()
)
```

### Dynamic Service Reference
```kotlin
// WebServerManager.kt
private fun getService(): BaseBleService? = BaseBleService.getInstance()

// Use dynamically instead of storing reference
val mqttPublisher = getService()?.getMqttPublisher() ?: return error()
```

## Next Steps

1. **Test polling persistence** - Verify toggle stays ON after restart
2. **Investigate cellular/status Unknown values:**
   - Enable polling
   - Check logs for polling activity
   - Check MQTT topics being published
   - Verify API response data
3. **Test command functionality:**
   - Priority switching
   - Cellular modem reset
4. **Code cleanup:**
   - Add logging for cellular data publishing
   - Verify all edge cases handled
5. **Documentation:**
   - Update CLAUDE.md with Peplink plugin info
   - Create release notes for v2.6.0
6. **Merge to main** when all working

## Build & Deploy

```bash
# Build debug APK
./gradlew assembleDebug

# Install to USB device (preserves data)
adb -s HGR507AP install -r app/build/outputs/apk/debug/app-debug.apk

# View logs
adb -s HGR507AP logcat -d | grep "PeplinkPlugin"

# Clear logs and watch
adb -s HGR507AP logcat -c && adb -s HGR507AP logcat | grep -E "PeplinkPlugin|polling"
```

## Important Notes

- **DO NOT uninstall app** - Use `adb install -r` to preserve SharedPreferences data
- **Network security** - HTTP cleartext allowed for all connections (appropriate for IoT app)
- **Polling interval** - Currently 30 seconds (configurable per instance)
- **MQTT dependency** - Polling plugins require BaseBleService running for MQTT access
- **Web UI port** - Hardcoded to 8088 (should be configurable in future)
- **Authentication** - Currently no auth required for router API (uses OAuth client credentials)

## Git Branch Status

**Current Branch:** `peplink-plugin`
**Base Branch:** `main`
**Commits:** Multiple commits on peplink-plugin branch (needs cleanup before merge)
**Remote:** Pushed to origin/peplink-plugin

**Before Merging:**
1. Complete testing of all features
2. Fix cellular/status Unknown issue
3. Squash commits into logical units
4. Update version number in build.gradle.kts
5. Create release notes
6. Merge to main
7. Build release APK
8. Create GitHub release
