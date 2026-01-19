"""Sensor platform for ADB Bridge."""
from __future__ import annotations

from homeassistant.components.sensor import SensorEntity
from homeassistant.config_entries import ConfigEntry
from homeassistant.core import HomeAssistant
from homeassistant.helpers.entity_platform import AddEntitiesCallback
from homeassistant.helpers.update_coordinator import CoordinatorEntity

from .const import DOMAIN
from .coordinator import AdbBridgeCoordinator


async def async_setup_entry(
    hass: HomeAssistant,
    entry: ConfigEntry,
    async_add_entities: AddEntitiesCallback,
) -> None:
    """Set up sensors."""
    coordinator: AdbBridgeCoordinator = hass.data[DOMAIN][entry.entry_id]
    
    async_add_entities([
        AdbConnectionSensor(coordinator, entry),
        AdbWifiIpSensor(coordinator, entry),
    ])


class AdbConnectionSensor(CoordinatorEntity[AdbBridgeCoordinator], SensorEntity):
    """Sensor showing connection status."""

    _attr_has_entity_name = True
    _attr_name = "Connection Status"
    _attr_icon = "mdi:usb"

    def __init__(
        self,
        coordinator: AdbBridgeCoordinator,
        entry: ConfigEntry,
    ) -> None:
        """Initialize."""
        super().__init__(coordinator)
        self._attr_unique_id = f"{entry.entry_id}_connection"
        self._attr_device_info = {
            "identifiers": {(DOMAIN, entry.entry_id)},
            "name": f"ADB Bridge ({coordinator.device_serial or coordinator.device_ip or 'Device'})",
            "manufacturer": "Android",
            "model": "ADB Device",
        }

    @property
    def native_value(self) -> str:
        """Return connection status."""
        if self.coordinator.data and self.coordinator.data.get("connected"):
            return "Connected"
        return "Disconnected"

    @property
    def extra_state_attributes(self) -> dict:
        """Return extra attributes."""
        if not self.coordinator.data:
            return {}
        return {
            "serial": self.coordinator.data.get("serial"),
            "wifi_adb_enabled": self.coordinator.data.get("wifi_adb_enabled"),
        }


class AdbWifiIpSensor(CoordinatorEntity[AdbBridgeCoordinator], SensorEntity):
    """Sensor showing device WiFi IP."""

    _attr_has_entity_name = True
    _attr_name = "WiFi IP Address"
    _attr_icon = "mdi:wifi"

    def __init__(
        self,
        coordinator: AdbBridgeCoordinator,
        entry: ConfigEntry,
    ) -> None:
        """Initialize."""
        super().__init__(coordinator)
        self._attr_unique_id = f"{entry.entry_id}_wifi_ip"
        self._attr_device_info = {
            "identifiers": {(DOMAIN, entry.entry_id)},
            "name": f"ADB Bridge ({coordinator.device_serial or coordinator.device_ip or 'Device'})",
            "manufacturer": "Android",
            "model": "ADB Device",
        }

    @property
    def native_value(self) -> str | None:
        """Return WiFi IP."""
        if self.coordinator.data:
            return self.coordinator.data.get("wifi_ip")
        return None
