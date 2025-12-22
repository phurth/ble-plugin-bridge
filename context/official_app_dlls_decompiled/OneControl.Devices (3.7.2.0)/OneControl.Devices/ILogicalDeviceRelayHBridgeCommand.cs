using System;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface ILogicalDeviceRelayHBridgeCommand : IDeviceCommandPacket, IDeviceDataPacket, IEquatable<LogicalDeviceCommandPacket>
	{
		bool ClearingFault { get; }

		bool Latching { get; }

		bool TurningOnRelay1 { get; }

		bool TurningOnRelay2 { get; }

		bool IsForwardCommand { get; }

		bool IsReverseCommand { get; }

		bool IsHomeResetCommand { get; }

		bool IsAutoForwardCommand { get; }

		bool IsAutoReverseCommand { get; }

		RelayHBridgeDirection Direction { get; }

		HBridgeCommand Command { get; }

		bool IsStopCommand { get; }
	}
}
