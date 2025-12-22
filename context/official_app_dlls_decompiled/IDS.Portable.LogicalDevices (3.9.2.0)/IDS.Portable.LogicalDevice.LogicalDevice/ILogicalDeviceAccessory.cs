using System;
using System.ComponentModel;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice.LogicalDevice
{
	public interface ILogicalDeviceAccessory : ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged, IAccessoryDevice
	{
		bool AllowAutoOfflineLogicalDeviceRemoval { get; }
	}
}
