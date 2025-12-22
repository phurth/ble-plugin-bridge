using System;

namespace IDS.Portable.LogicalDevice
{
	public class ManifestBuilderLogicalDeviceNotSupportedException : Exception
	{
		public ManifestBuilderLogicalDeviceNotSupportedException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}
	}
}
