# MyRvLink BLE Gateway Authentication Algorithm

## Overview

MyRvLink BLE gateways (those advertising Data Service `00000030-0200-a58e-e411-afe28044e62c`) use a challenge-response authentication mechanism before allowing BLE notifications. This document describes the algorithm and how it was discovered.

## Discovery Process

### 1. Initial Observation (HCI Capture Analysis)

From Wireshark HCI capture (`2025-12-3-6-06.json`), we observed the official OneControl app performing these operations:

**Connection 1:**
- Read `0x002d` (00000012 - unlock status) → Response: `B0 0A 12 6E`
- Write `0x002f` (00000013 - KEY) → Value: `E5 CC 01 81`
- Read `0x002d` again → Response: `55 6E 6C 6F 63 6B 65 64` ("Unlocked")

**Connection 2:**
- Read `0x002d` (00000012) → Response: `8F 27 D9 0E` (different!)
- Write `0x002f` (00000013) → Value: `B5 51 6C A4` (different!)
- Read `0x002d` again → Response: "Unlocked"

**Key Insight:** The KEY value is dynamically calculated from the challenge, not a static value.

### 2. Decompiled Code Analysis

Located the authentication implementation in decompiled C# code:

#### File: `ids.portable_ble (2.1.0.0)/ids.portable.ble.Platforms.Shared/BleDeviceUnlockManager.cs`

**Lines 33-43: The Encryption Algorithm**
```csharp
public static uint Encrypt(uint cypher, uint seed)
{
    uint num = 2654435769u;  // 0x9E3779B9 - TEA delta constant
    for (int i = 0; i < 32; i++)
    {
        seed += ((cypher << 4) + 1131376761) ^ (cypher + num) ^ ((cypher >> 5) + 1919510376);
        cypher += ((seed << 4) + 1948272964) ^ (seed + num) ^ ((seed >> 5) + 1400073827);
        num += 2654435769u;
    }
    return seed;  // Returns the encrypted seed as the KEY
}
```

**Lines 45-153: The Authentication Sequence**
```csharp
public static async Task<BleDeviceKeySeedExchangeResult> PerformKeySeedExchange(
    this IBleManager bleManager, 
    IDevice? device, 
    uint cypher,  // Device-specific cypher constant
    Guid serviceGuid,
    Guid seedCharacteristicGuid,  // 00000012
    Guid keyCharacteristicGuid,   // 00000013
    CancellationToken ct)
{
    // ... service and characteristic discovery ...
    
    for (attempts = 0; attempts < 3; attempts++)
    {
        // Step 1: Read challenge from SEED characteristic (00000012)
        byte[] buffer2 = await bleManager.ReadCharacteristicAsync(seedCharacteristic, ct);
        
        // Check if already unlocked
        if (IsUnlocked(buffer2))  // Checks if buffer equals "unlocked" ASCII
            return BleDeviceKeySeedExchangeResult.Succeeded;
        
        // Step 2: Calculate KEY from challenge
        uint valueUInt = buffer2.GetValueUInt32(0);  // Read as uint32
        if (valueUInt != 0)
        {
            uint value = Encrypt(cypher, valueUInt);  // Calculate KEY
            byte[] array = new byte[4];
            array.SetValueUInt32(value, 0);  // Write as uint32
            
            // Step 3: Write KEY response
            await bleManager.WriteCharacteristicAsync(keyCharacteristic, array, ct);
            await TaskExtension.TryDelay(500, ct);
        }
    }
    // Loop retries up to 3 times, reading unlock status again each iteration
}

// Helper function
static bool IsUnlocked(byte[]? buffer)
{
    if (buffer != null && buffer.Length == "unlocked".Length)
        return string.Equals("unlocked", AsciiEncoder.GetString(buffer), StringComparison.OrdinalIgnoreCase);
    return false;
}
```

### 3. Finding the Cypher Constant

Different gateway types use different cypher constants:

#### File: `OneControl.Direct.MyRvLinkBle (2.0.0.0)/OneControl.Direct.MyRvLinkBle/MyRvLinkBleGatewayScanResult.cs`

