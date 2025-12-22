using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidSimEnum<TValue> : LogicalDevicePidSim, ILogicalDevicePidEnum<TValue>, ILogicalDevicePidProperty<TValue>, ILogicalDevicePid<TValue>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged where TValue : struct, Enum, IConvertible
	{
		public TValue ValueEnum
		{
			get
			{
				return LogicalDevicePidEnum<TValue>.DefaultValueToEnum(base.ValueRaw);
			}
			set
			{
				base.ValueRaw = (UInt48)LogicalDevicePidEnum<TValue>.DefaultEnumToValue(value);
			}
		}

		public LogicalDevicePidSimEnum(PID pid, TValue value)
			: base(pid, LogicalDevicePidEnum<TValue>.DefaultEnumToValue(value))
		{
		}

		public LogicalDevicePidSimEnum(PID pid, TValue value, Action<ulong> onChanged)
			: base(pid, LogicalDevicePidEnum<TValue>.DefaultEnumToValue(value), onChanged)
		{
		}

		public Task<TValue> ReadAsync(CancellationToken cancellationToken)
		{
			return ReadEnumAsync(cancellationToken);
		}

		public Task WriteAsync(TValue value, CancellationToken cancellationToken)
		{
			return WriteEnumAsync(value, cancellationToken);
		}

		public async Task<TValue> ReadEnumAsync(CancellationToken cancellationToken)
		{
			return LogicalDevicePidEnum<TValue>.DefaultValueToEnum(await ReadValueAsync(cancellationToken));
		}

		public Task WriteEnumAsync(TValue value, CancellationToken cancellationToken)
		{
			return WriteValueAsync(LogicalDevicePidEnum<TValue>.DefaultEnumToValue(value), cancellationToken);
		}

		protected override void ValueRawPropertyChanged()
		{
			base.ValueRawPropertyChanged();
			NotifyPropertyChanged("ValueEnum");
		}
	}
}
