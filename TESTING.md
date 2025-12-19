# Testing the MQTT Plugin

This document describes how to test the Phase 1 MqttOutputPlugin implementation with a real MQTT broker.

## Prerequisites

1. **MQTT Broker**: You need access to an MQTT broker (e.g., Mosquitto, HiveMQ, or your Home Assistant MQTT add-on)
2. **Android Device or Emulator**: Android 8.0+ (API 26+)
3. **ADB**: Android Debug Bridge installed and device connected

## Configuration

### 1. Configure Broker Credentials

You have **two** testing options - edit the configuration in either file:

#### Option A: Manual App Testing (MainActivity)
Edit [app/src/main/java/com/blemqttbridge/MainActivity.kt](app/src/main/java/com/blemqttbridge/MainActivity.kt):

```kotlin
private const val BROKER_URL = "tcp://192.168.1.100:1883"  // Your broker IP
private const val USERNAME = "your_username"                 // or "" if no auth
private const val PASSWORD = "your_password"                 // or "" if no auth
```

#### Option B: Automated Tests (Instrumented Tests)
Edit [app/src/androidTest/java/com/blemqttbridge/MqttPluginInstrumentedTest.kt](app/src/androidTest/java/com/blemqttbridge/MqttPluginInstrumentedTest.kt):

```kotlin
private const val BROKER_URL = "tcp://192.168.1.100:1883"  // Your broker IP
private const val USERNAME = "your_username"                 // or "" if no auth
private const val PASSWORD = "your_password"                 // or "" if no auth
```

### 2. Ensure Network Connectivity

Make sure your Android device/emulator can reach the MQTT broker on the network. For emulators:
- Use the host IP visible to the emulator (not 127.0.0.1)
- Or use `10.0.2.2` to reach the host machine's localhost

## Testing Methods

### Method 1: Manual Testing with MainActivity (Recommended for First Run)

This approach launches a simple app that runs all tests and displays results on screen.

**1. Build and install the app:**
```bash
./gradlew installDebug
```

**2. Launch the app:**
```bash
adb shell am start -n com.blemqttbridge/.MainActivity
```

**3. View logs in real-time:**
```bash
adb logcat -s MqttTest:* MqttOutputPlugin:*
```

**4. Expected output on screen:**
```
📡 Testing MQTT Plugin
Broker: tcp://192.168.1.100:1883

Connecting to broker...
✅ Connected!

Publishing availability...
✅ Availability published

Subscribing to commands...
✅ Subscribed to test/ble_bridge/command/#

Publishing test state...
✅ State published

Publishing HA discovery...
✅ Discovery published

🎉 ALL TESTS PASSED!

Connection Status: Connected to tcp://192.168.1.100:1883

Manual test:
Use an MQTT client to publish to:
  test/ble_bridge/command/test_device

You should see the message appear above.
```

**5. Verify with MQTT client:**

Subscribe to see published messages:
```bash
mosquitto_sub -h 192.168.1.100 -u your_username -P your_password -t 'test/ble_bridge/#' -v
```

Publish a command to test subscription:
```bash
mosquitto_pub -h 192.168.1.100 -u your_username -P your_password -t 'test/ble_bridge/command/test_device' -m 'TURN_ON'
```

You should see the message appear in the app's log output.

### Method 2: Automated Instrumented Tests

This runs 5 comprehensive tests that validate all plugin functionality.

**1. Connect device/emulator:**
```bash
adb devices
```

**2. Run all instrumented tests:**
```bash
./gradlew connectedDebugAndroidTest
```

**3. Test descriptions:**
- `testConnectToRealBroker`: Validates basic connection and authentication
- `testPublishState`: Tests publishing state messages with retained flag
- `testPublishAndSubscribe`: Tests full pub/sub loop (publish → receive callback)
- `testPublishAvailability`: Tests online/offline availability messages
- `testDisconnectAndReconnect`: Tests connection lifecycle

**4. View test results:**
```bash
# Summary in terminal output

# Detailed HTML report:
open app/build/reports/androidTests/connected/index.html
```

**5. Expected output:**
```
com.blemqttbridge.MqttPluginInstrumentedTest > testConnectToRealBroker PASSED
com.blemqttbridge.MqttPluginInstrumentedTest > testPublishState PASSED
com.blemqttbridge.MqttPluginInstrumentedTest > testPublishAndSubscribe PASSED
com.blemqttbridge.MqttPluginInstrumentedTest > testPublishAvailability PASSED
com.blemqttbridge.MqttPluginInstrumentedTest > testDisconnectAndReconnect PASSED

BUILD SUCCESSFUL
```

## Validation Checklist

Once tests pass, verify these MQTT topics exist on your broker:

- ✅ `test/ble_bridge/availability` - online/offline status
- ✅ `test/ble_bridge/device/test_device/state` - device state (retained)
- ✅ `homeassistant/switch/ble_bridge/test_device/config` - HA discovery payload

## Troubleshooting

### Connection Failed
```
❌ ERROR: Connection lost
```

**Solutions:**
1. Verify broker IP is correct and reachable: `ping YOUR_BROKER_IP`
2. Check broker is running: `mosquitto -v` or check Home Assistant MQTT add-on
3. Verify username/password (or set both to "" for anonymous)
4. Check firewall allows port 1883
5. For emulator, use `10.0.2.2` instead of `127.0.0.1` to reach host

### Authentication Failed
```
❌ ERROR: Not authorized
```

**Solutions:**
1. Verify username and password are correct
2. Check broker allows the user (e.g., Mosquitto `mosquitto_passwd` file)
3. Try with `USERNAME = ""` and `PASSWORD = ""` if broker allows anonymous

### Tests Timeout
```
Tests exceeded timeout
```

**Solutions:**
1. Increase delay values in tests (e.g., `delay(1000)` → `delay(2000)`)
2. Check network latency to broker
3. Verify broker isn't overloaded

### Build Failed
```
Task :app:compileDebugKotlin FAILED
```

**Solution:**
```bash
./gradlew clean build
```

### ADB Not Found
```
adb: command not found
```

**Solution:**
Add Android SDK platform-tools to PATH, or use full path:
```bash
export PATH=$PATH:~/Library/Android/sdk/platform-tools  # macOS
# or
export PATH=$PATH:$ANDROID_HOME/platform-tools
```

## Next Steps

Once MQTT plugin tests pass:
1. Verify all topics appear correctly in Home Assistant
2. Test retained messages persist after broker restart
3. Test Last Will Testament (disconnect device unexpectedly)
4. Ready for **Phase 2**: Implement BLE plugin infrastructure

## Example MQTT Messages

Here's what the plugin publishes:

**Availability:**
```
Topic: test/ble_bridge/availability
Payload: online
```

**State (retained):**
```
Topic: test/ble_bridge/device/test_device/state
Payload: {"state":"ON","brightness":100}
```

**Home Assistant Discovery:**
```
Topic: homeassistant/switch/ble_bridge/test_device/config
Payload: {
  "name": "BLE Bridge Test Device",
  "state_topic": "test/ble_bridge/device/test_device/state",
  "command_topic": "test/ble_bridge/command/test_device",
  "unique_id": "ble_bridge_test_001"
}
```

**Command Subscription:**
```
Subscribed to: test/ble_bridge/command/#
Callback fires when messages arrive matching pattern
```
