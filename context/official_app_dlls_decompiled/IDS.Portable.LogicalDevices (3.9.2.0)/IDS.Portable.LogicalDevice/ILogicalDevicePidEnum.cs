using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDevicePidEnum<TValue> : ILogicalDevicePidProperty<TValue>, ILogicalDevicePid<TValue>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged where TValue : struct, Enum, IConvertible
	{
		TValue ValueEnum { get; set; }

		Task<TValue> ReadEnumAsync(CancellationToken cancellationToken);

		Task WriteEnumAsync(TValue value, CancellationToken cancellationToken);
	}
}
