using System;

namespace IDS.Portable.LogicalDevice.FirmwareUpdate
{
	public class LogicalDeviceFirmwareUpdateManager : ILogicalDeviceFirmwareUpdateManager
	{
		private readonly object _lock = new object();

		private LogicalDeviceFirmwareUpdateSession? _currentUpdateSession;

		public ILogicalDeviceService DeviceService { get; }

		public bool IsFirmwareUpdateSessionStarted => GetStartedSessionFirmwareUpdateDevice() != null;

		public LogicalDeviceFirmwareUpdateManager(ILogicalDeviceService deviceService)
		{
			DeviceService = deviceService;
		}

		public ILogicalDeviceFirmwareUpdateDevice? GetStartedSessionFirmwareUpdateDevice()
		{
			lock (_lock)
			{
				if (_currentUpdateSession == null || _currentUpdateSession!.IsDisposed)
				{
					return null;
				}
				if (_currentUpdateSession!.LogicalDevice.IsDisposed)
				{
					return null;
				}
				return _currentUpdateSession!.LogicalDevice;
			}
		}

		public ILogicalDeviceFirmwareUpdateSession StartFirmwareUpdateSession(ILogicalDeviceFirmwareUpdateDevice logicalDevice)
		{
			if (logicalDevice == null)
			{
				throw new ArgumentNullException("logicalDevice");
			}
			lock (_lock)
			{
				if (IsFirmwareUpdateSessionStarted)
				{
					throw new FirmwareUpdateSessionNotAvailableException($"Unable to get update session for {logicalDevice} as currently {_currentUpdateSession?.LogicalDevice} has the session");
				}
				return _currentUpdateSession = new LogicalDeviceFirmwareUpdateSession(logicalDevice);
			}
		}
	}
}
