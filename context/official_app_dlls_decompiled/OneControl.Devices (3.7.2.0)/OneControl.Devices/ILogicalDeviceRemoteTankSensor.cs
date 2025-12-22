using System;
using System.ComponentModel;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using IDS.Portable.LogicalDevice.LogicalDevice;

namespace OneControl.Devices
{
	public interface ILogicalDeviceRemoteTankSensor : ILogicalDeviceTankSensor, ITankSensor, ILogicalDeviceWithStatus<LogicalDeviceTankSensorStatus>, ILogicalDeviceWithStatus, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged, ILogicalDeviceWithStatusUpdate<LogicalDeviceTankSensorStatus>, ILogicalDeviceWithStatusAlerts, ILogicalDeviceWithStatusAlertsLocap, ILogicalDeviceAccessory, IAccessoryDevice, ILogicalDeviceVoltageMeasurementBatteryPid, ILogicalDeviceVoltageMeasurementBattery, ILogicalDeviceVoltageMeasurement, ILogicalDeviceReadVoltageMeasurement, IReadVoltageMeasurement, ILogicalDeviceRemote
	{
	}
}
