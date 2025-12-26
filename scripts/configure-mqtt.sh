#!/bin/bash
# Configure MQTT broker credentials via ADB
# This allows completely remote operation without any device interaction

if [ "$#" -lt 1 ]; then
    echo "Usage: $0 <broker_url> [username] [password]"
    echo ""
    echo "Examples:"
    echo "  $0 tcp://192.168.1.100:1883"
    echo "  $0 tcp://homeassistant.local:1883 mqtt mypassword"
    echo "  $0 tcp://broker.hivemq.com:1883"
    echo ""
    echo "Current configuration:"
    adb shell "run-as com.blemqttbridge cat /data/data/com.blemqttbridge/shared_prefs/ble_bridge_config.xml 2>/dev/null | grep -E 'mqtt_broker_url|mqtt_username|mqtt_password'" || echo "  (Not accessible - app may not be installed)"
    exit 1
fi

BROKER_URL="$1"
USERNAME="${2:-mqtt}"
PASSWORD="${3:-mqtt}"

echo "==================================="
echo "MQTT Broker Configuration via ADB"
echo "==================================="
echo ""
echo "Broker URL: $BROKER_URL"
echo "Username: $USERNAME"
echo "Password: ${PASSWORD:0:1}***"
echo ""

# Send broadcast to configure MQTT
adb shell am broadcast --receiver-foreground \
    -a com.blemqttbridge.CONFIGURE_MQTT \
    --es broker_url "$BROKER_URL" \
    --es username "$USERNAME" \
    --es password "$PASSWORD"

echo ""
echo "Configuration sent!"
echo ""
echo "Next steps:"
echo "1. Start service via ADB:"
echo "   adb shell am broadcast --receiver-foreground -a com.blemqttbridge.CONTROL_COMMAND --es command start_service"
echo ""
echo "2. Or use MQTT to control it:"
echo "   mosquitto_pub -h YOUR_BROKER -u $USERNAME -P '***' -t 'homeassistant/bridge/control' -m '{\"command\":\"start_service\"}'"
echo ""
