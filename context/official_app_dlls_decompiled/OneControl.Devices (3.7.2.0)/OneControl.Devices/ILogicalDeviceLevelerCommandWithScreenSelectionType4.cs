using System;
using System.Collections.Generic;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface ILogicalDeviceLevelerCommandWithScreenSelectionType4 : ILogicalDeviceLevelerCommandType4, IDeviceCommandPacket, IDeviceDataPacket, IEquatable<LogicalDeviceCommandPacket>
	{
		LogicalDeviceLevelerScreenType4 ScreenSelected { get; }

		IReadOnlyList<byte> RawButtonData { get; }
	}
}
