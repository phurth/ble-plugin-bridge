using System;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidJumpToBoot : LogicalDevicePid, ILogicalDevicePidJumpToBoot, ILogicalDevicePid
	{
		public const string LogTag = "LogicalDevicePidJumpToBoot";

		public const double HoldTimeMsPerTick = 10.0;

		public static readonly BitPositionValue64 JumpToBootStateBitPosition = new BitPositionValue64(281474976645120uL);

		public static readonly BitPositionValue64 JumpToBootMsBitPosition = new BitPositionValue64(65535uL);

		public LogicalDevicePidJumpToBoot(ILogicalDevice logicalDevice, PID pid, LogicalDeviceSessionType writeAccess)
			: base(logicalDevice, pid, writeAccess, ValidityCheckJumpToBootRead, ValidityCheckJumpToBootWrite)
		{
		}

		public static bool ValidityCheckJumpToBootRead(ulong value)
		{
			if (value > uint.MaxValue)
			{
				TaggedLog.Error("LogicalDevicePidJumpToBoot", $"Value (0x{value:X}) Read from Jump To Boot too large");
				return false;
			}
			if (!Enum<LogicalDeviceJumpToBootState>.TryConvert(value, out var _))
			{
				TaggedLog.Error("LogicalDevicePidJumpToBoot", string.Format("Value (0x{0:X}) read from Jump To Boot isn't in {1} ", value, "LogicalDeviceJumpToBootState"));
				return false;
			}
			return true;
		}

		public async Task<LogicalDeviceJumpToBootState> ReadJumpToBootStateAsync(CancellationToken cancellationToken)
		{
			return Enum<LogicalDeviceJumpToBootState>.TryConvert(await ReadValueAsync(cancellationToken));
		}

		public static bool ValidityCheckJumpToBootWrite(ulong value)
		{
			if (value > (ulong)UInt48.MaxValue)
			{
				TaggedLog.Error("LogicalDevicePidJumpToBoot", $"Value (0x{value:X}) to write to Jump To Boot tool large");
				return false;
			}
			(LogicalDeviceJumpToBootState, TimeSpan) jumpToBootStateFromRawValue = GetJumpToBootStateFromRawValue((UInt48)value);
			if (jumpToBootStateFromRawValue.Item1 != LogicalDeviceJumpToBootState.RequestBootLoaderWithHoldTime)
			{
				TaggedLog.Error("LogicalDevicePidJumpToBoot", $"Value (0x{value:X}) to write to Jump To Boot is invalid.  Expected {LogicalDeviceJumpToBootState.RequestBootLoaderWithHoldTime} but got {jumpToBootStateFromRawValue.Item1}");
				return false;
			}
			if (jumpToBootStateFromRawValue.Item2 == TimeSpan.Zero)
			{
				TaggedLog.Error("LogicalDevicePidJumpToBoot", string.Format("Value (0x{0:X}) to write to Jump To Boot is invalid. {1} must be > 0", value, "HoldTime"));
				return false;
			}
			return true;
		}

		public static (LogicalDeviceJumpToBootState State, TimeSpan HoldTime) GetJumpToBootStateFromRawValue(UInt48 value)
		{
			uint num = (uint)JumpToBootStateBitPosition.DecodeValue(value);
			if (!Enum<LogicalDeviceJumpToBootState>.TryConvert(num, out var toValue))
			{
				throw new ArgumentOutOfRangeException("value", string.Format("Expected value 0x{0:X} to one of {1}", num, "LogicalDeviceJumpToBootState"));
			}
			TimeSpan timeSpan = TimeSpan.FromMilliseconds((double)(int)(ushort)JumpToBootMsBitPosition.DecodeValue(num) * 10.0);
			return (toValue, timeSpan);
		}

		public Task WriteRequestJumpToBoot(TimeSpan holdTime, CancellationToken cancellationToken)
		{
			ulong mergeValue = JumpToBootStateBitPosition.EncodeValue(1432778632uL, 0uL);
			double num = holdTime.TotalMilliseconds / 10.0;
			mergeValue = JumpToBootMsBitPosition.EncodeValue((ulong)num, mergeValue);
			return WriteValueAsync((UInt48)mergeValue, cancellationToken);
		}
	}
}
