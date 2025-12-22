using System;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceRelayBasicLatchingCommandType2 : LogicalDeviceCommandPacket, ILogicalDeviceRelayBasicCommand, IDeviceCommandPacket, IDeviceDataPacket, IEquatable<LogicalDeviceCommandPacket>
	{
		public enum EnhancedCommand : byte
		{
			Off = 0,
			On = 1,
			ClearUserClearRequiredLatch = 3
		}

		public bool ClearingFault => base.CommandByte == 3;

		public bool Latching => true;

		public bool IsOn => base.CommandByte == 1;

		public LogicalDeviceRelayBasicLatchingCommandType2(EnhancedCommand command)
			: base((byte)command)
		{
		}

		public LogicalDeviceRelayBasicLatchingCommandType2(byte data)
			: base(data)
		{
		}

		public static LogicalDeviceRelayBasicLatchingCommandType2 MakeLatchTurnOffRelayCommand()
		{
			return new LogicalDeviceRelayBasicLatchingCommandType2(EnhancedCommand.Off);
		}

		public static LogicalDeviceRelayBasicLatchingCommandType2 MakeLatchTurnOnRelayCommand()
		{
			return new LogicalDeviceRelayBasicLatchingCommandType2(EnhancedCommand.On);
		}

		public static LogicalDeviceRelayBasicLatchingCommandType2 MakeClearFaultCommand()
		{
			return new LogicalDeviceRelayBasicLatchingCommandType2(EnhancedCommand.ClearUserClearRequiredLatch);
		}

		public override string ToString()
		{
			return $"{(EnhancedCommand)base.CommandByte}";
		}
	}
}
