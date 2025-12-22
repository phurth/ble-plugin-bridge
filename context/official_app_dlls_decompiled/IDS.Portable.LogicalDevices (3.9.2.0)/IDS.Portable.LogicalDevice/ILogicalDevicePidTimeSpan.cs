using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDevicePidTimeSpan : ILogicalDevicePidProperty<TimeSpan>, ILogicalDevicePid<TimeSpan>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		TimeSpan ValueTimeSpan { get; set; }

		Task<TimeSpan> ReadTimeSpanAsync(CancellationToken cancellationToken);

		Task WriteTimeSpanAsync(TimeSpan value, CancellationToken cancellationToken);
	}
}
