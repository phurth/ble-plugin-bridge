# Multi-Instance Plugin Testing Plan

## Overview
This document describes how to set up and execute automated tests for the multi-instance plugin functionality, plus a comprehensive manual testing checklist.

---

## Automated Testing

### What We're Testing
- Plugin instance creation, update, and removal
- Instance ID generation from MAC addresses
- Configuration serialization/deserialization
- Multiple instances of the same plugin type
- MAC address changes requiring new instance IDs
- BLE Scanner special cases (no MAC required)

### Test Framework
- **JUnit 4** for test execution
- **Mockito** for mocking Android Context
- **Kotlin Test** for assertions

### Running Unit Tests

#### Option 1: Android Studio GUI
1. Open Android Studio
2. Navigate to `app/src/test/java/com/blemqttbridge/web/MultiInstanceWebApiTest.kt`
3. Right-click on the class name or a test method
4. Select "Run 'MultiInstanceWebApiTest'" (or individual test)
5. View results in the "Run" panel at bottom

#### Option 2: Command Line (Gradle)
```bash
cd /Users/petehurth/Downloads/Decom/android_ble_plugin_bridge

# Run ALL unit tests
./gradlew test

# Run only multi-instance tests
./gradlew test --tests "com.blemqttbridge.web.MultiInstanceWebApiTest"

# Run a specific test method
./gradlew test --tests "com.blemqttbridge.web.MultiInstanceWebApiTest.testMultipleInstancesOfSameType"

# Generate HTML test report
./gradlew test
# Report location: app/build/reports/tests/testDebugUnitTest/index.html
open app/build/reports/tests/testDebugUnitTest/index.html
```

#### Option 3: Continuous Testing (Watch Mode)
```bash
# Re-run tests on code changes
./gradlew test --continuous
```

### Understanding Test Results

**Success Output:**
```
> Task :app:testDebugUnitTest

MultiInstanceWebApiTest > testCreateInstanceId PASSED
MultiInstanceWebApiTest > testInstanceSerialization PASSED
MultiInstanceWebApiTest > testMultipleInstancesOfSameType PASSED
MultiInstanceWebApiTest > testUpdateInstance PASSED
...

BUILD SUCCESSFUL in 2s
```

**Failure Output:**
```
> Task :app:testDebugUnitTest FAILED

MultiInstanceWebApiTest > testUpdateInstance FAILED
    org.junit.ComparisonFailure: expected:<[new]> but was:<[old]>
        at MultiInstanceWebApiTest.testUpdateInstance(MultiInstanceWebApiTest.kt:95)
```

### Adding More Tests

To add new test cases, edit `MultiInstanceWebApiTest.kt`:

```kotlin
@Test
fun testYourNewScenario() {
    // Arrange: Setup test data
    val instance = PluginInstance(...)
    
    // Act: Perform the operation
    ServiceStateManager.saveInstance(mockContext, instance)
    
    // Assert: Verify expected outcome
    val result = ServiceStateManager.getAllInstances(mockContext)
    assertEquals(expected, result["instanceId"])
}
```

---

## Integration Testing (Web API)

### Setup Required
You need a tool to make HTTP requests. Choose one:

**Option A: cURL (Command Line)**
```bash
# Already installed on macOS
curl --version
```

**Option B: Postman (GUI)**
1. Download from https://www.postman.com/downloads/
2. Install and open
3. Create a new request collection

**Option C: HTTPie (Command Line, user-friendly)**
```bash
# Install with Homebrew
brew install httpie
```

### Running Integration Tests

**Prerequisites:**
1. App must be installed on device/emulator
2. Service must be STOPPED (for add/update/remove operations)
3. Device connected via ADB
4. Know your device IP address (shown in web UI)

