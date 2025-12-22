using System;
using System.ComponentModel;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.Leveler.Type3;

namespace OneControl.Devices
{
	public class LogicalDeviceLevelerStatusType3 : LogicalDeviceStatusPacketMutable, ILogicalDeviceStatus<LogicalDeviceLevelerType3StatusSerializable>, ILogicalDeviceStatus, IDeviceDataPacketMutable, IDeviceDataPacket, INotifyPropertyChanged
	{
		private const string LogTag = "LogicalDeviceLevelerStatusType3";

		public const int MinimumStatusPacketSize = 6;

		public const int ScreenEnumIndex = 0;

		public const int DisableButtonFlagsStartIndex = 1;

		public const int BlinkRateIndex = 3;

		public const int IndicatorStartIndex = 4;

		public const int ScreenEnumNumBytes = 1;

		public const int DisableButtonFlagsNumBytes = 2;

		public const int IndicatorNumBytes = 2;

		public int IndicatorBlinkCycleTimePollRateMs => 62;

		public uint IndicatorBlinkOnTimeMs
		{
			get
			{
				return (uint)((((base.Data[3] & 0xF0) >> 4) + 1) * 125);
			}
			set
			{
				uint num = value / 125u;
				if (num != 0)
				{
					num--;
				}
				base.Data[3] = SetBlinkRate((byte)num, base.Data[3]);
			}
		}

		public uint IndicatorBlinkOffTimeMs
		{
			get
			{
				return (uint)(((base.Data[3] & 0xF) + 1) * 125);
			}
			set
			{
				uint num = value / 125u;
				if (num != 0)
				{
					num--;
				}
				base.Data[3] = SetBlinkRate(base.Data[3], (byte)num);
			}
		}

		public uint IndicatorBlinkTotalTimeMs => IndicatorBlinkOnTimeMs + IndicatorBlinkOffTimeMs;

		public double IndicatorBlinkDutyCycle => (double)IndicatorBlinkOnTimeMs / (double)IndicatorBlinkTotalTimeMs;

		public LogicalDeviceLevelerButtonType3 ButtonsEnabled
		{
			get
			{
				return (LogicalDeviceLevelerButtonType3)(~GetUInt16(1u));
			}
			set
			{
				SetUInt16((ushort)(~(uint)value), 1);
			}
		}

		public LogicalDeviceLevelerButtonType3 ButtonsDisabled
		{
			get
			{
				return (LogicalDeviceLevelerButtonType3)GetUInt16(1u);
			}
			set
			{
				SetUInt16((ushort)value, 1);
			}
		}

		public LogicalDeviceLevelerScreenType3 CurrentScreenShowing
		{
			get
			{
				LogicalDeviceLevelerScreenType3 logicalDeviceLevelerScreenType = (LogicalDeviceLevelerScreenType3)base.Data[0];
				if (!Enum.IsDefined(typeof(LogicalDeviceLevelerScreenType3), logicalDeviceLevelerScreenType))
				{
					TaggedLog.Error("LogicalDeviceLevelerStatusType3", $"Unexpected/Unknown Screen Type {logicalDeviceLevelerScreenType}");
					return LogicalDeviceLevelerScreenType3.Unknown;
				}
				return logicalDeviceLevelerScreenType;
			}
			set
			{
				base.Data[0] = (byte)value;
			}
		}

		public LogicalDeviceLevelerStatusType3()
			: base(6u)
		{
		}

		public LogicalDeviceLevelerStatusIndicatorStateType3 IndicatorStateType3(LogicalDeviceLevelerStatusIndicatorType3 indicator)
		{
			if (indicator == LogicalDeviceLevelerStatusIndicatorType3.Unknown)
			{
				return LogicalDeviceLevelerStatusIndicatorStateType3.Unknown;
			}
			BitPositionValue bitPositionValue = new BitPositionValue((uint)indicator, 4, 2);
			LogicalDeviceLevelerStatusIndicatorStateType3 value = (LogicalDeviceLevelerStatusIndicatorStateType3)GetValue(bitPositionValue);
			if (!Enum.IsDefined(typeof(LogicalDeviceLevelerStatusIndicatorStateType3), value))
			{
				TaggedLog.Error("LogicalDeviceLevelerStatusType3", $"Unexpected/Unknown Indicator State {value}@{bitPositionValue}");
				return LogicalDeviceLevelerStatusIndicatorStateType3.Unknown;
			}
			return value;
		}

		public void SetIndicatorStateType3(LogicalDeviceLevelerStatusIndicatorType3 indicator, LogicalDeviceLevelerStatusIndicatorStateType3 indicatorState)
		{
			if (indicatorState != LogicalDeviceLevelerStatusIndicatorStateType3.Unknown && indicator != LogicalDeviceLevelerStatusIndicatorType3.Unknown)
			{
				BitPositionValue bitPosition = new BitPositionValue((uint)indicator, 4, 2);
				SetValue((uint)indicatorState, bitPosition);
			}
		}

		public bool IsIndicatorActive(LogicalDeviceLevelerStatusIndicatorType3 indicator)
		{
			return IndicatorStateType3(indicator).IsActive();
		}

		protected byte SetBlinkRate(byte onTime125Ms, byte offTime125Ms)
		{
			return (byte)(((uint)(onTime125Ms << 4) & 0xF0u) | (offTime125Ms & 0xFu));
		}

		public LogicalDeviceLevelerStatusIndicatorBlinkCycle IndicatorCurrentBlinkCycleSim(LogicalDeviceLevelerStatusIndicatorType3 statusIndicatorType3)
		{
			if (statusIndicatorType3 == LogicalDeviceLevelerStatusIndicatorType3.Unknown)
			{
				return LogicalDeviceLevelerStatusIndicatorBlinkCycle.Unknown;
			}
			return IndicatorStateType3(statusIndicatorType3) switch
			{
				LogicalDeviceLevelerStatusIndicatorStateType3.Off => LogicalDeviceLevelerStatusIndicatorBlinkCycle.Off, 
				LogicalDeviceLevelerStatusIndicatorStateType3.On => LogicalDeviceLevelerStatusIndicatorBlinkCycle.On, 
				LogicalDeviceLevelerStatusIndicatorStateType3.Blink => LogicalDeviceLevelerIndicatorBlinkAttribute.CalculateCurrentBlinkCycle((uint)((double)IndicatorBlinkTotalTimeMs / 2.0), IndicatorBlinkDutyCycle, inverseBlink: false), 
				LogicalDeviceLevelerStatusIndicatorStateType3.InverseBlink => LogicalDeviceLevelerIndicatorBlinkAttribute.CalculateCurrentBlinkCycle((uint)((double)IndicatorBlinkTotalTimeMs / 2.0), IndicatorBlinkDutyCycle, inverseBlink: true), 
				_ => LogicalDeviceLevelerStatusIndicatorBlinkCycle.Unknown, 
			};
		}

		public bool IsButtonEnabled(LogicalDeviceLevelerButtonType3 buttonFlagsType3)
		{
			if (buttonFlagsType3 == LogicalDeviceLevelerButtonType3.None)
			{
				return false;
			}
			ushort num = (ushort)(~GetUInt16(1u));
			if (((uint)buttonFlagsType3 & (uint)num) == (uint)buttonFlagsType3)
			{
				return true;
			}
			return false;
		}

		public LogicalDeviceLevelerType3StatusSerializable CopyAsSerializable()
		{
			return new LogicalDeviceLevelerType3StatusSerializable(this);
		}
	}
}
