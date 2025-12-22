using System;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceRelayHBridgeMomentaryCommandType2 : LogicalDeviceCommandPacket, ILogicalDeviceRelayHBridgeHomeResetCommand, ILogicalDeviceRelayHBridgeCommand, IDeviceCommandPacket, IDeviceDataPacket, IEquatable<LogicalDeviceCommandPacket>
	{
		public bool ClearingFault => Command == HBridgeCommand.ClearUserClearRequiredLatch;

		public bool Latching => false;

		public bool TurningOnRelay1 { get; }

		public bool TurningOnRelay2 { get; }

		public bool IsForwardCommand => Command == HBridgeCommand.Forward;

		public bool IsReverseCommand => Command == HBridgeCommand.Reverse;

		public RelayHBridgeDirection Direction => ToDirection(Command);

		public bool IsStopCommand => Command == HBridgeCommand.Stop;

		public bool IsHomeResetCommand => Command == HBridgeCommand.HomeReset;

		public bool IsAutoForwardCommand => Command == HBridgeCommand.AutoForward;

		public bool IsAutoReverseCommand => Command == HBridgeCommand.AutoReverse;

		public HBridgeCommand Command => (HBridgeCommand)base.CommandByte;

		private static HBridgeCommand ToCommand(RelayHBridgeDirection direction)
		{
			return direction switch
			{
				RelayHBridgeDirection.Forward => HBridgeCommand.Forward, 
				RelayHBridgeDirection.Reverse => HBridgeCommand.Reverse, 
				_ => HBridgeCommand.Stop, 
			};
		}

		private static RelayHBridgeDirection ToDirection(HBridgeCommand command)
		{
			switch (command)
			{
			case HBridgeCommand.Stop:
			case HBridgeCommand.ClearUserClearRequiredLatch:
				return RelayHBridgeDirection.Stop;
			case HBridgeCommand.Forward:
				return RelayHBridgeDirection.Forward;
			case HBridgeCommand.Reverse:
				return RelayHBridgeDirection.Reverse;
			default:
				return RelayHBridgeDirection.Unknown;
			}
		}

		public LogicalDeviceRelayHBridgeMomentaryCommandType2(bool clearUserClearRequiredLatch)
			: this(clearUserClearRequiredLatch ? HBridgeCommand.ClearUserClearRequiredLatch : HBridgeCommand.Stop)
		{
		}

		public LogicalDeviceRelayHBridgeMomentaryCommandType2(ILogicalDeviceId logicalId, HBridgeCommand command)
			: this(command)
		{
			RelayHBridgeEnergized relayHBridgeEnergized = Direction.ConvertToRelayEnergized(logicalId);
			TurningOnRelay1 = relayHBridgeEnergized == RelayHBridgeEnergized.Relay1;
			TurningOnRelay2 = relayHBridgeEnergized == RelayHBridgeEnergized.Relay2;
		}

		public LogicalDeviceRelayHBridgeMomentaryCommandType2(LogicalDeviceRelayHBridgeDirection relayDirection)
			: this(relayDirection.LogicalId, ToCommand(relayDirection.RelayDirection))
		{
		}

		private LogicalDeviceRelayHBridgeMomentaryCommandType2(HBridgeCommand command)
			: base((byte)command)
		{
			TurningOnRelay1 = false;
			TurningOnRelay2 = false;
		}

		public static LogicalDeviceRelayHBridgeMomentaryCommandType2 MakeHomeResetCommand()
		{
			return new LogicalDeviceRelayHBridgeMomentaryCommandType2(HBridgeCommand.HomeReset);
		}

		public static LogicalDeviceRelayHBridgeMomentaryCommandType2 MakeAutoCommand(HBridgeCommand command)
		{
			return new LogicalDeviceRelayHBridgeMomentaryCommandType2(command);
		}

		public override string ToString()
		{
			return $"{Command}";
		}
	}
}
