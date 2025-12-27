# BLE MQTT Bridge v2.3.2

## Performance & Optimization Release

This release focuses on improving app performance and Home Assistant integration.

### 🚀 Performance Improvements

**Trace-Aware Debug Logging**
- Implemented `DebugLog` utility for conditional debug logging
- Debug logs only execute when `BuildConfig.DEBUG` OR BLE trace capture is active
- Reduces production logging overhead: CPU usage, battery consumption, logcat buffer pressure
- Estimated 60-90% reduction in production log volume
- Users can still enable full debug logging via "BLE Trace Capture" in settings

**Emoji Removal**
- Removed all emoji characters from log statements (80+ occurrences)
- Eliminates UTF-8 encoding overhead
- Reduces individual log entry sizes
- Improves logcat compatibility with automated tools

**Affected Components:**
- EasyTouch Plugin: 30+ debug logs optimized
- GoPower Plugin: 15+ debug logs optimized
- All plugins prepared for future optimization

### 🔧 Bug Fixes

**Home Assistant Energy Dashboard Integration**
- Added `state_class` attributes to GoPower sensors
- `solar_power`, `battery_voltage`, `solar_voltage`, `solar_current`, `temperature`, `state_of_charge`: `state_class: measurement`
- `energy_today`, `amp_hours`: `state_class: total_increasing`
- GoPower sensors now appear as selectable options in HA energy dashboard configuration

### 📚 Documentation

**INTERNALS.md Updates**
- New Section 10: Debug Logging & Performance
- Complete documentation of DebugLog utility
- BLE trace capture usage guidelines
- Performance impact analysis
- Best practices for plugin developers

### 🔄 Changes

**From v2.3.1:**
- Performance optimizations
- Energy dashboard compatibility
- Documentation enhancements

**Commits:**
- `e23b02f` - perf: Optimize logging and remove emoji overhead
- `64a0d2a` - docs: Update INTERNALS.md with logging optimization details
- `ca79c49` - fix: Add state_class to GoPower sensors for energy dashboard
- `63d4f1c` - chore: Bump version to 2.3.2

### 📦 Installation

1. Download `ble-mqtt-bridge-v2.3.2.apk`
2. Install on your Android device (requires Android 8.0+)
3. For energy dashboard: Delete and re-add GoPower device in Home Assistant to pick up new `state_class` attributes

### ⚠️ Breaking Changes

None. This is a backward-compatible performance and bug fix release.

### 🐛 Known Issues

None identified in this release.

---

**Full Changelog**: https://github.com/phurth/ble-plugin-bridge/compare/v2.3.1...v2.3.2
