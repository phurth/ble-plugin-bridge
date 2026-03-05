# OneControl Device Metadata Discovery Analysis

Date: 2026-02-27  
Scope: Decompiled official OneControl app code and secondary Cloud Gateway code

---

## Executive Summary

The OneControl ecosystem does **not** use a single metadata discovery mechanism. The codebase contains three distinct patterns:

1. **MyRvLink table-based discovery (primary BLE gateway flow)**
   - Devices and metadata are fetched as **separate tables**.
   - Gateway broadcasts table identifiers and CRCs.
   - App validates CRCs, merges rows by index, and materializes logical devices.

2. **IDS-CAN live identity discovery (direct CAN / BLE-CAN adapter flow)**
   - Metadata comes from live CAN identity and status messages.
   - No separate metadata table fetch step.
   - Transport framing differs by BLE gateway version (V1 vs V2/V2_D).

3. **Cloud IoT component-model discovery (remote cloud flow)**
   - Device inventory comes from cloud component APIs.
   - Metadata (such as software part number) is fetched per component via command/response trackers.

The app explicitly supports multiple gateway families and encoding schemes, including legacy and newer advertisement formats and different runtime frame formats.

---

## Sources Reviewed

### Official App Decompiled Tree

- `docs/onecontrol_plugin_docs/official_onecontrol_app_dlls_decompiled/OneControl.Direct.MyRvLink/...`
- `docs/onecontrol_plugin_docs/official_onecontrol_app_dlls_decompiled/OneControl.Direct.IdsCan/...`
- `docs/onecontrol_plugin_docs/official_onecontrol_app_dlls_decompiled/IDS.Portable.CAN/...`
- `docs/onecontrol_plugin_docs/official_onecontrol_app_dlls_decompiled/ids.portable.ble/...`
- `docs/onecontrol_plugin_docs/official_onecontrol_app_dlls_decompiled/OneControl.Direct.RvCloudIoT/...`

### Secondary Cloud Gateway Code

- `docs/onecontrol_plugin_docs/Cloud Gateway Code/OneControl.Program/...`
- `docs/onecontrol_plugin_docs/Cloud Gateway Code/IDS.Portable.CAN/...`

---

## 1) Primary BLE Gateway Path: MyRvLink Table + Metadata Table

## 1.1 Gateway info event seeds discovery

Key class:
- `MyRvLinkGatewayInformation`

Fields extracted from gateway event payload:
- Protocol major version
- Options (config/production mode flags)
- Device count
- Device table id
- Device table CRC
- Device metadata table CRC

This event is decoded by `MyRvLinkEventFactory` and consumed by `DirectConnectionMyRvLink.OnReceivedEvent(...)`.

When `GatewayInformation` arrives:
- `GatewayInfo` is set on the connection.
- A `MyRvLinkDeviceTracker` instance is created or refreshed using table id/CRC.
- Metadata tracker is initialized with metadata table CRC.
- Device fetch and metadata fetch are both triggered.

## 1.2 Device list command (physical table)

Command classes:
- `MyRvLinkCommandGetDevices`
- `MyRvLinkCommandGetDevicesResponse`
- `MyRvLinkCommandGetDevicesResponseCompleted`

Protocol behavior:
- Request includes `(tableId, startDeviceId, maxCount)`.
- Partial responses contain one or more serialized device entries.
- Completed response includes final `(deviceTableCrc, deviceCount)`.
- Command marks success only if count and completion checks pass.

Decode path:
- `MyRvLinkCommandGetDevicesResponse.DecodeDevice(...)`
- `MyRvLinkDevice.TryDecodeFromRawBuffer(...)`
  - dispatches by protocol byte to Host, IdsCan, or None.

## 1.3 Metadata list command (parallel metadata table)

Command classes:
- `MyRvLinkCommandGetDevicesMetadata`
- `MyRvLinkCommandGetDevicesMetadataResponse`
- `MyRvLinkCommandGetDevicesMetadataResponseCompleted`

Protocol behavior mirrors device list:
- Request by table id/range.
- Partial metadata entries streamed.
- Completed response includes `(metadataTableCrc, deviceCount)`.
- Validated for count and completion.

Decode path:
- `MyRvLinkCommandGetDevicesMetadataResponse.DecodeMetadata(...)`
- `MyRvLinkDeviceMetadata.TryDecodeFromRawBuffer(...)`
  - dispatches by protocol byte to HostMetadata, IdsCanMetadata, or fallback.

## 1.4 Merge and logical device materialization

Key class:
- `MyRvLinkDeviceTracker`

