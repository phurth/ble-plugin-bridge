using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidDateTime : LogicalDevicePid, ILogicalDevicePidDateTime, ILogicalDevicePidProperty<DateTime>, ILogicalDevicePid<DateTime>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		public delegate DateTime ValueToDateTime(ulong value);

		public delegate ulong ValueFromDateTime(DateTime value);

		protected readonly ValueToDateTime ConvertToDateTime;

		protected readonly ValueFromDateTime ConvertFromDateTime;

		public DateTime ValueDateTime
		{
			get
			{
				return ConvertToDateTime(base.ValueRaw);
			}
			set
			{
				base.ValueRaw = (UInt48)ConvertFromDateTime(value);
			}
		}

		public LogicalDevicePidDateTime(ILogicalDevice logicalDevice, PID pid, LogicalDeviceSessionType writeAccess, Func<ulong, bool>? validityCheck = null)
			: this(logicalDevice, pid, writeAccess, null, null, validityCheck)
		{
		}

		public LogicalDevicePidDateTime(ILogicalDevice logicalDevice, PID pid, LogicalDeviceSessionType writeAccess, ValueToDateTime? convertToDateTime, ValueFromDateTime? convertFromDateTime, Func<ulong, bool>? validityCheck = null)
			: base(logicalDevice, pid, writeAccess, validityCheck)
		{
			ConvertToDateTime = convertToDateTime ?? new ValueToDateTime(PidExtension.PidEpoch2000ValueToDatetime);
			ConvertFromDateTime = convertFromDateTime ?? new ValueFromDateTime(PidExtension.PidDateTimeToSecondsSinceEpoch2000);
		}

		public Task<DateTime> ReadAsync(CancellationToken cancellationToken)
		{
			return ReadDateTimeAsync(cancellationToken);
		}

		public Task WriteAsync(DateTime value, CancellationToken cancellationToken)
		{
			return WriteDateTimeAsync(value, cancellationToken);
		}

		public async Task<DateTime> ReadDateTimeAsync(CancellationToken cancellationToken)
		{
			ulong value = await ReadValueAsync(cancellationToken);
			return ConvertToDateTime(value);
		}

		public Task WriteDateTimeAsync(DateTime value, CancellationToken cancellationToken)
		{
			ulong value2 = ConvertFromDateTime(value);
			return WriteValueAsync(value2, cancellationToken);
		}

		protected override void ValueRawPropertyChanged()
		{
			base.ValueRawPropertyChanged();
			OnPropertyChanged("DateTime");
		}
	}
}
