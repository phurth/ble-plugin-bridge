#!/bin/bash
# Install debug APK to connected device (preserves config and permissions)
# Usage: ./scripts/install-dev.sh

set -e

echo "Building debug APK..."
./gradlew assembleDebug -q

echo "Installing with -r flag (replace, preserves data)..."
adb install -r app/build/outputs/apk/debug/app-debug.apk

echo "Force-stopping app to ensure clean restart..."
adb shell am force-stop com.blemqttbridge
sleep 1

echo "Launching app..."
adb shell am start -n com.blemqttbridge/.MainActivity

echo "Waiting for app to start..."
sleep 2
adb shell pidof com.blemqttbridge && echo "✅ App is running (PID: $(adb shell pidof com.blemqttbridge))" || echo "❌ App failed to start"

echo "✅ Done"
