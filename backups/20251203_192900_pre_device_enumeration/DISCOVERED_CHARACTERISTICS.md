# Discovered BLE Characteristics - Analysis

## Services Found (5 total)

### 1. **Generic Attribute Profile Service** (`00001801-0000-1000-8000-00805f9b34fb`)
- Standard GATT service
- 1 characteristic:
  - `00002a05` - Service Changed [INDICATE]

### 2. **Generic Access Profile Service** (`00001800-0000-1000-8000-00805f9b34fb`)
- Standard GAP service
- 4 characteristics:
  - `00002a00` - Device Name [READ]
  - `00002a01` - Appearance [READ]
  - `00002a04` - Peripheral Preferred Connection Parameters [READ]
  - `00002aa6` - Central Address Resolution [READ]

### 3. **Auth Service** (`00000010-0200-a58e-e411-afe28044e62c`) ✅
- **OneControl Authentication Service**
- 4 characteristics:
  - `00000011` - [READ, NOTIFY] - **Unknown purpose**
  - `00000012` - [READ] - **Seed characteristic** ✅ (used for TEA auth)
  - `00000013` - [WRITE, WRITE_NO_RESPONSE] - **Key characteristic** ✅ (used for TEA auth)
  - `00000014` - [READ, NOTIFY] - **Unknown purpose**

### 4. **Data Service** (`00000030-0200-a58e-e411-afe28044e62c`) ✅
- **OneControl CAN-over-BLE Service**
- 3 characteristics:
  - `00000031` - [READ] - **Unknown purpose**
  - `00000033` - [WRITE, WRITE_NO_RESPONSE] - **Data Write** ✅ (CAN TX)
  - `00000034` - [READ, NOTIFY] - **Data Read** ✅ (CAN RX - subscribed)

### 5. **Device Information Service** (`0000180a-0000-1000-8000-00805f9b34fb`)
- Standard GATT service
- 2 characteristics:
  - `00002a26` - Firmware Revision String [READ]
  - `00002a29` - Manufacturer Name String [READ]

## Key Findings

### ✅ What We're Using:
1. **Auth Service** - Seed/Key exchange for TEA authentication ✅
2. **Data Service** - CAN-over-BLE communication ✅
   - Write to `00000033` to send CAN commands
   - Subscribe to `00000034` to receive CAN data

### ❌ What's Missing:
1. **CAN Service** (`00000000-0200-a58e-e411-afe28044e62c`) - **NOT FOUND**
   - Expected characteristics:
     - `00000001` - CAN Write
     - `00000002` - CAN Read
     - `00000005` - Unlock
   - **This gateway model uses Data Service instead of CAN Service!**

2. **Unlock Characteristic** (`00000005`) - **NOT FOUND**
   - Either:
     - Gateway is already unlocked (no PIN required)
     - This gateway model doesn't require unlock
     - Unlock is handled differently

### 🔍 Unknown Characteristics:
- `00000011` (Auth Service) - [READ, NOTIFY] - What is this?
- `00000014` (Auth Service) - [READ, NOTIFY] - What is this?
- `00000031` (Data Service) - [READ] - What is this?

## Current Status

✅ **Working:**
- Connection and pairing ✅
- Service discovery ✅
- Authentication (TEA) ✅
- Subscribed to Data Read notifications ✅
- MTU negotiation (185 bytes) ✅

⚠️ **Issues:**
- MQTT connection failing (authentication issue)
- No CAN data received yet (may need to send a command first)

## Next Steps

1. **Read unknown characteristics** to understand their purpose:
   - `00000011` - Auth Service
   - `00000014` - Auth Service
   - `00000031` - Data Service
   - Device Info characteristics (`00002a26`, `00002a29`)

2. **Send CAN command** via Data Write (`00000033`) to trigger response

3. **Fix MQTT authentication** to publish data to Home Assistant

4. **Monitor Data Read notifications** - should receive CAN bus data

