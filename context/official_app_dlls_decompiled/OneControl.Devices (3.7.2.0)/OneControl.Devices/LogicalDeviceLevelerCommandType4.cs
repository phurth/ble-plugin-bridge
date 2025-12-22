using System;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceLevelerCommandType4 : LogicalDeviceCommandPacket, ILogicalDeviceLevelerCommandType4, IDeviceCommandPacket, IDeviceDataPacket, IEquatable<LogicalDeviceCommandPacket>
	{
		public enum LevelerCommandCode : byte
		{
			ButtonPress = 0,
			Abort = 1,
			Back = 2,
			Home = 3,
			Unknown = byte.MaxValue
		}

		private const string LogTag = "LogicalDeviceLevelerCommandType4";

		public const int ScreenSelectedIndex = 0;

		public LevelerCommandCode Command => (LevelerCommandCode)base.CommandByte;

		protected LogicalDeviceLevelerScreenType4 ScreenSelectedImpl
		{
			get
			{
				if (base.Data.Length == 0)
				{
					return LogicalDeviceLevelerScreenType4.Unknown;
				}
				LogicalDeviceLevelerScreenType4 logicalDeviceLevelerScreenType = (LogicalDeviceLevelerScreenType4)base.Data[0];
				if (!Enum.IsDefined(typeof(LogicalDeviceLevelerScreenType4), logicalDeviceLevelerScreenType))
				{
					TaggedLog.Error("LogicalDeviceLevelerCommandType4", $"Unexpected/Unknown Selected Screen Type {logicalDeviceLevelerScreenType}");
					return LogicalDeviceLevelerScreenType4.Unknown;
				}
				return logicalDeviceLevelerScreenType;
			}
		}

		protected LogicalDeviceLevelerCommandType4(LevelerCommandCode command, int commandResponseTimeMs)
			: base((byte)command, commandResponseTimeMs)
		{
		}

		protected LogicalDeviceLevelerCommandType4(LevelerCommandCode command, LogicalDeviceLevelerScreenType4 screenSelected, int commandResponseTimeMs)
			: this(command, new byte[1] { (byte)screenSelected }, commandResponseTimeMs)
		{
		}

		protected LogicalDeviceLevelerCommandType4(LevelerCommandCode command, byte[] data, int commandResponseTimeMs)
			: base((byte)command, data, commandResponseTimeMs)
		{
		}

		internal LogicalDeviceLevelerCommandType4(byte commandByte, byte[] data, int commandResponseTimeMs)
			: base(commandByte, data, commandResponseTimeMs)
		{
		}

		public static LogicalDeviceLevelerCommandType4 MakeWakeupCommand(LogicalDeviceLevelerScreenType4 screenSelected, int commandResponseTimeMs = 200)
		{
			return new LogicalDeviceLevelerCommandButtonPressedType4<LogicalDeviceLevelerButtonNoneType4>(screenSelected, LogicalDeviceLevelerButtonNoneType4.None, commandResponseTimeMs);
		}

		public static LogicalDeviceLevelerCommandType4 MakeAbortCommand(int commandResponseTimeMs = 200)
		{
			return new LogicalDeviceLevelerCommandAbortType4(commandResponseTimeMs);
		}

		public static LogicalDeviceLevelerCommandType4 MakeBackCommand(LogicalDeviceLevelerScreenType4 screenSelected, int commandResponseTimeMs = 200)
		{
			return new LogicalDeviceLevelerCommandBackType4(screenSelected, commandResponseTimeMs);
		}

		public static LogicalDeviceLevelerCommandType4 MakeAbortToHomeCommand(int commandResponseTimeMs = 200)
		{
			return new LogicalDeviceLevelerCommandHomeType4(commandResponseTimeMs);
		}

		public override string ToString()
		{
			return GetType().Name + ": " + base.Data.DebugDump();
		}
	}
}
