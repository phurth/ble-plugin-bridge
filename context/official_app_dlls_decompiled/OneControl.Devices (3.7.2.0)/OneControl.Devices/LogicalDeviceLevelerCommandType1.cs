using System;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceLevelerCommandType1 : LogicalDeviceCommandPacket
	{
		public const int CommandPacketSize = 2;

		public static readonly LogicalDeviceLevelerCommandType1 RightButtonCommand = new LogicalDeviceLevelerCommandType1(LogicalDeviceLevelerButtonType1.Right);

		public static readonly LogicalDeviceLevelerCommandType1 LeftButtonCommand = new LogicalDeviceLevelerCommandType1(LogicalDeviceLevelerButtonType1.Left);

		public static readonly LogicalDeviceLevelerCommandType1 RearButtonCommand = new LogicalDeviceLevelerCommandType1(LogicalDeviceLevelerButtonType1.Rear);

		public static readonly LogicalDeviceLevelerCommandType1 FrontButtonCommand = new LogicalDeviceLevelerCommandType1(LogicalDeviceLevelerButtonType1.Front);

		public static readonly LogicalDeviceLevelerCommandType1 AutoLevelButtonCommand = new LogicalDeviceLevelerCommandType1(LogicalDeviceLevelerButtonType1.AutoLevel);

		public static readonly LogicalDeviceLevelerCommandType1 RetractButtonCommand = new LogicalDeviceLevelerCommandType1(LogicalDeviceLevelerButtonType1.Retract);

		public static readonly LogicalDeviceLevelerCommandType1 EnterButtonCommand = new LogicalDeviceLevelerCommandType1(LogicalDeviceLevelerButtonType1.Enter);

		public static readonly LogicalDeviceLevelerCommandType1 MenuDownButtonCommand = new LogicalDeviceLevelerCommandType1(LogicalDeviceLevelerButtonType1.MenuDown);

		public static readonly LogicalDeviceLevelerCommandType1 PowerOnCommand = new LogicalDeviceLevelerCommandType1(LogicalDeviceLevelerButtonType1.Power, 500);

		public static readonly LogicalDeviceLevelerCommandType1 MenuUpButtonCommand = new LogicalDeviceLevelerCommandType1(LogicalDeviceLevelerButtonType1.MenuUp);

		public LogicalDeviceLevelerButtonType1 ButtonCommand => (LogicalDeviceLevelerButtonType1)MsbUInt16(0u);

		public LogicalDeviceLevelerCommandType1(LogicalDeviceLevelerButtonType1 buttonType, int commandResponseTimeMs = 200)
			: base(0, (ushort)buttonType, commandResponseTimeMs)
		{
		}

		public LogicalDeviceLevelerCommandType1(byte commandByte, byte[] data, int commandResponseTimeMs)
			: base(commandByte, data, commandResponseTimeMs)
		{
		}

		public override string ToString()
		{
			if (!Enum.IsDefined(typeof(LogicalDeviceLevelerButtonType1), ButtonCommand))
			{
				return $"ButtonCommand({(ushort)ButtonCommand:X})";
			}
			return $"ButtonCommand({ButtonCommand})";
		}
	}
}
