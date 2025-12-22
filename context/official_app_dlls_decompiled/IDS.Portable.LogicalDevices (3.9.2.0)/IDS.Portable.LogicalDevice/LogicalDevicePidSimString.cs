using System;
using IDS.Core.IDS_CAN;

namespace IDS.Portable.LogicalDevice
{
	[Obsolete("Should use LogicalDevicePidSimText instead")]
	public class LogicalDevicePidSimString : LogicalDevicePidSimText
	{
		public LogicalDevicePidSimString(PID pid, string value)
			: base(pid, value)
		{
		}
	}
}
