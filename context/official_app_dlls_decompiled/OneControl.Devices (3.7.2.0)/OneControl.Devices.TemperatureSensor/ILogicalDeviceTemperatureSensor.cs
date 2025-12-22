using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using IDS.Portable.LogicalDevice.LogicalDevice;

namespace OneControl.Devices.TemperatureSensor
{
	public interface ILogicalDeviceTemperatureSensor : ITemperatureSensor, IAccessoryDevice, ILogicalDeviceWithStatus<LogicalDeviceTemperatureSensorStatus>, ILogicalDeviceWithStatus, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged, ILogicalDeviceWithStatusAlertsLocap, ILogicalDeviceAccessory
	{
		Task RequestTransitionToSleepMode(CancellationToken cancellationToken);
	}
}
