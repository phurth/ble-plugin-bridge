using IDS.Portable.LogicalDevice.LogicalDeviceSource.ConnectionFailure;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceSourceConnectionFailure : ILogicalDeviceSourceConnection, ILogicalDeviceSource
	{
		IConnectionFailure ConnectionFailure { get; }
	}
}
