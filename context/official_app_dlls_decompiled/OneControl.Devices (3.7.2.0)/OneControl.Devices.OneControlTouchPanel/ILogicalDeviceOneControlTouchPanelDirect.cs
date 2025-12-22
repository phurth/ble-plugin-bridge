using System;
using System.ComponentModel;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.Interfaces;

namespace OneControl.Devices.OneControlTouchPanel
{
	public interface ILogicalDeviceOneControlTouchPanelDirect : ILogicalDeviceOneControlTouchPanel, IOneControlTouchPanel, ILogicalDeviceWithCapability<ILogicalDeviceOneControlTouchPanelCapability>, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged, ILogicalDeviceWithStatus<LogicalDeviceOneControlTouchPanelStatus>, ILogicalDeviceWithStatus, ILogicalDeviceWithStatusUpdate<LogicalDeviceOneControlTouchPanelStatus>, IHighResolutionTankSupport, ILogicalDeviceIdsCan, ILogicalDeviceMyRvLink
	{
	}
}
