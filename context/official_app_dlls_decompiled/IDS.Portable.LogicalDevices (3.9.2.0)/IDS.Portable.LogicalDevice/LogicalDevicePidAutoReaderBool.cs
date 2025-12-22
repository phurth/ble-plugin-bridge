namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidAutoReaderBool : LogicalDevicePidAutoReader<ILogicalDevicePid<bool>, bool>
	{
		public LogicalDevicePidAutoReaderBool(ILogicalDevicePid<bool> logicalDevicePid, int autoRefreshTimeMs = 10000, bool defaultValue = false, LogicalDevicePidAutoReader<ILogicalDevicePid<bool>, bool>.ValueWasSetAction? valueUpdated = null)
			: base(logicalDevicePid, autoRefreshTimeMs, defaultValue, valueUpdated)
		{
		}
	}
}
