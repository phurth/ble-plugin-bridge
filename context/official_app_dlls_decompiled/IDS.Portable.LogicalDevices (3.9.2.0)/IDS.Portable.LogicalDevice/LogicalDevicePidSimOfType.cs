using System;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidSimOfType<TValue> : LogicalDevicePidSim, ILogicalDevicePid<TValue>, ILogicalDevicePid
	{
		private readonly Func<UInt48, TValue> _readValueConverter;

		private readonly Func<TValue, UInt48>? _writeValueConverter;

		public LogicalDevicePidSimOfType(PID pid, Func<UInt48, TValue> readValueConverter, Func<TValue, UInt48> writeValueConverter, TValue value)
			: base(pid, writeValueConverter(value))
		{
			_readValueConverter = readValueConverter;
			_writeValueConverter = writeValueConverter;
		}

		public LogicalDevicePidSimOfType(PID pid, UInt48 value, Func<UInt48, TValue> readValueConverter, Func<TValue, UInt48> writeValueConverter)
			: base(pid, value)
		{
			_readValueConverter = readValueConverter;
			_writeValueConverter = writeValueConverter;
		}

		public LogicalDevicePidSimOfType(PID pid, UInt48 value, Func<UInt48, TValue> readValueConverter)
			: base(pid, value, isReadOnly: true)
		{
			_readValueConverter = readValueConverter;
			_writeValueConverter = null;
		}

		public async Task<TValue> ReadAsync(CancellationToken cancellationToken)
		{
			ulong num = await ReadValueAsync(cancellationToken);
			return _readValueConverter((UInt48)num);
		}

		public Task WriteAsync(TValue value, CancellationToken cancellationToken)
		{
			UInt48 uInt = (_writeValueConverter ?? throw new LogicalDevicePidValueWriteNotSupportedException(base.PropertyId))!(value);
			return WriteValueAsync(uInt, cancellationToken);
		}
	}
}
