using System;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidAutoReaderTimeSpan : LogicalDevicePidAutoReader<ILogicalDevicePid<TimeSpan>, TimeSpan>
	{
		public LogicalDevicePidAutoReaderTimeSpan(ILogicalDevicePid<TimeSpan> logicalDevicePid, int autoRefreshTimeMs = 10000, TimeSpan defaultValue = default(TimeSpan), LogicalDevicePidAutoReader<ILogicalDevicePid<TimeSpan>, TimeSpan>.ValueWasSetAction? valueUpdated = null)
			: base(logicalDevicePid, autoRefreshTimeMs, defaultValue, valueUpdated)
		{
		}
	}
}
