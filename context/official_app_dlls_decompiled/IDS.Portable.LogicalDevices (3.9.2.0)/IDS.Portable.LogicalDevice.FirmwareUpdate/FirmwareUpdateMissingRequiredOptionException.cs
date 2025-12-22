using System;

namespace IDS.Portable.LogicalDevice.FirmwareUpdate
{
	public class FirmwareUpdateMissingRequiredOptionException : FirmwareUpdateException
	{
		public FirmwareUpdateMissingRequiredOptionException(ILogicalDevice logicalDevice, FirmwareUpdateOption optionKey, Exception? innerException = null)
			: base($"Firmware Update options missing required option key of {optionKey} for {logicalDevice}", innerException)
		{
		}

		public FirmwareUpdateMissingRequiredOptionException(ILogicalDeviceSource deviceSource, FirmwareUpdateOption optionKey, Exception? innerException = null)
			: base($"Firmware Update options missing required option key of {optionKey} for {deviceSource}", innerException)
		{
		}
	}
}
