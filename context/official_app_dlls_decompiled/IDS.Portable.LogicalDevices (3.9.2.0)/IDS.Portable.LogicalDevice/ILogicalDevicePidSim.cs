using System;
using System.ComponentModel;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDevicePidSim : ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
	}
}
