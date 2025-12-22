using System;
using System.ComponentModel;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.Leveler.Type4;

namespace OneControl.Devices
{
	public class LogicalDeviceLevelerStatusType4 : LogicalDeviceStatusPacketMutable, ILogicalDeviceStatus<LogicalDeviceLevelerType4StatusSerializable>, ILogicalDeviceStatus, IDeviceDataPacketMutable, IDeviceDataPacket, INotifyPropertyChanged
	{
		private const string LogTag = "LogicalDeviceLevelerStatusType4";

		public const int MinimumStatusPacketSize = 8;

		private const int FlagsIndex = 0;

		private const int ScreenEnumIndex = 1;

		private const int ButtonsEnabledStartIndex = 2;

		public const int NonCriticalDataStartIndex = 5;

		private const int AngleStartIndex = 5;

		private const int AngleXInfoStartIndex = 6;

		private const int AngleYInfoStartIndex = 7;

		private static readonly BitPositionValue XAngleBitPosition = new BitPositionValue(240u, 5);

		private static readonly BitPositionValue YAngleBitPosition = new BitPositionValue(15u, 5);

		private static readonly BitPositionValue XAngleFractionBitPosition = new BitPositionValue(248u, 6);

		private static readonly BitPositionValue XAngleSignBitPosition = new BitPositionValue(4u, 6);

		private static readonly BitPositionValue YAngleFractionBitPosition = new BitPositionValue(248u, 7);

		private static readonly BitPositionValue YAngleSignBitPosition = new BitPositionValue(4u, 7);

		private const BasicBitMask FlagBitLevel = BasicBitMask.BitMask0X01;

		private const BasicBitMask FlagBitJacksFullyRetracted = BasicBitMask.BitMask0X02;

		private const BasicBitMask FlagBitJacksGrounded = BasicBitMask.BitMask0X04;

		private const BasicBitMask FlagBitJacksMoving = BasicBitMask.BitMask0X08;

		private const BasicBitMask FlagBitExcessAngleDetected = BasicBitMask.BitMask0X10;

		private const BasicBitMask FlagBitExcessTwistDetected = BasicBitMask.BitMask0X20;

		private bool _debugShowedUnknownScreenSelectedMessage;

		public bool IsLevel
		{
			get
			{
				return GetBit(BasicBitMask.BitMask0X01, 0);
			}
			set
			{
				SetBit(BasicBitMask.BitMask0X01, value, 0);
			}
		}

		public bool AreJacksFullyRetracted
		{
			get
			{
				return GetBit(BasicBitMask.BitMask0X02, 0);
			}
			set
			{
				SetBit(BasicBitMask.BitMask0X02, value, 0);
			}
		}

		public bool AreJacksGrounded
		{
			get
			{
				return GetBit(BasicBitMask.BitMask0X04, 0);
			}
			set
			{
				SetBit(BasicBitMask.BitMask0X04, value, 0);
			}
		}

		public bool AreJacksMoving
		{
			get
			{
				return GetBit(BasicBitMask.BitMask0X08, 0);
			}
			set
			{
				SetBit(BasicBitMask.BitMask0X08, value, 0);
			}
		}

		public bool IsExcessAngleDetected
		{
			get
			{
				return GetBit(BasicBitMask.BitMask0X10, 0);
			}
			set
			{
				SetBit(BasicBitMask.BitMask0X10, value, 0);
			}
		}

		public bool IsExcessTwistDetected
		{
			get
			{
				return GetBit(BasicBitMask.BitMask0X20, 0);
			}
			set
			{
				SetBit(BasicBitMask.BitMask0X20, value, 0);
			}
		}

