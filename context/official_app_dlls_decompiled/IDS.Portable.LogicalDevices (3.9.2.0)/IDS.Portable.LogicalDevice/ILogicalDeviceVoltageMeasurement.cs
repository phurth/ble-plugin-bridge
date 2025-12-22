using System;
using System.ComponentModel;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceVoltageMeasurement : ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged
	{
	}
}
