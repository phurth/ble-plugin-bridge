using System;

namespace IDS.Portable.LogicalDevice.FirmwareUpdate
{
	public class FirmwareUpdateNotAuthorizedException : FirmwareUpdateException
	{
		public FirmwareUpdateNotAuthorizedException(ILogicalDevice logicalDevice, Exception? innerException = null)
			: base($"Firmware Update Not Authorized by Device {logicalDevice}", innerException)
		{
		}

		public FirmwareUpdateNotAuthorizedException(ILogicalDeviceSource deviceSource, Exception? innerException = null)
			: base($"Firmware Update Not Authorized by Host {deviceSource}", innerException)
		{
		}
	}
}
