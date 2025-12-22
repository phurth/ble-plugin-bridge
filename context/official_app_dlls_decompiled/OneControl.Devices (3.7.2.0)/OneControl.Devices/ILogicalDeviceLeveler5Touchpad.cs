using System;
using System.ComponentModel;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using IDS.Portable.LogicalDevice.FirmwareUpdate;

namespace OneControl.Devices
{
	public interface ILogicalDeviceLeveler5Touchpad : ILogicalDeviceWithStatus<LogicalDeviceLeveler5TouchpadStatus>, ILogicalDeviceWithStatus, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged, ILogicalDeviceWithStatusUpdate<LogicalDeviceLeveler5TouchpadStatus>, ILogicalDeviceIdsCan, ILogicalDeviceMyRvLink, ILogicalDeviceFirmwareUpdateDevice
	{
	}
}
