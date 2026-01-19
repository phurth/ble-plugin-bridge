#!/bin/bash

# ADB Bridge for Home Assistant
# Enables wireless ADB on USB-connected Android devices

# Read config
CONFIG_PATH=/data/options.json
MODE=$(jq -r '.mode' $CONFIG_PATH)
DEVICE_SERIAL=$(jq -r '.device_serial // empty' $CONFIG_PATH)
ADB_PORT=$(jq -r '.adb_port' $CONFIG_PATH)
CHECK_INTERVAL=$(jq -r '.check_interval' $CONFIG_PATH)

echo "======================================"
echo "ADB Bridge v1.0.6 - HAOS Edition"
echo "======================================"
echo "Mode: ${MODE}"
echo ""

echo "Waiting for connected USB devices..."
WAIT_COUNT=0
while [ $WAIT_COUNT -lt 30 ]; do
  DEVICES=$(adb devices 2>&1 | grep -c "device$" || true)
  if [ "$DEVICES" -gt 0 ]; then
    break
  fi
  echo "  No devices yet... waiting ($WAIT_COUNT/30)"
  sleep 2
  WAIT_COUNT=$((WAIT_COUNT + 1))
done

if [ "$DEVICES" -eq 0 ]; then
  echo "ERROR: No USB devices detected after 60 seconds"
  echo ""
  echo "Troubleshooting:"
  echo "1. Verify USB device is plugged into HAOS host"
  echo "2. Check Proxmox USB passthrough settings"
  echo "3. Enable USB debugging on Android device"
  echo "4. Accept authorization prompt on device"
  sleep infinity
fi

echo "Found $DEVICES device(s)"
echo ""

# List devices
echo "======================================"
echo "Connected Devices:"
echo "======================================"
adb devices -l | grep "device" | while read -r line; do
  SERIAL=$(echo "$line" | awk '{print $1}')
  echo "  $SERIAL"
done
echo ""

# Select device
if [ -n "$DEVICE_SERIAL" ]; then
  if ! adb devices | grep "^${DEVICE_SERIAL}" >/dev/null 2>&1; then
    echo "ERROR: Device '$DEVICE_SERIAL' not found!"
    sleep infinity
  fi
  TARGET_SERIAL="$DEVICE_SERIAL"
else
  TARGET_SERIAL=$(adb devices | grep "device$" | head -n1 | awk '{print $1}')
fi

echo "Target device: $TARGET_SERIAL"
echo ""

# Determine network interface
echo "======================================"
echo "Checking device network..."
echo "======================================"

NETWORK_IFACE=$(adb -s "$TARGET_SERIAL" shell ip route 2>/dev/null | grep "^default" | awk '{print $5}' || echo "unknown")
echo "Network interface: $NETWORK_IFACE"
echo ""

# Auto-detect mode
if [ "$MODE" = "auto" ]; then
  if [[ "$NETWORK_IFACE" == "wlan"* ]]; then
    MODE="wireless"
    echo "Auto-detected WiFi → using wireless mode"
  else
    MODE="bridge"
    echo "Auto-detected Ethernet/Unknown → using bridge mode"
  fi
fi

echo ""
echo "======================================"
echo "Starting in $MODE mode"
echo "======================================"
echo ""

if [ "$MODE" = "wireless" ]; then
  echo "Enabling wireless ADB on device..."
  adb -s "$TARGET_SERIAL" tcpip $ADB_PORT
  sleep 2
  
  DEVICE_IP=$(adb -s "$TARGET_SERIAL" shell ip addr show $NETWORK_IFACE 2>/dev/null | grep "inet " | awk '{print $2}' | cut -d'/' -f1 || echo "unknown")
  
  if [ "$DEVICE_IP" = "unknown" ]; then
    echo "ERROR: Could not determine device IP!"
    sleep infinity
  fi
  
  echo ""
  echo "======================================"
  echo "Wireless ADB Enabled"
  echo "======================================"
  echo "Device IP: $DEVICE_IP"
  echo "Port: $ADB_PORT"
  echo ""
  echo "Next steps:"
  echo "  adb connect $DEVICE_IP:$ADB_PORT"
  echo ""
  echo "To deploy APKs:"
  echo "  adb install -r app.apk"
  echo "======================================"
  echo ""
  
  echo "Monitoring connection every ${CHECK_INTERVAL}s..."
  while true; do
    sleep "$CHECK_INTERVAL"
    if adb -s "$TARGET_SERIAL" get-state >/dev/null 2>&1; then
      echo "[$(date '+%H:%M:%S')] Device still connected (USB)"
    else
      echo "[$(date '+%H:%M:%S')] WARNING: Device lost - checking..."
      if ! adb devices | grep "^${TARGET_SERIAL}" >/dev/null 2>&1; then
        echo "Device disconnected! Re-enabling wireless ADB..."
        adb -s "$TARGET_SERIAL" tcpip $ADB_PORT 2>/dev/null || true
        sleep 2
      fi
    fi
  done

else
  # Bridge mode - device is on Ethernet
  echo "Device is on Ethernet or unknown network"
  echo "Set to bridge mode for network forwarding"
  echo ""
  echo "Checking device connectivity..."
  
  while true; do
    if adb -s "$TARGET_SERIAL" get-state >/dev/null 2>&1; then
      echo "[$(date '+%H:%M:%S')] Device connected"
    else
      echo "[$(date '+%H:%M:%S')] Device disconnected"
    fi
    sleep "$CHECK_INTERVAL"
  done
fi
