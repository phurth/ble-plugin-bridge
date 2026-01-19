#!/bin/bash
set -e

# Read config from /data/options.json
CONFIG_PATH=/data/options.json

MODE=$(jq -r '.mode' $CONFIG_PATH)
DEVICE_SERIAL=$(jq -r '.device_serial // empty' $CONFIG_PATH)
ADB_PORT=$(jq -r '.adb_port' $CONFIG_PATH)
BRIDGE_PORT=$(jq -r '.bridge_port' $CONFIG_PATH)
CHECK_INTERVAL=$(jq -r '.check_interval' $CONFIG_PATH)

echo "====================================="
echo "Starting ADB Bridge v1.0.0"
echo "====================================="
echo "Mode: ${MODE}"

# Debug: List available USB devices
echo "====================================="
echo "Checking USB devices..."
echo "====================================="
if [ -d /dev/bus/usb ]; then
  echo "USB bus found. Devices:"
  find /dev/bus/usb -type c 2>/dev/null | head -20 || echo "  (No devices found)"
else
  echo "ERROR: /dev/bus/usb not found in container!"
fi

# Wait for USB device to appear
echo "Probing for Android devices via USB..."
RETRY_COUNT=0
MAX_RETRIES=30

while ! adb devices | grep -q "device$"; do
  sleep 2
  RETRY_COUNT=$((RETRY_COUNT + 1))
  
  if [ $RETRY_COUNT -ge $MAX_RETRIES ]; then
    echo "ERROR: No Android device detected after 60 seconds"
    echo "Please check:"
    echo "  1. USB cable is connected"
    echo "  2. USB debugging is enabled on Android device"
    echo "  3. Device is authorized (check device screen)"
    exit 1
  fi
done

# List all connected devices
echo "====================================="
echo "Connected Android Devices:"
echo "====================================="

DEVICE_COUNT=$(adb devices | grep -c "device$" || true)

if [ "$DEVICE_COUNT" -eq 0 ]; then
  echo "ERROR: No devices found"
  exit 1
fi

adb devices -l | grep "device " | while read -r line; do
  DEV_SERIAL=$(echo "$line" | awk '{print $1}')
  MODEL=$(echo "$line" | grep -o 'model:[^ ]*' | cut -d: -f2 || true)
  PRODUCT=$(echo "$line" | grep -o 'product:[^ ]*' | cut -d: -f2 || true)
  echo "  Serial: ${DEV_SERIAL}"
  [ -n "$MODEL" ] && echo "    Model: ${MODEL}"
  [ -n "$PRODUCT" ] && echo "    Product: ${PRODUCT}"
done

echo "====================================="

# Select device
if [ -n "$DEVICE_SERIAL" ]; then
  if ! adb devices | grep "^${DEVICE_SERIAL}" | grep -q "device$"; then
    echo "ERROR: Specified device serial '${DEVICE_SERIAL}' not found"
    echo "Available devices:"
    adb devices | grep "device$" | awk '{print "  " $1}'
    exit 1
  fi
  SERIAL="$DEVICE_SERIAL"
  echo "Using specified device: ${SERIAL}"
else
  SERIAL=$(adb devices | grep "device$" | head -n1 | awk '{print $1}')
  
  if [ "$DEVICE_COUNT" -gt 1 ]; then
    echo "WARNING: Multiple devices detected, using first: ${SERIAL}"
    echo "To select a specific device, set 'device_serial' in config"
  else
    echo "Using device: ${SERIAL}"
  fi
fi

# Detect network interface
echo "Detecting device network configuration..."
DEVICE_IFACE=$(adb -s "$SERIAL" shell ip route get 1.1.1.1 2>/dev/null | grep -o 'dev [^ ]*' | awk '{print $2}' | head -n1 || true)

if [ -z "$DEVICE_IFACE" ]; then
  echo "WARNING: Could not detect network interface, defaulting to bridge mode"
  DEVICE_IFACE="unknown"
fi

echo "Device network interface: ${DEVICE_IFACE}"

# Auto-detect mode if set to auto
if [ "$MODE" = "auto" ]; then
  if [[ "$DEVICE_IFACE" == wlan* ]]; then
    MODE="wireless"
    echo "Auto-detected WiFi, using wireless mode"
  else
    MODE="bridge"
    echo "Auto-detected Ethernet (or unknown), using bridge mode"
  fi
fi

echo "====================================="
echo "Operating in ${MODE} mode"
echo "====================================="

if [ "$MODE" = "wireless" ]; then
  echo "Enabling wireless ADB on port ${ADB_PORT}..."
  adb -s "$SERIAL" tcpip "${ADB_PORT}"
  sleep 2
  
  DEVICE_IP=$(adb -s "$SERIAL" shell ip route get 1.1.1.1 2>/dev/null | awk '{print $7}' | head -n1 || true)
  
  if [ -z "$DEVICE_IP" ]; then
    echo "ERROR: Could not determine device IP address"
    echo "Please ensure device is connected to WiFi"
    exit 1
  fi
  
  echo "====================================="
  echo "Wireless ADB enabled successfully!"
  echo "====================================="
  echo "Device IP: ${DEVICE_IP}"
  echo "Port: ${ADB_PORT}"
  echo ""
  echo "Connect from your computer:"
  echo "  adb connect ${DEVICE_IP}:${ADB_PORT}"
  echo ""
  echo "Deploy APKs:"
  echo "  adb install -r /path/to/app.apk"
  echo "====================================="
  
  echo "Monitoring wireless ADB connection (checking every ${CHECK_INTERVAL}s)..."
  
  while true; do
    sleep "${CHECK_INTERVAL}"
    
    if adb connect "${DEVICE_IP}:${ADB_PORT}" 2>&1 | grep -q "connected"; then
      echo "Wireless ADB still active at ${DEVICE_IP}:${ADB_PORT}"
    else
      echo "WARNING: Wireless ADB disconnected, re-enabling..."
      adb -s "$SERIAL" tcpip "${ADB_PORT}"
      sleep 2
      
      NEW_IP=$(adb -s "$SERIAL" shell ip route get 1.1.1.1 2>/dev/null | awk '{print $7}' | head -n1 || true)
      if [ -n "$NEW_IP" ] && [ "$NEW_IP" != "$DEVICE_IP" ]; then
        echo "Device IP changed from ${DEVICE_IP} to ${NEW_IP}"
        DEVICE_IP="$NEW_IP"
      fi
      
      echo "Wireless ADB re-enabled at ${DEVICE_IP}:${ADB_PORT}"
    fi
  done

else
  echo "Starting ADB bridge on port ${BRIDGE_PORT}..."
  echo "This mode works with Ethernet-connected devices"
  
  HAOS_IP=$(hostname -i | awk '{print $1}')
  
  echo "====================================="
  echo "ADB bridge started successfully!"
  echo "====================================="
  echo "Bridge address: ${HAOS_IP}:${BRIDGE_PORT}"
  echo ""
  echo "Connect from your computer:"
  echo "  adb connect ${HAOS_IP}:${BRIDGE_PORT}"
  echo ""
  echo "Deploy APKs:"
  echo "  adb install -r /path/to/app.apk"
  echo ""
  echo "Note: Commands are forwarded to USB device"
  echo "====================================="
  
  exec adb -a -P "${BRIDGE_PORT}" server nodaemon
fi
