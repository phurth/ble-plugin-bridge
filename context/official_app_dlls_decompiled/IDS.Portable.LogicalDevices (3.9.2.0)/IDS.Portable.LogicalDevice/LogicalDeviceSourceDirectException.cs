using System;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceSourceDirectException : Exception
	{
		public LogicalDeviceSourceDirectException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}
	}
}
