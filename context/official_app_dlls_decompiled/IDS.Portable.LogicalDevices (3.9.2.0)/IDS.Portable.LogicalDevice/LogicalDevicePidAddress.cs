using System;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidAddress<TValue, TPidAddress> : LogicalDevicePid, ILogicalDevicePidAddress<TValue, TPidAddress>, ILogicalDevicePidAddressValue<TValue>, ILogicalDevicePidAddress, ILogicalDevicePid, ILogicalDevicePid<TValue>, ILogicalDevicePidAddress<TPidAddress> where TPidAddress : Enum, IConvertible
	{
		private const string LogTag = "LogicalDevicePidAddress";

		private readonly Func<TValue, uint>? _writeValueConverter;

		private readonly Func<uint, TValue>? _readValueConverter;

		public TPidAddress PidAddress { get; }

		public PidAccess PidAccess { get; }

		private LogicalDevicePidAddress(ILogicalDevice logicalDevice, PID pid, TPidAddress pidAddress, LogicalDeviceSessionType writeAccess, Func<ulong, bool>? validityCheck = null)
			: base(logicalDevice, pid, (ushort)Convert.ChangeType(pidAddress, TypeCode.UInt16), writeAccess, validityCheck)
		{
			PidAddress = pidAddress;
			PidAccess = PidAccess.Unknown;
		}

		public LogicalDevicePidAddress(ILogicalDevice logicalDevice, PID pid, TPidAddress pidAddress, LogicalDeviceSessionType writeAccess, Func<TValue, uint> writeValueConverter, Func<uint, TValue> readValueConverter, Func<ulong, bool>? validityCheck = null)
			: this(logicalDevice, pid, pidAddress, writeAccess, writeValueConverter, validityCheck)
		{
			_readValueConverter = readValueConverter ?? throw new LogicalDevicePidException(pid, base.RawPidAddress, logicalDevice, "Read Value Converter is NULL");
			PidAccess |= PidAccess.Readable;
		}

		public LogicalDevicePidAddress(ILogicalDevice logicalDevice, PID pid, TPidAddress pidAddress, LogicalDeviceSessionType writeAccess, Func<TValue, uint> writeValueConverter, Func<ulong, bool>? validityCheck = null)
			: this(logicalDevice, pid, pidAddress, writeAccess, validityCheck)
		{
			_writeValueConverter = writeValueConverter ?? throw new LogicalDevicePidException(pid, base.RawPidAddress, logicalDevice, "Read Value Converter is NULL");
			PidAccess |= PidAccess.Writable;
			if (writeAccess == LogicalDeviceSessionType.None)
			{
				throw new LogicalDevicePidException(pid, base.RawPidAddress, logicalDevice, $"Write session can't be {writeAccess} for a writable PID");
			}
		}

		public LogicalDevicePidAddress(ILogicalDevice logicalDevice, PID pid, TPidAddress pidAddress, Func<uint, TValue> readValueConverter, Func<ulong, bool>? validityCheck = null)
			: this(logicalDevice, pid, pidAddress, LogicalDeviceSessionType.None, validityCheck)
		{
			_readValueConverter = readValueConverter ?? throw new LogicalDevicePidException(pid, base.RawPidAddress, logicalDevice, "Read Value Converter is NULL");
			PidAccess |= PidAccess.Readable;
		}

		public async Task<TValue> ReadAsync(CancellationToken cancellationToken)
		{
			uint arg = (uint)(await ReadValueAsync(cancellationToken));
			if (_readValueConverter == null)
			{
				throw new LogicalDevicePidException(base.PropertyId, base.RawPidAddress, LogicalDevice, "Read Value Converter is NULL");
			}
			return _readValueConverter!(arg);
		}

		public async Task WriteAsync(TValue value, CancellationToken cancellationToken)
		{
			uint num = (_writeValueConverter ?? throw new LogicalDevicePidException(base.PropertyId, base.RawPidAddress, LogicalDevice, "Write Value Converter is NULL"))!(value);
			await base.WriteValueAsync(num, cancellationToken);
		}

		public override Task<ulong> ReadValueImplAsync(CancellationToken cancellationToken)
		{
			if (!PidAccess.HasFlag(PidAccess.Readable))
			{
				throw new LogicalDevicePidValueReadNotSupportedException(base.PropertyId, base.RawPidAddress, LogicalDevice);
			}
			return base.ReadValueImplAsync(cancellationToken);
		}

		public override Task WriteValueAsync(ulong value, CancellationToken cancellationToken)
		{
			if (!PidAccess.HasFlag(PidAccess.Writable))
			{
				throw new LogicalDevicePidValueWriteNotSupportedException(base.PropertyId, base.RawPidAddress, LogicalDevice);
			}
			return base.WriteValueAsync(value, cancellationToken);
		}
	}
}
