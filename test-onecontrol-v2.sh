#!/bin/bash
# Test script for OneControl v2 plugin

set -e

DEVICE="10.115.19.172:5555"

echo "🧪 Testing OneControl v2 Plugin"
echo "================================"
echo ""

# Stop legacy app
echo "1️⃣ Stopping legacy app..."
adb -s $DEVICE shell am force-stop com.onecontrol.blebridge
echo "✓ Legacy app stopped"
echo ""

# Clear and reinstall
echo "2️⃣ Installing latest build..."
adb -s $DEVICE install -r app/build/outputs/apk/debug/app-debug.apk
echo "✓ App installed"
echo ""

# Clear app data for fresh start
echo "3️⃣ Clearing app data..."
adb -s $DEVICE shell pm clear com.blemqttbridge
echo "✓ App data cleared"
echo ""

# Clear logcat
echo "4️⃣ Clearing logcat..."
adb -s $DEVICE logcat -c
echo "✓ Logcat cleared"
echo ""

# Start app
echo "5️⃣ Starting app..."
adb -s $DEVICE shell am start -n com.blemqttbridge/.ui.ServiceStatusActivity
sleep 3
echo "✓ App started"
echo ""

# Enable onecontrol_v2 plugin via monkey (tap Enable button)
echo "6️⃣ Enabling onecontrol_v2 plugin (tapping button)..."
# Tap "Enable OneControl" button (approximate coordinates)
adb -s $DEVICE shell input tap 540 900
sleep 1
echo "✓ Plugin enabled"
echo ""

# Start service (tap Start button)
echo "7️⃣ Starting BLE service (tapping button)..."
# Tap "Start BLE Service" button
adb -s $DEVICE shell input tap 540 1100
sleep 10
echo "✓ Service started - waiting for connection..."
echo ""

# Show logs
echo "8️⃣ Checking logs for new plugin activity..."
echo "================================"
adb -s $DEVICE logcat -d | grep -E "BlePluginBridge|OneControlDevice|OneControlGatt|createGattCallback|Using.*callback|🔑|🌱|📨📨📨|Authentication" | tail -100

echo ""
echo "================================"
echo "Test complete! Check logs above for plugin activity."
