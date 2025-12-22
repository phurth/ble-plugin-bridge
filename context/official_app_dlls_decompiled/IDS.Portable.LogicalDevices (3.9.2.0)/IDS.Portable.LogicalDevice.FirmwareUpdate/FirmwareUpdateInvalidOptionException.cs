using System;

namespace IDS.Portable.LogicalDevice.FirmwareUpdate
{
	public class FirmwareUpdateInvalidOptionException : FirmwareUpdateException
	{
		public FirmwareUpdateInvalidOptionException(ILogicalDevice logicalDevice, FirmwareUpdateOption optionKey, Exception? innerException = null)
			: base($"Firmware Update options key of {optionKey} is invalid or has invalid value/type for {logicalDevice}", innerException)
		{
		}

		public FirmwareUpdateInvalidOptionException(ILogicalDeviceSource deviceSource, FirmwareUpdateOption optionKey, Exception? innerException = null)
			: base($"Firmware Update options key of {optionKey} is invalid or has invalid value/type for {deviceSource}", innerException)
		{
		}
	}
}
