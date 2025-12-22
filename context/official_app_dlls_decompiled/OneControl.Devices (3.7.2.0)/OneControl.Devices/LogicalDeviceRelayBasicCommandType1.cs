using System;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceRelayBasicCommandType1 : LogicalDeviceCommandPacket, ILogicalDeviceRelayBasicCommand, IDeviceCommandPacket, IDeviceDataPacket, IEquatable<LogicalDeviceCommandPacket>
	{
		public const BasicBitMask LatchBit = BasicBitMask.BitMask0X80;

		public const BasicBitMask ClearingFaultBit = BasicBitMask.BitMask0X40;

		public const BasicBitMask DisconnectStateBit = BasicBitMask.BitMask0X02;

		public const BasicBitMask CurrentCommandingStateBit = BasicBitMask.BitMask0X01;

		public bool ClearingFault => GetBit(BasicBitMask.BitMask0X40);

		public bool Latching => GetBit(BasicBitMask.BitMask0X80);

		public bool IsOn => GetBit(BasicBitMask.BitMask0X01);

		private LogicalDeviceRelayBasicCommandType1(bool state, bool disconnectState)
			: base(0, 0, 200)
		{
			LogicalDeviceCommandPacket.SetBit(ref base.Data[0], BasicBitMask.BitMask0X80, value: true);
			LogicalDeviceCommandPacket.SetBit(ref base.Data[0], BasicBitMask.BitMask0X40, value: false);
			LogicalDeviceCommandPacket.SetBit(ref base.Data[0], BasicBitMask.BitMask0X02, disconnectState);
			LogicalDeviceCommandPacket.SetBit(ref base.Data[0], BasicBitMask.BitMask0X01, state);
		}

		public LogicalDeviceRelayBasicCommandType1(bool state)
			: this(state, state)
		{
		}

		public LogicalDeviceRelayBasicCommandType1(byte data)
			: base(0, data)
		{
		}

		public static LogicalDeviceRelayBasicCommandType1 MakeLatchTurnOffRelayCommand()
		{
			return new LogicalDeviceRelayBasicCommandType1(state: false);
		}

		public static LogicalDeviceRelayBasicCommandType1 MakeLatchTurnOnRelayCommand()
		{
			return new LogicalDeviceRelayBasicCommandType1(state: true);
		}

		public static LogicalDeviceRelayBasicCommandType1 MakeClearFaultCommand()
		{
			LogicalDeviceRelayBasicCommandType1 logicalDeviceRelayBasicCommandType = new LogicalDeviceRelayBasicCommandType1(state: false);
			LogicalDeviceCommandPacket.SetBit(ref logicalDeviceRelayBasicCommandType.Data[0], BasicBitMask.BitMask0X80, value: false);
			LogicalDeviceCommandPacket.SetBit(ref logicalDeviceRelayBasicCommandType.Data[0], BasicBitMask.BitMask0X40, value: true);
			return logicalDeviceRelayBasicCommandType;
		}
	}
}
