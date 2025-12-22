namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidAutoReaderUInt64 : LogicalDevicePidAutoReader<ILogicalDevicePid<ulong>, ulong>
	{
		public LogicalDevicePidAutoReaderUInt64(ILogicalDevicePid<ulong> logicalDevicePid, int autoRefreshTimeMs = 10000, ulong defaultValue = 0uL, LogicalDevicePidAutoReader<ILogicalDevicePid<ulong>, ulong>.ValueWasSetAction? valueUpdated = null)
			: base(logicalDevicePid, autoRefreshTimeMs, defaultValue, valueUpdated)
		{
		}
	}
}
