using System;
using System.ComponentModel;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceServiceIdsCan : ILogicalDeviceService, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
	}
}
