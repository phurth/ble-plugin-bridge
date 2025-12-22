namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidAutoReaderByte : LogicalDevicePidAutoReader<ILogicalDevicePid<byte>, byte>
	{
		public LogicalDevicePidAutoReaderByte(ILogicalDevicePid<byte> logicalDevicePid, int autoRefreshTimeMs = 10000, byte defaultValue = 0, LogicalDevicePidAutoReader<ILogicalDevicePid<byte>, byte>.ValueWasSetAction? valueUpdated = null)
			: base(logicalDevicePid, autoRefreshTimeMs, defaultValue, valueUpdated)
		{
		}
	}
}
