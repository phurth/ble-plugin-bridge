using System;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceHistoryDataWriteException : LogicalDeviceHistoryDataException
	{
		public LogicalDeviceHistoryDataWriteException(ILogicalDevice? device, string message, Exception? innerException = null)
			: base(device, "Unable to WRITE HistorData", innerException)
		{
		}
	}
}
