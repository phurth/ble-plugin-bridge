# Light Control Fixes — Technical Notes

## Event Processing Order
- **Change**: Process MyRvLink frames as events first, and only if event decode fails, treat them as command responses.
- **Why**: Some status/event frames start with bytes that look like valid command IDs; handling responses first caused status events (including lights) to be dropped.
- **Implementation**: In `OneControlBleService.processMyRvLinkEvent`, moved `tryDecodeEvent()` before `isCommandResponse()`. If an event is decoded, it is handled immediately; otherwise the frame is evaluated as a command response.
- **Effect**: Relay/dimmable/RGB status events now reach their handlers, enabling state updates and MQTT/HA publishing.

## ActionDimmable Command Alignment
- **Command type**: `0x43` (ActionDimmable), per HCI capture and decompiled C# (`MyRvLinkCommandActionDimmable`).
- **Payload structure**: `[ClientCommandId (LE 2 bytes)] [0x43] [DeviceTableId] [DeviceId] [Value...]`
  - For simple on/off/brightness: one byte `Value` (0x00–0x64). `0x01` used in capture for “on”.
- **Framing**: COBS with CRC8, `prependStartFrame = true`, matching HCI capture framing.
- **DeviceTableId**: Uses the runtime `GatewayInformation` value (`0x08` in capture); falls back to `DEFAULT_DEVICE_TABLE_ID` only if not yet known.
- **Command IDs**: Monotonic `nextCommandId` per session to mirror official app behavior; no reset between writes.
- **Write characteristic**: Uses data write characteristic (`dataWriteChar`), `WRITE_TYPE_DEFAULT` when issuing ActionDimmable in the debug flow and `WRITE_TYPE_NO_RESPONSE` in control methods.

## Related Files/Touched Areas
- `app/src/main/java/com/onecontrol/blebridge/OneControlBleService.kt`
  - Event-first processing in `processMyRvLinkEvent`.
  - Debug auto-send removed; manual control relies on corrected ActionDimmable encoding.
- `app/src/main/java/com/onecontrol/blebridge/MyRvLinkCommandEncoder.kt`
  - ActionDimmable encoding aligned to HCI capture: `0x43` command type, correct payload layout, CRC/COBS framing.