**Test Script:**
```bash
#!/bin/bash
# Save as test-multi-instance.sh and run: bash test-multi-instance.sh

DEVICE_IP="10.115.19.214"  # Change to your device IP
BASE_URL="http://$DEVICE_IP:8088"

echo "=== Multi-Instance Integration Tests ==="

# 1. Stop service
echo "1. Stopping service..."
adb shell "am broadcast -a com.blemqttbridge.STOP_SERVICE"
sleep 2

# 2. Get current instances
echo "2. Getting current instances..."
curl -s "$BASE_URL/api/instances" | python3 -m json.tool

# 3. Add EasyTouch instance 1
echo "3. Adding EasyTouch instance 1..."
curl -X POST "$BASE_URL/api/instances/add" \
  -H "Content-Type: application/json" \
  -d '{
    "pluginType": "easytouch",
    "deviceMac": "EC:C9:FF:B1:24:1E",
    "displayName": "Master Bedroom",
    "config": {"password": "secret123"}
  }' | python3 -m json.tool

# 4. Add EasyTouch instance 2
echo "4. Adding EasyTouch instance 2..."
curl -X POST "$BASE_URL/api/instances/add" \
  -H "Content-Type: application/json" \
  -d '{
    "pluginType": "easytouch",
    "deviceMac": "AA:BB:CC:C4:F8:92",
    "displayName": "Guest Room",
    "config": {"password": "guest456"}
  }' | python3 -m json.tool

# 5. Verify both instances exist
echo "5. Verifying instances..."
curl -s "$BASE_URL/api/instances" | python3 -m json.tool

# 6. Update instance 1 display name
echo "6. Updating instance display name..."
curl -X POST "$BASE_URL/api/instances/update" \
  -H "Content-Type: application/json" \
  -d '{
    "instanceId": "easytouch_b1241e",
    "displayName": "Primary Bedroom",
    "config": {"password": "newsecret"}
  }' | python3 -m json.tool

# 7. Remove instance 2
echo "7. Removing instance 2..."
curl -X POST "$BASE_URL/api/instances/remove" \
  -H "Content-Type: application/json" \
  -d '{"instanceId": "easytouch_c4f892"}' | python3 -m json.tool

# 8. Final state check
echo "8. Final state..."
curl -s "$BASE_URL/api/instances" | python3 -m json.tool

# 9. Start service
echo "9. Starting service..."
adb shell "am broadcast -a com.blemqttbridge.START_SERVICE"

echo "=== Tests Complete ==="
```

**Make script executable:**
```bash
chmod +x test-multi-instance.sh
```

**Run:**
```bash
bash test-multi-instance.sh
```

### Expected Results

**After adding 2 instances:**
```json
[
  {
    "instanceId": "easytouch_b1241e",
    "pluginType": "easytouch",
    "deviceMac": "EC:C9:FF:B1:24:1E",
    "displayName": "Master Bedroom",
    "config": {"password": "secret123"}
  },
  {
    "instanceId": "easytouch_c4f892",
    "pluginType": "easytouch",
    "deviceMac": "AA:BB:CC:C4:F8:92",
    "displayName": "Guest Room",
    "config": {"password": "guest456"}
  }
]
```

**After update:**
```json
{
  "success": true,
  "instanceId": "easytouch_b1241e"
}
```

**After remove:**
```json
{
  "success": true
}
```

---

## Manual Testing Checklist

### Web UI Testing

#### ✅ Add Instance
- [ ] Service stopped before adding
- [ ] Can select plugin type from dropdown
- [ ] Can enter display name
- [ ] Can enter MAC address (hidden for BLE Scanner)
- [ ] Plugin-specific fields appear (PIN for OneControl, password for EasyTouch)
- [ ] Success message appears
- [ ] New instance appears in list
- [ ] Instance has correct health status indicator

#### ✅ Add Multiple Instances
- [ ] Can add 2+ EasyTouch instances with different MACs
- [ ] Each instance appears in its own card
- [ ] Each card shows correct MAC and config
- [ ] OneControl/GoPower show error if trying to add duplicate
- [ ] BLE Scanner can be added without MAC

#### ✅ Edit Instance
- [ ] Service stopped before editing
- [ ] Edit dialog pre-fills with current values
- [ ] Can change display name
- [ ] Can change MAC address (generates new instanceId)
- [ ] Can change plugin-specific config
- [ ] MAC field hidden for BLE Scanner
- [ ] Success message appears
- [ ] Instance list refreshes with new values

