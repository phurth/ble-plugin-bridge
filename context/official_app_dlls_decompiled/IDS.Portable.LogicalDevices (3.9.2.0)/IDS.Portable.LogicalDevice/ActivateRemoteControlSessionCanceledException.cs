using System;

namespace IDS.Portable.LogicalDevice
{
	public class ActivateRemoteControlSessionCanceledException : OperationCanceledException
	{
		public ActivateRemoteControlSessionCanceledException(string tag, string message)
			: base(tag + " - ActivateRemoteControlSession was canceled for " + message)
		{
		}
	}
}
