using System;
using IDS.Core.IDS_CAN;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidMappingNotSupportedException : Exception
	{
		public LogicalDevicePidMappingNotSupportedException(PID pid, string message)
			: base($"{pid} - {message}")
		{
		}
	}
}
