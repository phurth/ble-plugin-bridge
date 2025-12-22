using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidTimeSpan : LogicalDevicePid, ILogicalDevicePidTimeSpan, ILogicalDevicePidProperty<TimeSpan>, ILogicalDevicePid<TimeSpan>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		private const string LogTag = "LogicalDevicePidTimeSpan";

		public const int NumMinutesInDay = 1439;

		public LogicalDevicePidTimeSpanPrecision TimeSpanPrecision { get; }

		public TimeSpan ValueTimeSpan
		{
			get
			{
				return PidValueToTimeSpan(TimeSpanPrecision, base.ValueRaw);
			}
			set
			{
				base.ValueRaw = (UInt48)TimeSpanToPidValue(TimeSpanPrecision, value);
			}
		}

		public static TimeSpan PidValueToTimeSpan(LogicalDevicePidTimeSpanPrecision timeSpanPrecision, ulong value)
		{
			return timeSpanPrecision switch
			{
				LogicalDevicePidTimeSpanPrecision.OneDayOfMinutes => new TimeSpan(0, 0, MathCommon.Clamp((int)value, 0, 1439), 0), 
				LogicalDevicePidTimeSpanPrecision.UInt16Minutes => new TimeSpan(0, 0, MathCommon.Clamp((int)value, 0, 65535), 0), 
				LogicalDevicePidTimeSpanPrecision.UInt32Seconds => TimeSpan.FromSeconds(MathCommon.Clamp((uint)value, 0u, uint.MaxValue)), 
				_ => throw new ArgumentException("Unknown LogicalDevicePidTimeSpanPrecision"), 
			};
		}

		public static ulong TimeSpanToPidValue(LogicalDevicePidTimeSpanPrecision timeSpanPrecision, TimeSpan timeSpan)
		{
			switch (timeSpanPrecision)
			{
			case LogicalDevicePidTimeSpanPrecision.OneDayOfMinutes:
				if (timeSpan.Days > 0)
				{
					TaggedLog.Debug("LogicalDevicePidTimeSpan", $"TimeSpan conversion, Ignoring days {timeSpan.Days}");
				}
				if (timeSpan.Seconds != 0)
				{
					TaggedLog.Debug("LogicalDevicePidTimeSpan", $"TimeSpan conversion, Ignoring seconds {timeSpan.Seconds}");
				}
				return (uint)(timeSpan.Hours * 60 + timeSpan.Minutes);
			case LogicalDevicePidTimeSpanPrecision.UInt16Minutes:
				if (timeSpan.Seconds != 0)
				{
					TaggedLog.Debug("LogicalDevicePidTimeSpan", $"TimeSpan conversion, Ignoring seconds {timeSpan.Seconds}");
				}
				return MathCommon.Clamp((uint)(timeSpan.Days * 24 * 60 + timeSpan.Hours * 60 + timeSpan.Minutes), 0u, 65535u);
			case LogicalDevicePidTimeSpanPrecision.UInt32Seconds:
				return MathCommon.Clamp((uint)((int)((long)(uint)(timeSpan.Days * 24 * 60 + timeSpan.Hours * 60 + timeSpan.Minutes) * 60L) + timeSpan.Seconds), 0u, uint.MaxValue);
			default:
				throw new ArgumentException("Unknown LogicalDevicePidTimeSpanPrecision");
			}
		}

		public LogicalDevicePidTimeSpan(LogicalDevicePidTimeSpanPrecision timeSpanPrecision, ILogicalDevice logicalDevice, PID pid, LogicalDeviceSessionType writeAccess, Func<ulong, bool>? validityCheck = null)
			: base(logicalDevice, pid, writeAccess, validityCheck)
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
			return PidValueToTimeSpan(TimeSpanPrecision, value);
		}

		public Task WriteTimeSpanAsync(TimeSpan value, CancellationToken cancellationToken)
		{
			return WriteValueAsync(TimeSpanToPidValue(TimeSpanPrecision, value), cancellationToken);
		}

		protected override void ValueRawPropertyChanged()
		{
			base.ValueRawPropertyChanged();
			OnPropertyChanged("ValueTimeSpan");
		}
	}
}
