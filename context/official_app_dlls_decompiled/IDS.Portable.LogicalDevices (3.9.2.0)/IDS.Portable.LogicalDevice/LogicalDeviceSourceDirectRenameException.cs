using System;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceSourceDirectRenameException : LogicalDeviceSourceDirectException
	{
		public LogicalDeviceSourceDirectRenameException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}
	}
}
