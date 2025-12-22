using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;

namespace OneControl.Devices
{
	internal static class LogicalDeviceLevelerJackMovementExtensionType4
	{
		private const string LogTag = "LogicalDeviceLevelerJackMovementExtensionType4";

		public const uint MiddleJackBitmask = 15360u;

		public static LogicalDeviceLevelerJackMovementType4 MakeJackMovement(LevelerJackDirection direction, LevelerJackLocation jackLocation)
		{
			switch (jackLocation)
			{
			case LevelerJackLocation.None:
				return LogicalDeviceLevelerJackMovementType4.None;
			case LevelerJackLocation.Tongue:
				return MakeJackMovement(LogicalDeviceLevelerJackMovementType4.JackTongueExtend, LogicalDeviceLevelerJackMovementType4.JackTongueRetract);
			case LevelerJackLocation.FrontRight:
				return MakeJackMovement(LogicalDeviceLevelerJackMovementType4.JackRightFrontExtend, LogicalDeviceLevelerJackMovementType4.JackRightFrontRetract);
			case LevelerJackLocation.FrontLeft:
				return MakeJackMovement(LogicalDeviceLevelerJackMovementType4.JackLeftFrontExtend, LogicalDeviceLevelerJackMovementType4.JackLeftFrontRetract);
			case LevelerJackLocation.RearRight:
				return MakeJackMovement(LogicalDeviceLevelerJackMovementType4.JackRightRearExtend, LogicalDeviceLevelerJackMovementType4.JackRightRearRetract);
			case LevelerJackLocation.RearLeft:
				return MakeJackMovement(LogicalDeviceLevelerJackMovementType4.JackLeftRearExtend, LogicalDeviceLevelerJackMovementType4.JackLeftRearRetract);
			case LevelerJackLocation.MiddleRight:
				return MakeJackMovement(LogicalDeviceLevelerJackMovementType4.JackRightMiddleExtend, LogicalDeviceLevelerJackMovementType4.JackRightMiddleRetract);
			case LevelerJackLocation.MiddleLeft:
				return MakeJackMovement(LogicalDeviceLevelerJackMovementType4.JackLeftMiddleExtend, LogicalDeviceLevelerJackMovementType4.JackLeftMiddleRetract);
			default:
				TaggedLog.Warning("LogicalDeviceLevelerJackMovementExtensionType4", $"The Leveler 4 device doesn't support the supplied jack location {jackLocation} going {direction} ");
				return LogicalDeviceLevelerJackMovementType4.Unknown;
			}
			LogicalDeviceLevelerJackMovementType4 MakeJackMovement(LogicalDeviceLevelerJackMovementType4 extend, LogicalDeviceLevelerJackMovementType4 retract)
			{
				return direction switch
				{
					LevelerJackDirection.Extend => extend, 
					LevelerJackDirection.Retract => retract, 
					LevelerJackDirection.Both => extend | retract, 
					_ => LogicalDeviceLevelerJackMovementType4.None, 
				};
			}
		}

		public static LevelerJackDirection JackDirection(this LogicalDeviceLevelerJackMovementType4 jackMovement, LevelerJackLocation jackLocation)
		{
			if (jackMovement.HasFlag(LogicalDeviceLevelerJackMovementType4.Unknown))
			{
				return LevelerJackDirection.None;
			}
			LogicalDeviceLevelerJackMovementType4 logicalDeviceLevelerJackMovementType = MakeJackMovement(LevelerJackDirection.Extend, jackLocation);
			LogicalDeviceLevelerJackMovementType4 logicalDeviceLevelerJackMovementType2 = MakeJackMovement(LevelerJackDirection.Retract, jackLocation);
			bool flag = (jackMovement & logicalDeviceLevelerJackMovementType) != 0 && !logicalDeviceLevelerJackMovementType.HasFlag(LogicalDeviceLevelerJackMovementType4.Unknown);
			bool flag2 = (jackMovement & logicalDeviceLevelerJackMovementType2) != 0 && !logicalDeviceLevelerJackMovementType.HasFlag(LogicalDeviceLevelerJackMovementType4.Unknown);
			if (flag && flag2)
			{
				return LevelerJackDirection.Both;
			}
			if (flag)
			{
				return LevelerJackDirection.Extend;
			}
			if (flag2)
			{
				return LevelerJackDirection.Retract;
			}
			return LevelerJackDirection.None;
		}

		internal static LogicalDeviceLevelerButtonJackMovementZeroType4 ToButtonJackMovementZero(this LogicalDeviceLevelerJackMovementType4 jackMovement, bool setZeroPoint)
		{
			if (jackMovement.HasFlag(LogicalDeviceLevelerJackMovementType4.Unknown))
			{
				return LogicalDeviceLevelerButtonJackMovementZeroType4.None;
			}
			uint num = (uint)jackMovement;
			if ((num & 0x3C00u) != 0)
			{
				TaggedLog.Warning("LogicalDeviceLevelerButtonJackMovementZeroType4", "Ignoring unsupported middle jack movement bit(s), " + num.DebugDumpAsFlags<LogicalDeviceLevelerJackMovementType4>() + ", that were supplied but aren't supported");
				num &= 0xFFFFC3FFu;
			}
			LogicalDeviceLevelerButtonJackMovementZeroType4 logicalDeviceLevelerButtonJackMovementZeroType = (LogicalDeviceLevelerButtonJackMovementZeroType4)num;
			if (setZeroPoint)
			{
				logicalDeviceLevelerButtonJackMovementZeroType |= LogicalDeviceLevelerButtonJackMovementZeroType4.SetZeroPoint;
			}
			return logicalDeviceLevelerButtonJackMovementZeroType;
		}

		internal static LogicalDeviceLevelerButtonJackMovementManualType4 ToButtonJackMovementManual(this LogicalDeviceLevelerJackMovementType4 jackMovement)
		{
			if (jackMovement.HasFlag(LogicalDeviceLevelerJackMovementType4.Unknown))
			{
				return LogicalDeviceLevelerButtonJackMovementManualType4.None;
			}
			uint num = (uint)jackMovement;
			if ((num & 0x3C00u) != 0)
			{
				TaggedLog.Warning("LogicalDeviceLevelerButtonJackMovementManualType4", "Ignoring unsupported middle jack movement bit(s), " + num.DebugDumpAsFlags<LogicalDeviceLevelerJackMovementType4>() + ", that were supplied but aren't supported");
				num &= 0xFFFFC3FFu;
			}
			return (LogicalDeviceLevelerButtonJackMovementManualType4)num;
		}

		internal static LogicalDeviceLevelerButtonJackMovementFaultManualType4 ToButtonJackMovementFaultManual(this LogicalDeviceLevelerJackMovementType4 jackMovement, bool setAutoRetract)
		{
			if (jackMovement.HasFlag(LogicalDeviceLevelerJackMovementType4.Unknown))
			{
				return LogicalDeviceLevelerButtonJackMovementFaultManualType4.None;
			}
			LogicalDeviceLevelerButtonJackMovementFaultManualType4 logicalDeviceLevelerButtonJackMovementFaultManualType = (LogicalDeviceLevelerButtonJackMovementFaultManualType4)jackMovement;
			if (setAutoRetract)
			{
				logicalDeviceLevelerButtonJackMovementFaultManualType |= LogicalDeviceLevelerButtonJackMovementFaultManualType4.AutoRetract;
			}
			return logicalDeviceLevelerButtonJackMovementFaultManualType;
		}
	}
}
