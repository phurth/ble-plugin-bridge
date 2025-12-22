using System;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidSimJumpToBoot : LogicalDevicePidSim, ILogicalDevicePidJumpToBoot, ILogicalDevicePid
	{
		public LogicalDevicePidSimJumpToBoot(PID pid, ulong value)
			: base(pid, value)
		{
		}

		protected async Task<UInt48> ReadAsync(CancellationToken cancellationToken)
		{
			ulong num = await ReadValueAsync(cancellationToken);
			if (!LogicalDevicePidJumpToBoot.ValidityCheckJumpToBootRead(num))
			{
				throw new LogicalDevicePidInvalidValueException(base.PropertyId, num);
			}
			return (UInt48)num;
		}

		protected Task WriteAsync(UInt48 value, CancellationToken cancellationToken)
		{
			if (!LogicalDevicePidJumpToBoot.ValidityCheckJumpToBootWrite(value))
			{
				throw new LogicalDevicePidInvalidValueException(base.PropertyId, value);
			}
			return WriteValueAsync(value, cancellationToken);
		}

		public async Task<LogicalDeviceJumpToBootState> ReadJumpToBootStateAsync(CancellationToken cancellationToken)
		{
			return Enum<LogicalDeviceJumpToBootState>.TryConvert(await ReadValueAsync(cancellationToken));
		}

		public Task WriteRequestJumpToBoot(TimeSpan holdTime, CancellationToken cancellationToken)
		{
			ulong mergeValue = LogicalDevicePidJumpToBoot.JumpToBootStateBitPosition.EncodeValue(1432778632uL, 0uL);
			mergeValue = LogicalDevicePidJumpToBoot.JumpToBootMsBitPosition.EncodeValue((ulong)holdTime.TotalMilliseconds, mergeValue);
			return WriteValueAsync((UInt48)mergeValue, cancellationToken);
		}
	}
}
