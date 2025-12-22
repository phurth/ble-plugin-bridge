using System;
using System.ComponentModel;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceVoltageMeasurementBatteryPid : ILogicalDeviceVoltageMeasurementBattery, ILogicalDeviceVoltageMeasurement, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged, ILogicalDeviceReadVoltageMeasurement, IReadVoltageMeasurement
	{
		LogicalDeviceExScope VoltageMeasurementBatteryPidScope { get; }

		ILogicalDevicePidFixedPoint VoltageMeasurementBatteryPid { get; }

		bool IsVoltagePidReadSupported { get; }
	}
}
