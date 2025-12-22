using System;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceHistoryDataException : LogicalDeviceException
	{
		public LogicalDeviceHistoryDataException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}

		public LogicalDeviceHistoryDataException(ILogicalDevice? device, string message, Exception? innerException = null)
			: base(message + " for " + (device?.ToString() ?? "unknown"), innerException)
		{
		}
	}
}
