# ADB Bridge Add-on for Home Assistant

Maintain wireless ADB connection to USB-connected Android devices, enabling remote APK deployment and device management.

## Features

- **Automatic Mode Detection**: Detects WiFi vs Ethernet and chooses optimal mode
- **Wireless ADB**: Keeps wireless ADB alive for WiFi-connected devices
- **Bridge Mode**: ADB over network for Ethernet-connected devices
- **Works with any Android device**: Not limited to specific apps

## Installation

1. Add this repository to Home Assistant:
   - Go to **Supervisor** → **Add-on Store** → **⋮** (three dots) → **Repositories**
   - Add: `https://github.com/phurth/ble-plugin-bridge`

2. Install the **ADB Bridge** add-on

3. Connect your Android device via USB to the Home Assistant host

4. Enable **USB Debugging** on your Android device:
   - Settings → About Phone → Tap Build Number 7 times
   - Settings → Developer Options → Enable USB Debugging
   - Authorize the computer when prompted

5. Configure and start the add-on

## Configuration

### Mode Options

- `auto` (default): Automatically detect WiFi or Ethernet and choose best mode
- `wireless`: Force wireless ADB mode (WiFi only)
- `bridge`: Force bridge mode (works with Ethernet)

### Port Settings

- `adb_port`: Port for wireless ADB (default: 5555)
- `bridge_port`: Port for ADB bridge (default: 5037)
- `check_interval`: How often to check wireless ADB connection in seconds (default: 3600)

### Example Configuration

```yaml
mode: auto
adb_port: 5555
bridge_port: 5037
check_interval: 3600
```

## Usage

### Wireless Mode (WiFi devices)

Once the add-on starts, check the logs for the device IP:

```bash
adb connect 192.168.1.100:5555
adb install -r app.apk
```

### Bridge Mode (Ethernet devices)

Connect to the Home Assistant host instead:

```bash
adb connect homeassistant.local:5037
adb install -r app.apk
```

## Troubleshooting

**No device detected:**
- Ensure USB cable is properly connected
- Check that USB debugging is enabled
- Look for authorization prompt on device screen

**Wireless ADB keeps disconnecting:**
- Increase `check_interval` to check more frequently
- Ensure device WiFi power saving is disabled
- Check router firewall settings

**Bridge mode not working:**
- Verify Home Assistant firewall allows port 5037
- Try using IP address instead of hostname
- Check add-on logs for errors

## Support

For issues or questions, visit: https://github.com/phurth/ble-plugin-bridge/issues
