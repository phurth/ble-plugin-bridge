using System;

namespace IDS.Portable.LogicalDevice.FirmwareUpdate
{
	public class FirmwareUpdateException : Exception
	{
		public FirmwareUpdateException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}
	}
}
