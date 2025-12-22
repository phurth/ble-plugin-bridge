using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDevicePidText : ILogicalDevicePidProperty<string>, ILogicalDevicePid<string>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		string ValueText { get; set; }

		Task<string> ReadTextAsync(CancellationToken cancellationToken);

		Task WriteTextAsync(string value, CancellationToken cancellationToken);
	}
}
