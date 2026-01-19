# ADB Bridge for Home Assistant
# Manages USB Android devices via ADB

# Read config
CONFIG_PATH=/data/options.json
MODE=$(jq -r '.mode' $CONFIG_PATH)
DEVICE_SERIAL=$(jq -r '.device_serial // empty' $CONFIG_PATH)
ADB_PORT=$(jq -r '.adb_port' $CONFIG_PATH)
CHECK_INTERVAL=$(jq -r '.check_interval' $CONFIG_PATH)

echo "======================================"
echo "ADB Bridge v1.0.9 - HAOS Edition"
echo "======================================"
echo "Mode: ${MODE}"
echo ""

# Start ADB server (will use USB devices via gpio: true)
echo "Starting ADB server..."
adb start-server
sleep 2

echo "Scanning for USB devices..."
WAIT_COUNT=0
while [ $WAIT_COUNT -lt 30 ]; do
  DEVICES=$(adb devices 2>&1 | grep -c "device$" || true)
  if [ "$DEVICES" -gt 0 ]; then
    echo "Found $DEVICES device(s)!"
    break
  fi
  echo "  Waiting... ($WAIT_COUNT/30)"
  sleep 2
  WAIT_COUNT=$((WAIT_COUNT + 1))
done

if [ "$DEVICES" -eq 0 ]; then
  echo ""
  echo "ERROR: No USB devices detected"
  echo ""
  echo "Debugging info:"
  echo "USB devices visible:"
  ls -la /dev/bus/usb/ 2>/dev/null | head -5
  echo ""
  echo "Troubleshooting:"
  echo "1. Is device plugged into host?"
  echo "2. Check Proxmox USB passthrough"
  echo "3. Enable USB debugging on device"
  sleep infinity
fi

echo ""
echo "======================================"
echo "Connected Devices:"
echo "======================================"
adb devices -l | grep "device"
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
echo "Checking device network interface..."
NETWORK_IFACE=$(adb -s "$TARGET_SERIAL" shell ip route 2>/dev/null | grep "^default" | awk '{print $5}' || echo "unknown")
echo "Network interface: $NETWORK_IFACE"
echo ""

# Auto-detect mode
if [ "$MODE" = "auto" ]; then
  if [[ "$NETWORK_IFACE" == "wlan"* ]]; then
    MODE="wireless"
    echo "Auto-detected WiFi → wireless mode"
  else
    MODE="bridge"
    echo "Auto-detected Ethernet/Unknown → bridge mode"
  fi
fi

echo ""
echo "======================================"
echo "Operating in $MODE mode"
echo "======================================"
echo ""

if [ "$MODE" = "wireless" ]; then
  echo "Enabling wireless ADB..."
  adb -s "$TARGET_SERIAL" tcpip $ADB_PORT
  sleep 2
  
  DEVICE_IP=$(adb -s "$TARGET_SERIAL" shell ip addr show $NETWORK_IFACE 2>/dev/null | grep "inet " | awk '{print $2}' | cut -d'/' -f1)
  
  if [ -z "$DEVICE_IP" ]; then
    echo "ERROR: Could not get device IP!"
    sleep infinity
  fi
  
  echo ""
  echo "SUCCESS!"
  echo "Device IP: $DEVICE_IP"
  echo "Port: $ADB_PORT"
  echo ""
  echo "Connect from computer:"
  echo "  adb connect $DEVICE_IP:$ADB_PORT"
  echo ""
  echo "Monitor:"
  while true; do
    sleep "$CHECK_INTERVAL"
    if adb -s "$TARGET_SERIAL" get-state >/dev/null 2>&1; then
      echo "[$(date '+%H:%M:%S')] Connected"
    else
      echo "[$(date '+%H:%M:%S')] Device lost, re-enabling..."
      adb -s "$TARGET_SERIAL" tcpip $ADB_PORT 2>/dev/null || true
      sleep 2
    fi
  done
else
  echo "Bridge mode - monitoring device..."
  while true; do
    if adb -s "$TARGET_SERIAL" get-state >/dev/null 2>&1; then
      echo "[$(date '+%H:%M:%S')] Device connected"
    else
      echo "[$(date '+%H:%M:%S')] Device disconnected"
    fi
    sleep "$CHECK_INTERVAL"
  done
fi
