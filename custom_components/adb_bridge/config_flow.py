"""Config flow for ADB Bridge integration."""
from __future__ import annotations

import logging
from typing import Any

import voluptuous as vol

from homeassistant import config_entries
from homeassistant.core import HomeAssistant
from homeassistant.data_entry_flow import FlowResult
from homeassistant.exceptions import HomeAssistantError

from .const import (
    DOMAIN,
    CONF_CONNECTION_TYPE,
    CONF_DEVICE_SERIAL,
    CONF_DEVICE_IP,
    CONF_ADB_PORT,
    CONNECTION_USB,
    CONNECTION_WIFI,
    DEFAULT_ADB_PORT,
)

_LOGGER = logging.getLogger(__name__)


async def _discover_usb_devices(hass: HomeAssistant) -> list[dict[str, str]]:
    """Discover USB-connected Android devices."""
    devices = []
    
    def _scan():
        try:
            from adb_shell.adb_device import AdbDeviceUsb
            import usb.core
            
            # Find all Android devices (common vendor IDs)
            android_vendors = [
                0x18d1,  # Google
                0x04e8,  # Samsung
                0x0fce,  # Sony
                0x0bb4,  # HTC
                0x22b8,  # Motorola
                0x1004,  # LG
                0x12d1,  # Huawei
                0x2717,  # Xiaomi
                0x1949,  # Amazon
            ]
            
            for vendor_id in android_vendors:
                usb_devices = usb.core.find(find_all=True, idVendor=vendor_id)
                for dev in usb_devices or []:
                    serial = dev.serial_number if dev.serial_number else f"{vendor_id:04x}:{dev.idProduct:04x}"
                    devices.append({
                        "serial": serial,
                        "vendor": f"{vendor_id:04x}",
                        "product": f"{dev.idProduct:04x}",
                    })
        except Exception as e:
            _LOGGER.error("Error scanning USB devices: %s", e)
        
        return devices
    
    return await hass.async_add_executor_job(_scan)


async def _test_usb_connection(hass: HomeAssistant, serial: str | None) -> bool:
    """Test USB connection to device."""
    def _test():
        try:
            from adb_shell.adb_device import AdbDeviceUsb
            from adb_shell.auth.keygen import keygen
            from adb_shell.auth.sign_pythonrsa import PythonRSASigner
            import os
            
            # Key path
            key_path = "/config/.android/adbkey"
            os.makedirs(os.path.dirname(key_path), exist_ok=True)
            
            # Generate key if needed
            if not os.path.exists(key_path):
                keygen(key_path)
            
            # Load key
            with open(key_path) as f:
                priv = f.read()
            with open(key_path + ".pub") as f:
                pub = f.read()
            signer = PythonRSASigner(pub, priv)
            
            # Try connection
            if serial:
                device = AdbDeviceUsb(serial=serial)
            else:
                device = AdbDeviceUsb()
            
            device.connect(rsa_keys=[signer], auth_timeout_s=30)
            result = device.available
            device.close()
            return result
        except Exception as e:
            _LOGGER.error("USB connection test failed: %s", e)
            return False
    
    return await hass.async_add_executor_job(_test)


async def _test_wifi_connection(hass: HomeAssistant, ip: str, port: int) -> bool:
    """Test WiFi ADB connection."""
    def _test():
        try:
            from adb_shell.adb_device import AdbDeviceTcp
            from adb_shell.auth.keygen import keygen
            from adb_shell.auth.sign_pythonrsa import PythonRSASigner
            import os
            
            key_path = "/config/.android/adbkey"
            os.makedirs(os.path.dirname(key_path), exist_ok=True)
            
            if not os.path.exists(key_path):
                keygen(key_path)
            
            with open(key_path) as f:
                priv = f.read()
            with open(key_path + ".pub") as f:
                pub = f.read()
            signer = PythonRSASigner(pub, priv)
            
            device = AdbDeviceTcp(ip, port)
            device.connect(rsa_keys=[signer], auth_timeout_s=10)
            result = device.available
            device.close()
            return result
        except Exception as e:
            _LOGGER.error("WiFi connection test failed: %s", e)
            return False
    
    return await hass.async_add_executor_job(_test)


class ConfigFlow(config_entries.ConfigFlow, domain=DOMAIN):
    """Handle a config flow for ADB Bridge."""

    VERSION = 1

    def __init__(self) -> None:
        """Initialize."""
        self._discovered_devices: list[dict[str, str]] = []
        self._connection_type: str = CONNECTION_USB

    async def async_step_user(
        self, user_input: dict[str, Any] | None = None
    ) -> FlowResult:
        """Handle connection type selection."""
        if user_input is not None:
            self._connection_type = user_input[CONF_CONNECTION_TYPE]
            
            if self._connection_type == CONNECTION_USB:
                return await self.async_step_usb()
            else:
                return await self.async_step_wifi()

        return self.async_show_form(
            step_id="user",
            data_schema=vol.Schema({
                vol.Required(CONF_CONNECTION_TYPE, default=CONNECTION_USB): vol.In({
                    CONNECTION_USB: "USB (device plugged in)",
                    CONNECTION_WIFI: "WiFi (device already in wireless ADB mode)",
                }),
            }),
        )

    async def async_step_usb(
        self, user_input: dict[str, Any] | None = None
    ) -> FlowResult:
        """Handle USB device selection."""
        errors = {}
        
        if user_input is not None:
            serial = user_input.get(CONF_DEVICE_SERIAL)
            
            # Test connection
            if await _test_usb_connection(self.hass, serial):
                return self.async_create_entry(
                    title=f"ADB Bridge ({serial or 'USB'})",
                    data={
                        CONF_CONNECTION_TYPE: CONNECTION_USB,
                        CONF_DEVICE_SERIAL: serial,
                    },
                )
            else:
                errors["base"] = "cannot_connect"

        # Discover devices
        self._discovered_devices = await _discover_usb_devices(self.hass)
        
        if self._discovered_devices:
            device_options = {d["serial"]: f"{d['serial']} ({d['vendor']}:{d['product']})" 
                           for d in self._discovered_devices}
            schema = vol.Schema({
                vol.Required(CONF_DEVICE_SERIAL): vol.In(device_options),
            })
        else:
            schema = vol.Schema({
                vol.Optional(CONF_DEVICE_SERIAL): str,
            })

        return self.async_show_form(
            step_id="usb",
            data_schema=schema,
            errors=errors,
            description_placeholders={
                "device_count": str(len(self._discovered_devices)),
            },
        )

    async def async_step_wifi(
        self, user_input: dict[str, Any] | None = None
    ) -> FlowResult:
        """Handle WiFi connection setup."""
        errors = {}
        
        if user_input is not None:
            ip = user_input[CONF_DEVICE_IP]
            port = user_input.get(CONF_ADB_PORT, DEFAULT_ADB_PORT)
            
            if await _test_wifi_connection(self.hass, ip, port):
                return self.async_create_entry(
                    title=f"ADB Bridge ({ip})",
                    data={
                        CONF_CONNECTION_TYPE: CONNECTION_WIFI,
                        CONF_DEVICE_IP: ip,
                        CONF_ADB_PORT: port,
                    },
                )
            else:
                errors["base"] = "cannot_connect"

        return self.async_show_form(
            step_id="wifi",
            data_schema=vol.Schema({
                vol.Required(CONF_DEVICE_IP): str,
                vol.Optional(CONF_ADB_PORT, default=DEFAULT_ADB_PORT): int,
            }),
            errors=errors,
        )


class CannotConnect(HomeAssistantError):
    """Error to indicate we cannot connect."""
