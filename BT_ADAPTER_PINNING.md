# BT Adapter Pinning for HACS Integrations

## Problem

HA's Bluetooth router scores available connection paths (local HCI adapters + ESPHome proxies) and
automatically selects the "best" one based on RSSI, failure history, and slot availability. For most
devices this is desirable, but some devices are incompatible with certain paths. For example:

- A device that requires GATT operations (CCCD writes, specific characteristic access patterns) that
  an ESPHome proxy doesn't forward correctly
- A device that needs a direct HCI connection for reliable bonding/pairing

There is currently no way in HA's UI to force a specific integration to use a specific BT path.

## Solution

Add an **options flow** to the integration (post-setup, via the ⚙ gear on the integration card)
that lets the user pin a device to a specific BT source.

### API

`bluetooth.async_scanner_devices_by_address(hass, address, connectable=True)` returns a list of
`BluetoothServiceInfoBleak` objects — one per scanner that can currently see the device. Each has:

- `.source` — scanner identifier: `"hci0"`, `"hci1"`, or a proxy Bluetooth MAC (e.g. `"AA:BB:CC:DD:EE:FF"`)
- `.device` — the `BLEDevice` object to pass to `establish_connection()`

`bluetooth.async_get_adapters(hass)` returns info on local HCI adapters only (not proxies).

### Coordinator change

```python
# In _do_connect(), replace:
device = bluetooth.async_ble_device_from_address(hass, address, connectable=True)

# With:
preferred_source = self._entry.options.get(CONF_BT_SOURCE)  # None = "auto"
if preferred_source and preferred_source != BT_SOURCE_AUTO:
    all_infos = bluetooth.async_scanner_devices_by_address(
        self.hass, self._address, connectable=True
    )
    match = next((i for i in all_infos if i.source == preferred_source), None)
    device = match.device if match else bluetooth.async_ble_device_from_address(
        self.hass, self._address, connectable=True
    )
else:
    device = bluetooth.async_ble_device_from_address(
        self.hass, self._address, connectable=True
    )
```

### Options flow

```python
class OptionsFlowHandler(config_entries.OptionsFlow):
    async def async_step_init(self, user_input=None):
        if user_input is not None:
            return self.async_create_entry(title="", data=user_input)

        address = self.config_entry.data[CONF_ADDRESS]

        # Build list of currently-visible sources for this device
        all_infos = bluetooth.async_scanner_devices_by_address(
            self.hass, address, connectable=True
        )
        sources = {BT_SOURCE_AUTO: "Auto (let HA decide)"}
        for info in all_infos:
            label = info.source  # could be enriched with adapter name if desired
            sources[info.source] = label

        current = self.config_entry.options.get(CONF_BT_SOURCE, BT_SOURCE_AUTO)

        return self.async_show_form(
            step_id="init",
            data_schema=vol.Schema({
                vol.Optional(CONF_BT_SOURCE, default=current): vol.In(sources)
            }),
        )
```

### Constants to add

```python
CONF_BT_SOURCE = "bt_source"
BT_SOURCE_AUTO = "auto"
```

### Strings to add (`strings.json` / `translations/en.json`)

```json
"options": {
  "step": {
    "init": {
      "title": "Bluetooth Adapter",
      "description": "Select which Bluetooth adapter or proxy to use for this device. Use Auto unless you have a specific compatibility reason to pin it.",
      "data": {
        "bt_source": "Bluetooth adapter / proxy"
      }
    }
  }
}
```

## Notes

- The source list is built dynamically at options-open time — only sources that can currently see
  the device are shown. If the device is out of range of a proxy, that proxy won't appear.
- Fall back to auto if the pinned source disappears (proxy offline, adapter removed).
- This pattern applies equally to ha-hughes, ha-onecontrol, ha-gopower, ha-mopeka, or any future
  integration in this workspace that uses `establish_connection()`.
- The options flow requires registering `async_get_options_flow` on the config flow handler class.
