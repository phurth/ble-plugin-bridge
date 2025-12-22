using System;
using System.ComponentModel;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.Interfaces;

namespace OneControl.Devices
{
	public interface ILogicalDeviceGeneratorGenie : ILogicalDeviceGenerator, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged, IGenerator, IHourMeter, ILogicalDeviceWithCapability<ILogicalDeviceGeneratorGenieCapability>, ILogicalDeviceWithStatus<LogicalDeviceGeneratorGenieStatus>, ILogicalDeviceWithStatus, ILogicalDeviceWithStatusUpdate<LogicalDeviceGeneratorGenieStatus>, ILogicalDeviceCommandable<AutoStartLowVoltageMode>, ILogicalDeviceCommandable, ICommandable<AutoStartLowVoltageMode>, ILogicalDeviceCommandable<AutoStartDurationMode>, ICommandable<AutoStartDurationMode>, ILogicalDeviceCommandable<AutoStartOffTimeMode>, ICommandable<AutoStartOffTimeMode>, ILogicalDeviceSwitchable, ISwitchableDevice, ILogicalDeviceSwitchableReadonly
	{
	}
}
