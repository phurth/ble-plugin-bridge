using System;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidAddressSim<TPidAddress> : ILogicalDevicePidAddress, ILogicalDevicePid where TPidAddress : Enum, IConvertible
	{
		private uint _value;

		public int PidReadTimeoutSec { get; set; } = 3;


		public int PidWriteTimeoutSec { get; set; } = 6;


		public PID PropertyId { get; protected set; }

		public TPidAddress PidAddress { get; }

		public bool IsReadOnly { get; }

		public PidAccess PidAccess { get; }

		public virtual IPidDetail PidDetail => PropertyId.ConvertToPid().GetPidDetailDefault();

		public LogicalDevicePidAddressSim(PID pid, TPidAddress pidAddress, uint value = 0u, bool isReadOnly = false)
		{
			PidAccess = PidAccess.Readable;
			PidAddress = pidAddress;
			IsReadOnly = isReadOnly;
			if (!isReadOnly)
			{
				PidAccess |= PidAccess.Writable;
			}
			PropertyId = pid;
			_value = value;
		}

		public Task<ulong> ReadValueAsync(CancellationToken cancellationToken)
		{
			return Task.FromResult((ulong)_value);
		}

		public Task WriteValueAsync(ulong value, CancellationToken cancellationToken)
		{
			if (IsReadOnly)
			{
				throw new LogicalDevicePidValueWriteNotSupportedException(PropertyId);
			}
			_value = (uint)value;
			return Task.CompletedTask;
		}

		public override string ToString()
		{
			return $"PID SIM {PropertyId} [{PidAddress}]";
		}
	}
}
