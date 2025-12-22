using IDS.Core.IDS_CAN;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceSourceDirectConnectionIdsCan : ILogicalDeviceSourceDirectConnection, ILogicalDeviceSourceDirect, ILogicalDeviceSource, ILogicalDeviceSourceConnection
	{
		IAdapter? Gateway { get; }

		IRemoteDevice? FindRemoteDevice(ILogicalDevice? logicalDevice);

		void UpdateLogicalDeviceOnlineStatus(ILogicalDevice logicalDevice);
	}
}
