using System;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceLevelerCommandType3 : LogicalDeviceCommandPacket
	{
		public const string LogTag = "LogicalDeviceLevelerCommandType3";

		public const int CommandPacketSize = 3;

		public const int ScreenSelectedIndex = 0;

		public const int ButtonsPressedStartIndex = 1;

		public LogicalDeviceLevelerScreenType3 ScreenSelected
		{
			get
			{
				LogicalDeviceLevelerScreenType3 logicalDeviceLevelerScreenType = (LogicalDeviceLevelerScreenType3)base.Data[0];
				if (!Enum.IsDefined(typeof(LogicalDeviceLevelerScreenType3), logicalDeviceLevelerScreenType))
				{
					TaggedLog.Error("LogicalDeviceLevelerCommandType3", $"Unexpected/Unknown Selected Screen Type {logicalDeviceLevelerScreenType}");
					return LogicalDeviceLevelerScreenType3.Unknown;
				}
				return logicalDeviceLevelerScreenType;
			}
		}

		public LogicalDeviceLevelerButtonType3 ButtonsPressed => (LogicalDeviceLevelerButtonType3)MsbUInt16(1u);

		public LogicalDeviceLevelerCommandType3(LogicalDeviceLevelerScreenType3 screenSelected, LogicalDeviceLevelerButtonType3 buttonsPressed, int commandResponseTimeMs = 200)
			: base(0, MakeDataForCommand(screenSelected, buttonsPressed), commandResponseTimeMs)
		{
		}

		public LogicalDeviceLevelerCommandType3(byte commandByte, byte[] data, int commandResponseTimeMs)
			: base(commandByte, data, commandResponseTimeMs)
		{
		}

		protected static byte[] MakeDataForCommand(LogicalDeviceLevelerScreenType3 screenSelected, LogicalDeviceLevelerButtonType3 buttonsPressed)
		{
			return new byte[3]
			{
				(byte)screenSelected,
				(byte)((int)(buttonsPressed & (LogicalDeviceLevelerButtonType3.Extend | LogicalDeviceLevelerButtonType3.Back | LogicalDeviceLevelerButtonType3.MenuUp | LogicalDeviceLevelerButtonType3.AutoHitch | LogicalDeviceLevelerButtonType3.EnterSetup | LogicalDeviceLevelerButtonType3.Reserved1 | LogicalDeviceLevelerButtonType3.Reserved2 | LogicalDeviceLevelerButtonType3.Reserved3)) >> 8),
				(byte)(buttonsPressed & (LogicalDeviceLevelerButtonType3.Right | LogicalDeviceLevelerButtonType3.Left | LogicalDeviceLevelerButtonType3.Rear | LogicalDeviceLevelerButtonType3.Front | LogicalDeviceLevelerButtonType3.AutoLevel | LogicalDeviceLevelerButtonType3.Retract | LogicalDeviceLevelerButtonType3.Enter | LogicalDeviceLevelerButtonType3.MenuDown))
			};
		}
	}
}
