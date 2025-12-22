using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDevicePidDateTime : ILogicalDevicePidProperty<DateTime>, ILogicalDevicePid<DateTime>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		DateTime ValueDateTime { get; set; }

		Task<DateTime> ReadDateTimeAsync(CancellationToken cancellationToken);

		Task WriteDateTimeAsync(DateTime value, CancellationToken cancellationToken);
	}
}
