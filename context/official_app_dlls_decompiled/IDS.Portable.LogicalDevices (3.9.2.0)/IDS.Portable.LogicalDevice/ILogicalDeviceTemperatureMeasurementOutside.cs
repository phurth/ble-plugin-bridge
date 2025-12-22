using System;
using System.ComponentModel;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceTemperatureMeasurementOutside : ILogicalDeviceTemperatureMeasurement, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged
	{
		LogicalDeviceExScope TemperatureMeasurementOutsideScope { get; }

		ITemperatureMeasurement TemperatureMeasurementOutside { get; }
	}
}
