using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDevicePidMap<TValue> : ILogicalDevicePidProperty<TValue>, ILogicalDevicePid<TValue>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		TValue ValueMap { get; set; }

		Task<TValue> ReadMapAsync(CancellationToken cancellationToken);

		Task WriteMapAsync(TValue value, CancellationToken cancellationToken);
	}
}
