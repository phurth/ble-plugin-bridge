using System;
using System.ComponentModel;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface ILogicalDeviceLatchingRelayRemote<TRelayBasicStatus> : ILogicalDeviceLatchingRelay<TRelayBasicStatus>, ILogicalDeviceLatchingRelay, IRelayBasic, ISwitchableDevice, ILogicalDeviceSwitchableReadonly, IDevicesCommon, INotifyPropertyChanged, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, ILogicalDeviceWithStatus<TRelayBasicStatus>, ILogicalDeviceWithStatus, ILogicalDeviceWithStatusUpdate<TRelayBasicStatus>, ILogicalDeviceVoltageMeasurementBatteryPid, ILogicalDeviceVoltageMeasurementBattery, ILogicalDeviceVoltageMeasurement, ILogicalDeviceReadVoltageMeasurement, IReadVoltageMeasurement, ILogicalDeviceSwitchableRemote, ILogicalDeviceSwitchable, ILogicalDeviceRemote where TRelayBasicStatus : ILogicalDeviceRelayBasicStatus, new()
	{
	}
}
