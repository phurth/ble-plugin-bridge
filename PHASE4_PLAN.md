# Phase 4: OneControl Protocol Implementation

## Current Status: BLE Authentication COMPLETE ✅

**Authentication Working (January 2025):**
- ✅ Data Service gateway TEA authentication working
- ✅ Challenge-response handshake verified
- ✅ BIG-ENDIAN byte order for Data Service gateways
- ✅ WRITE_NO_RESPONSE for KEY characteristic
- ✅ All 3 notification subscriptions active (SEED, Auth, DATA)
- ✅ GetDevices command sent successfully
- ✅ Heartbeat running every 5 seconds
- ✅ Auto-reconnect on disconnection
- ✅ Default plugin enabled on fresh install

**Infrastructure Complete:**
- Plugin architecture with state persistence
- BLE scanning and connection working
- Service discovery operational
- OneControl gateway found and connected (24:DC:C3:ED:1E:0A)
- Protocol files already migrated to `onecontrol/protocol/`

## Remaining Phase 4 Work: MQTT Integration

Now that BLE authentication is working, wire device data to MQTT:

1. ✅ ~~Authenticate with gateway using TEA encryption~~
2. ✅ ~~Subscribe to CAN bus notifications~~
3. **→ Parse CAN messages into device states (IN PROGRESS)**
4. **→ Publish to MQTT/Home Assistant**
5. **→ Handle commands from MQTT**

## Architecture Gap - RESOLVED ✅

**Solution Implemented:** Extended `BlePluginInterface` with `onServicesDiscovered()` and `GattOperations`:
- Plugins now receive GATT operations interface
- Can read/write characteristics and enable notifications
- BaseBleService implements GattOperations for each device


## Implementation Plan

### Step 1: Extend Plugin Interface

Add to `BlePluginInterface`:

```kotlin
/**
 * Called after services are discovered.
 * Plugin can request characteristic operations.
 * 
 * @param device The connected device
 * @param gattOperations Interface for GATT operations
 * @return Result indicating if setup succeeded
 */
suspend fun onServicesDiscovered(
    device: BluetoothDevice,
    gattOperations: GattOperations
): Result<Unit>

interface GattOperations {
    suspend fun readCharacteristic(uuid: String): Result<ByteArray>
    suspend fun writeCharacteristic(uuid: String, value: ByteArray): Result<Unit>
    suspend fun enableNotifications(uuid: String): Result<Unit>
    suspend fun disableNotifications(uuid: String): Result<Unit>
}
```

### Step 2: Implement in BaseBleService

Update `BaseBleService.onServicesDiscovered()`:

```kotlin
override fun onServicesDiscovered(gatt: BluetoothGatt, status: Int) {
    // Existing service discovery...
    
    // Create GATT operations handler for this device
    val gattOps = object : BlePluginInterface.GattOperations {
        override suspend fun readCharacteristic(uuid: String) = 
            readCharacteristicAsync(gatt, uuid)
        // ... etc
    }
    
    // Call plugin's setup
    scope.launch {
        plugin.onServicesDiscovered(device, gattOps)
    }
}
```

### Step 3: OneControl Authentication Flow

Implement in `OneControlPlugin.onServicesDiscovered()`:

```kotlin
override suspend fun onServicesDiscovered(
    device: BluetoothDevice,
    gattOperations: GattOperations
): Result<Unit> {
    Log.i(TAG, "🔐 Starting authentication sequence...")
    
    // Step 1: Read SEED from auth service
    val seedResult = gattOperations.readCharacteristic(Constants.SEED_CHAR_UUID)
    if (seedResult.isFailure) {
        return Result.failure(Exception("Failed to read SEED"))
    }
    
    val seedBytes = seedResult.getOrThrow()
    val seed = bytesToLong(seedBytes)  // Little-endian
    
    // Step 2: Encrypt with TEA
    val encryptedKey = TeaEncryption.encrypt(gatewayCypher, seed)
    val keyBytes = longToBytes(encryptedKey)  // Little-endian
    
    // Step 3: Write KEY back
    val keyResult = gattOperations.writeCharacteristic(
        Constants.KEY_CHAR_UUID, 
        keyBytes
    )
    if (keyResult.isFailure) {
        return Result.failure(Exception("Failed to write KEY"))
    }
    
    Log.i(TAG, "✅ Authentication complete!")
    
    // Step 4: Enable CAN notifications
    gattOperations.enableNotifications(Constants.CAN_READ_CHAR_UUID)
    
    isAuthenticated = true
    return Result.success(Unit)
}
```