#### ✅ Remove Instance
- [ ] Service stopped before removing
- [ ] Confirmation dialog shows correct instance name
- [ ] Cancel works
- [ ] Confirm removes instance
- [ ] Instance disappears from list
- [ ] Other instances unaffected

#### ✅ Service Integration
- [ ] Service start fails with helpful error if instances invalid
- [ ] After starting service, instances show connection status
- [ ] Green border for healthy, red for unhealthy
- [ ] BLE Scanner always shows green border
- [ ] Connection status updates every 5 seconds
- [ ] MQTT status reflects actual MQTT connection

#### ✅ Drag and Drop
- [ ] Can grab plugin section by header
- [ ] Section becomes transparent while dragging
- [ ] Other sections shift to show drop position
- [ ] Can drop in new position
- [ ] Order persists after page refresh
- [ ] New plugins appear in alphabetical position

### Backend Testing

#### ✅ Storage
- [ ] Instances persist across app restarts
- [ ] Migration from old single-instance format works
- [ ] Can handle empty config maps
- [ ] Can handle complex config with multiple fields

#### ✅ Connection Management
- [ ] Each instance connects to its own device
- [ ] Multiple EasyTouch instances maintain separate BLE connections
- [ ] Disconnecting one instance doesn't affect others
- [ ] Status tracking is per-instance

#### ✅ MQTT
- [ ] Each instance publishes to correct topic (uses MAC in topic)
- [ ] Multiple instances don't interfere with each other
- [ ] HA discovery works for each instance
- [ ] Entity names include display name or MAC

### Edge Cases

- [ ] MAC address with lowercase letters (should normalize to uppercase)
- [ ] MAC address with inconsistent separators
- [ ] Very long display names
- [ ] Special characters in display names
- [ ] Empty password for EasyTouch (should be allowed)
- [ ] Rapid add/remove/add of same instance
- [ ] Service crash during multi-instance operation

### Performance

- [ ] UI responsive with 5+ instances
- [ ] Page load time acceptable
- [ ] Drag-drop smooth with multiple sections
- [ ] Status updates don't cause UI flicker

---

## Debugging Failed Tests

### Unit Test Failures

**View full stack trace:**
```bash
./gradlew test --info
```

**Run with debugging:**
1. Android Studio → Set breakpoint in test
2. Right-click test → "Debug"
3. Step through code

### Integration Test Failures

**Check app logs:**
```bash
adb logcat | grep "WebServerManager\|PluginInstance\|ServiceState"
```

**Verify service state:**
```bash
curl http://DEVICE_IP:8088/api/status
```

**Check stored instances:**
```bash
adb shell "run-as com.blemqttbridge cat /data/data/com.blemqttbridge/shared_prefs/service_state.xml"
```

### Common Issues

**"Service must be stopped" error:**
- Run: `adb shell "am broadcast -a com.blemqttbridge.STOP_SERVICE"`
- Wait 2 seconds
- Retry

**Connection refused:**
- Check device IP address
- Ensure web service is running
- Check firewall/network

**Instance not found:**
- Verify instanceId format (plugintype_macsuffix)
- Check if instance was actually saved
- Review app logs

---

## Continuous Integration (Future)

To run tests automatically on every commit:

**GitHub Actions example:**
```yaml
name: Run Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-java@v3
        with:
          java-version: '17'
      - name: Run Unit Tests
        run: ./gradlew test
      - name: Publish Test Report
        uses: mikepenz/action-junit-report@v3
        if: always()
        with:
          report_paths: '**/build/test-results/test/TEST-*.xml'
```

---

## Test Coverage

To generate code coverage report:

```bash
./gradlew testDebugUnitTestCoverage
open app/build/reports/coverage/test/debug/index.html
```

This shows which lines of code are covered by tests (green) and which aren't (red).

---

## Next Steps

1. ✅ Implement `/api/instances/update` endpoint (DONE)
2. ✅ Create unit tests (DONE)
3. ⏳ Run automated tests
4. ⏳ Execute integration test script
5. ⏳ Complete manual testing checklist
6. ⏳ Update Android UI to handle multi-instance
7. ⏳ Full end-to-end testing