**Line 43:**
```csharp
public const uint RvLinkKeySeedCypher = 612643285u;  // 0x2483FFD5
```

**Other Gateway Types:**
- **X180T** (in `X180TGatewayScanResult.cs`): `3357376288u` (0xC8240020)
- **SureShade** (in `SureShadeGatewayScanResult.cs`): `1360062733u` (0x51137D0D)

### 4. Byte Order Discovery

Initial testing showed algorithm produced wrong results. Testing different byte orders revealed:

- **Seed (challenge)**: Read as **BIG-ENDIAN** uint32
- **KEY (response)**: Write as **BIG-ENDIAN** uint32

Example:
```
Challenge bytes: B0 0A 12 6E
Seed uint32: 0xB00A126E (big-endian)
KEY uint32: 0xE5CC0181 (from Encrypt function)
KEY bytes: E5 CC 01 81 (big-endian)
```

### 5. Verification

Tested algorithm against both HCI capture examples:

| Challenge (BE) | Calculated KEY (BE) | Expected (HCI) | Match |
|----------------|---------------------|----------------|-------|
| `B0 0A 12 6E` | `E5 CC 01 81` | `E5 CC 01 81` | ✅ |
| `8F 27 D9 0E` | `B5 51 6C A4` | `B5 51 6C A4` | ✅ |

**100% match on both test cases!**

## Algorithm Implementation

**Note:** The current Android app implementation calculates the KEY dynamically using the TEA algorithm for each authentication session. It does not use hardcoded or captured KEY values. Each challenge from the gateway is processed through the encryption function to generate the correct response.

### Kotlin Implementation

```kotlin
/**
 * Calculate authentication KEY from challenge
 * Algorithm from BleDeviceUnlockManager.Encrypt()
 * 
 * @param seed Challenge value (big-endian uint32)
 * @return KEY value (4 bytes, big-endian)
 */
private fun calculateAuthKey(seed: Long): ByteArray {
    val cypher = 612643285L  // MyRvLink RvLinkKeySeedCypher
    
    var cypherVar = cypher
    var seedVar = seed
    var num = 2654435769L  // TEA delta
    
    // 32 rounds of modified TEA encryption
    for (i in 0 until 32) {
        seedVar += ((cypherVar shl 4) + 1131376761L) xor 
                   (cypherVar + num) xor 
                   ((cypherVar shr 5) + 1919510376L)
        seedVar = seedVar and 0xFFFFFFFFL
        
        cypherVar += ((seedVar shl 4) + 1948272964L) xor 
                     (seedVar + num) xor 
                     ((seedVar shr 5) + 1400073827L)
        cypherVar = cypherVar and 0xFFFFFFFFL
        
        num += 2654435769L
        num = num and 0xFFFFFFFFL
    }
    
    // Return as big-endian bytes
    val result = seedVar.toInt()
    return byteArrayOf(
        ((result shr 24) and 0xFF).toByte(),
        ((result shr 16) and 0xFF).toByte(),
        ((result shr 8) and 0xFF).toByte(),
        ((result shr 0) and 0xFF).toByte()
    )
}
```

### Python Implementation

```python
def encrypt(cypher, seed):
    """
    BleDeviceUnlockManager.Encrypt() algorithm
    cypher: 612643285 for MyRvLink gateways
    seed: Challenge from gateway (big-endian uint32)
    returns: KEY value (big-endian uint32)
    """
    num = 2654435769  # 0x9E3779B9
    
    for i in range(32):
        seed += ((cypher << 4) + 1131376761) ^ (cypher + num) ^ ((cypher >> 5) + 1919510376)
        seed = seed & 0xFFFFFFFF
        cypher += ((seed << 4) + 1948272964) ^ (seed + num) ^ ((seed >> 5) + 1400073827)
        cypher = cypher & 0xFFFFFFFF
        num += 2654435769
        num = num & 0xFFFFFFFF
    
    return seed

# Example usage
challenge_bytes = [0xB0, 0x0A, 0x12, 0x6E]
seed = (challenge_bytes[0] << 24) | (challenge_bytes[1] << 16) | \
       (challenge_bytes[2] << 8) | (challenge_bytes[3] << 0)  # Big-endian

key = encrypt(612643285, seed)

key_bytes = [(key >> 24) & 0xFF, (key >> 16) & 0xFF, 
             (key >> 8) & 0xFF, (key >> 0) & 0xFF]  # Big-endian
# Result: [0xE5, 0xCC, 0x01, 0x81]
```

