using System;
using System.Collections.Generic;
using System.ComponentModel;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice.LogicalDevice
{
	public interface ILogicalDeviceWithStatusAlerts : ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged
	{
		IEnumerable<ILogicalDeviceAlert> Alerts { get; }

		void UpdateAlert(string alertName, bool isActive, int? count);

		void UpdateAlert(byte alertId, byte rawData);
	}
}
