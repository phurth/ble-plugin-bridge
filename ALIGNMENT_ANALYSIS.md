# Android App Alignment Analysis

## What We've Implemented ✅

1. **BLE Connection & Bonding**
   - ✅ Connection establishment
   - ✅ Pairing/bonding handling
   - ✅ Service/characteristic discovery

2. **Authentication**
   - ✅ TEA encryption (seed/key exchange)
   - ✅ Unlock (if needed)

3. **MyRvLink Protocol**
   - ✅ GetDevices command encoding (matches decompiled code exactly)
   - ✅ GatewayInformation event parsing
   - ✅ Command ID management
   - ✅ DeviceTableId extraction

4. **Data Handling**
   - ✅ COBS encoding/decoding
   - ✅ CRC8 verification
   - ✅ Notification subscription

## Critical Differences Found 🔴

### 1. **Service Mismatch**
- **Official App Uses:** CAN Service (`00000000-0200-a58e-e411-afe28044e62c`)
  - Characteristics: `00000001` (CAN Write), `00000002` (CAN Read), `00000005` (Unlock)
- **Your Gateway Has:** Data Service (`00000030-0200-a58e-e411-afe28044e62c`)
  - Characteristics: `00000033` (Data Write), `00000034` (Data Read)
- **Impact:** Your gateway model uses a different service structure. This might be a newer/older firmware version.

### 2. **Data Flow Processing**
The official app processes incoming BLE data in multiple layers:

```
BLE Notification (COBS-encoded)
  ↓
COBS Decode
  ↓
V2MessageType Parsing (Packed/ElevenBit/TwentyNineBit)
  ↓
CAN Message Format Conversion
  ↓
MyRvLink Event Decoding (GatewayInformation, DeviceCommand, etc.)
```

**What We're Doing:**
- ✅ COBS decode
- ❌ V2MessageType parsing (we're treating everything as MyRvLink events directly)
- ❌ CAN message format conversion

**Issue:** We might be missing the intermediate CAN message layer. The gateway might send V2MessageType messages that need to be converted to CAN format before MyRvLink events can be decoded.

### 3. **Initialization Sequence**
**Official App:**
1. Connect BLE
2. Get CAN Service
3. Request MTU (185)
4. Unlock ECU
5. Get CAN Read characteristic
6. Subscribe to notifications
7. **Wait for GatewayInformation event** (comes automatically)
8. **Then** send commands

**What We're Doing:**
1. Connect BLE ✅
2. Get Data Service ✅
3. Request MTU (185) ✅
4. Unlock (if needed) ✅
5. Get Data Read characteristic ✅
6. Subscribe to notifications ✅
7. **Immediately send GetDevices** ❌ (should wait for GatewayInformation first)

### 4. **Missing Code Layers**
The decompiled code shows these layers we might not have:

1. **BleCommunicationsAdapter** - Low-level BLE handling
   - `OnDataReceived()` - Processes raw BLE notifications
   - V2MessageType parsing (Packed, ElevenBit, TwentyNineBit)
   - Converts to internal CAN message format

2. **MyRvLinkEventFactory** - Event decoding
   - `TryDecodeEvent()` - Decodes MyRvLink events from CAN messages
   - Handles GatewayInformation, DeviceCommand, etc.

3. **DirectConnectionMyRvLink** - High-level protocol
   - `Start()` - Initializes but doesn't send commands
   - `OnReceivedEvent()` - Processes decoded events
   - Command tracking and response handling

## What We Might Be Missing 🔍

### 1. **V2MessageType Parsing**
The official app checks the first byte for V2MessageType:
- `V2MessageType.Packed` (0x00) - Packed CAN message
- `V2MessageType.ElevenBit` (0x01) - 11-bit CAN ID
- `V2MessageType.TwentyNineBit` (0x02) - 29-bit CAN ID

We're currently treating all decoded COBS data as MyRvLink events directly, but we might need to:
1. Check first byte for V2MessageType
2. Parse according to message type
3. Convert to CAN message format
4. Then decode as MyRvLink event

### 2. **GatewayInformation Event Timing**
The official app **waits** for GatewayInformation before sending any commands. We're sending GetDevices immediately, which might be:
- Rejected by the gateway
- Sent before the gateway is ready
- Using wrong DeviceTableId (0x00)

### 3. **Event vs Command Response**
- **Events** (like GatewayInformation): First byte is EventType (0x01 = GatewayInformation)
- **Command Responses**: First 2 bytes are ClientCommandId, then CommandType

We need to distinguish these correctly.

## Questions to Answer ❓

1. **Does your gateway send V2MessageType messages?**
   - Check first byte of decoded COBS data
   - Should be 0x00 (Packed), 0x01 (ElevenBit), or 0x02 (TwentyNineBit)

2. **Does GatewayInformation come automatically?**
   - Should arrive shortly after connection
   - Format: `[0x01][ProtocolVersion][Options][DeviceCount][DeviceTableId][...]` (13 bytes)

3. **Are we using the right service?**
   - Your gateway has Data Service, not CAN Service
   - But the protocol might be the same (just different UUIDs)

## Recommended Next Steps 🎯

1. **Add V2MessageType Parsing**
   - Check first byte after COBS decode
   - Parse according to message type
   - Convert to CAN format before MyRvLink decoding

2. **Wait for GatewayInformation**
   - Don't send GetDevices until GatewayInformation received
   - Use DeviceTableId from GatewayInformation

3. **Enhanced Logging**
   - Log raw BLE notifications (before COBS)
   - Log decoded COBS data (with hex dump)
   - Log V2MessageType detection
   - Log all MyRvLink events

4. **Check if Data Service = CAN Service**
   - The protocol might be identical
   - Just different UUIDs for different gateway models

## Conclusion

We're **mostly aligned** with the decompiled app, but there are **critical gaps**:

1. ✅ Command encoding is correct
2. ✅ Event parsing structure is correct
3. ❌ Missing V2MessageType layer (might be critical)
4. ❌ Timing issue (sending commands too early)
5. ⚠️ Service mismatch (but might be OK if protocol is same)

The **most likely issue** is that we're missing the V2MessageType parsing layer, which means we're not correctly interpreting the gateway's data format.