		public LogicalDeviceLevelerScreenType4 ScreenSelected
		{
			get
			{
				LogicalDeviceLevelerScreenType4 logicalDeviceLevelerScreenType = (LogicalDeviceLevelerScreenType4)base.Data[1];
				if (!Enum.IsDefined(typeof(LogicalDeviceLevelerScreenType4), logicalDeviceLevelerScreenType))
				{
					if (!_debugShowedUnknownScreenSelectedMessage)
					{
						TaggedLog.Error("LogicalDeviceLevelerStatusType4", $"Unexpected/Unknown Screen Type {logicalDeviceLevelerScreenType}");
					}
					_debugShowedUnknownScreenSelectedMessage = true;
					return LogicalDeviceLevelerScreenType4.Unknown;
				}
				_debugShowedUnknownScreenSelectedMessage = false;
				return logicalDeviceLevelerScreenType;
			}
			set
			{
				base.Data[1] = (byte)value;
			}
		}

		public uint ButtonsEnabledRaw
		{
			get
			{
				return GetUInt24(2u);
			}
			set
			{
				SetUInt24(value, 2);
			}
		}

		public (LogicalDeviceLevelerScreenType4 forScreen, dynamic buttonsEnabled, byte[] data) AtomicState
		{
			get
			{
				byte[] array = CopyCurrentData();
				if (array.Length < 8)
				{
					return (LogicalDeviceLevelerScreenType4.Unknown, LogicalDeviceLevelerButtonNoneType4.None, array);
				}
				LogicalDeviceLevelerScreenType4 logicalDeviceLevelerScreenType = (LogicalDeviceLevelerScreenType4)array[1];
				if (!Enum.IsDefined(typeof(LogicalDeviceLevelerScreenType4), logicalDeviceLevelerScreenType))
				{
					if (!_debugShowedUnknownScreenSelectedMessage)
					{
						TaggedLog.Error("LogicalDeviceLevelerStatusType4", $"Unexpected/Unknown Screen Type {logicalDeviceLevelerScreenType}");
					}
					_debugShowedUnknownScreenSelectedMessage = true;
					return (LogicalDeviceLevelerScreenType4.Unknown, LogicalDeviceLevelerButtonNoneType4.None, array);
				}
				_debugShowedUnknownScreenSelectedMessage = false;
				uint num = LogicalDeviceDataPacketMutableDoubleBuffer.GetUInt24(array, 2u);
				switch (logicalDeviceLevelerScreenType)
				{
				case LogicalDeviceLevelerScreenType4.Home:
					return (logicalDeviceLevelerScreenType, (LogicalDeviceLevelerButtonHomeType4)num, array);
				case LogicalDeviceLevelerScreenType4.AutoLevel:
				case LogicalDeviceLevelerScreenType4.AutoHitch:
				case LogicalDeviceLevelerScreenType4.AutoRetractAllJacks:
				case LogicalDeviceLevelerScreenType4.AutoRetractFrontJacks:
				case LogicalDeviceLevelerScreenType4.AutoRetractRearJacks:
				case LogicalDeviceLevelerScreenType4.AutoHomeJacks:
					return (logicalDeviceLevelerScreenType, LogicalDeviceLevelerButtonNoneType4.None, array);
				case LogicalDeviceLevelerScreenType4.JackMovementManual:
				case LogicalDeviceLevelerScreenType4.JackMovementManualConsole:
					return (logicalDeviceLevelerScreenType, (LogicalDeviceLevelerButtonJackMovementManualType4)num, array);
				case LogicalDeviceLevelerScreenType4.JackMovementZero:
					return (logicalDeviceLevelerScreenType, (LogicalDeviceLevelerButtonJackMovementZeroType4)num, array);
				case LogicalDeviceLevelerScreenType4.PromptInfo:
					return (logicalDeviceLevelerScreenType, (LogicalDeviceLevelerButtonOkType4)num, array);
				case LogicalDeviceLevelerScreenType4.PromptYesNo:
					return (logicalDeviceLevelerScreenType, (LogicalDeviceLevelerButtonYesNoType4)num, array);
				case LogicalDeviceLevelerScreenType4.PromptAirbagTimeSelect:
					return (logicalDeviceLevelerScreenType, (LogicalDeviceLevelerButtonAirbagTimeSelectType4)num, array);
				case LogicalDeviceLevelerScreenType4.PromptFault:
					return (logicalDeviceLevelerScreenType, (LogicalDeviceLevelerButtonOkType4)num, array);
				case LogicalDeviceLevelerScreenType4.JackMovementFaultManual:
				case LogicalDeviceLevelerScreenType4.JackMovementFaultManualConsole:
					return (logicalDeviceLevelerScreenType, (LogicalDeviceLevelerButtonJackMovementFaultManualType4)num, array);
				case LogicalDeviceLevelerScreenType4.AirSuspensionControlManual:
					return (logicalDeviceLevelerScreenType, (LogicalDeviceLevelerButtonAirSuspensionType4)num, array);
				default:
					return (logicalDeviceLevelerScreenType, LogicalDeviceLevelerButtonNoneType4.None, array);
				}
			}
		}

