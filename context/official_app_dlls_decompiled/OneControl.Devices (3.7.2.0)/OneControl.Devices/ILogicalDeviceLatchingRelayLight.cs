using System;
using System.ComponentModel;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface ILogicalDeviceLatchingRelayLight : ILogicalDeviceSwitchableLight, ILogicalDeviceSwitchable, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged, ISwitchableDevice, ILogicalDeviceSwitchableReadonly, ILogicalDeviceLight, ILogicalDeviceWithStatus, ILogicalDeviceLatchingRelay, IRelayBasic
	{
	}
}
