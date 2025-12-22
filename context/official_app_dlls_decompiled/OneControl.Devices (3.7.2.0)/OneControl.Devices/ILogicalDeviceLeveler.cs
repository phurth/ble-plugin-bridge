using System;
using System.ComponentModel;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface ILogicalDeviceLeveler : ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged, IDevicesActivation, ILogicalDeviceVoltageMeasurementBatteryPid, ILogicalDeviceVoltageMeasurementBattery, ILogicalDeviceVoltageMeasurement, ILogicalDeviceReadVoltageMeasurement, IReadVoltageMeasurement
	{
	}
}