		public bool IsButtonOkEnabled
		{
			get
			{
				if (((ValueTuple<LogicalDeviceLevelerScreenType4, object, byte[]>)AtomicState).Item2 is LogicalDeviceLevelerButtonOkType4 logicalDeviceLevelerButtonOkType)
				{
					return logicalDeviceLevelerButtonOkType.HasFlag(LogicalDeviceLevelerButtonOkType4.Ok);
				}
				return false;
			}
		}

		public bool IsButtonYesEnabled
		{
			get
			{
				if (((ValueTuple<LogicalDeviceLevelerScreenType4, object, byte[]>)AtomicState).Item2 is LogicalDeviceLevelerButtonYesNoType4 logicalDeviceLevelerButtonYesNoType)
				{
					return logicalDeviceLevelerButtonYesNoType.HasFlag(LogicalDeviceLevelerButtonYesNoType4.Yes);
				}
				return false;
			}
		}

		public bool IsButtonNoEnabled
		{
			get
			{
				if (((ValueTuple<LogicalDeviceLevelerScreenType4, object, byte[]>)AtomicState).Item2 is LogicalDeviceLevelerButtonYesNoType4 logicalDeviceLevelerButtonYesNoType)
				{
					return logicalDeviceLevelerButtonYesNoType.HasFlag(LogicalDeviceLevelerButtonYesNoType4.No);
				}
				return false;
			}
		}

		public bool IsButtonShortEnabled
		{
			get
			{
				if (((ValueTuple<LogicalDeviceLevelerScreenType4, object, byte[]>)AtomicState).Item2 is LogicalDeviceLevelerButtonAirbagTimeSelectType4 logicalDeviceLevelerButtonAirbagTimeSelectType)
				{
					return logicalDeviceLevelerButtonAirbagTimeSelectType.HasFlag(LogicalDeviceLevelerButtonAirbagTimeSelectType4.Short);
				}
				return false;
			}
		}

		public bool IsButtonLongEnabled
		{
			get
			{
				if (((ValueTuple<LogicalDeviceLevelerScreenType4, object, byte[]>)AtomicState).Item2 is LogicalDeviceLevelerButtonAirbagTimeSelectType4 logicalDeviceLevelerButtonAirbagTimeSelectType)
				{
					return logicalDeviceLevelerButtonAirbagTimeSelectType.HasFlag(LogicalDeviceLevelerButtonAirbagTimeSelectType4.Long);
				}
				return false;
			}
		}

		public bool IsButtonSetZeroPointEnabled
		{
			get
			{
				if (((ValueTuple<LogicalDeviceLevelerScreenType4, object, byte[]>)AtomicState).Item2 is LogicalDeviceLevelerButtonJackMovementZeroType4 logicalDeviceLevelerButtonJackMovementZeroType)
				{
					return logicalDeviceLevelerButtonJackMovementZeroType.HasFlag(LogicalDeviceLevelerButtonJackMovementZeroType4.SetZeroPoint);
				}
				return false;
			}
		}

		public bool IsButtonAutoRetractEnabled
		{
			get
			{
				if (((ValueTuple<LogicalDeviceLevelerScreenType4, object, byte[]>)AtomicState).Item2 is LogicalDeviceLevelerButtonJackMovementFaultManualType4 logicalDeviceLevelerButtonJackMovementFaultManualType)
				{
					return logicalDeviceLevelerButtonJackMovementFaultManualType.HasFlag(LogicalDeviceLevelerButtonJackMovementFaultManualType4.AutoRetract);
				}
				return false;
			}
		}

