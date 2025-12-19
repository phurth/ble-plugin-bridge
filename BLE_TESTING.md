# BLE Service End-to-End Testing

## Current Status
- ✅ MQTT plugin validated (real broker 10.115.19.131:1883)
- ✅ Plugin infrastructure unit tested (18 tests passing)
- ⚠️ BLE scanning **not yet tested with real devices**
- ✅ Diagnostic UI installed on device

## What We Have

### 1. ServiceStatusActivity (Diagnostic UI)
**Location**: Launcher activity (opens on app start)

**Shows**:
- Bluetooth adapter status
- Permission status (BLE, Location, Notifications)
- Location services status (required for BLE scanning)
- Loaded plugins
- Memory usage

**Controls**:
- **Start Service**: Begins BLE scanning with MockBatteryPlugin
- **Stop Service**: Stops scanning
- **Refresh**: Updates status display

### 2. MockBatteryPlugin
**Matches devices**: Any BLE device with name starting with "MockBattery"

**Since we don't have matching hardware**, the plugin won't load during scanning.

## Testing Approaches

### Option 1: BLE Simulator (Recommended for Infrastructure Testing)
Use **nRF Connect** app on another Android device to advertise as a BLE peripheral:

1. Install nRF Connect on second Android device
2. Tap "Advertiser" tab
3. Create advertisement:
   - Device Name: `MockBatteryTest`
   - Add Service: `0000FF00-0000-1000-8000-00805F9B34FB` (generic)
   - Add Characteristic (optional): `0000FF01-...` with Notify property
4. Start advertising
5. On test device:
   - Open BLE Plugin Bridge app
   - Tap "Start Service"
   - Watch logcat: `adb logcat -s BaseBleService PluginRegistry MockBatteryPlugin`
   
**Expected behavior**:
```
BaseBleService: Starting BLE scan
BaseBleService: Found device: MockBatteryTest (XX:XX:XX:XX:XX:XX)
PluginRegistry: MockBatteryPlugin.canHandleDevice() = true
PluginRegistry: Loading plugin: mock_battery
MockBatteryPlugin: Loaded for device MockBatteryTest
BaseBleService: Connecting to MockBatteryTest
MockBatteryPlugin: Connected to MockBatteryTest
```

### Option 2: Real Hardware Testing (Deferred to Phase 3)
**Wait for OneControl migration**, then test with real RV hardware:
- Micro-Air thermostat (OneControl device)
- Will scan for real service UUID
- Full protocol validation
- Zero-regression requirement ensures thorough testing

### Option 3: Skip to Phase 3
Since MockBatteryPlugin is just a test implementation, we could:
1. Skip BLE hardware testing for now
2. Proceed directly to **OneControl migration**  
3. Test with real RV hardware (comprehensive validation)
4. EasyTouch plugin follows same pattern

## What to Test

### Permissions Flow
1. Fresh install (no permissions granted)
2. Open app → ServiceStatusActivity shows ❌ for all permissions
3. Tap "Start Service" → Should prompt for permissions
4. Grant all permissions
5. Enable Location Services (Android Settings)
6. Tap "Refresh" → Should show ✅ for all permissions
7. Tap "Start Service" → Service should start

### Logs to Watch
```bash
# On Mac:
adb logcat -s BlePluginBridgeApp BaseBleService PluginRegistry MockBatteryPlugin MqttOutputPlugin

# Expected on app start:
BlePluginBridgeApp: Application starting - registering plugins
BlePluginBridgeApp: Plugin registration complete

# Expected when "Start Service" tapped:
BaseBleService: Service created
BaseBleService: onStartCommand: com.blemqttbridge.START_SCAN
BaseBleService: Initializing plugins...
PluginRegistry: Loading BLE plugin: mock_battery
PluginRegistry: Loading output plugin: mqtt
MqttOutputPlugin: Connecting to broker...
BaseBleService: Starting BLE scan
```

### Known Limitations (Current Phase)
1. **No matching BLE devices**: MockBatteryPlugin won't find hardware
2. **MQTT config hardcoded**: Broker details in code, not UI configurable yet
3. **No device list UI**: Can't see discovered devices (logs only)
4. **No connection status UI**: Service runs in background silently

### Success Criteria for Phase 2
- ✅ App builds and installs
- ✅ Permissions can be granted
- ✅ Service starts without crashing
- ✅ BLE scanning initiates
- ✅ Plugins register correctly
- ❓ Plugin loads when matching device found (needs BLE simulator or Phase 3)

## Next Steps

### Immediate (Optional Infrastructure Validation):
```bash
# 1. Start the app on device
adb shell am start -n com.blemqttbridge/.ui.ServiceStatusActivity

# 2. Watch logs
adb logcat -s BlePluginBridgeApp BaseBleService PluginRegistry

# 3. Tap "Start Service" in app
# 4. Verify no crashes, service starts

# 5. Check for BLE scanning:
adb logcat -s BluetoothAdapter BluetoothLeScanner
```

### Critical Path (Recommended):
**Proceed to Phase 3: OneControl Migration**

Reasons:
- MockBatteryPlugin is artificial (no real hardware)
- OneControl plugin will validate entire stack with real devices
- Real-world RV hardware is ultimate validation
- Zero-regression requirement forces comprehensive testing
- Same patterns apply to EasyTouch plugin

## Files Created for Testing
- `app/src/main/java/com/blemqttbridge/ui/ServiceStatusActivity.kt` (273 lines)
- `app/src/main/res/layout/activity_service_status.xml` (61 lines)
- `app/src/main/java/com/blemqttbridge/BlePluginBridgeApplication.kt` (37 lines)

## Current Build Status
```
BUILD SUCCESSFUL in 6s
Installed on 1 device (TB300FU - 13)
```

App ready to test!
