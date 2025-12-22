using System;
using System.ComponentModel;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using IDS.Portable.LogicalDevice.FirmwareUpdate;
using OneControl.Devices.Interfaces;

namespace OneControl.Devices
{
	public interface ILogicalDeviceMonitorPanel : IMonitorPanel, IDevicesCommon, INotifyPropertyChanged, ILogicalDeviceWithCapability<ILogicalDeviceMonitorPanelCapability>, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, ILogicalDeviceWithStatus<LogicalDeviceMonitorPanelStatus>, ILogicalDeviceWithStatus, ILogicalDeviceWithStatusUpdate<LogicalDeviceMonitorPanelStatus>, IHighResolutionTankSupport, ILogicalDeviceFirmwareUpdateDevice
	{
	}
}
