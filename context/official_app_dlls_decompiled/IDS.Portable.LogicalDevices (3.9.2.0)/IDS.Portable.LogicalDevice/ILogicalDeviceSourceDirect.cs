using IDS.Core.IDS_CAN;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceSourceDirect : ILogicalDeviceSource
	{
		ILogicalDeviceService DeviceService { get; }

		IN_MOTION_LOCKOUT_LEVEL InTransitLockoutLevel { get; }

		bool IsLogicalDeviceSupported(ILogicalDevice? logicalDevice);

		bool IsLogicalDeviceOnline(ILogicalDevice? logicalDevice);

		IN_MOTION_LOCKOUT_LEVEL GetLogicalDeviceInTransitLockoutLevel(ILogicalDevice? logicalDevice);

		bool IsLogicalDeviceHazardous(ILogicalDevice? logicalDevice);
	}
}
