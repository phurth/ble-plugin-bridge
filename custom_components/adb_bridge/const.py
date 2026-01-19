"""Constants for ADB Bridge integration."""

DOMAIN = "adb_bridge"

CONF_DEVICE_SERIAL = "device_serial"
CONF_CONNECTION_TYPE = "connection_type"
CONF_DEVICE_IP = "device_ip"
CONF_ADB_PORT = "adb_port"

CONNECTION_USB = "usb"
CONNECTION_WIFI = "wifi"

DEFAULT_ADB_PORT = 5555

# Services
SERVICE_ENABLE_WIFI_ADB = "enable_wifi_adb"
SERVICE_INSTALL_APK = "install_apk"
SERVICE_RUN_COMMAND = "run_command"
