using System;
using System.Collections.Generic;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceLightDimmableCommand : LogicalDeviceCommandPacket
	{
		public const int CommandPacketSize = 8;

		public const int LightCommandByteIndex = 0;

		public const int MaxBrightnessByteIndex = 1;

		public const int DurationByteIndex = 2;

		public const int CycleTime1MsbIndex = 3;

		public const int CycleTime1LsbIndex = 4;

		public const int CycleTime2MsbIndex = 5;

		public const int CycleTime2LsbIndex = 6;

		public const int UndefinedByteIndex = 7;

		private const int InvalidCycleTime = 0;

		private const int DefaultCycleTime = 220;

		public DimmableLightCommand Command
		{
			get
			{
				try
				{
					return DimmableLightCommandFromByte(base.Data[0]);
				}
				catch
				{
					return DimmableLightCommand.Off;
				}
			}
		}

		public byte MaxBrightness => base.Data[1];

		public byte Duration => base.Data[2];

		public int CycleTime1 => (base.Data[3] << 8) | base.Data[4];

		public int CycleTime2 => (base.Data[5] << 8) | base.Data[6];

		public IReadOnlyList<byte> DataMinimum => new ArraySegment<byte>(base.Data, 0, DataMinimumLength(Command));

		public LogicalDeviceLightDimmableCommand()
			: this(DimmableLightMode.Off, 0, 0, 0, 0)
		{
		}

		public LogicalDeviceLightDimmableCommand(DimmableLightMode mode, byte maxBrightness, byte duration, int cycleTime1, int cycleTime2)
			: this(mode.ConvertToCommand(), maxBrightness, duration, cycleTime1, cycleTime2)
		{
		}

		public LogicalDeviceLightDimmableCommand(byte commandByte, byte[] data)
			: base(commandByte, data)
		{
		}

		public LogicalDeviceLightDimmableCommand(IReadOnlyList<byte> data)
			: base(0, data)
		{
			DimmableLightCommand command = DimmableLightCommandFromByte(data[0]);
			if (data.Count < DataMinimumLength(command))
			{
				throw new ArgumentOutOfRangeException("data", "Buffer not big enough to represent command");
			}
		}

		public LogicalDeviceLightDimmableCommand(DimmableLightCommand command, byte maxBrightness, byte duration, int cycleTime1, int cycleTime2)
			: this(0, new byte[8])
		{
			if (command - 2 <= DimmableLightCommand.On && (cycleTime1 == 0 || cycleTime2 == 0))
			{
				cycleTime1 = (cycleTime2 = 220);
			}
			base.Data[0] = (byte)command;
			base.Data[1] = maxBrightness;
			base.Data[2] = duration;
			base.Data[3] = (byte)((cycleTime1 & 0xFF00) >> 8);
			base.Data[4] = (byte)((uint)cycleTime1 & 0xFFu);
			base.Data[5] = (byte)((cycleTime2 & 0xFF00) >> 8);
			base.Data[6] = (byte)((uint)cycleTime2 & 0xFFu);
			base.Data[7] = 0;
		}

		public static LogicalDeviceLightDimmableCommand MakeSettingsCommand(byte maxBrightness, byte duration)
		{
			return new LogicalDeviceLightDimmableCommand(DimmableLightCommand.Settings, maxBrightness, duration, 0, 0);
		}

		public static LogicalDeviceLightDimmableCommand MakeRestoreCommand()
		{
			return new LogicalDeviceLightDimmableCommand(DimmableLightCommand.Restore, 0, 0, 0, 0);
		}

		public static DimmableLightCommand DimmableLightCommandFromByte(byte rawValue)
		{
			return rawValue switch
			{
				0 => DimmableLightCommand.Off, 
				1 => DimmableLightCommand.On, 
				2 => DimmableLightCommand.Blink, 
				3 => DimmableLightCommand.Swell, 
				126 => DimmableLightCommand.Settings, 
				127 => DimmableLightCommand.Restore, 
				_ => throw new ArgumentOutOfRangeException($"Unknown DimmableLightMode value 0x{rawValue:x2}"), 
			};
		}

		public int DataMinimumLength(DimmableLightCommand command)
		{
			switch (Command)
			{
			case DimmableLightCommand.Off:
			case DimmableLightCommand.Restore:
				return 1;
			case DimmableLightCommand.On:
			case DimmableLightCommand.Settings:
				return 3;
			case DimmableLightCommand.Blink:
			case DimmableLightCommand.Swell:
				return 7;
			default:
				throw new NotSupportedException($"Command {Command} not supported");
			}
		}
	}
}
