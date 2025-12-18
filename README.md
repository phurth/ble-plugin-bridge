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

## Documentation

- [Architecture Design](docs/ARCHITECTURE.md) - Detailed system architecture and design decisions
- [Migration Guide](docs/MIGRATION.md) - (Coming soon) OneControl migration plan
- [Plugin Development](docs/PLUGIN_DEVELOPMENT.md) - (Coming soon) How to create new plugins

## Project Status

**Current Phase**: Design & Planning  
**Target Release**: Q1 2025

### Roadmap

- [x] Feasibility assessment
- [x] Architecture design
- [ ] Phase 1: Output abstraction (MQTT plugin)
- [ ] Phase 2: Core plugin infrastructure
- [ ] Phase 3: OneControl migration
- [ ] Phase 4: EasyTouch plugin
- [ ] Phase 5: UI improvements
- [ ] Phase 6: Memory optimization
- [ ] Phase 7: Release

## Development Setup

*Coming soon - Android Studio setup instructions*

## Contributing

This project is currently in active development. Contributions welcome after initial release.

## License

*TBD*

## Related Projects

- [OneControl BLE Bridge](https://github.com/phurth/onecontrol-ble-mqtt-gateway) - Original OneControl implementation
- [HACS Micro-Air Integration](https://github.com/k3vmcd/ha-micro-air-easytouch) - EasyTouch Home Assistant integration
