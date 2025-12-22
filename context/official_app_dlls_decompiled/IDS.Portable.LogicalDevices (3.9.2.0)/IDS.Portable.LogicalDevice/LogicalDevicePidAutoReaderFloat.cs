namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidAutoReaderFloat : LogicalDevicePidAutoReader<ILogicalDevicePid<float>, float>
	{
		public LogicalDevicePidAutoReaderFloat(ILogicalDevicePid<float> logicalDevicePid, int autoRefreshTimeMs = 10000, float defaultValue = 0f, LogicalDevicePidAutoReader<ILogicalDevicePid<float>, float>.ValueWasSetAction? valueUpdated = null)
			: base(logicalDevicePid, autoRefreshTimeMs, defaultValue, valueUpdated)
		{
		}
	}
}
