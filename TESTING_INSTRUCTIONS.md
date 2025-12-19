# BLE Infrastructure Testing Instructions

## Current Status
✅ App installed on device T812128GB24328551448 (iPlay60_mini_Pro)
✅ App launches successfully (permission crash fixed)
⚠️ Ready to test BLE scanning

## Testing Steps

### 1. Launch App & Check Status
The app should already be open showing **ServiceStatusActivity**.

**You should see:**
- 📡 Bluetooth Adapter status
- 🔐 Permission checklist (most will show ❌ initially)
- 📍 Location Services status
- 🔌 Plugin Registry info
- 💾 Memory usage
- Three buttons: **Start Service**, **Stop Service**, **Refresh**

### 2. Grant Permissions
Tap **"Start Service"** button. Android will prompt for permissions:

**Required permissions (Android 13+):**
1. **BLUETOOTH_SCAN** - For BLE device discovery
2. **BLUETOOTH_CONNECT** - For connecting to BLE devices  
3. **ACCESS_FINE_LOCATION** - Required for BLE scanning (Android system requirement)
4. **POST_NOTIFICATIONS** - For foreground service notification

**Grant all permissions when prompted.**

**Also Required:**
5. **Enable Location Services** - Go to Settings > Location and turn it ON
   - Android requires location enabled for BLE scanning, even though we don't use GPS

### 3. Start BLE Service
After granting permissions:
1. Tap **"Refresh"** button to update status display
2. Verify all permissions show ✅
3. Verify Location Services shows ✅ Enabled
4. Tap **"Start Service"** again

**Expected behavior:**
- Service starts in foreground
- Notification appears: "BLE Bridge Service" 
- Service begins BLE scanning
- Logs will show scanning activity

### 4. Watch Logs (From Mac Terminal)
```bash
# Monitor BLE activity
adb -s T812128GB24328551448 logcat -s BlePluginBridgeApp BaseBleService PluginRegistry MockBatteryPlugin
```

**Expected log output:**
```
BlePluginBridgeApp: Application starting - registering plugins
BlePluginBridgeApp: Plugin registration complete
BaseBleService: Service created
BaseBleService: onStartCommand: com.blemqttbridge.START_SCAN
BaseBleService: Initializing plugins...
PluginRegistry: Loading BLE plugin: mock_battery
PluginRegistry: Loading output plugin: mqtt
BaseBleService: Plugins initialized successfully
BaseBleService: BLE scan started
BaseBleService: Scanning for devices...
```

**If permissions missing:**
```
BaseBleService: Permission denied for BLE scan
SecurityException: Need android.permission.BLUETOOTH_SCAN
```

### 5. What Will Happen During Scan

**Without matching BLE device:**
- Service scans continuously
- No devices found (MockBatteryPlugin matches "MockBattery*" name prefix)
- Notification shows "Scanning for devices..."
- No crashes or errors

**If BLE device with "MockBattery" name prefix found:**
```
BaseBleService: Found matching device: XX:XX:XX:XX:XX:XX -> plugin: mock_battery
PluginRegistry: Loading plugin: mock_battery
MockBatteryPlugin: Loaded for device MockBatteryXXX
BaseBleService: Connecting to device...
```

### 6. Verify Success

**Check notification area:**
- Should see persistent notification: "BLE Bridge Service"
- Notification should show current status (scanning/connected)

**Check app UI:**
- Tap **"Refresh"** button
- Loaded BLE Plugins section should show:
  ```
  Loaded BLE Plugins:
    ✅ mock_battery: Mock Battery Monitor v1.0
  ```

**Check logs for errors:**
```bash
# Look for crashes or exceptions
adb -s T812128GB24328551448 logcat -s AndroidRuntime:E
```

Should show nothing (no errors).

## Common Issues & Solutions

### Issue: SecurityException on scan
**Error:** `Permission denied for BLE scan`

**Solution:**
1. Go to Android Settings > Apps > BLE Plugin Bridge
2. Tap Permissions
3. Ensure ALL permissions are granted:
   - Location: "Allow all the time" or "Allow only while using"
   - Nearby devices: "Allow"
4. Go to Settings > Location and enable location
5. Return to app, tap "Stop Service", then "Start Service"

### Issue: Service starts but immediately stops
**Possible causes:**
- Android killed service due to battery optimization
- Foreground service permission denied

**Solution:**
1. Settings > Apps > BLE Plugin Bridge > Battery
2. Set to "Unrestricted" or "Not optimized"
3. Restart service

### Issue: No BLE devices found
**This is expected!** 
- MockBatteryPlugin only matches devices with name "MockBattery*"
- No real hardware has this name
- This validates the scanning infrastructure works

**To test with actual device:**
- Use nRF Connect app on another Android phone
- Create advertisement with name "MockBatteryTest"
- Should see device discovered and connected

## Success Criteria ✅

For Phase 2 infrastructure validation, success means:

- [x] App launches without crash
- [ ] Permissions can be granted via system UI
- [ ] Service starts in foreground
- [ ] Foreground notification appears
- [ ] BLE scanning begins without SecurityException
- [ ] Logs show "BLE scan started"
- [ ] No crashes or fatal errors
- [ ] Service can be stopped cleanly

**Note:** Finding actual devices is NOT required for Phase 2 success.
The goal is to validate the infrastructure (service, permissions, scanning, plugin loading).

## Next Steps After Success

Once all permissions work and scanning starts cleanly:

**Option 1: Test with BLE Simulator**
- Use nRF Connect to simulate a "MockBattery" device
- Validate plugin loading, connection, MQTT publishing

**Option 2: Proceed to Phase 3 (Recommended)**
- Begin OneControl migration
- Test with real RV hardware (Micro-Air thermostat)
- Full end-to-end validation with actual BLE protocol
- This is the critical path to deployment

## Monitoring Commands

```bash
# Watch all app activity
adb -s T812128GB24328551448 logcat -s BlePluginBridgeApp:* BaseBleService:* PluginRegistry:* MockBatteryPlugin:* MqttOutputPlugin:*

# Watch for errors only
adb -s T812128GB24328551448 logcat *:E | grep blemqtt

# Watch BLE system activity
adb -s T812128GB24328551448 logcat -s BluetoothAdapter BluetoothLeScanner

# Clear logs before test
adb -s T812128GB24328551448 logcat -c

# Dump current state
adb -s T812128GB24328551448 dumpsys activity services com.blemqttbridge
```

## Current Testing Device
- Model: iPlay60_mini_Pro
- Android: 13
- ADB ID: T812128GB24328551448
- Connection: USB

Ready to test! Start with Step 2 above (tap "Start Service" button).
