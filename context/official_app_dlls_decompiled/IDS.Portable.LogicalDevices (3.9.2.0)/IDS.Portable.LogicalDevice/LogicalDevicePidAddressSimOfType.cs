using System;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidAddressSimOfType<TValue, TPidAddress> : LogicalDevicePidAddressSim<TPidAddress>, ILogicalDevicePidAddress<TValue, TPidAddress>, ILogicalDevicePidAddressValue<TValue>, ILogicalDevicePidAddress, ILogicalDevicePid, ILogicalDevicePid<TValue>, ILogicalDevicePidAddress<TPidAddress> where TPidAddress : Enum, IConvertible
	{
		private readonly Func<uint, TValue> _readValueConverter;

		private readonly Func<TValue, uint>? _writeValueConverter;

		public LogicalDevicePidAddressSimOfType(PID pid, TPidAddress pidAddress, TValue value, Func<uint, TValue> readValueConverter, Func<TValue, uint> writeValueConverter)
			: base(pid, pidAddress, writeValueConverter(value), isReadOnly: false)
		{
			_readValueConverter = readValueConverter;
			_writeValueConverter = writeValueConverter;
		}

		public LogicalDevicePidAddressSimOfType(PID pid, TPidAddress pidAddress, ushort rawPidAddress, uint value, Func<uint, TValue> readValueConverter)
			: base(pid, pidAddress, value, isReadOnly: true)
		{
			_readValueConverter = readValueConverter;
			_writeValueConverter = null;
		}

		public async Task<TValue> ReadAsync(CancellationToken cancellationToken)
		{
			ulong num = await ReadValueAsync(cancellationToken);
			return _readValueConverter((uint)num);
		}

		public Task WriteAsync(TValue value, CancellationToken cancellationToken)
		{
			uint num = (_writeValueConverter ?? throw new LogicalDevicePidValueWriteNotSupportedException(base.PropertyId))!(value);
			return WriteValueAsync(num, cancellationToken);
		}
	}
}
