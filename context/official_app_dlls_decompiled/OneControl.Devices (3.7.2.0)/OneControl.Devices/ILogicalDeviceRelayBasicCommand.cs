using System;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface ILogicalDeviceRelayBasicCommand : IDeviceCommandPacket, IDeviceDataPacket, IEquatable<LogicalDeviceCommandPacket>
	{
		bool ClearingFault { get; }

		bool Latching { get; }

		bool IsOn { get; }
	}
}
