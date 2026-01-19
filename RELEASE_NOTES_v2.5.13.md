# Release Notes - v2.5.13

**Version**: 2.5.13 (versionCode 46)  
**Release Date**: January 18, 2026  
**Type**: Bug fix + enhancements

## Summary

This release fixes a critical service startup issue where the app would fail to auto-start BLE scanning when launched via the main UI (MainActivity). It also adds enhanced state visibility to help diagnose Bluetooth and scanning state issues.

## Root Cause Analysis

**Issue**: When the app was launched manually (not from boot), plugins would fail to connect despite the service toggle showing "ON".

**Technical Details**:
- MainActivity sends `START_WEB_SERVER` action to BaseBleService
- v2.5.12 had no handler for this action, so it fell through to the `null` action case
- The `null` case only scheduled keepalive timer without initializing plugins
- Result: Service appeared "running" (notification shown) but never scanned or connected

**Affected Scenarios**:
1. Manual app launch after power outage (service didn't auto-start from boot)
2. Manual app launch during testing/development
3. Any restart of the app when service was not previously running

## Changes

### 1. Fixed Service Startup Path (BaseBleService.kt)

**Enhancement**: `onStartCommand()` now handles both `null` and `START_WEB_SERVER` actions by:
1. Checking `serviceEnabled` preference from AppSettings
2. Auto-starting BLE scanning if service was previously enabled
3. Ensuring consistent behavior regardless of how service was started

**Code Location**: [BaseBleService.kt](app/src/main/java/com/blemqttbridge/core/BaseBleService.kt#L384-L397)

```kotlin
null, "START_WEB_SERVER" -> {
    // Service restarted (null) or app launched (START_WEB_SERVER)
    // Check if service should auto-start based on settings
    Log.i(TAG, "⚙️ Service started with action=$action - checking if service should auto-start")
    serviceScope.launch {
        val serviceEnabled = AppSettings(this@BaseBleService).serviceEnabled.first()
        if (serviceEnabled) {
            Log.i(TAG, "⚙️ Service was enabled in settings - auto-starting BLE scanning")
            initializeMultiplePlugins()
        } else {
            Log.i(TAG, "⚙️ Service disabled in settings - not auto-starting")
            updateNotification("Service disabled - toggle to start")
        }
    }
    scheduleKeepalive("onStartCommand($action)")
}
```

### 2. Added BLE Scanning State Visibility

**New StateFlow**: `bleScanningActive` - Tracks whether BLE scanner is actually running (distinct from service preference)

**Purpose**: Distinguish between:
- Service enabled preference (persisted setting)
- Service process running (foreground service active)
- **BLE scanner actively scanning** (new visibility)

**Updates**:
- Set to `true` in `startScanning()` after successful scan start
- Set to `false` in `stopScanning()`, `onScanFailed()`, and when Bluetooth turns off
- Exposed to UI via SettingsViewModel

### 3. Added Bluetooth Adapter State Visibility

**New StateFlow**: `bluetoothAvailable` - Tracks whether Bluetooth adapter is enabled

**Purpose**: Differentiate between:
- BLE scanning disabled by user
- BLE scanning stopped due to Bluetooth being OFF
- BLE scanning failed due to other issues

**Updates**:
- Set to `true` when Bluetooth turns ON
- Set to `false` when Bluetooth turns OFF
- Exposed to UI via SettingsViewModel

### 4. Enhanced UI Status Display (SettingsScreen.kt)

**3-State Service Indicator**:
- ⚫ **Stopped**: Service disabled (`serviceEnabled=false`)
- ⚠️ **Bluetooth OFF**: Service enabled but BT adapter disabled
- 🟢 **Scanning**: Service enabled, BT on, scanner active
- 🟡 **Running (not scanning)**: Service enabled, BT on, but scanner inactive

**Warning Message**: Shows yellow warning when service is enabled but not scanning, suggesting to toggle Bluetooth or service.

**Code Location**: [SettingsScreen.kt](app/src/main/java/com/blemqttbridge/ui/SettingsScreen.kt#L340-L365)

## Testing Results

**Test Device**: Android device at 10.115.19.214  
**Test Date**: January 18, 2026

### Fresh App Launch Test
1. ✅ Force-stopped app
2. ✅ Launched via MainActivity (sends `START_WEB_SERVER` action)
3. ✅ Service detected `serviceEnabled=true` in settings
4. ✅ Service auto-started BLE scanning
5. ✅ All plugins initialized and connected:
   - onecontrol_ed1e0a: Connected, authenticated, data healthy
   - easytouch_b1241e: Connected, authenticated, data healthy
   - gopower_1325be: Connected, authenticated, data healthy
   - mopeka_29effd: Data healthy (passive scan)
   - mopeka_aa12f9: Data healthy (passive scan)

### State Visibility Test
1. ✅ `bleScanningActive` correctly set to `true` when scanning starts
2. ✅ `bluetoothAvailable` correctly reflects BT adapter state
3. ✅ UI shows "🟢 Scanning" when all conditions met
4. ✅ UI would show "⚠️ Bluetooth OFF" if BT disabled (not tested live)
5. ✅ UI would show "🟡 Running (not scanning)" if scanner fails (not tested live)

## Migration Notes

**No action required**. This is a bug fix release with backward-compatible changes.

**Settings**: All existing settings and plugin configurations are preserved.

**Data**: No database or storage format changes.

## Files Changed

1. `app/build.gradle.kts` - Version bump to 2.5.13
2. `app/src/main/java/com/blemqttbridge/core/BaseBleService.kt`:
   - Added `_bleScanningActive` and `_bluetoothAvailable` StateFlows
   - Updated `onStartCommand()` to handle `START_WEB_SERVER`/`null` actions
   - Updated `startScanning()` to set `_bleScanningActive = true`
   - Updated `stopScanning()` to set `_bleScanningActive = false`
   - Updated `onScanFailed()` to set `_bleScanningActive = false`
   - Updated `handleBluetoothStateChange()` to manage both StateFlows
3. `app/src/main/java/com/blemqttbridge/ui/viewmodel/SettingsViewModel.kt`:
   - Added `bleScanningActive` property exposure
   - Added `bluetoothAvailable` property exposure
   - Added StateFlow collectors in init block
4. `app/src/main/java/com/blemqttbridge/ui/SettingsScreen.kt`:
   - Updated service status display with 3-state indicator
   - Added warning message for out-of-sync state
   - Collect `bleScanningActive` and `bluetoothAvailable` states

## Known Issues

None identified in this release.

## Future Enhancements

See [docs/PLANNING_CONFIG_BACKUP_AND_AUTOUPDATE.md](docs/PLANNING_CONFIG_BACKUP_AND_AUTOUPDATE.md) for planned features:
- Configuration backup/restore to external storage
- Auto-update from GitHub releases

## Related Issues

- Power outage causing app not to auto-start: **FIXED** (root cause was BT stack corruption, unrelated to this release)
- Plugins not connecting after manual launch: **FIXED** (this release addresses the startup path bug)
- BT state visibility: **ENHANCED** (new StateFlows provide better observability)
