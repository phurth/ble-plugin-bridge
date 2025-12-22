using System;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceCapabilityFeatureEnableVerificationFailed : LogicalDeviceException
	{
		public LogicalDeviceCapabilityFeatureEnableVerificationFailed(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}
	}
}
