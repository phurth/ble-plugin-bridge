using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDevicePidFixedPoint : ILogicalDevicePidProperty<float>, ILogicalDevicePid<float>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		float ValueFloat { get; set; }

		Task<float> ReadFloatAsync(CancellationToken cancellationToken);

		Task WriteFloatAsync(float value, CancellationToken cancellationToken);
	}
}
