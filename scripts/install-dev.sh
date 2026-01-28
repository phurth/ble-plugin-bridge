#!/bin/bash
# Install debug APK to connected device (preserves config and permissions)
# Usage: ./scripts/install-dev.sh

set -e

echo "Building debug APK..."
./gradlew assembleDebug -q

echo "Installing with -r flag (replace, preserves data)..."
adb install -r app/build/outputs/apk/debug/app-debug.apk

echo "Launching app..."
adb shell am start -n com.blemqttbridge/.MainActivity

echo "✅ Done"
