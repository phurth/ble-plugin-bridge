# BLE to MQTT Bridge - Plugin Architecture

Multi-device BLE to Home Assistant MQTT bridge with extensible plugin system.

## Overview

This Android app provides a bridge between BLE devices and Home Assistant via MQTT, with a plugin architecture that supports multiple device protocols:

- **OneControl**: Lippert OneControl RV gateway
- **EasyTouch**: Micro-Air EasyTouch RV thermostat
- **Extensible**: Add new device plugins easily

## Key Features

- 🔌 Plugin-based architecture for multiple BLE device types
- 📡 Configurable output destinations (MQTT, REST API, webhooks)
- 🧠 Memory-efficient design for low-end Android tablets
- 🏠 Home Assistant MQTT Discovery integration
- ⚡ Optimized for coexistence with Fully Kiosk Browser
- 🔒 Zero-regression guarantee for existing OneControl users
- 🎮 **Remote control via MQTT** - Start/stop service and manage plugins remotely

## Documentation

- [Architecture Design](docs/ARCHITECTURE.md) - Detailed system architecture and design decisions
- [MQTT Setup Guide](MQTT_SETUP_GUIDE.md) - **Complete hands-free operation setup**
- [Remote Control API](REMOTE_CONTROL_API.md) - Complete API reference for remote service control
- [Remote Control Quick Start](REMOTE_CONTROL_QUICKSTART.md) - Practical examples and usage guide
- [ADB Control Guide](ADB_CONTROL_GUIDE.md) - Complete guide for ADB-based remote control (development)
- [Migration Guide](docs/MIGRATION.md) - (Coming soon) OneControl migration plan
- [Plugin Development](docs/PLUGIN_DEVELOPMENT.md) - (Coming soon) How to create new plugins

## Project Status

**Current Phase**: Phase 4 - OneControl MQTT Integration  
**Target Release**: Q1 2025

### Roadmap

- [x] Feasibility assessment
- [x] Architecture design
- [x] Phase 1: Output abstraction (MQTT plugin)
- [x] Phase 2: Core plugin infrastructure
- [x] Phase 3: OneControl BLE authentication ✅
  - Data Service gateway TEA authentication working
  - Auto-start service with default plugin enabled
  - Full authentication cycle: connect → auth → unlock → subscribe
  - Heartbeat and auto-reconnect implemented
- [ ] Phase 4: OneControl MQTT data integration (IN PROGRESS)
  - Wire CAN data to MQTT output
  - Home Assistant discovery payloads
  - Command handling (MQTT → CAN)
- [ ] Phase 5: EasyTouch plugin
- [ ] Phase 6: UI improvements
- [ ] Phase 7: Memory optimization
- [ ] Phase 8: Release

## Development Setup

*Coming soon - Android Studio setup instructions*

## Contributing

This project is currently in active development. Contributions welcome after initial release.

## License

*TBD*

## Related Projects

- [OneControl BLE Bridge](https://github.com/phurth/onecontrol-ble-mqtt-gateway) - Original OneControl implementation
- [HACS Micro-Air Integration](https://github.com/k3vmcd/ha-micro-air-easytouch) - EasyTouch Home Assistant integration
