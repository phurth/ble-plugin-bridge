using System;

namespace IDS.Portable.LogicalDevice
{
	public class ActivateSessionTimeoutException : TimeoutException
	{
		public ActivateSessionTimeoutException(string tag, string message)
			: base(tag + " - ActivateRemoteControlSession timed out for " + message)
		{
		}
	}
}