		public bool IsButtonAutoLevelEnabled
		{
			get
			{
				if (((ValueTuple<LogicalDeviceLevelerScreenType4, object, byte[]>)AtomicState).Item2 is LogicalDeviceLevelerButtonHomeType4 logicalDeviceLevelerButtonHomeType)
				{
					return logicalDeviceLevelerButtonHomeType.HasFlag(LogicalDeviceLevelerButtonHomeType4.AutoLevel);
				}
				return false;
			}
		}

		public bool IsButtonAutoHitchEnabled
		{
			get
			{
				if (((ValueTuple<LogicalDeviceLevelerScreenType4, object, byte[]>)AtomicState).Item2 is LogicalDeviceLevelerButtonHomeType4 logicalDeviceLevelerButtonHomeType)
				{
					return logicalDeviceLevelerButtonHomeType.HasFlag(LogicalDeviceLevelerButtonHomeType4.AutoHitch);
				}
				return false;
			}
		}

		public bool IsButtonAutoRetractAllJacksEnabled
		{
			get
			{
				if (((ValueTuple<LogicalDeviceLevelerScreenType4, object, byte[]>)AtomicState).Item2 is LogicalDeviceLevelerButtonHomeType4 logicalDeviceLevelerButtonHomeType)
				{
					return logicalDeviceLevelerButtonHomeType.HasFlag(LogicalDeviceLevelerButtonHomeType4.AutoRetractAllJacks);
				}
				return false;
			}
		}

		public bool IsButtonAutoRetractFrontJacksEnabled
		{
			get
			{
				if (((ValueTuple<LogicalDeviceLevelerScreenType4, object, byte[]>)AtomicState).Item2 is LogicalDeviceLevelerButtonHomeType4 logicalDeviceLevelerButtonHomeType)
				{
					return logicalDeviceLevelerButtonHomeType.HasFlag(LogicalDeviceLevelerButtonHomeType4.AutoRetractFrontJacks);
				}
				return false;
			}
		}

		public bool IsButtonAutoRetractRearJacksEnabled
		{
			get
			{
				if (((ValueTuple<LogicalDeviceLevelerScreenType4, object, byte[]>)AtomicState).Item2 is LogicalDeviceLevelerButtonHomeType4 logicalDeviceLevelerButtonHomeType)
				{
					return logicalDeviceLevelerButtonHomeType.HasFlag(LogicalDeviceLevelerButtonHomeType4.AutoRetractRearJacks);
				}
				return false;
			}
		}

		public bool IsButtonManualModeEnabled
		{
			get
			{
				if (((ValueTuple<LogicalDeviceLevelerScreenType4, object, byte[]>)AtomicState).Item2 is LogicalDeviceLevelerButtonHomeType4 logicalDeviceLevelerButtonHomeType)
				{
					return logicalDeviceLevelerButtonHomeType.HasFlag(LogicalDeviceLevelerButtonHomeType4.ManualMode);
				}
				return false;
			}
		}

		public bool IsButtonManualAirSuspensionEnabled
		{
			get
			{
				if (((ValueTuple<LogicalDeviceLevelerScreenType4, object, byte[]>)AtomicState).Item2 is LogicalDeviceLevelerButtonHomeType4 logicalDeviceLevelerButtonHomeType)
				{
					return logicalDeviceLevelerButtonHomeType.HasFlag(LogicalDeviceLevelerButtonHomeType4.ManualAirSuspension);
				}
				return false;
			}
		}

		public bool IsButtonZeroModeEnabled
		{
			get
			{
				if (((ValueTuple<LogicalDeviceLevelerScreenType4, object, byte[]>)AtomicState).Item2 is LogicalDeviceLevelerButtonHomeType4 logicalDeviceLevelerButtonHomeType)
				{
					return logicalDeviceLevelerButtonHomeType.HasFlag(LogicalDeviceLevelerButtonHomeType4.ZeroMode);
				}
				return false;
			}
		}

