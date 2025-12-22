using System;

namespace IDS.Portable.LogicalDevice.FirmwareUpdate
{
	public class FirmwareUpdateTooBigException : FirmwareUpdateException
	{
		public FirmwareUpdateTooBigException(ILogicalDevice logicalDevice, int size, Exception? innerException = null)
			: base($"Firmware Update too big {size} for {logicalDevice}", innerException)
		{
		}

		public FirmwareUpdateTooBigException(ILogicalDeviceSource deviceSource, int size, Exception? innerException = null)
			: base($"Firmware Update too big {size} for {deviceSource}", innerException)
		{
		}
	}
}