Merge semantics:
- Device list and metadata list must have equal length.
- Merge occurs by **index** (device row i + metadata row i).
- For each merged item:
  - Update physical row metadata (`MyRvLinkDeviceIdsCan.UpdateMetadata` or `MyRvLinkDeviceHost.UpdateMetadata`).
  - Build/refresh `LogicalDeviceId` from product/device/function/capability.
  - Add logical device via `MyRvLinkDeviceManager.AddLogicalDevice(...)`.
  - Apply circuit id from metadata to logical device.

Materialized identity fields include:
- Product ID
- Device Type
- Device Instance
- Function Name
- Function Instance
- Default capability byte
- Product MAC

## 1.5 Caching and integrity strategy

- Device table and metadata table are cached separately.
- Cache lookup keyed by table CRC values.
- If cached counts mismatch current table expectations, cache is rejected.
- Fresh pulls are CRC-validated before use.

Implication: metadata in this path is intentionally **table-versioned** and resilient to stale data.

---

## 2) Metadata Encodings Observed in MyRvLink Payloads

## 2.1 Device protocol discriminator

`MyRvLinkDeviceProtocol` enum values:
- `None`
- `Host`
- `IdsCan`

This discriminator appears in both device and metadata rows.

## 2.2 Host device row encoding variants

`MyRvLinkDeviceHost.Decode(...)` supports:
- Payload size `0` (legacy host row; no IDS-CAN style fields)
- Payload size `10` (host row carrying IDS-CAN-like identity fields)

## 2.3 Host metadata encoding variants

`MyRvLinkDeviceHostMetadata.Decode(...)` supports:
- Payload size `0` (legacy/no extra metadata)
- Payload size `17` (extended IDS-CAN-style metadata)

Extended fields include:
- Function Name
- Function Instance
- Raw capability
- IDS-CAN version
- Circuit ID
- Software Part Number (fixed-length string segment)

## 2.4 IDS-CAN metadata row encoding

`MyRvLinkDeviceIdsCanMetadata` expects payload size `17` and includes:
- Function Name
- Function Instance
- Raw capability
- IDS-CAN version
- Circuit ID
- Software Part Number

This row provides the metadata needed to finalize many logical IDs and capabilities.

---

## 3) BLE Gateway Family / Version Differences (Discovery-Relevant)

The app contains explicit BLE gateway-family and version handling.

## 3.1 Advertisement-level version/family parsing

### Legacy parsing path

`BleGatewayScanResult` derives gateway version from manufacturer bytes by:
- Validating Lippert manufacturer id bytes
- Decoding part number and revision nibble fields
- Mapping `(partNumber, rev)` to gateway version

### IDS manufacturer-specific path

`BleCanGatewayProtocolVersion` (1-byte payload) is parsed from IDS manufacturer-specific data and consumed by:
- `X180TGatewayScanResult`
- `SureShadeGatewayScanResult`

Both scan results expose `AdvertisedGatewayVersion` and alter required-advertisement validation behavior.

## 3.2 Distinct gateway families in scan-result model

Observed specialized gateway scan result classes:
- `BleGatewayScanResult` (generic/legacy)
- `X180TGatewayScanResult`
- `SureShadeGatewayScanResult`

Additional signals:
- `SureShadeGatewayScanResult` also parses connection-count manufacturer data.
- `X180T` and `SureShade` both use key-seed cypher fields for pairing/security logic.

## 3.3 BLE transport frame decoding differences (critical)

In `BleCommunicationsAdapter`:

- **V1 behavior**:
  - Raw incoming payload path
  - Additional synthetic messages inserted (e.g., fake circuit-id message)

- **V2/V2_D behavior**:
  - Decodes wrapped V2 frame types (`Packed`, `ElevenBit`, `TwentyNineBit`)
  - Reconstructs canonical internal CAN message segments
  - Uses `DeviceInstanceManager` on V2 to resolve instance mapping

Implication: identical logical CAN semantics may arrive in structurally different BLE frames depending on gateway generation.

---

## 4) Direct IDS-CAN Path (Non-MyRvLink Table Model)

Key class:
- `DirectConnectionIdsCan`

Discovery characteristics:
- Uses live gateway/device inventory from CAN adapter objects.
- Device identity from `DEVICE_ID + MAC` and live network address.
- Adds/fetches logical devices from physical devices directly (`AddLogicalDeviceForPhysicalDevice`).
- Handles function rename flows when function name/instance changes.

Session behavior differs by BLE gateway version:
- For BLE gateway version `V1`, uses `IdsCanSessionManagerAuto`.
- Otherwise uses standard `IdsCanSessionManager`.

