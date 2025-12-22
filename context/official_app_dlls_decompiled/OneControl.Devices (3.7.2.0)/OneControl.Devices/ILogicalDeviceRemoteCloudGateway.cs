using System;
using System.ComponentModel;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface ILogicalDeviceRemoteCloudGateway : ILogicalDeviceCloudGateway, ICloudGateway, ILogicalDeviceWithStatus<LogicalDeviceCloudGatewayStatus>, ILogicalDeviceWithStatus, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged, ILogicalDeviceWithStatusUpdate<LogicalDeviceCloudGatewayStatus>, ILogicalDeviceMyRvLink, ILogicalDeviceIdsCan, ILogicalDeviceRemote
	{
	}
}
