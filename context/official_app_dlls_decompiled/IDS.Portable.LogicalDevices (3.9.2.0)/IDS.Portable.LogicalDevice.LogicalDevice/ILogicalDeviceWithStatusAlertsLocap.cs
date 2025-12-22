using System;
using System.ComponentModel;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice.LogicalDevice
{
	public interface ILogicalDeviceWithStatusAlertsLocap : ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged
	{
		bool UpdateDeviceStatusAlerts(byte[] alertData);
	}
}
