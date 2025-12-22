using IDS.Core.IDS_CAN;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceIdsCan
	{
		NETWORK_STATUS LastReceivedNetworkStatus { get; }
	}
}
