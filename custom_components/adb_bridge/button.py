"""Button platform for ADB Bridge."""
from __future__ import annotations

from homeassistant.components.button import ButtonEntity
from homeassistant.config_entries import ConfigEntry
from homeassistant.core import HomeAssistant
from homeassistant.helpers.entity_platform import AddEntitiesCallback
from homeassistant.helpers.update_coordinator import CoordinatorEntity

from .const import DOMAIN, CONNECTION_USB
from .coordinator import AdbBridgeCoordinator


async def async_setup_entry(
    hass: HomeAssistant,
    entry: ConfigEntry,
    async_add_entities: AddEntitiesCallback,
) -> None:
    """Set up buttons."""
    coordinator: AdbBridgeCoordinator = hass.data[DOMAIN][entry.entry_id]
    
    entities = []
    
    # Only show Enable WiFi ADB button for USB connections
    if coordinator.connection_type == CONNECTION_USB:
        entities.append(EnableWifiAdbButton(coordinator, entry))
    
    entities.append(ReconnectButton(coordinator, entry))
    
    async_add_entities(entities)


class EnableWifiAdbButton(CoordinatorEntity[AdbBridgeCoordinator], ButtonEntity):
    """Button to enable WiFi ADB."""

    _attr_has_entity_name = True
    _attr_name = "Enable WiFi ADB"
    _attr_icon = "mdi:wifi-plus"

    def __init__(
        self,
        coordinator: AdbBridgeCoordinator,
        entry: ConfigEntry,
    ) -> None:
        """Initialize."""
        super().__init__(coordinator)
        self._attr_unique_id = f"{entry.entry_id}_enable_wifi_adb"
        self._attr_device_info = {
            "identifiers": {(DOMAIN, entry.entry_id)},
            "name": f"ADB Bridge ({coordinator.device_serial or coordinator.device_ip or 'Device'})",
            "manufacturer": "Android",
            "model": "ADB Device",
        }

    async def async_press(self) -> None:
        """Handle button press."""
        ip = await self.coordinator.async_enable_wifi_adb()
        if ip:
            # Trigger update to refresh state
            await self.coordinator.async_request_refresh()


class ReconnectButton(CoordinatorEntity[AdbBridgeCoordinator], ButtonEntity):
    """Button to reconnect to device."""

    _attr_has_entity_name = True
    _attr_name = "Reconnect"
    _attr_icon = "mdi:refresh"

    def __init__(
        self,
        coordinator: AdbBridgeCoordinator,
        entry: ConfigEntry,
    ) -> None:
        """Initialize."""
        super().__init__(coordinator)
        self._attr_unique_id = f"{entry.entry_id}_reconnect"
        self._attr_device_info = {
            "identifiers": {(DOMAIN, entry.entry_id)},
            "name": f"ADB Bridge ({coordinator.device_serial or coordinator.device_ip or 'Device'})",
            "manufacturer": "Android",
            "model": "ADB Device",
        }

    async def async_press(self) -> None:
        """Handle button press."""
        await self.coordinator.async_disconnect()
        await self.coordinator.async_request_refresh()
