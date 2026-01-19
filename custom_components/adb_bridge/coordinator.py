"""DataUpdateCoordinator for ADB Bridge."""
from __future__ import annotations

import asyncio
import logging
import os
from datetime import timedelta
from typing import Any

from homeassistant.config_entries import ConfigEntry
from homeassistant.core import HomeAssistant
from homeassistant.helpers.update_coordinator import DataUpdateCoordinator, UpdateFailed

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

UPDATE_INTERVAL = timedelta(seconds=30)


class AdbBridgeCoordinator(DataUpdateCoordinator[dict[str, Any]]):
    """Coordinator to manage ADB device connection."""

    def __init__(self, hass: HomeAssistant, entry: ConfigEntry) -> None:
        """Initialize the coordinator."""
        super().__init__(
            hass,
            _LOGGER,
            name=DOMAIN,
            update_interval=UPDATE_INTERVAL,
        )
        self.entry = entry
        self._device = None
        self._signer = None
        self._lock = asyncio.Lock()
        
        self.connection_type = entry.data.get(CONF_CONNECTION_TYPE, CONNECTION_USB)
        self.device_serial = entry.data.get(CONF_DEVICE_SERIAL)
        self.device_ip = entry.data.get(CONF_DEVICE_IP)
        self.adb_port = entry.data.get(CONF_ADB_PORT, DEFAULT_ADB_PORT)

    async def _async_setup(self) -> None:
        """Set up the ADB connection."""
        def _setup():
            from adb_shell.auth.keygen import keygen
            from adb_shell.auth.sign_pythonrsa import PythonRSASigner
            
            key_path = "/config/.android/adbkey"
            os.makedirs(os.path.dirname(key_path), exist_ok=True)
            
            if not os.path.exists(key_path):
                keygen(key_path)
            
            with open(key_path) as f:
                priv = f.read()
            with open(key_path + ".pub") as f:
                pub = f.read()
            
            self._signer = PythonRSASigner(pub, priv)
        
        await self.hass.async_add_executor_job(_setup)

    async def _async_connect(self) -> bool:
        """Connect to the device."""
        if self._signer is None:
            await self._async_setup()
        
        def _connect():
            try:
                if self.connection_type == CONNECTION_USB:
                    from adb_shell.adb_device import AdbDeviceUsb
                    if self.device_serial:
                        self._device = AdbDeviceUsb(serial=self.device_serial)
                    else:
                        self._device = AdbDeviceUsb()
                else:
                    from adb_shell.adb_device import AdbDeviceTcp
                    self._device = AdbDeviceTcp(self.device_ip, self.adb_port)
                
                self._device.connect(rsa_keys=[self._signer], auth_timeout_s=30)
                return self._device.available
            except Exception as e:
                _LOGGER.error("Connection failed: %s", e)
                self._device = None
                return False
        
        return await self.hass.async_add_executor_job(_connect)

    async def async_disconnect(self) -> None:
        """Disconnect from device."""
        def _disconnect():
            if self._device:
                try:
                    self._device.close()
                except Exception:
                    pass
                self._device = None
        
        await self.hass.async_add_executor_job(_disconnect)

    async def _async_update_data(self) -> dict[str, Any]:
        """Fetch data from device."""
        async with self._lock:
            if self._device is None or not self._device.available:
                if not await self._async_connect():
                    raise UpdateFailed("Could not connect to device")
            
            def _get_data():
                data = {
                    "connected": False,
                    "serial": None,
                    "wifi_ip": None,
                    "wifi_adb_enabled": False,
                }
                
                try:
                    if not self._device or not self._device.available:
                        return data
                    
                    data["connected"] = True
                    
                    # Get serial
                    try:
                        result = self._device.shell("getprop ro.serialno")
                        data["serial"] = result.strip() if result else self.device_serial
                    except Exception:
                        data["serial"] = self.device_serial
                    
                    # Get WiFi IP
                    try:
                        result = self._device.shell("ip addr show wlan0 2>/dev/null | grep 'inet ' | awk '{print $2}' | cut -d/ -f1")
                        if result and result.strip():
                            data["wifi_ip"] = result.strip()
                    except Exception:
                        pass
                    
                    # Check if WiFi ADB is enabled (port 5555 listening)
                    try:
                        result = self._device.shell("getprop service.adb.tcp.port")
                        if result and result.strip() == "5555":
                            data["wifi_adb_enabled"] = True
                    except Exception:
                        pass
                    
                    return data
                except Exception as e:
                    _LOGGER.error("Error getting device data: %s", e)
                    self._device = None
                    raise
            
            try:
                return await self.hass.async_add_executor_job(_get_data)
            except Exception as e:
                raise UpdateFailed(f"Error communicating with device: {e}")

    async def async_enable_wifi_adb(self, port: int = 5555) -> str | None:
        """Enable WiFi ADB on the device. Returns device IP if successful."""
        async with self._lock:
            if self._device is None or not self._device.available:
                if not await self._async_connect():
                    return None
            
            def _enable():
                try:
                    # Enable tcpip mode
                    self._device.shell(f"setprop service.adb.tcp.port {port}")
                    self._device.shell("stop adbd")
                    self._device.shell("start adbd")
                    
                    # Get IP
                    result = self._device.shell("ip addr show wlan0 2>/dev/null | grep 'inet ' | awk '{print $2}' | cut -d/ -f1")
                    if result and result.strip():
                        return result.strip()
                    
                    # Try eth0
                    result = self._device.shell("ip addr show eth0 2>/dev/null | grep 'inet ' | awk '{print $2}' | cut -d/ -f1")
                    if result and result.strip():
                        return result.strip()
                    
                    return None
                except Exception as e:
                    _LOGGER.error("Error enabling WiFi ADB: %s", e)
                    return None
            
            return await self.hass.async_add_executor_job(_enable)

    async def async_run_command(self, command: str) -> str | None:
        """Run a shell command on the device."""
        async with self._lock:
            if self._device is None or not self._device.available:
                if not await self._async_connect():
                    return None
            
            def _run():
                try:
                    return self._device.shell(command)
                except Exception as e:
                    _LOGGER.error("Error running command: %s", e)
                    return None
            
            return await self.hass.async_add_executor_job(_run)

    async def async_install_apk(self, apk_path: str) -> bool:
        """Install an APK on the device."""
        async with self._lock:
            if self._device is None or not self._device.available:
                if not await self._async_connect():
                    return False
            
            def _install():
                try:
                    from adb_shell.adb_device import AdbDevice
                    # Push APK to device
                    remote_path = "/data/local/tmp/install.apk"
                    self._device.push(apk_path, remote_path)
                    
                    # Install
                    result = self._device.shell(f"pm install -r {remote_path}")
                    
                    # Clean up
                    self._device.shell(f"rm {remote_path}")
                    
                    return "Success" in result if result else False
                except Exception as e:
                    _LOGGER.error("Error installing APK: %s", e)
                    return False
            
            return await self.hass.async_add_executor_job(_install)
