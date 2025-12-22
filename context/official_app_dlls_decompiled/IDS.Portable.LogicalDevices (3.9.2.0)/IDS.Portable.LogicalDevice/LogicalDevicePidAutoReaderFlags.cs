using System;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidAutoReaderFlags<TFlags> : LogicalDevicePidAutoReader<ILogicalDevicePid<TFlags>, TFlags> where TFlags : Enum
	{
		public LogicalDevicePidAutoReaderFlags(ILogicalDevicePid<TFlags> logicalDevicePid, int autoRefreshTimeMs = 10000, TFlags defaultValue = default(TFlags), LogicalDevicePidAutoReader<ILogicalDevicePid<TFlags>, TFlags>.ValueWasSetAction? valueUpdated = null)
			: base(logicalDevicePid, autoRefreshTimeMs, defaultValue, valueUpdated)
		{
		}
	}
}
