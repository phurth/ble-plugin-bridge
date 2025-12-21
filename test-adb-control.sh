#!/bin/bash
# Quick ADB Control Test for BLE Bridge Service

echo "==================================="
echo "BLE Bridge ADB Control Quick Test"
echo "==================================="
echo ""

# Clear logcat
echo "Clearing logcat..."
adb logcat -c

# Start monitoring in background
echo "Starting logcat monitor..."
adb logcat | grep "ControlCmd:" &
LOGCAT_PID=$!
sleep 1

# Test 1: Service Status (before start)
echo ""
echo "TEST 1: Check initial service status"
adb shell am broadcast -a com.blemqttbridge.CONTROL_COMMAND --es command "service_status"
sleep 2

# Test 2: List plugins
echo ""
echo "TEST 2: List available plugins"
adb shell am broadcast -a com.blemqttbridge.CONTROL_COMMAND --es command "list_plugins"
sleep 2

# Test 3: Start service
echo ""
echo "TEST 3: Start BLE service"
adb shell am broadcast -a com.blemqttbridge.CONTROL_COMMAND --es command "start_service"
sleep 3

# Test 4: Service status (after start)
echo ""
echo "TEST 4: Check service status after start"
adb shell am broadcast -a com.blemqttbridge.CONTROL_COMMAND --es command "service_status"
sleep 2

# Test 5: Stop service
echo ""
echo "TEST 5: Stop service"
adb shell am broadcast -a com.blemqttbridge.CONTROL_COMMAND --es command "stop_service"
sleep 2

# Test 6: Final status check
echo ""
echo "TEST 6: Final status check"
adb shell am broadcast -a com.blemqttbridge.CONTROL_COMMAND --es command "service_status"
sleep 2

# Stop monitoring
echo ""
echo "Stopping logcat monitor..."
kill $LOGCAT_PID

echo ""
echo "==================================="
echo "Test complete!"
echo "==================================="
echo ""
echo "To monitor manually:"
echo "  adb logcat | grep 'ControlCmd:'"
echo ""
echo "Available commands:"
echo "  start_service, stop_service, restart_service"
echo "  load_plugin, unload_plugin, reload_plugin"
echo "  list_plugins, service_status"
