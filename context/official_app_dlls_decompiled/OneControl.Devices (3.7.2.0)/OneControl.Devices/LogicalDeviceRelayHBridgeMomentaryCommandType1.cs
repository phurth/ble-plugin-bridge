using System;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceRelayHBridgeMomentaryCommandType1 : LogicalDeviceCommandPacket, ILogicalDeviceRelayHBridgeCommand, IDeviceCommandPacket, IDeviceDataPacket, IEquatable<LogicalDeviceCommandPacket>
	{
		public const BasicBitMask ClearingFaultBit = BasicBitMask.BitMask0X40;

		public const BasicBitMask Relay1Bit = BasicBitMask.BitMask0X01;

		public const BasicBitMask Relay2Bit = BasicBitMask.BitMask0X04;

		public bool ClearingFault => GetBit(BasicBitMask.BitMask0X40);

		public bool Latching => false;

		public bool TurningOnRelay1 => GetBit(BasicBitMask.BitMask0X01);

		public bool TurningOnRelay2 => GetBit(BasicBitMask.BitMask0X04);

		public RelayHBridgeDirection Direction { get; }

		public HBridgeCommand Command
		{
			get
			{
				if (ClearingFault)
				{
					return HBridgeCommand.ClearUserClearRequiredLatch;
				}
				return Direction switch
				{
					RelayHBridgeDirection.Forward => HBridgeCommand.Forward, 
					RelayHBridgeDirection.Reverse => HBridgeCommand.Reverse, 
					RelayHBridgeDirection.Stop => HBridgeCommand.Stop, 
					_ => HBridgeCommand.Stop, 
				};
			}
		}

		public bool IsForwardCommand => Direction == RelayHBridgeDirection.Forward;

		public bool IsReverseCommand => Direction == RelayHBridgeDirection.Reverse;

		public bool IsStopCommand
		{
			get
			{
				if (!TurningOnRelay1 && !TurningOnRelay2)
				{
					return !ClearingFault;
				}
				return false;
			}
		}

		public bool IsAutoForwardCommand => Command == HBridgeCommand.AutoForward;

		public bool IsAutoReverseCommand => Command == HBridgeCommand.AutoReverse;

		public bool IsHomeResetCommand => false;

		public LogicalDeviceRelayHBridgeMomentaryCommandType1(bool clearFault)
			: base(0, 0, 200)
		{
			LogicalDeviceCommandPacket.SetBit(ref base.Data[0], BasicBitMask.BitMask0X01, value: false);
			LogicalDeviceCommandPacket.SetBit(ref base.Data[0], BasicBitMask.BitMask0X04, value: false);
			LogicalDeviceCommandPacket.SetBit(ref base.Data[0], BasicBitMask.BitMask0X40, clearFault);
			Direction = RelayHBridgeDirection.Stop;
		}

		public LogicalDeviceRelayHBridgeMomentaryCommandType1(ILogicalDeviceId logicalId, bool relay1, bool relay2, bool clearFault)
			: base(0, 0, 200)
		{
			LogicalDeviceCommandPacket.SetBit(ref base.Data[0], BasicBitMask.BitMask0X01, relay1);
			LogicalDeviceCommandPacket.SetBit(ref base.Data[0], BasicBitMask.BitMask0X04, relay2);
			LogicalDeviceCommandPacket.SetBit(ref base.Data[0], BasicBitMask.BitMask0X40, clearFault);
			Direction = RelayHBridgeDirectionExtension.ConvertToHBridgeDirection(relay1, relay2, logicalId);
		}

		public LogicalDeviceRelayHBridgeMomentaryCommandType1(LogicalDeviceRelayHBridgeDirection relayDirection, bool clearFault)
			: base(0, 0, 200)
		{
			bool flag = relayDirection.RelayEnergized == RelayHBridgeEnergized.Relay1;
			bool flag2 = relayDirection.RelayEnergized == RelayHBridgeEnergized.Relay2;
			LogicalDeviceCommandPacket.SetBit(ref base.Data[0], BasicBitMask.BitMask0X01, flag);
			LogicalDeviceCommandPacket.SetBit(ref base.Data[0], BasicBitMask.BitMask0X04, flag2);
			LogicalDeviceCommandPacket.SetBit(ref base.Data[0], BasicBitMask.BitMask0X40, clearFault);
			Direction = RelayHBridgeDirectionExtension.ConvertToHBridgeDirection(flag, flag2, relayDirection.LogicalId);
		}

		public LogicalDeviceRelayHBridgeMomentaryCommandType1(ILogicalDeviceId logicalId, byte data)
			: base(0, data)
		{
			Direction = RelayHBridgeDirectionExtension.ConvertToHBridgeDirection(TurningOnRelay1, TurningOnRelay1, logicalId);
		}

		public static LogicalDeviceRelayHBridgeMomentaryCommandType1 MakeTurnOffRelaysCommand(bool clearFault = false)
		{
			return new LogicalDeviceRelayHBridgeMomentaryCommandType1(clearFault);
		}

		public override string ToString()
		{
			if (IsStopCommand)
			{
				return "CommandRelayStop";
			}
			if (ClearingFault)
			{
				return "CommandRelayClearFault";
			}
			if (TurningOnRelay1)
			{
				return "CommandRelay1On";
			}
			if (TurningOnRelay2)
			{
				return "CommandRelay2On";
			}
			return "CommandUnknown";
		}
	}
}
