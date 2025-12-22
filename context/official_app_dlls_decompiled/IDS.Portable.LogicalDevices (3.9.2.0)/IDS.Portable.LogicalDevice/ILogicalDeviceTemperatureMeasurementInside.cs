using System;
using System.ComponentModel;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceTemperatureMeasurementInside : ILogicalDeviceTemperatureMeasurement, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged
	{
		LogicalDeviceExScope TemperatureMeasurementInsideScope { get; }

		ITemperatureMeasurement TemperatureMeasurementInside { get; }
	}
}
