using System;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceSourceDirectRenameNotSupportedException : LogicalDeviceSourceDirectException
	{
		public LogicalDeviceSourceDirectRenameNotSupportedException(Exception? innerException = null)
			: base("Rename Not Supported", innerException)
		{
		}
	}
}
