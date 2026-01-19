# Configuration

## Mode

The add-on supports three modes:

### Auto (Recommended)
Automatically detects whether your Android device is connected via WiFi or Ethernet and selects the appropriate mode.

### Wireless
Forces wireless ADB mode. Best for devices connected to WiFi. The add-on will enable wireless ADB and keep the connection alive automatically.

**Requirements:**
- Device must be connected to WiFi (same network as Home Assistant)
- Wireless ADB only works over WiFi interface (Android limitation)

### Bridge
Forces bridge mode. Best for devices connected via Ethernet or when you want to connect from anywhere on your network.

**How it works:**
- Add-on accepts ADB commands over network
- Forwards commands to device via USB connection
- Works regardless of device network configuration

## Device Selection

### device_serial (Optional)
Specify which Android device to use when multiple devices are connected via USB.

**How to find device serial:**
1. Start the add-on without this setting
2. Check the add-on logs - all connected devices will be listed with their serials
3. Copy the serial of the device you want to use
4. Stop the add-on, add the serial to config, restart

**Example:**
```yaml
device_serial: "R58M123456A"
```

**If not set:** The add-on will use the first device found. If multiple devices are connected, a warning will be shown.

## Port Configuration

### adb_port (Default: 5555)
Port used for wireless ADB mode. Standard Android wireless ADB port. You can change this if port 5555 is already in use.

### bridge_port (Default: 5037)
Port used for bridge mode. Standard ADB server port. Change this if you're running another ADB server on your network.

### check_interval (Default: 3600)
How often (in seconds) to verify wireless ADB is still active and re-enable if needed.

- Minimum: 60 seconds
- Maximum: 86400 seconds (24 hours)
- Recommended: 3600 seconds (1 hour)

Lower values = more frequent checks but higher battery usage on Android device.

## Example Configurations

### WiFi Tablet (Auto Mode)
```yaml
mode: auto
adb_port: 5555
check_interval: 3600
```

### Ethernet Tablet (Forced Bridge)
```yaml
mode: bridge
bridge_port: 5037
```

### Multiple Devices (Custom Ports)
If running multiple instances for different devices:
```yaml
mode: auto
adb_port: 5556
bridge_port: 5038
```

## Network Access

After starting the add-on, you can connect to your Android device from:

- **Your laptop/desktop** on the same network
- **CI/CD pipelines** (if accessible)
- **Remote locations** (if you expose the port)

The add-on eliminates the need to physically access the Android device for app deployment.
