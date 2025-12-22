using System;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceCapabilityFeatureNotSupported : LogicalDeviceException
	{
		public LogicalDeviceCapabilityFeatureNotSupported(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}
	}
}
