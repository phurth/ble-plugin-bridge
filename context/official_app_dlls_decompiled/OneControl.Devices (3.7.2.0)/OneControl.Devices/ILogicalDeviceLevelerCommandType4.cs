using System;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface ILogicalDeviceLevelerCommandType4 : IDeviceCommandPacket, IDeviceDataPacket, IEquatable<LogicalDeviceCommandPacket>
	{
		LogicalDeviceLevelerCommandType4.LevelerCommandCode Command { get; }
	}
}
