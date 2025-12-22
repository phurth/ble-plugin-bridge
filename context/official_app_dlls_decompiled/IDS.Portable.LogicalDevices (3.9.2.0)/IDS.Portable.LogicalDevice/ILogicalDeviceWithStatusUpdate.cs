using System;
using System.ComponentModel;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceWithStatusUpdate<in TDeviceStatus> : ILogicalDeviceWithStatus, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged where TDeviceStatus : IDeviceDataPacketMutable
	{
		bool UpdateDeviceStatus(TDeviceStatus status);
	}
}
