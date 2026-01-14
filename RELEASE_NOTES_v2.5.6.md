Major features in this release:

## Multi-Instance Plugin Architecture (v2.6.0)
- **Multiple instances of same plugin type** can now coexist independently
- Each instance manages its own BLE device connection with unique configuration
- Instance-based storage with unique instanceId format: {pluginType}_{macSuffix}
- Per-instance status tracking (connected, healthy indicators)
- Full web API for instance management: /api/instances/add, /api/instances/remove, /api/instances/update

## Comprehensive Testing Infrastructure (43 tests, 0 failures)
- New TestContextHelper utility with in-memory SharedPreferences mock
- 6 unit tests for instance persistence (ServiceStateManagerTest)
- 4 unit tests for protocol parsing (DeviceStatusParserTest)
- 6 unit tests for HTTP API validation (WebServerEndpointTest)
- 7 unit tests for plugin lifecycle (PluginRegistryTest)
- 9 unit tests for multi-instance operations (MultiInstanceWebApiTest)
- Additional tests for MQTT, GoPower, and mock battery plugins

## Other Improvements
- Comprehensive INTERNALS.md documentation revision
  - New section on Multi-Instance Plugin Architecture
  - New section on Testing Infrastructure with examples
  - Updated version history and table of contents
- EasyTouch plugin fully supports multi-instance mode
- All plugins prepared for future multi-instance support

## APK Details
- versionCode: 36
- versionName: 2.5.6
- minSdk: 26 (Android 8.0)
- targetSdk: 34 (Android 14)
- Size: 12MB (release signed APK)

## Testing
All automated tests pass successfully:
```
./gradlew testDebugUnitTest
BUILD SUCCESSFUL
```

## What's New Since v2.5.5
- Multi-instance support for simultaneous management of multiple devices per plugin type
- Comprehensive automated test suite for regression prevention
- Enhanced documentation for future LLM-assisted development
- Web UI instance management with full CRUD operations

**Note:** This is a stable release ready for production deployment. No breaking changes from v2.5.5.
