## MyRvLink over BLE – Protocol Documentation (Current Understanding)

This document describes the **MyRvLink‑over‑BLE protocol** as we currently understand it from:

- Decompiled .NET DLLs (`OneControl.Direct.MyRvLinkBle`, `OneControl.Direct.MyRvLinkTcpIp`, etc.)
- Live HCI captures from the official OneControl Android app
- Behavior of the Unity/LCI gateway hardware

It focuses on the **Data Service gateway** variant (your hardware), which exposes the `00000030‑…` service and does **not** require TEA auth on the BLE link once bonded.

---

## 1. Transport & Link Layer

- **Transport**: Bluetooth Low Energy (BLE)
- **Roles**:
  - Client (central): Android device (e.g., Lenovo Tab M8)
  - Server (peripheral): LCI gateway (`LCIRemotebzB9CztDZ`, MAC `24:DC:C3:ED:1E:0A`)
- **Pairing/Bonding**:
  - LE Secure Connections with bonding:
    - SMP Pairing Request/Response
    - Public Key exchange
    - Pairing Confirm/Random
    - LE Data Length Change, etc.
  - Once bonded, the Data Service does not perform an additional TEA unlock step.
- **MTU / Data Length**:
  - Client requests increased MTU and data length after connection.
  - In logs we see MTU ~185 and LE Data Length Change events.

---

## 2. GATT Layout

### 2.1 Standard Services

The gateway exposes common GATT services:

- `0x1800` – Generic Access (GAP)
- `0x1801` – Generic Attribute (GATT)
- `0x180a` – Device Information

These behave as expected (device name, appearance, manufacturer, etc.).

### 2.2 Auth Service – `00000010-0200-a58e-e411-afe28044e62c`

- **Service UUID**: `00000010-0200-a58e-e411-afe28044e62c`
- **Typical handle range (example)**: `0x0028..0x0032`

Characteristics and descriptors (from HCI JSON):

- **Characteristic declarations** (`uuid16 = 0x2803`):
  - At `handle 0x0030`:
    - `service_uuid128 = 00000010-0200-a58e-e411-afe28044e62c`
    - `characteristic_uuid128 = 00000013-0200-a58e-e411-afe28044e62c`
    - Properties: includes **Read** + **Notify**.
  - At `handle 0x0031`:
    - `service_uuid128 = 00000010-0200-…`
    - `uuid128 = 00000014-0200-a58e-e411-afe28044e62c`
- **Descriptor**:
  - CCCD (`uuid16 = 0x2902`) under at least one characteristic (e.g., handle `0x0032`).

**Role**:

- In CAN Service gateways, this service participates in **TEA/seed/key auth**.
- For your **Data Service** gateway, once the bond exists, the gateway reports as “Data Service – no auth needed”, and MyRvLink flows on the Data Service without a separate TEA unlock.

### 2.3 Data Service – `00000030-0200-a58e-e411-afe28044e62c`

- **Service UUID**: `00000030-0200-a58e-e411-afe28044e62c`
- **Typical handle range (example)**: `0x0033..0x003a`

#### 2.3.1 Data Write Characteristic – `00000033-0200-a58e-e411-afe28044e62c`

- Declaration at `handle 0x0037` (`uuid16 = 0x2803`):
  - `service_uuid128 = 00000030-0200-…`
  - `characteristic_uuid128 = 00000033-0200-a58e-e411-afe28044e62c`
  - Properties: **Write**, **Write Without Response** (seen as `[WRITE, WRITE_NO_RESPONSE]`).
- **Role**: TX path **client → gateway** for encoded MyRvLink commands.

#### 2.3.2 Data Read / Notify Characteristic – `00000034-0200-a58e-e411-afe28044e62c`

- Characteristic at `handle 0x0039`:
  - `service_uuid128 = 00000030-0200-…`
  - `uuid128 = 00000034-0200-a58e-e411-afe28044e62c`
  - Properties: **Read**, **Notify** (in Android discovery).
- CCCD descriptor at `handle 0x003a`:
  - `uuid16 = 0x2902`
  - When written with `0x0001`, notifications are enabled.
- **Role**: RX path **gateway → client** delivering MyRvLink events as BLE notifications.

---

## 3. GATT Procedure Sequence (Official App)

From the HCI capture of the official OneControl app:

### 3.1 After Connection and Pairing

1. **Server Supported Features**
   - ATT opcode `0x08` (Read By Group Type Request)
   - `starting_handle = 0x0001`, `ending_handle = 0xffff`
   - `uuid16 = 0x2b3a` (Server Supported Features).

2. **Primary Service Discovery** (uuid16 = `0x2800`)
   - Multiple `Read By Group Type Request` calls:
   - Responses include:
     - GAP
     - GATT
     - Device Information
     - **Auth Service** (`00000010-0200-…`)
     - **Data Service** (`00000030-0200-…`)

