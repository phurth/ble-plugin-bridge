using System;
using System.ComponentModel;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface ILogicalDeviceRelayHBridgeWithAssociatedLight : ILogicalDeviceRelayHBridge, IRelayHBridge, IDevicesCommon, INotifyPropertyChanged, IRelayAutoOperations, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, ILogicalDeviceVoltageMeasurementBatteryPid, ILogicalDeviceVoltageMeasurementBattery, ILogicalDeviceVoltageMeasurement, ILogicalDeviceReadVoltageMeasurement, IReadVoltageMeasurement
	{
		ILogicalDeviceLight GetAssociatedLight();

		DeviceAssociation GetLightAssociation(ILogicalDeviceLight light);
	}
}
