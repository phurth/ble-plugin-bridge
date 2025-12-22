# UI Implementation Summary - v0.0.5-dev

## Overview

Implemented Material Design 3 settings UI with Jetpack Compose, replacing the old MQTT test activity with a complete configuration interface.

## Changes Made

### 1. Build Configuration

**File:** `app/build.gradle.kts`

- Added Jetpack Compose support:
  - `compose = true` build feature
  - Compose BOM 2024.02.00
  - Material 3 components
  - Activity Compose 1.8.2
- Added state management:
  - ViewModel Compose 2.7.0
- Added persistence:
  - DataStore Preferences 1.0.0

### 2. Data Layer

**New File:** `app/src/main/java/com/blemqttbridge/data/AppSettings.kt`

- DataStore-based settings repository
- Reactive Flow-based access to all settings
- Settings included:
  - **MQTT:** enabled, host, port, username, password, topic_prefix
  - **Service:** enabled
  - **OneControl Plugin:** enabled, gateway_mac, gateway_pin
- Default values:
  - MQTT: 10.115.19.131:1883, mqtt/mqtt, prefix "homeassistant"
  - OneControl: MAC 24:DC:C3:ED:1E:0A, PIN 090336

**Removed:** Cypher setting (now hardcoded constant 0x8100080DL)

### 3. ViewModel Layer

**New File:** `app/src/main/java/com/blemqttbridge/ui/viewmodel/SettingsViewModel.kt`

- MVVM pattern for UI state management
- StateFlow exposure of all settings
- Update functions for all settings with automatic service restart
- Expandable section state (collapsed by default)
- Service lifecycle management (start/stop/restart)

### 4. UI Components

**New File:** `app/src/main/java/com/blemqttbridge/ui/components/ExpandableSection.kt`

- Reusable collapsible section component
- Animated expand/collapse with Material 3 design
- Title header with arrow icon
- Content slot for any composable content

**New File:** `app/src/main/java/com/blemqttbridge/ui/SettingsScreen.kt`

- Main settings screen with scrollable layout
- **Common Settings Section:**
  - MQTT Output toggle with expandable broker settings
  - Background Service toggle
- **Permissions Section:**
  - "Request All Permissions" button
  - Lists required permissions
- **Plugins Section:**
  - "Add Plugin" button (disabled placeholder)
  - OneControl Plugin card with expandable gateway settings

**Updated File:** `app/src/main/java/com/blemqttbridge/MainActivity.kt`

- Replaced old TextView-based MQTT test UI
- Now uses Compose with Material 3 theme
- Handles runtime permission requests
- Requests battery optimization exemption

### 5. Backend Updates

**Updated File:** `app/src/main/java/com/blemqttbridge/core/AppConfig.kt`

- Migrated from SharedPreferences to DataStore
- Reads settings via AppSettings Flow API (using `runBlocking` for synchronous access)
- Added topic_prefix support for MQTT config
- Removed deprecated methods (setMqttBroker, resetMqttToDefaults)

**Updated File:** `app/src/main/java/com/blemqttbridge/plugins/onecontrol/OneControlDevicePlugin.kt`

- Removed cypher from config reading
- Cypher now hardcoded as constant (0x8100080DL)
- Updated log message to mask PIN for security

**Updated File:** `app/src/main/java/com/blemqttbridge/receivers/MqttConfigReceiver.kt`

- Updated to use DataStore via AppSettings
- Changed from broker_url to separate broker_host and broker_port
- Added topic_prefix parameter
- Uses coroutines for async DataStore writes

## UI Features

### Material Design 3

- Modern Material 3 components (ElevatedCard, OutlinedTextField, Switch)
- Dynamic color scheme with light/dark theme support
- Proper spacing and typography

### User Experience

- All expandable sections collapsed by default
- Smooth animations for expand/collapse
- Password fields with masking
- Input validation (e.g., port must be integer)
- Settings persist across app restarts via DataStore

### Settings Management

1. **MQTT Configuration:**
   - Enable/disable toggle
   - Expandable broker settings when enabled
   - Separate host and port fields
   - Separate username and password fields
   - Configurable topic prefix

2. **Service Control:**
   - Enable/disable background service
   - Automatic service start/stop on toggle

3. **Plugin Management:**
   - OneControl plugin enable/disable
   - Expandable gateway settings (MAC address, PIN)
   - Service auto-restarts when plugin settings change

4. **Permissions:**
   - One-button request for all required permissions
   - Battery optimization exemption

## Migration Notes

### Breaking Changes

- **AppConfig API:** Removed `setMqttBroker()` and `resetMqttToDefaults()`
  - Use `AppSettings` directly instead
- **MqttConfigReceiver ADB commands:** Updated parameters
  - Old: `--es broker_url "tcp://HOST:PORT"`
  - New: `--es broker_host "HOST" --ei broker_port PORT`
- **Cypher configuration:** No longer user-configurable
  - Hardcoded as 0x8100080DL for all OneControl gateways

### Data Migration

DataStore will use default values on first launch:
- Existing SharedPreferences data will not be migrated
- Users may need to reconfigure MQTT broker settings

## Testing Checklist

- [ ] UI renders correctly with default values
- [ ] Settings persist across app restarts
- [ ] Expandable sections animate smoothly
- [ ] MQTT settings updates reflected in service
- [ ] Plugin settings updates trigger service restart
- [ ] Permission requests work on Android 12+ and 13+
- [ ] Password fields properly masked
- [ ] Service toggle starts/stops background service
- [ ] MQTT topic prefix passed to plugin correctly

## Next Steps

### Immediate Todos

1. Test UI on physical device
2. Verify DataStore persistence
3. Test service restart on setting changes
4. Validate MQTT connection with new settings flow

### Future Enhancements

1. Add validation feedback (e.g., invalid MAC address format)
2. Add connection status indicators (MQTT connected, BLE devices)
3. Add device discovery/enumeration UI
4. Add plugin installation/removal flow
5. Add debug logging toggle
6. Add export/import settings functionality

## Known Limitations

- DataStore reads use `runBlocking` in `AppConfig` (synchronous context requirement)
  - This is acceptable for config initialization but should be refactored if called frequently
- Service restarts on every setting change (could be optimized to batch changes)
- No connection status feedback yet (user doesn't know if MQTT connected)
- No input validation errors shown to user (silent failures)

## File Structure

```
app/src/main/java/com/blemqttbridge/
├── data/
│   └── AppSettings.kt (NEW)
├── ui/
│   ├── MainActivity.kt (UPDATED)
│   ├── SettingsScreen.kt (NEW)
│   ├── components/
│   │   └── ExpandableSection.kt (NEW)
│   └── viewmodel/
│       └── SettingsViewModel.kt (NEW)
├── core/
│   └── AppConfig.kt (UPDATED)
├── plugins/
│   └── onecontrol/
│       └── OneControlDevicePlugin.kt (UPDATED)
└── receivers/
    └── MqttConfigReceiver.kt (UPDATED)
```

## Dependencies Added

```kotlin
// Compose
implementation(platform("androidx.compose:compose-bom:2024.02.00"))
implementation("androidx.compose.ui:ui")
implementation("androidx.compose.material3:material3")
implementation("androidx.compose.ui:ui-tooling-preview")
implementation("androidx.activity:activity-compose:1.8.2")

// ViewModel
implementation("androidx.lifecycle:lifecycle-viewmodel-compose:2.7.0")

// DataStore
implementation("androidx.datastore:datastore-preferences:1.0.0")
```

## Version

This implementation is for **v0.0.5-dev** (in development).
Once tested and validated, will be tagged as v0.0.5.
