using System;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface ILogicalDeviceRelayHBridgeHomeResetCommand : ILogicalDeviceRelayHBridgeCommand, IDeviceCommandPacket, IDeviceDataPacket, IEquatable<LogicalDeviceCommandPacket>
	{
		new bool IsHomeResetCommand { get; }
	}
}
