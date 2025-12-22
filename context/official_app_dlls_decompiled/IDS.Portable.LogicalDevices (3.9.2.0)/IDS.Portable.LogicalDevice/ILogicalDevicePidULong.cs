using System;
using System.ComponentModel;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDevicePidULong : ILogicalDevicePidProperty<ulong>, ILogicalDevicePid<ulong>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		ulong ValueULong { get; set; }
	}
}
