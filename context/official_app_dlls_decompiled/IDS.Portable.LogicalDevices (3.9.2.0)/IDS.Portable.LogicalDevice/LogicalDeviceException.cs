using System;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceException : Exception
	{
		public LogicalDeviceException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}
	}
}
