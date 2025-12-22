using System;
using System.ComponentModel;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface ILogicalDeviceDirectLevelerType4 : ILogicalDeviceLevelerType4, ILogicalDeviceLeveler, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged, IDevicesActivation, ILogicalDeviceVoltageMeasurementBatteryPid, ILogicalDeviceVoltageMeasurementBattery, ILogicalDeviceVoltageMeasurement, ILogicalDeviceReadVoltageMeasurement, IReadVoltageMeasurement, ILogicalDeviceWithStatus<LogicalDeviceLevelerStatusType4>, ILogicalDeviceWithStatus, ILogicalDeviceWithCapability<LogicalDeviceLevelerCapabilityType4>, ITextConsole, ILogicalDeviceIdsCan, ILogicalDeviceMyRvLink
	{
	}
}
