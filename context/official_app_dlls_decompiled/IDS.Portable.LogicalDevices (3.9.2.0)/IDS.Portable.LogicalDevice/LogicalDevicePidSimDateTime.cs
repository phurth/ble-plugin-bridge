using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidSimDateTime : LogicalDevicePidSimOfType<DateTime>, ILogicalDevicePidDateTime, ILogicalDevicePidProperty<DateTime>, ILogicalDevicePid<DateTime>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		public DateTime ValueDateTime
		{
			get
			{
				return ReadConverter(base.ValueRaw);
			}
			set
			{
				base.ValueRaw = WriteConverter(value);
			}
		}

		public LogicalDevicePidSimDateTime(PID pid, DateTime value)
			: base(pid, (Func<UInt48, DateTime>)ReadConverter, (Func<DateTime, UInt48>)WriteConverter, value)
		{
		}

		public LogicalDevicePidSimDateTime(PID pid, UInt48 rawValue)
			: base(pid, rawValue, (Func<UInt48, DateTime>)ReadConverter, (Func<DateTime, UInt48>)WriteConverter)
		{
		}

		public static DateTime ReadConverter(UInt48 rawValue)
		{
			return PidExtension.PidEpoch2000ValueToDatetime(rawValue);
		}

		public static UInt48 WriteConverter(DateTime value)
		{
			return (UInt48)PidExtension.PidDateTimeToSecondsSinceEpoch2000(value);
		}

		public Task<DateTime> ReadDateTimeAsync(CancellationToken cancellationToken)
		{
			return ReadAsync(cancellationToken);
		}

		public Task WriteDateTimeAsync(DateTime value, CancellationToken cancellationToken)
		{
			return WriteAsync(value, cancellationToken);
		}

		protected override void ValueRawPropertyChanged()
		{
			base.ValueRawPropertyChanged();
			NotifyPropertyChanged("ValueDateTime");
		}
	}
}
