using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidEnum<TValue> : LogicalDevicePid, ILogicalDevicePidEnum<TValue>, ILogicalDevicePidProperty<TValue>, ILogicalDevicePid<TValue>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged where TValue : struct, Enum, IConvertible
	{
		public delegate TValue ValueToEnum(ulong value);

		public delegate ulong ValueFromEnum(TValue value);

		protected readonly ValueToEnum ConvertToEnum;

		protected readonly ValueFromEnum ConvertFromEnum;

		public TValue ValueEnum
		{
			get
			{
				return ConvertToEnum(base.ValueRaw);
			}
			set
			{
				base.ValueRaw = (UInt48)ConvertFromEnum(value);
			}
		}

		public LogicalDevicePidEnum(ILogicalDevice logicalDevice, PID pid, LogicalDeviceSessionType writeAccess)
			: this(logicalDevice, pid, writeAccess, (ValueToEnum?)null, (ValueFromEnum?)null, (Func<ulong, bool>?)null)
		{
		}

		public LogicalDevicePidEnum(ILogicalDevice logicalDevice, PID pid, LogicalDeviceSessionType writeAccess, ValueToEnum? convertToEnum, ValueFromEnum? convertFromEnum, Func<ulong, bool>? validityCheck = null)
			: base(logicalDevice, pid, writeAccess, validityCheck)
		{
			ConvertToEnum = convertToEnum ?? new ValueToEnum(DefaultValueToEnum);
			ConvertFromEnum = convertFromEnum ?? new ValueFromEnum(DefaultEnumToValue);
		}

		public static TValue DefaultValueToEnum(ulong value)
		{
			return Enum<TValue>.TryConvert(value);
		}

		public static ulong DefaultEnumToValue(TValue value)
		{
			return Convert.ToUInt64(value);
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
			ulong value = await ReadValueAsync(cancellationToken);
			return ConvertToEnum(value);
		}

		public Task WriteEnumAsync(TValue value, CancellationToken cancellationToken)
		{
			ulong value2 = ConvertFromEnum(value);
			return WriteValueAsync(value2, cancellationToken);
		}

		protected override void ValueRawPropertyChanged()
		{
			base.ValueRawPropertyChanged();
			OnPropertyChanged("ValueEnum");
		}
	}
}