## Authentication Sequence

### GATT Characteristics

**Service:** `00000010-0200-a58e-e411-afe28044e62c` (Auth Service)

| UUID | Name | Properties | Purpose |
|------|------|------------|---------|
| `00000011` | SEED | READ, NOTIFY | Seed/challenge for TEA (CAN Service) |
| `00000012` | UNLOCK_STATUS | READ | Challenge/status for Data Service |
| `00000013` | KEY | WRITE, WRITE_NO_RESPONSE | Response to unlock gateway |
| `00000014` | - | READ, NOTIFY | Unknown (CAN-related notifications?) |

### Step-by-Step Procedure

1. **Connect to gateway** and perform service discovery

2. **Request MTU** (185 bytes)

3. **Authentication Handshake:**
   ```
   a. Read characteristic 00000012 (UNLOCK_STATUS)
   b. If response is 4 bytes (not "Unlocked"):
      - Parse as big-endian uint32: seed
      - Calculate: key = Encrypt(612643285, seed)
      - Write key to characteristic 00000013 (KEY)
      - Write type: WRITE_TYPE_NO_RESPONSE
   c. Wait 500ms
   d. Read characteristic 00000012 again
   e. If response is "Unlocked" (ASCII string), authentication succeeded
   ```

4. **Enable Notifications:**
   - Subscribe to `00000011` (Auth Service SEED - may carry CAN events)
   - Subscribe to `00000014` (Auth Service - may carry CAN events)
   - Subscribe to `00000034` (Data Service READ - main data channel)

5. **Initiate Communication:**
   - Send initial MyRvLink GetDevices command
   - Start periodic heartbeat (GetDevices every 5 seconds)

6. **Receive Data:**
   - Notifications arrive on one or more subscribed characteristics
   - Data is COBS-encoded MyRvLink frames
   - First notification typically arrives 69ms after CCCD write

## Important Notes

### UUIDs - Corrected Mapping

Previous documentation had `00000012` labeled as SEED, but:
- ✅ **Correct:** `00000011` = SEED (for CAN Service TEA auth)
- ✅ **Correct:** `00000012` = UNLOCK_STATUS (for Data Service challenge-response)
- ✅ **Correct:** `00000013` = KEY (for both auth methods)

### Gateway Type Detection

```csharp
// From MyRvLinkBleGatewayScanResult.cs
public static RvLinkGatewayType GatewayTypeFromDeviceName(string deviceName)
{
    if (deviceName.StartsWith("LCIABS", StringComparison.OrdinalIgnoreCase))
        return RvLinkGatewayType.AntiLockBraking;
    
    if (deviceName.StartsWith("LCISWAY", StringComparison.OrdinalIgnoreCase))
        return RvLinkGatewayType.Sway;
    
    return RvLinkGatewayType.Gateway;  // Default: Standard OneControl gateway
}
```

All gateway types use the same authentication algorithm with the same cypher constant (612643285).

### Why Data Service Gateways Need Authentication

Initially confusing because:
- `DirectConnectionMyRvLinkBle.cs` shows no explicit authentication calls
- This led us to believe Data Service = no auth required

**Reality:** The authentication happens at a **lower level** in `BleManager.cs`:
```csharp
// From BleManager.cs line 700-706
if (connectionParams.KeySeedCypher.HasValue)
{
    TaggedLog.Debug("BleManager", "Key/Seed Exchange - Starting");
    switch (await this.PerformKeySeedExchange(bleDevice, connectionParams.KeySeedCypher.Value, cancellationToken))
    {
        case BleDeviceKeySeedExchangeResult.Failed:
            throw new BleManagerConnectionKeySeedException("Key/Seed Exchange - Failed");
        // ...
    }
}
```