This path does not rely on a separate “device metadata table CRC” model.

---

## 5) Cloud IoT Path (Remote Component Model)

Key class:
- `DirectConnectionRvCloudIoT`

Discovery characteristics:
- Fetches component inventory using cloud API (`GetVehicleViewAsync`).
- Applies component status objects to logical devices.
- Metadata fetch is command-based per component (`RvCloudDeviceComponentCommandMetadataGet`) with async tracker correlation.

Metadata operation example:
- `GetSoftwarePartNumberAsync(...)` sends component metadata-get command.
- Waits via `MetadataTracker` keyed by component id.
- Returns software part number from response payload.

This is a third model: cloud component metadata, separate from both MyRvLink table metadata and direct IDS-CAN frame metadata.

---

## 6) Secondary Cloud Gateway Code Corroboration

In `Cloud Gateway Code`, discovery is primarily IDS-CAN identity driven.

Key observations:
- `Program` wires `GatewayManagedConnection` with a CAN adapter factory.
- `CloudGatewayCanDeviceInfo` defines local host product/type/function identity for the cloud gateway node.
- `GatewayManagedConnection` chooses transport by connection type and BLE gateway version.
- Logical device lifecycle starts/stops with `LogicalDeviceService.StartCan(...)` and `StopCan(...)`.

This supports the same conclusion: non-MyRvLink flows rely on live CAN identity and adapter semantics rather than table + metadata table fetches.

---

## 7) Unified Model of “How metadata is discovered”

You can normalize app behavior into this runtime decision tree:

1. **Connection archetype selected**
   - MyRvLink (BLE/TCP COBS event stream)
   - IDS-CAN direct (BLE/TCP adapter)
   - Cloud IoT component API

2. **For MyRvLink**
   - Consume GatewayInformation
   - Pull device table
   - Pull metadata table
   - Validate counts + CRCs
   - Merge by index
   - Materialize logical devices

3. **For IDS-CAN direct**
   - Decode transport frames based on gateway version
   - Track physical devices from live CAN network data
   - Build logical devices directly from `DEVICE_ID + MAC + capabilities`

4. **For Cloud IoT**
   - Enumerate components from cloud view
   - Apply status and online state
   - Fetch metadata by component command when needed

---

## 8) Practical Implications for `android_ble_plugin_bridge`

If your bridge needs to be robust across all observed hardware/gateway paths, it should:

1. Detect and preserve **connection archetype** (MyRvLink vs IDS-CAN vs Cloud).
2. For MyRvLink, model **dual tables + CRCs** (device and metadata are separate assets).
3. Parse metadata rows with protocol-dependent decoding (`Host` vs `IdsCan`, legacy vs extended payload sizes).
4. Keep transport decoding strategy version-aware (`V1` vs `V2`/`V2_D` wrappers).
5. Avoid assuming software part number exists at initial enumeration time in all paths.
6. Treat metadata as mutable/versioned and support refresh on table CRC changes.

---

## 9) Known Limits / Confidence Notes

- Decompiled output may rename symbols and sometimes flatten source structure.
- Despite that, the control/data flow relationships are consistent across multiple classes and strongly corroborated.
- Confidence is high for:
  - MyRvLink dual-table discovery model
  - Merge-by-index + CRC validation behavior
  - BLE version-specific transport decoding differences
  - Distinct cloud component metadata path

---

## 10) Suggested Next Step (Optional)

Create a bridge-side “metadata normalization spec” with:
- Input source type (`myrvlink_table`, `idscan_live`, `cloud_component`)
- Required/optional fields per source
- Confidence flags and update semantics
- Version/source provenance for each field

This makes downstream Home Assistant entities resilient to partial metadata availability and gateway variation.

---

## Appendix A) Bridge Implementation Checklist

Use this as an execution checklist when implementing metadata discovery in `android_ble_plugin_bridge`.

## A.1 Architecture and model

- [ ] Define a top-level source discriminator for each discovered endpoint:
   - `myrvlink_table`
   - `idscan_live`
   - `cloud_component`
- [ ] Define one normalized metadata DTO used by all pipelines.
- [ ] Include provenance on every field (`source`, `decoder`, `gateway_version`, `timestamp`).
- [ ] Include confidence/quality flags (`exact`, `derived`, `missing`, `stale`).

## A.2 MyRvLink table pipeline

