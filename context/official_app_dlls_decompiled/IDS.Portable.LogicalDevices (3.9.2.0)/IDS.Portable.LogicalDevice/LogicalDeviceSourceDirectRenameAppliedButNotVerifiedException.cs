using System;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceSourceDirectRenameAppliedButNotVerifiedException : LogicalDeviceSourceDirectException
	{
		public LogicalDeviceSourceDirectRenameAppliedButNotVerifiedException(Exception? innerException = null)
			: base("Rename was successful requested, but was unable to be verified", innerException)
		{
		}
	}
}
