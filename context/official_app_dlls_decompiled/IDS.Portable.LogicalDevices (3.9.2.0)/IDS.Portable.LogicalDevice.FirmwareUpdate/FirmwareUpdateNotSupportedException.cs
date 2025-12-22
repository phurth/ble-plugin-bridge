using System;

namespace IDS.Portable.LogicalDevice.FirmwareUpdate
{
	public class FirmwareUpdateNotSupportedException : FirmwareUpdateException
	{
		public FirmwareUpdateNotSupportedException(ILogicalDevice logicalDevice, FirmwareUpdateSupport firmwareUpdateSupport, Exception? innerException = null)
			: base($"Firmware Update Not Supported ({firmwareUpdateSupport}) by Device {logicalDevice}", innerException)
		{
		}

		public FirmwareUpdateNotSupportedException(ILogicalDeviceSource deviceSource, FirmwareUpdateSupport firmwareUpdateSupport, Exception? innerException = null)
			: base($"Firmware Update Not Supported ({firmwareUpdateSupport}) by Host {deviceSource}", innerException)
		{
		}
	}
}
