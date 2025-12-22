using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidSimTimeSpan : LogicalDevicePidSim, ILogicalDevicePidTimeSpan, ILogicalDevicePidProperty<TimeSpan>, ILogicalDevicePid<TimeSpan>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		public LogicalDevicePidTimeSpanPrecision TimeSpanPrecision { get; }

		public TimeSpan ValueTimeSpan
		{
			get
			{
				return LogicalDevicePidTimeSpan.PidValueToTimeSpan(TimeSpanPrecision, base.ValueRaw);
			}
			set
			{
				base.ValueRaw = (UInt48)LogicalDevicePidTimeSpan.TimeSpanToPidValue(TimeSpanPrecision, value);
			}
		}

		public LogicalDevicePidSimTimeSpan(LogicalDevicePidTimeSpanPrecision timeSpanPrecision, PID pid, ulong value = 0uL)
			: base(pid, value)
		{
			TimeSpanPrecision = timeSpanPrecision;
		}

		public LogicalDevicePidSimTimeSpan(LogicalDevicePidTimeSpanPrecision timeSpanPrecision, PID pid, TimeSpan timeSpan)
			: base(pid, LogicalDevicePidTimeSpan.TimeSpanToPidValue(timeSpanPrecision, timeSpan))
		{
			TimeSpanPrecision = timeSpanPrecision;
		}

		public Task<TimeSpan> ReadAsync(CancellationToken cancellationToken)
		{
			return ReadTimeSpanAsync(cancellationToken);
		}

		public Task WriteAsync(TimeSpan value, CancellationToken cancellationToken)
		{
			return WriteTimeSpanAsync(value, cancellationToken);
		}

		public async Task<TimeSpan> ReadTimeSpanAsync(CancellationToken cancellationToken)
		{
			ulong value = await ReadValueAsync(cancellationToken);
			return LogicalDevicePidTimeSpan.PidValueToTimeSpan(TimeSpanPrecision, value);
		}

		public Task WriteTimeSpanAsync(TimeSpan value, CancellationToken cancellationToken)
		{
			return WriteValueAsync(LogicalDevicePidTimeSpan.TimeSpanToPidValue(TimeSpanPrecision, value), cancellationToken);
		}

		protected override void ValueRawPropertyChanged()
		{
			base.ValueRawPropertyChanged();
			NotifyPropertyChanged("ValueTimeSpan");
		}
	}
}