The `KeySeedCypher` value comes from the scan result (`MyRvLinkBleGatewayScanResult`), and authentication is performed during the connection setup by `BleManager`, **before** `DirectConnectionMyRvLinkBle` even opens the stream.

### CAN Service vs Data Service

- **CAN Service** (`00000000-0200-a58e-e411-afe28044e62c`):
  - Uses TEA encryption with SEED characteristic (`00000011`)
  - Older gateway firmware
  
- **Data Service** (`00000030-0200-a58e-e411-afe28044e62c`):
  - Uses challenge-response with UNLOCK_STATUS characteristic (`00000012`)
  - Newer/current gateway firmware
  - Still a CAN device underneath - talks to OneControl via CAN bus
  - Uses same encryption algorithm, just different characteristics

## Test Cases for Verification

Use these known challenge/response pairs to verify your implementation:

```python
# Test Case 1
challenge_1 = 0xB00A126E
expected_key_1 = 0xE5CC0181

# Test Case 2
challenge_2 = 0x8F27D90E
expected_key_2 = 0xB5516CA4

# Verify
cypher = 612643285
assert encrypt(cypher, challenge_1) == expected_key_1
assert encrypt(cypher, challenge_2) == expected_key_2
```

## Common Pitfalls

### 1. Byte Order Confusion
❌ **Wrong:** Reading/writing as little-endian (Android/Kotlin default)
✅ **Correct:** Both seed and KEY are **big-endian** (network byte order)

### 2. UUID Confusion
❌ **Wrong:** Using `00000011` (SEED) for Data Service authentication
✅ **Correct:** Use `00000012` (UNLOCK_STATUS) for Data Service

### 3. Static KEY Values
❌ **Wrong:** Using hardcoded KEY value from HCI capture
✅ **Correct:** Calculate KEY dynamically from each challenge

### 4. Wrong Cypher Constant
❌ **Wrong:** Using cypher from other gateway types (X180T, SureShade)
✅ **Correct:** MyRvLink gateways use `612643285` (0x2483FFD5)

### 5. Missing Authentication
❌ **Wrong:** Assuming Data Service doesn't need authentication
✅ **Correct:** Data Service DOES require authentication, just uses different characteristic

## Integration Example

### Android/Kotlin

```kotlin
// Constants
const val AUTH_SERVICE_UUID = "00000010-0200-a58e-e411-afe28044e62c"
const val UNLOCK_STATUS_UUID = "00000012-0200-a58e-e411-afe28044e62c"
const val KEY_UUID = "00000013-0200-a58e-e411-afe28044e62c"
const val MYRVLINK_CYPHER = 612643285L

// Step 1: Read challenge
fun startAuthentication() {
    val unlockChar = authService.getCharacteristic(UUID.fromString(UNLOCK_STATUS_UUID))
    bluetoothGatt.readCharacteristic(unlockChar)
}

// Step 2: Calculate and write KEY
override fun onCharacteristicRead(gatt: BluetoothGatt, characteristic: BluetoothGattCharacteristic, status: Int) {
    if (characteristic.uuid.toString() == UNLOCK_STATUS_UUID) {
        val data = characteristic.value
        
        if (data.size == 4) {
            // Parse challenge as big-endian
            val seed = ((data[0].toInt() and 0xFF) shl 24) or
                      ((data[1].toInt() and 0xFF) shl 16) or
                      ((data[2].toInt() and 0xFF) shl 8) or
                      ((data[3].toInt() and 0xFF) shl 0)
            
            // Calculate KEY
            val keyValue = calculateAuthKey(seed.toLong() and 0xFFFFFFFFL)
            
            // Write KEY
            val keyChar = authService.getCharacteristic(UUID.fromString(KEY_UUID))
            keyChar.value = keyValue
            keyChar.writeType = BluetoothGattCharacteristic.WRITE_TYPE_NO_RESPONSE
            gatt.writeCharacteristic(keyChar)
            
            // Wait 500ms then read unlock status again
            handler.postDelayed({
                gatt.readCharacteristic(unlockChar)
            }, 500)
        } else if (String(data, Charsets.UTF_8).equals("Unlocked", ignoreCase = true)) {
            // Authentication successful!
            enableNotifications()
        }
    }
}
```

