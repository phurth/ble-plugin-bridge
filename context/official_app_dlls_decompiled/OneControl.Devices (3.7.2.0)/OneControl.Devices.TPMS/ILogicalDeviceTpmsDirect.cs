using System;
using System.ComponentModel;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using IDS.Portable.LogicalDevice.LogicalDevice;

namespace OneControl.Devices.TPMS
{
	public interface ILogicalDeviceTpmsDirect : ILogicalDeviceTpms, ITpms, ILogicalDeviceWithStatus<LogicalDeviceTpmsStatus>, ILogicalDeviceWithStatus, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged, ILogicalDeviceWithStatusUpdate<LogicalDeviceTpmsStatus>, ILogicalDeviceWithStatusExtendedMultiplexed<LogicalDeviceTpmsStatusExtended, TpmsPositionalSensorId>, ILogicalDeviceWithStatusExtendedMultiplexed, ILogicalDeviceWithStatusExtended, ILogicalDeviceWithStatusAlertsLocap, ILogicalDeviceWithCapability<LogicalDeviceTpmsCapability>, ILogicalDeviceAccessory, IAccessoryDevice, ILogicalDeviceIdsCan, ILogicalDeviceMyRvLink
	{
	}
}
