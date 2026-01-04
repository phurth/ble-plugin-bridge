# Release Notes v2.4.4

## 🔐 Security: Controlled Bonding for OneControl

This release adds a critical security enhancement for BLE bonding in multi-device environments like RV parks.

### What's New

#### Controlled Bonding for Configured Devices Only
- **SECURITY FIX**: BLE bonding (`createBond()`) is now ONLY initiated for explicitly configured devices
- Prevents accidental pairing with neighbors' gateways in crowded environments
- OneControl plugin now declares `requiresBonding() = true` to indicate pairing requirement

#### Plugin Registry Cleanup on Service Restart
- Added `pluginRegistry.cleanup()` call before plugin initialization
- Ensures fresh plugin state when service restarts
- Fixes potential issues with stale plugin instances

#### ADB Configuration Command for OneControl
- New broadcast action: `com.blemqttbridge.CONFIGURE_ONECONTROL`
- Configure gateway MAC and PIN via ADB without UI:
  ```bash
  adb shell am broadcast -a com.blemqttbridge.CONFIGURE_ONECONTROL \
    --es gateway_mac "24:DC:C3:ED:1E:0A" \
    --es gateway_pin "090336"
  ```
- Added to AndroidManifest.xml intent filters

#### UI Improvements
- Settings fields now properly load saved values (fixed empty fields on app restart)
- Plugin configuration fields are disabled while service is running
- Add/Remove plugin buttons disabled during service operation
- Added setup hint for OneControl pairing process

### Technical Details

#### BleDevicePlugin Interface
- Added `requiresBonding(): Boolean` method (default: `false`)
- Plugins can override to indicate explicit bonding requirement
- Only affects configured devices from `getConfiguredDevices()`

#### Security Flow
1. Device connects via GATT
2. Check if device is in `getConfiguredDevices()` list
3. Check if plugin `requiresBonding()` returns true
4. Only then initiate `createBond()`
5. Non-configured devices are logged with warning but NOT bonded

### Upgrade Notes
- Fully backward compatible
- Existing paired devices will continue to work
- New devices must be explicitly configured before pairing

---
*Built with Android Gradle Plugin 8.3.0, Kotlin 1.9.22, Compose 2024.02.00*
