using System;
using System.Collections.Generic;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceLevelerCommandButtonPressedType4<TButtonPressed> : LogicalDeviceLevelerCommandType4, ILogicalDeviceLevelerCommandButtonPressedType4, ILogicalDeviceLevelerCommandWithScreenSelectionType4, ILogicalDeviceLevelerCommandType4, IDeviceCommandPacket, IDeviceDataPacket, IEquatable<LogicalDeviceCommandPacket> where TButtonPressed : struct, IConvertible
	{
		private const string LogTag = "LogicalDeviceLevelerCommandButtonPressedType4";

		public const int ButtonDataSize = 3;

		public const int ButtonCommandPacketSize = 4;

		public const int UiModeIndex = 0;

		public const int ButtonsPressedStartIndex = 1;

		public IReadOnlyList<byte> RawButtonData => new ArraySegment<byte>(base.Data, 1, 3);

		public LogicalDeviceLevelerScreenType4 ScreenSelected => base.ScreenSelectedImpl;

		public TButtonPressed ButtonsPressed
		{
			get
			{
				if (!base.HasData || base.Size < 4)
				{
					return default(TButtonPressed);
				}
				if (!IsScreenSupported(ScreenSelected))
				{
					TaggedLog.Error("LogicalDeviceLevelerCommandButtonPressedType4", $"Command is invalid because screen of {ScreenSelected} is invalid for this command");
					return default(TButtonPressed);
				}
				if (!Enum<TButtonPressed>.TryConvert(MsbUInt24(1u), out var toValue))
				{
					TaggedLog.Error("LogicalDeviceLevelerCommandButtonPressedType4", $"ButtonsPressed for command is invalid, unable to convert UInt32 to {typeof(TButtonPressed)}");
					return default(TButtonPressed);
				}
				return toValue;
			}
		}

		public LogicalDeviceLevelerCommandButtonPressedType4(LogicalDeviceLevelerScreenType4 screenSelected, TButtonPressed buttonsPressed, int commandResponseTimeMs = 200)
			: base(LevelerCommandCode.ButtonPress, MakeDataForButtonCommand(screenSelected, buttonsPressed), commandResponseTimeMs)
		{
		}

		private static byte[] MakeDataForButtonCommand(LogicalDeviceLevelerScreenType4 screenSelected, TButtonPressed buttonsPressed)
		{
			uint num = Convert.ToUInt32(buttonsPressed);
			return new byte[4]
			{
				(byte)screenSelected,
				(byte)((num & 0xFF0000) >> 16),
				(byte)((num & 0xFF00) >> 8),
				(byte)(num & 0xFFu)
			};
		}

		protected virtual bool IsScreenSupported(LogicalDeviceLevelerScreenType4 screenSelected)
		{
			return true;
		}

		public override string ToString()
		{
			return $"{base.ToString()} screen: {ScreenSelected} buttons:{ButtonsPressed.DebugDumpAsFlags()}";
		}
	}
}
