using System;
using System.ComponentModel;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface ILogicalDeviceHourMeterRemote : ILogicalDeviceHourMeter, IHourMeter, IDevicesCommon, INotifyPropertyChanged, ILogicalDeviceWithStatus<LogicalDeviceHourMeterStatus>, ILogicalDeviceWithStatus, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, ILogicalDeviceWithStatusUpdate<LogicalDeviceHourMeterStatus>, ILogicalDeviceRemote
	{
	}
}
