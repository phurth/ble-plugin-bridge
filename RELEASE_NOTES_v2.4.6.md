# BLE-MQTT Bridge v2.4.6 Release Notes

## Android TV Power Fix

This release provides a stable version with the Android TV Power Fix feature that prevents the foreground service from being killed when the TV enters standby mode.

### Features

- **AndroidTvHelper utility class** - Detects Android TV devices and manages HDMI-CEC settings
- **New "Android TV Power Fix" section** in System Settings (only visible on Android TV devices)
- **Automatic CEC fix on service startup** - When permission is granted, the service automatically disables CEC auto-off when it starts
- **Toggle control** - Manually control CEC auto-off setting once permission is granted
- **ADB command copy buttons** - Easy one-tap copy of ADB commands for setup

### How to Use

#### Option 1: Grant Permission (Recommended)
This allows the app to automatically manage the CEC setting:

1. Connect to your Android TV device via ADB:
   ```bash
   adb connect <device-ip>:5555
   ```

2. Grant the permission:
   ```bash
   adb shell pm grant com.blemqttbridge android.permission.WRITE_SECURE_SETTINGS
   ```

3. Restart the app - the service will now automatically disable CEC auto-off on startup

4. Navigate to **Settings → System Settings** to verify the status

#### Option 2: Manual ADB Fix
If you prefer not to grant the permission, you can manually disable CEC:

```bash
adb shell settings put global hdmi_control_auto_device_off_enabled 0
```

Note: This setting may reset after a factory reset or system update.

### Technical Details
- Detection method: `PackageManager.FEATURE_LEANBACK`
- Setting location: `Settings.Global.hdmi_control_auto_device_off_enabled`
- Permission required: `android.permission.WRITE_SECURE_SETTINGS`
- Permission grant method: ADB only (cannot be granted via UI)

---

## Documentation Updates
- Updated README with Android TV Power Fix section
- Updated INTERNALS.md with v2.4.6 changes

## Download
- `ble-mqtt-bridge-v2.4.6-release.apk` - Signed release build
