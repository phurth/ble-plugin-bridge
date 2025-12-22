namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceSourceDirectConnection : ILogicalDeviceSourceDirect, ILogicalDeviceSource, ILogicalDeviceSourceConnection
	{
		ILogicalDeviceSessionManager? SessionManager { get; }
	}
}