3. **Characteristic Discovery** (uuid16 = `0x2803`)
   - Over the Auth service handle range → chars `00000011`, `00000012`, `00000013`, `00000014`.
   - Over the Data service handle range → chars `00000031`, `00000033`, `00000034`.

4. **Descriptor Discovery** (especially CCCD `0x2902`)
   - Finds CCCDs for:
     - Auth characteristics (especially `00000014-…`).
     - Data Read characteristic `00000034-…` at `handle 0x003a`.

### 3.2 Enabling Notifications on Data Read

To start the MyRvLink data stream, the official app:

- Sends an ATT **Write Request** (`opcode = 0x12`) to CCCD at `handle 0x003a`:
  - `btatt.handle = 0x003a`
  - `btatt.characteristic_configuration_client = 0x0001`  
    → `notification = 1`, `indication = 0`.
- This is equivalent to writing bytes `01 00` to the CCCD of the `00000034-…` characteristic.

Once this is done, the gateway begins delivering notifications on `00000034…` (handle `0x0039`).

---

## 4. Data Transport: COBS Stream over Notifications

### 4.1 Notification Frames

After CCCD enable, the gateway sends:

- ATT opcode `0x1b` (Handle Value Notification)
- `btatt.handle = 0x0039` (Data Read characteristic)

Example from capture:

- Raw value: `00:06:03:08:0e:ff:fc:b7:00`
  - Starts with `0x00`, ends with `0x00` → consistent with **COBS framing** using 0x00 as a frame delimiter.
  - Length is small (9 bytes), not a large fixed size.

From the .NET code (`CobsStream` + `DirectConnectionMyRvLinkBle`):

- The app wraps the underlying stream (BLE or TCP) with a **COBS encoder/decoder**:
  - Frames are delimited by `0x00`.
  - CRC8 may be appended for integrity.
- Receive path (simplified):

  ```csharp
  int num = await cobsStream.ReadAsync(readBuffer, 0, readBuffer.Length, cancellationToken, TimeSpan.FromMilliseconds(8000.0));
  IMyRvLinkEvent ev = MyRvLinkEventFactory.TryDecodeEvent(new ArraySegment<byte>(readBuffer, 0, num), ...);
  ```

In other words:

- **BLE notifications → byte queue → COBS decode → MyRvLink event decode.**
- There is **no active `readCharacteristic` polling** of `00000034…` in the official app.

### 4.2 Our Android Bridge Alignment

In `OneControlBleService`:

- `handleCharacteristicNotification` enqueues notification payloads from:
  - `CAN_READ_CHAR_UUID` (when present),
  - `DATA_READ_CHAR_UUID` (`00000034-0200-…`).
- `startActiveStreamReading()` runs a background thread that:
  - Waits (up to 8s) on a `notificationQueue`.
  - Feeds bytes into a stateful COBS decoder (`CobsByteDecoder`).
  - On complete frames, calls `processIncomingData()` which mimics `MyRvLinkEventFactory.TryDecodeEvent`.
- **Important change based on HCI capture**:
  - We **removed** a fallback `readCharacteristic` on the Data Read characteristic that ran when the queue was empty.
    - That fallback produced 244‑byte, all‑zero responses that we fed into the COBS decoder, corrupting the stream.
  - The official app **never** uses `readCharacteristic` for this path; only notifications are used.

---

## 5. MyRvLink Message Layer

Above COBS, all messages are **MyRvLink events or commands**.

### 5.1 General Structure

- **Commands** (client → gateway, written to `00000033-…`):

  \[
    \text{[ClientCommandId (2 bytes, LE)] [CommandType (1 byte)] [Command-specific payload…]}
  \]

- **Events** (gateway → client, via notifications on `00000034-…`):

  \[
    \text{[EventType (1 byte)] [Event-specific payload…]}
  \]

### 5.2 GatewayInformation Event (EventType `0x01`)

Parsed by `MyRvLinkGatewayInformation.Decode()` and our Kotlin `handleGatewayInformationEvent`:

- Layout (after COBS decode, MyRvLink payload only):
  - `data[0]` – `EventType = 0x01`
  - `data[1]` – `ProtocolVersion`
  - `data[2]` – `Options`
  - `data[3]` – `DeviceCount`
  - `data[4]` – `DeviceTableId`
  - `data[5..8]` – `DeviceTableCrc` (LE 32‑bit)
  - `data[9..12]` – `DeviceMetadataTableCrc` (LE 32‑bit)

Behavior on receipt:

- Log protocol version, device count, table IDs/CRCs.
- If `DeviceTableId != 0x00`, update cached `deviceTableId`.
- Mark `gatewayInfoReceived = true` and call `onGatewayInfoReceived()`:
  - Set `isStarted = true` (MyRvLink started).
  - Schedule an initial `GetDevices` command after ~500ms.
  - Start periodic **heartbeat** (`GetDevices` keepalive).

