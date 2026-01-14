# Release v2.5.7 - Mopeka Pro Tank Sensor Integration

## Overview
This release adds comprehensive support for Mopeka Pro tank sensors with temperature-compensated ultrasonic readings and accurate geometric volume calculations.

## New Features

### Mopeka Pro Tank Sensor Plugin
- **Passive BLE scanning** - No GATT connection required, sensors broadcast readings
- **Temperature-compensated distance readings** - Uses polynomial coefficients for accuracy across temperature ranges
- **Geometric tank percentage calculations** - Accurate volume-based percentage for different tank shapes
- **Multiple tank type support**:
  - Vertical propane: 20lb, 30lb, 40lb
  - Horizontal tanks: 250gal, 500gal, 1000gal
  - European tanks: 6kg, 11kg, 14kg
  - Custom tank dimensions
- **Multiple medium types**: propane, air, fresh water, waste water, gasoline, diesel, LNG, oil, hydraulic oil, and custom
- **Web UI configuration** - Easy dropdown selection for medium type and tank type

### Home Assistant Integration
Four MQTT discovery entities per sensor:
- **Battery** - Percentage with diagnostic classification
- **Temperature** - Celsius reading from sensor
- **Tank Level** - Percentage with JSON attributes including:
  - Raw distance in mm
  - Localized distance in inches
- **Read Quality** - 0-3 scale diagnostic sensor

### Improvements
- Passive plugin health indicators (green when receiving data)
- Fixed instance update validation bug (service must be stopped to edit)
- Enhanced documentation in INTERNALS.md

## Technical Details

### Temperature Compensation
Uses polynomial coefficients from the mopeka-iot-ble library to adjust ultrasonic readings based on temperature:
```
distance_mm = raw_reading × (c0 + c1×temp + c2×temp²)
```

### Geometric Volume Calculations
- **Vertical tanks**: 2:1 ellipsoid caps + cylindrical middle section
- **Horizontal tanks**: Spherical caps + horizontal cylinder partial volume
- Formulas based on Home Assistant community contributions

## Credits
- **sbrogan** - Original BLE protocol decoding work
- **jrhelbert** - Volumetric calculation formulas (Home Assistant community)
- **Home Assistant mopeka-iot-ble library** - Temperature compensation coefficients

## Testing
- All 43 automated tests passing
- Build verified on Android 8.0+ devices
- Live sensor verification with propane tanks

## Installation
Install the APK and configure Mopeka sensors through the web UI:
1. Add new plugin instance → Select "Mopeka"
2. Set unique instance ID
3. Select medium type (e.g., "propane")
4. Select tank type (e.g., "20lb_v" for vertical 20lb propane)
5. Start service
6. Sensors will auto-discover in Home Assistant

## Version Info
- Version: 2.5.7
- Build: 39
- Min Android: 8.0 (API 26)
- Target Android: 14 (API 34)
