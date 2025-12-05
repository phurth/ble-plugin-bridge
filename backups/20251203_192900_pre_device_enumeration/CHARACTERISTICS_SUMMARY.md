# OneControl BLE Gateway - Available Characteristics

## Services and Characteristics Discovered

Based on the C# decompiled code and our connection flow, here are the characteristics available:

### 1. **CAN Service** (`00000000-0200-a58e-e411-afe28044e62c`)
This is the primary service for CAN-over-BLE communication.

#### Characteristics:
- **`00000001-0200-a58e-e411-afe28044e62c`** - CAN Write (TX)
  - **Properties**: WRITE, WRITE_NO_RESPONSE
  - **Purpose**: Send CAN messages to the gateway
  - **Usage**: Write CAN frames here to control devices
  
- **`00000002-0200-a58e-e411-afe28044e62c`** - CAN Read (RX)
  - **Properties**: READ, NOTIFY
  - **Purpose**: Receive CAN messages from the gateway
  - **Usage**: Subscribe to notifications to receive CAN bus data
  
- **`00000004-0200-a58e-e411-afe28044e62c`** - Read Version
  - **Properties**: READ
  - **Purpose**: Read gateway firmware version
  - **Usage**: Read to get version info
  
- **`00000005-0200-a58e-e411-afe28044e62c`** - Unlock
  - **Properties**: READ, WRITE
  - **Purpose**: Application-level unlock (not BLE pairing)
  - **Usage**: Read to check lock status, write PIN to unlock

### 2. **Auth Service** (`00000010-0200-a58e-e411-afe28044e62c`)
Used for TEA encryption authentication.

#### Characteristics:
- **`00000012-0200-a58e-e411-afe28044e62c`** - Seed
  - **Properties**: READ (protected)
  - **Purpose**: Read random seed for TEA encryption
  - **Usage**: Read seed, encrypt with cypher, write to Key characteristic
  
- **`00000013-0200-a58e-e411-afe28044e62c`** - Key
  - **Properties**: WRITE (protected)
  - **Purpose**: Write encrypted key to authenticate
  - **Usage**: Write TEA-encrypted seed value

### 3. **Data Service** (`00000030-0200-a58e-e411-afe28044e62c`)
Alternative/legacy service for CAN-over-BLE (may not be used by all gateways).

#### Characteristics:
- **`00000033-0200-a58e-e411-afe28044e62c`** - Data Write
  - **Properties**: WRITE
  - **Purpose**: Alternative CAN write path
  
- **`00000034-0200-a58e-e411-afe28044e62c`** - Data Read
  - **Properties**: READ, NOTIFY
  - **Purpose**: Alternative CAN read path

## What We Can Do

### ✅ Currently Implemented:
1. **Connect & Pair** - Establish BLE connection and bond
2. **Unlock Gateway** - Application-level unlock with PIN
3. **Authenticate** - TEA key/seed exchange
4. **Subscribe to CAN Data** - Receive CAN messages via notifications
5. **Publish to MQTT** - Send received data to Home Assistant

### 🔧 Can Be Added:
1. **Read Gateway Version** - Read characteristic `00000004` to get firmware version
2. **Send CAN Commands** - Write to CAN Write characteristic (`00000001`) to control devices
3. **Parse CAN Messages** - Decode received CAN frames and extract device states
4. **Device Control** - Turn lights on/off, control switches, etc. via CAN commands

## Next Steps

After rebuilding with enhanced logging, you'll see:
- All services and characteristics discovered
- Properties of each characteristic (READ, WRITE, NOTIFY, etc.)
- Which characteristics are actually available on your gateway

This will help us understand what capabilities your specific gateway has!

