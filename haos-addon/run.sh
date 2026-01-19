#!/usr/bin/with-contenv bashio

# Get configuration options
MODE=$(bashio::config 'mode')
ADB_PORT=$(bashio::config 'adb_port')
BRIDGE_PORT=$(bashio::config 'bridge_port')
CHECK_INTERVAL=$(bashio::config 'check_interval')

bashio::log.info "====================================="
bashio::log.info "Starting ADB Bridge v1.0.0"
bashio::log.info "====================================="
bashio::log.info "Mode: ${MODE}"

# Wait for USB device to appear
bashio::log.info "Waiting for Android device via USB..."
RETRY_COUNT=0
MAX_RETRIES=30

while ! adb devices | grep -q "device$"; do
  sleep 2
  RETRY_COUNT=$((RETRY_COUNT + 1))
  
  if [ $RETRY_COUNT -ge $MAX_RETRIES ]; then
    bashio::log.error "No Android device detected after 60 seconds"
    bashio::log.error "Please check:"
    bashio::log.error "  1. USB cable is connected"
    bashio::log.error "  2. USB debugging is enabled on Android device"
    bashio::log.error "  3. Device is authorized (check device screen)"
    exit 1
  fi
done

SERIAL=$(adb devices | grep "device$" | awk '{print $1}')
bashio::log.info "✓ Device found: ${SERIAL}"

# Detect network interface
bashio::log.info "Detecting device network configuration..."
DEVICE_IFACE=$(adb shell ip route get 1.1.1.1 2>/dev/null | grep -o 'dev [^ ]*' | awk '{print $2}' | head -n1)

if [ -z "$DEVICE_IFACE" ]; then
  bashio::log.warning "Could not detect network interface, defaulting to bridge mode"
  DEVICE_IFACE="unknown"
fi

bashio::log.info "Device network interface: ${DEVICE_IFACE}"

# Auto-detect mode if set to auto
if [ "$MODE" = "auto" ]; then
  if [[ "$DEVICE_IFACE" == "wlan"* ]]; then
    MODE="wireless"
    bashio::log.info "✓ Auto-detected WiFi, using wireless mode"
  else
    MODE="bridge"
    bashio::log.info "✓ Auto-detected Ethernet (or unknown), using bridge mode"
  fi
fi

bashio::log.info "====================================="
bashio::log.info "Operating in ${MODE} mode"
bashio::log.info "====================================="

if [ "$MODE" = "wireless" ]; then
  # Mode 1: Enable wireless ADB (WiFi only)
  bashio::log.info "Enabling wireless ADB on port ${ADB_PORT}..."
  adb tcpip "${ADB_PORT}"
  sleep 2
  
  # Get device IP
  DEVICE_IP=$(adb shell ip route get 1.1.1.1 2>/dev/null | awk '{print $7}' | head -n1)
  
  if [ -z "$DEVICE_IP" ]; then
    bashio::log.error "Could not determine device IP address"
    bashio::log.error "Please ensure device is connected to WiFi"
    exit 1
  fi
  
  bashio::log.info "====================================="
  bashio::log.info "Wireless ADB enabled successfully!"
  bashio::log.info "====================================="
  bashio::log.info "Device IP: ${DEVICE_IP}"
  bashio::log.info "Port: ${ADB_PORT}"
  bashio::log.info ""
  bashio::log.info "Connect from your computer:"
  bashio::log.info "  adb connect ${DEVICE_IP}:${ADB_PORT}"
  bashio::log.info ""
  bashio::log.info "Deploy APKs:"
  bashio::log.info "  adb install -r /path/to/app.apk"
  bashio::log.info "====================================="
  
  # Keep wireless ADB alive
  bashio::log.info "Monitoring wireless ADB connection (checking every ${CHECK_INTERVAL}s)..."
  
  while true; do
    sleep "${CHECK_INTERVAL}"
    
    # Check if wireless ADB is still active
    if adb connect "${DEVICE_IP}:${ADB_PORT}" 2>&1 | grep -q "connected"; then
      bashio::log.debug "Wireless ADB still active at ${DEVICE_IP}:${ADB_PORT}"
    else
      bashio::log.warning "Wireless ADB disconnected, re-enabling..."
      
      # Re-enable via USB
      adb tcpip "${ADB_PORT}"
      sleep 2
      
      NEW_IP=$(adb shell ip route get 1.1.1.1 2>/dev/null | awk '{print $7}' | head -n1)
      if [ -n "$NEW_IP" ] && [ "$NEW_IP" != "$DEVICE_IP" ]; then
        bashio::log.info "Device IP changed from ${DEVICE_IP} to ${NEW_IP}"
        DEVICE_IP="$NEW_IP"
      fi
      
      bashio::log.info "Wireless ADB re-enabled at ${DEVICE_IP}:${ADB_PORT}"
    fi
  done

else
  # Mode 2: ADB Bridge (Ethernet compatible)
  bashio::log.info "Starting ADB bridge on port ${BRIDGE_PORT}..."
  bashio::log.info "This mode works with Ethernet-connected devices"
  
  # Get HAOS IP address
  HAOS_IP=$(hostname -i | awk '{print $1}')
  
  bashio::log.info "====================================="
  bashio::log.info "ADB bridge started successfully!"
  bashio::log.info "====================================="
  bashio::log.info "Bridge address: ${HAOS_IP}:${BRIDGE_PORT}"
  bashio::log.info ""
  bashio::log.info "Connect from your computer:"
  bashio::log.info "  adb connect ${HAOS_IP}:${BRIDGE_PORT}"
  bashio::log.info ""
  bashio::log.info "Deploy APKs:"
  bashio::log.info "  adb install -r /path/to/app.apk"
  bashio::log.info ""
  bashio::log.info "Note: Commands are forwarded to USB device"
  bashio::log.info "====================================="
  
  # Start ADB server in network mode and keep it running
  exec adb -a -P "${BRIDGE_PORT}" server nodaemon
fi
