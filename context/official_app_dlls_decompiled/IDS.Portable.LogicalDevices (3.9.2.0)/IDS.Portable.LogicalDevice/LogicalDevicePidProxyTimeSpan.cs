using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidProxyTimeSpan : LogicalDevicePidProxy<ILogicalDevicePidTimeSpan>, ILogicalDevicePidTimeSpan, ILogicalDevicePidProperty<TimeSpan>, ILogicalDevicePid<TimeSpan>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		public TimeSpan ValueTimeSpan { get; set; }

		public LogicalDevicePidProxyTimeSpan(ILogicalDevicePidTimeSpan? devicePid = null)
			: base(devicePid)
		{
		}

		public Task<TimeSpan> ReadTimeSpanAsync(CancellationToken cancellationToken)
		{
			ILogicalDevicePidTimeSpan? devicePid = base.DevicePid;
			if (devicePid == null || base.IsDisposed)
			{
				throw new PhysicalDeviceNotFoundException("LogicalDevicePidProxyTimeSpan", "ReadTimeSpanAsync DevicePid not setup for proxy.");
			}
			return devicePid!.ReadTimeSpanAsync(cancellationToken);
		}

		public Task WriteTimeSpanAsync(TimeSpan value, CancellationToken cancellationToken)
		{
			ILogicalDevicePidTimeSpan? devicePid = base.DevicePid;
			if (devicePid == null || base.IsDisposed)
			{
				throw new PhysicalDeviceNotFoundException("LogicalDevicePidProxyTimeSpan", "WriteTimeSpanAsync, DevicePid not setup for proxy.");
			}
			return devicePid!.WriteTimeSpanAsync(value, cancellationToken);
		}

		public Task<TimeSpan> ReadAsync(CancellationToken cancellationToken)
		{
			return ReadTimeSpanAsync(cancellationToken);
		}

		public Task WriteAsync(TimeSpan value, CancellationToken cancellationToken)
		{
			return WriteTimeSpanAsync(value, cancellationToken);
		}
	}
}