### 5.3 GetDevices Command (CommandType `0x01`)

From decompiled C# and `MyRvLinkCommandBuilder`:

- Command layout:

  \[
    \text{[ClientCommandId (2 bytes, LE)] [0x01] [DeviceTableId] [StartDeviceId] [MaxDeviceRequestCount]}
  \]

Typical values:

- `ClientCommandId`:
  - Sequential `1..65535` (`ushort`), wraps back to 1.
- `DeviceTableId`:
  - Initially `0x00` (before GatewayInformation), then updated when `GatewayInformation` is received.
- `StartDeviceId`:
  - Usually `0` (start from first device).
- `MaxDeviceRequestCount`:
  - Often `0xFF` (request up to 255 devices).

Transport details:

- The MyRvLink command is **COBS‑encoded with CRC8**.
- Encoded bytes are written to `00000033-0200-…` (Data Write char), normally with `WRITE_TYPE_NO_RESPONSE`.

### 5.4 Heartbeat / Keepalive

MyRvLink does not define a dedicated “ping” event; instead, the official app sends **real commands** (notably `GetDevices`) periodically to:

- Keep the BLE link active.
- Refresh or confirm device state.

In `OneControlBleService`:

- `startHeartbeat()`:
  - Called after `onGatewayInfoReceived()` for Data Service gateways.
  - Every `HEARTBEAT_INTERVAL_MS` (default 5000ms):
    - If `isConnected`, `isAuthenticated`, and `bluetoothGatt != null`:
      - Build a `GetDevices` command using current `deviceTableId`.
      - COBS‑encode with CRC8.
      - Write to `00000033-…` (`dataWriteChar`) with `WRITE_NO_RESPONSE`.

Note:

- Before we receive GatewayInformation, `deviceTableId` may be `0x00`; the official app is cautious about sending commands until GatewayInformation is processed. Our current logic does not send GetDevices until GatewayInformation (or an explicit timeout strategy) says it is safe.

---

## 6. Data vs. CAN Service Modes

There are two major gateway “modes” exposed over BLE:

### 6.1 CAN Service Mode (Not Your Device)

- CAN service UUID: `00000000-0200-a58e-e411-afe28044e62c`
- Uses Auth service + TEA for gateway unlock:
  - Seed read
  - TEA key generation
  - Encrypted key/response write
- Data is wrapped in a `V2MessageType` and CAN frames on top of COBS.
- Our code has hooks for this but your gateway does not expose the CAN service, only the Data service.

### 6.2 Data Service Mode (Your Device)

- Only **Data Service** (`00000030-0200-…`) is present.
- After standard LE Secure Connections bonding, no additional TEA auth is required at the BLE layer.
- The stream is:
  - **MyRvLink events directly** on top of COBS, with no intermediate V2/CAN framing.
  - Commands are MyRvLink commands (GetDevices, ActionSwitch, ActionDimmable, etc.), not raw CAN frames.
- This is the mode our Android bridge is targeting and now aligns with.

---

## 7. Key Differences Between Official App and Earlier Bridge Attempts

Before incorporating HCI capture findings, our Android bridge:

- Correctly:
  - Discovered Auth and Data services.
  - Identified `00000033` (Data Write) and `00000034` (Data Read).
  - Enabled CCCD notifications on `00000034` (Data Read).
  - Implemented an active stream reading loop with COBS decoding, modeled on `CobsStream`.
- **Incorrectly**:
  - Added a fallback `readCharacteristic` on Data Read if no notifications arrived for 8 seconds.
    - This returned **244 bytes of all zeros** in practice.
    - We enqueued those bytes into the COBS pipeline, corrupting the decoded stream and preventing valid MyRvLink events.

The HCI capture shows:

- The official app **never performs active reads** on the Data Read characteristic for the MyRvLink stream.
- All MyRvLink data arrives via **notifications only** on `00000034…`.

Our latest code:

- Removed the active‑read fallback and now:
  - Relies exclusively on notifications to populate the COBS stream.
  - Keeps the stream logic faithful to the official app’s behavior.

---

## 8. Open Questions / Future Refinements

Even with this understanding, a few aspects remain open:

- **Full catalog of MyRvLink EventTypes and CommandTypes**:
  - We know key ones (`GatewayInformation`, `GetDevices`, various Action/Status events), but haven’t exhaustively verified each with your RV.
- **GatewayInformation timing**:
  - We know it should arrive once the MyRvLink layer is “started”, but we still need to confirm whether any additional preconditions (e.g., specific reads, options, or flags) are required for your specific gateway firmware.
- **Advertisement‑based alerts**:
  - The DLLs indicate some alerts can be sent via BLE advertisements (manufacturer data) even when not connected. We may exploit this path in future for lightweight status/alert monitoring without a persistent BLE link.

This document will be updated as we refine decoding of additional event and command types and confirm behavior against your live system.


