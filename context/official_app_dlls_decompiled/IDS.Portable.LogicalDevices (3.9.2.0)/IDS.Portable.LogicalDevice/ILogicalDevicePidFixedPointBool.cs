using System;
using System.ComponentModel;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDevicePidFixedPointBool : ILogicalDevicePidFixedPoint, ILogicalDevicePidProperty<float>, ILogicalDevicePid<float>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged, ILogicalDevicePidBool, ILogicalDevicePidProperty<bool>, ILogicalDevicePid<bool>
	{
	}
}
