using System;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceLevelerStatusType1 : LogicalDeviceStatusPacketMutable
	{
		private const string LogTag = "LogicalDeviceLevelerStatusType1";

		private const int MinimumStatusPacketSize = 3;

		public const byte IndicatorStartIndex = 0;

		public const byte IndicatorNumBytes = 3;

		public int IndicatorBlinkCycleTimePollRateMs => 62;

		public LogicalDeviceLevelerStatusType1()
			: base(3u)
		{
		}

		public LogicalDeviceLevelerStatusIndicatorStateType1 IndicatorStateType1(LogicalDeviceLevelerStatusIndicatorType1 indicator)
		{
			if (indicator == LogicalDeviceLevelerStatusIndicatorType1.Unknown)
			{
				return LogicalDeviceLevelerStatusIndicatorStateType1.Unknown;
			}
			BitPositionValue bitPositionValue = new BitPositionValue((uint)indicator, 0, 3);
			LogicalDeviceLevelerStatusIndicatorStateType1 value = (LogicalDeviceLevelerStatusIndicatorStateType1)GetValue(bitPositionValue);
			if (!Enum.IsDefined(typeof(LogicalDeviceLevelerStatusIndicatorStateType1), value))
			{
				TaggedLog.Error("LogicalDeviceLevelerStatusType1", $"Unexpected/Unknown Indicator State {value}@{bitPositionValue}");
				return LogicalDeviceLevelerStatusIndicatorStateType1.Unknown;
			}
			return value;
		}

		public bool IsIndicatorActive(LogicalDeviceLevelerStatusIndicatorType1 indicator)
		{
			return IndicatorStateType1(indicator).IsActive();
		}

		public LogicalDeviceLevelerStatusIndicatorBlinkCycle IndicatorCurrentBlinkCycleSim(LogicalDeviceLevelerStatusIndicatorType1 statusIndicatorType1)
		{
			if (statusIndicatorType1 == LogicalDeviceLevelerStatusIndicatorType1.Unknown)
			{
				return LogicalDeviceLevelerStatusIndicatorBlinkCycle.Unknown;
			}
			LogicalDeviceLevelerStatusIndicatorStateType1 logicalDeviceLevelerStatusIndicatorStateType = IndicatorStateType1(statusIndicatorType1);
			switch (logicalDeviceLevelerStatusIndicatorStateType)
			{
			case LogicalDeviceLevelerStatusIndicatorStateType1.Off:
				return LogicalDeviceLevelerStatusIndicatorBlinkCycle.Off;
			case LogicalDeviceLevelerStatusIndicatorStateType1.OnSolid:
				return LogicalDeviceLevelerStatusIndicatorBlinkCycle.On;
			case LogicalDeviceLevelerStatusIndicatorStateType1.OnBlinkHalfHz:
			case LogicalDeviceLevelerStatusIndicatorStateType1.OnBlinkOneHz:
			case LogicalDeviceLevelerStatusIndicatorStateType1.OnBlinkTwoHz:
			case LogicalDeviceLevelerStatusIndicatorStateType1.OnBlinkFourHz:
			case LogicalDeviceLevelerStatusIndicatorStateType1.OnBlinkBriefOn:
			case LogicalDeviceLevelerStatusIndicatorStateType1.OnBlinkBriefOff:
				return logicalDeviceLevelerStatusIndicatorStateType.GetAttribute<LogicalDeviceLevelerIndicatorBlinkAttribute>()?.CurrentBlinkCycle ?? LogicalDeviceLevelerStatusIndicatorBlinkCycle.Unknown;
			default:
				return LogicalDeviceLevelerStatusIndicatorBlinkCycle.Unknown;
			}
		}
	}
}
