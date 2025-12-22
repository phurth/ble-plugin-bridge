using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDevicePidBool : ILogicalDevicePidProperty<bool>, ILogicalDevicePid<bool>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		bool ValueBool { get; set; }

		Task<bool> ReadBoolAsync(CancellationToken cancellationToken);

		Task WriteBoolAsync(bool value, CancellationToken cancellationToken);
	}
}
