using System;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceSessionException : Exception
	{
		public LogicalDeviceSessionException(string tag, string message, bool verbose = true)
			: base(message)
		{
			if (verbose)
			{
				TaggedLog.Warning(tag ?? "unknown", "LogicalDeviceSessionException " + message);
			}
		}
	}
}