		public bool IsButtonHomeJacksEnabled
		{
			get
			{
				if (((ValueTuple<LogicalDeviceLevelerScreenType4, object, byte[]>)AtomicState).Item2 is LogicalDeviceLevelerButtonHomeType4 logicalDeviceLevelerButtonHomeType)
				{
					return logicalDeviceLevelerButtonHomeType.HasFlag(LogicalDeviceLevelerButtonHomeType4.AutoHomeJacks);
				}
				return false;
			}
		}

		public bool IsButtonRfPairingEnabled
		{
			get
			{
				if (((ValueTuple<LogicalDeviceLevelerScreenType4, object, byte[]>)AtomicState).Item2 is LogicalDeviceLevelerButtonHomeType4 logicalDeviceLevelerButtonHomeType)
				{
					return logicalDeviceLevelerButtonHomeType.HasFlag(LogicalDeviceLevelerButtonHomeType4.RfConfig);
				}
				return false;
			}
		}

		public float XAngle
		{
			get
			{
				uint value = GetValue(XAngleBitPosition);
				uint value2 = GetValue(XAngleFractionBitPosition);
				float num = (float)value + (float)value2 / 32f;
				if (GetValue(XAngleSignBitPosition) != 0)
				{
					num = 0f - num;
				}
				return num;
			}
			set
			{
				float num = Math.Abs(value);
				uint num2 = (uint)Math.Truncate(num);
				uint value2 = (uint)Math.Truncate((num - (float)num2) * 32f);
				int value3 = ((!(value >= 0f)) ? 1 : 0);
				SetValue(num2, XAngleBitPosition);
				SetValue(value2, XAngleFractionBitPosition);
				SetValue((uint)value3, XAngleSignBitPosition);
			}
		}

		public float YAngle
		{
			get
			{
				uint value = GetValue(YAngleBitPosition);
				uint value2 = GetValue(YAngleFractionBitPosition);
				float num = (float)value + (float)value2 / 32f;
				if (GetValue(YAngleSignBitPosition) != 0)
				{
					num = 0f - num;
				}
				return num;
			}
			set
			{
				float num = Math.Abs(value);
				uint num2 = (uint)Math.Truncate(num);
				uint value2 = (uint)Math.Truncate((num - (float)num2) * 32f);
				int value3 = ((!(value >= 0f)) ? 1 : 0);
				SetValue(num2, YAngleBitPosition);
				SetValue(value2, YAngleFractionBitPosition);
				SetValue((uint)value3, YAngleSignBitPosition);
			}
		}

		public LogicalDeviceLevelerStatusType4()
			: base(8u)
		{
		}

		public LogicalDeviceLevelerStatusType4(LogicalDeviceLevelerStatusType4 original)
			: base(8u)
		{
			byte[] array = original.CopyCurrentData();
			Update(array, array.Length);
		}

		public LogicalDeviceLevelerStatusType4(LogicalDeviceLevelerScreenType4 screenType)
			: base(8u)
		{
			ScreenSelected = screenType;
		}

		public LevelerJackDirection ButtonJackMovementEnabled(LevelerJackLocation jackLocation)
		{
			object item = ((ValueTuple<LogicalDeviceLevelerScreenType4, object, byte[]>)AtomicState).Item2;
			if (!(item is LogicalDeviceLevelerButtonJackMovementManualType4 button))
			{
				if (!(item is LogicalDeviceLevelerButtonJackMovementZeroType4 button2))
				{
					if (item is LogicalDeviceLevelerButtonJackMovementFaultManualType4 button3)
					{
						return button3.ToJackMovement().JackDirection(jackLocation);
					}
					return LevelerJackDirection.None;
				}
				return button2.ToJackMovement().JackDirection(jackLocation);
			}
			return button.ToJackMovement().JackDirection(jackLocation);
		}

		public LogicalDeviceLevelerType4StatusSerializable CopyAsSerializable()
		{
			return new LogicalDeviceLevelerType4StatusSerializable(this);
		}
	}
}
