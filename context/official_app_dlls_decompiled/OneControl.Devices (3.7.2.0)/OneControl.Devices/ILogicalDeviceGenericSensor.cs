using System;
using System.ComponentModel;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.GenericSensor;

namespace OneControl.Devices
{
	public interface ILogicalDeviceGenericSensor : ILogicalDeviceWithStatus<LogicalDeviceGenericSensorStatus>, ILogicalDeviceWithStatus, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged, ILogicalDeviceWithStatusUpdate<LogicalDeviceGenericSensorStatus>, ILogicalDeviceIdsCan, ILogicalDeviceMyRvLink
	{
		void UpdateAlert(byte[] alertData);
	}
}
