namespace IDS.Portable.LogicalDevice.FirmwareUpdate
{
	public interface ILogicalDeviceFirmwareUpdateManager
	{
		ILogicalDeviceService DeviceService { get; }

		bool IsFirmwareUpdateSessionStarted { get; }

		ILogicalDeviceFirmwareUpdateDevice? GetStartedSessionFirmwareUpdateDevice();

		ILogicalDeviceFirmwareUpdateSession StartFirmwareUpdateSession(ILogicalDeviceFirmwareUpdateDevice logicalDevice);
	}
}