## Troubleshooting

### Gateway Never Returns "Unlocked"
- ✅ Verify cypher constant (should be 612643285 for MyRvLink)
- ✅ Verify big-endian byte order
- ✅ Check encryption function matches decompiled code exactly
- ✅ Ensure using UNLOCK_STATUS (`00000012`), not SEED (`00000011`)

### Notifications Never Arrive
- ✅ Must complete authentication BEFORE enabling notifications
- ✅ Subscribe to ALL notification characteristics (00000011, 00000014, 00000034)
- ✅ Send initial GetDevices command after notifications enabled
- ✅ CAN gateways need to be "asked" for data - they won't send spontaneously

### Authentication Loop (Repeated Challenges)
- ✅ Wrong KEY calculation (verify algorithm)
- ✅ Wrong byte order (must be big-endian)
- ✅ Wrong cypher constant (must be 612643285)

## References

### Decompiled Source Files

**Algorithm:**
- `ids.portable_ble (2.1.0.0)/ids.portable.ble.Platforms.Shared/BleDeviceUnlockManager.cs`
  - Lines 33-43: `Encrypt()` function
  - Lines 45-153: `PerformKeySeedExchange()` sequence

**Cypher Constants:**
- `OneControl.Direct.MyRvLinkBle (2.0.0.0)/OneControl.Direct.MyRvLinkBle/MyRvLinkBleGatewayScanResult.cs`
  - Line 43: `RvLinkKeySeedCypher = 612643285u`
- `ids.portable_ble (2.1.0.0)/ids.portable.ble.Platforms.Shared.ScanResults/X180TGatewayScanResult.cs`
  - Line 12: `X180TKeySeedCypher = 3357376288u`
- `ids.portable_ble (2.1.0.0)/ids.portable.ble.Platforms.Shared.ScanResults/SureShadeGatewayScanResult.cs`
  - Line 13: `RvLinkKeySeedCypher = 1360062733u`

**Integration:**
- `ids.portable_ble (2.1.0.0)/ids.portable.ble.BleManager/BleManager.cs`
  - Lines 700-706: Where `PerformKeySeedExchange()` is called during connection

### HCI Capture Data

From official OneControl app Bluetooth HCI snoop log:
- Challenge/response pairs verified the algorithm
- Timing between steps (500ms delays)
- Byte order confirmation

## Magic Constants Explained

All constants from `BleDeviceUnlockManager.Encrypt()`:

```
2654435769   = 0x9E3779B9 = TEA delta (golden ratio * 2^32)
1131376761   = 0x43729561 = TEA constant 1
1919510376   = 0x7265746E = TEA constant 2 (ASCII "retn")
1948272964   = 0x7421ED44 = TEA constant 3
1400073827   = 0x5378A963 = TEA constant 4
```

These are standard TEA encryption constants, slightly modified.

## Summary

**Authentication is REQUIRED** for MyRvLink Data Service gateways, despite what initial code review suggested. The algorithm is a modified TEA encryption using a challenge-response mechanism:

1. Gateway sends 4-byte challenge via `00000012` read
2. Client calculates KEY = `Encrypt(612643285, challenge)` **dynamically** (not a static/hardcoded value)
3. Client writes KEY to `00000013`
4. Gateway validates and returns "Unlocked"
5. Notifications can now be enabled and will flow

**Current Implementation Status:** The Android app fully implements dynamic KEY calculation using the TEA algorithm. Each authentication session reads the challenge from the gateway and computes the correct KEY response in real-time. No hardcoded or captured KEY values are used.

Without this authentication, the gateway will accept CCCD subscription but **will not send any notifications**, leading to timeout and disconnect.

