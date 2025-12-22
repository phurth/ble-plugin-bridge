using System;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface ILogicalDeviceLevelerCommandButtonPressedType4 : ILogicalDeviceLevelerCommandWithScreenSelectionType4, ILogicalDeviceLevelerCommandType4, IDeviceCommandPacket, IDeviceDataPacket, IEquatable<LogicalDeviceCommandPacket>
	{
	}
}