### Step 4: CAN Message Parsing

Update `onCharacteristicNotification()`:

```kotlin
override suspend fun onCharacteristicNotification(
    device: BluetoothDevice,
    characteristicUuid: String,
    value: ByteArray
): Map<String, String> {
    when (characteristicUuid.lowercase()) {
        Constants.CAN_READ_CHAR_UUID.lowercase() -> {
            // Decode COBS framing
            val decoded = CobsDecoder.decode(value)
            
            // Parse CAN message
            val canMessage = CanMessageParser.parse(decoded)
            
            // Extract device states
            val states = DeviceStatusParser.parseCanMessage(canMessage)
            
            // Convert to MQTT payloads
            return states.mapValues { (_, state) ->
                state.toJson()  // Format for Home Assistant
            }
        }
    }
    return emptyMap()
}
```

### Step 5: Command Handling

Implement `handleCommand()`:

```kotlin
override suspend fun handleCommand(
    device: BluetoothDevice,
    commandTopic: String,
    payload: String
): Result<Unit> {
    // Parse topic: light/bedroom/set -> deviceType=light, id=bedroom, cmd=set
    val (deviceType, deviceId, command) = parseCommandTopic(commandTopic)
    
    // Build CAN command
    val canCommand = MyRvLinkCommandBuilder.buildCommand(
        deviceType = deviceType,
        deviceId = deviceId,
        command = command,
        payload = payload
    )
    
    // Encode with COBS
    val encoded = MyRvLinkCommandEncoder.encode(canCommand)
    
    // Write to gateway (need to store gattOperations reference!)
    return gattOperations.writeCharacteristic(
        Constants.CAN_WRITE_CHAR_UUID,
        encoded
    )
}
```

## Files Ready to Use

All protocol files already migrated to `onecontrol/protocol/`:
- ✅ `TeaEncryption.kt` - TEA cipher
- ✅ `CobsDecoder.kt` / `CobsByteDecoder.kt` - COBS framing
- ✅ `CanMessageParser.kt` - CAN message parsing
- ✅ `DeviceStatusParser.kt` - State extraction
- ✅ `MyRvLinkCommandBuilder.kt` - Command construction
- ✅ `MyRvLinkCommandEncoder.kt` - Command encoding
- ✅ `HomeAssistantMqttDiscovery.kt` - HA discovery payloads
- ✅ `Constants.kt` - UUIDs and protocol constants

## Testing Plan

1. **Authentication Test:**
   - Connect to gateway
   - Verify SEED read
   - Verify KEY write
   - Check logs for authentication success

2. **CAN Data Test:**
   - Monitor CAN notifications
   - Verify COBS decoding
   - Verify CAN parsing
   - Check MQTT payloads

3. **Command Test:**
   - Send command via MQTT
   - Verify CAN encoding
   - Verify gateway receives command
   - Verify device responds

## Alternative: Quick Prototype

If GATT operation interface is too complex for now, could implement a simplified version:

1. Store `BluetoothGatt` reference in plugin
2. Pass GATT to plugin in `onDeviceConnected()`
3. Plugin manages own GATT operations
4. Less clean but faster to prototype

## Recommendation

**Implement proper GATT operations interface** - it's the right architecture and will benefit all future plugins (EasyTouch, etc.). The work is minimal and sets up Phase 4+ correctly.

## Next Steps

1. Extend `BlePluginInterface` with `onServicesDiscovered()` and `GattOperations`
2. Implement GATT operations in `BaseBleService`
3. Implement OneControl authentication in plugin
4. Test with real gateway
5. Implement CAN parsing
6. Test end-to-end with real devices

## Success Criteria

- ✅ Gateway authenticates successfully
- ✅ CAN notifications received and parsed
- ✅ Device states published to MQTT
- ✅ Commands sent from MQTT execute on devices
- ✅ Home Assistant discovery working
- ✅ Real-time updates from RV systems
