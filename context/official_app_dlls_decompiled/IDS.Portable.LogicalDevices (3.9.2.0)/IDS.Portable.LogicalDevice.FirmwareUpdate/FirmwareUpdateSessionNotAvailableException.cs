using System;

namespace IDS.Portable.LogicalDevice.FirmwareUpdate
{
	public class FirmwareUpdateSessionNotAvailableException : FirmwareUpdateException
	{
		public FirmwareUpdateSessionNotAvailableException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}
	}
}