- [ ] Parse/track `GatewayInformation` events.
- [ ] Persist current `device_table_id`, `device_table_crc`, `metadata_table_crc`.
- [ ] Trigger `GetDevices` when table CRC changes or cache miss.
- [ ] Trigger `GetDevicesMetadata` when metadata CRC changes or cache miss.
- [ ] Validate completion conditions:
   - [ ] received count == decoded row count
   - [ ] response CRC == expected CRC
   - [ ] start row host constraints when start id is 0
- [ ] Merge device rows + metadata rows by index only after both pass validation.
- [ ] Reject merge when counts differ.

## A.3 MyRvLink row decoders

- [ ] Implement protocol dispatch for `None`, `Host`, `IdsCan`.
- [ ] Support Host device payload sizes:
   - [ ] `0` (legacy)
   - [ ] `10` (IDS-CAN style identity)
- [ ] Support Host metadata payload sizes:
   - [ ] `0` (legacy)
   - [ ] `17` (extended metadata)
- [ ] Support IDS-CAN metadata payload size:
   - [ ] `17`
- [ ] Decode and map fields:
   - [ ] product id
   - [ ] device type
   - [ ] device instance
   - [ ] function name
   - [ ] function instance
   - [ ] raw capability
   - [ ] IDS-CAN version
   - [ ] circuit id
   - [ ] software part number

## A.4 BLE gateway/version handling

- [ ] Detect gateway family/version from advertisement:
   - [ ] legacy part/rev nibble path
   - [ ] IDS manufacturer-specific protocol version path
- [ ] Distinguish special scan-result families where present:
   - [ ] generic BLE gateway
   - [ ] `X180T`
   - [ ] `SureShade`
- [ ] Preserve gateway version in runtime context and metadata provenance.

## A.5 BLE frame decoding compatibility

- [ ] Implement V1 receive handling (raw + synthetic compatibility message behavior if needed for parity).
- [ ] Implement V2/V2_D wrapper decode path:
   - [ ] Packed
   - [ ] ElevenBit
   - [ ] TwentyNineBit
- [ ] Reconstruct canonical internal message format before higher-level parsing.

## A.6 IDS-CAN direct pipeline

- [ ] Build logical identity from live `DEVICE_ID + MAC` observations.
- [ ] Track online/offline by current CAN address and heartbeat/state.
- [ ] Handle rename/morph behavior when function name/instance changes.
- [ ] Keep this pipeline independent from table-CRC assumptions.

## A.7 Cloud component pipeline

- [ ] Enumerate initial components from cloud vehicle view.
- [ ] Map each component to a logical device key.
- [ ] Support on-demand metadata command for software part number.
- [ ] Correlate async metadata responses by component id tracker key.

## A.8 Caching and invalidation

- [ ] Cache MyRvLink device rows keyed by `device_table_crc`.
- [ ] Cache MyRvLink metadata rows keyed by `metadata_table_crc`.
- [ ] Invalidate on CRC mismatch, count mismatch, or decode failure.
- [ ] Mark normalized metadata stale when live source disconnects.

## A.9 Error handling and observability

- [ ] Log decode failures with source type, gateway version, and raw payload length.
- [ ] Emit metric counters for:
   - [ ] CRC mismatch
   - [ ] count mismatch
   - [ ] unknown protocol/type
   - [ ] partial metadata merges blocked
- [ ] Keep non-fatal fallback behavior (preserve minimal identity even when metadata is partial).

## A.10 Home Assistant-facing contract checks

- [ ] Ensure unique/stable device identifiers do not depend on transient fields.
- [ ] Expose `software_part_number` only when confirmed from source.
- [ ] Expose `circuit_id` only when decoder confidence is exact.
- [ ] Surface source/provenance in diagnostics payload.
- [ ] Include gateway type/version and parse path in diagnostics.

## A.11 Validation test matrix

- [ ] MyRvLink + legacy host metadata (`payload 0`) 
- [ ] MyRvLink + extended host metadata (`payload 17`)
- [ ] MyRvLink + IDS-CAN metadata (`payload 17`)
- [ ] BLE gateway version `V1` frame path
- [ ] BLE gateway version `V2` frame path
- [ ] BLE gateway version `V2_D` frame path
- [ ] `X180T` advertisement parsing path
- [ ] `SureShade` advertisement parsing path
- [ ] IDS-CAN direct device add/rename/offline transitions
- [ ] Cloud metadata command round-trip (`software_part_number`)

## A.12 Definition of done

- [ ] All three pipelines produce the same normalized metadata schema.
- [ ] Provenance and confidence fields are present for every metadata payload.
- [ ] CRC and count guards prevent invalid merges.
- [ ] Gateway-version-specific decode branches are covered by tests.
- [ ] Diagnostics clearly explain where each metadata field came from.
