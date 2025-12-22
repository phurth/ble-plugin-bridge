using System;

namespace IDS.Portable.LogicalDevice.FirmwareUpdate
{
	public class FirmwareUpdateTooSmallException : FirmwareUpdateException
	{
		public FirmwareUpdateTooSmallException(ILogicalDevice logicalDevice, int size, Exception? innerException = null)
			: base($"Firmware Update too small {size} for {logicalDevice}", innerException)
		{
		}

		public FirmwareUpdateTooSmallException(ILogicalDeviceSource deviceSource, int size, Exception? innerException = null)
			: base($"Firmware Update too small {size} for {deviceSource}", innerException)
		{
		}
	}
}
