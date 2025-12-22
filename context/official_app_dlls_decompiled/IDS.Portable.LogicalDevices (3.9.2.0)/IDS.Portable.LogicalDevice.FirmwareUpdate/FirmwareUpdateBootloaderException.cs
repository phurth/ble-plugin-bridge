using System;

namespace IDS.Portable.LogicalDevice.FirmwareUpdate
{
	public class FirmwareUpdateBootloaderException : FirmwareUpdateException
	{
		public FirmwareUpdateBootloaderException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}
	}
}
