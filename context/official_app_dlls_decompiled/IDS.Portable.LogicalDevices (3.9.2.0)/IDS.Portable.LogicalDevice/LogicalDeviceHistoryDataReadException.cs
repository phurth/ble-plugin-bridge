using System;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceHistoryDataReadException : LogicalDeviceHistoryDataException
	{
		public LogicalDeviceHistoryDataReadException(ILogicalDevice? device, string message, Exception? innerException = null)
			: base(device, "Unable to READ HistorData", innerException)
		{
		}
	}
}
