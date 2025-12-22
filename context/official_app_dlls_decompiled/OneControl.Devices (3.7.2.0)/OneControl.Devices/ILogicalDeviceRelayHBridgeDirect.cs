using System;
using System.ComponentModel;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface ILogicalDeviceRelayHBridgeDirect<TRelayHBridgeStatus> : ILogicalDeviceRelayHBridge<TRelayHBridgeStatus>, ILogicalDeviceRelayHBridge, IRelayHBridge, IDevicesCommon, INotifyPropertyChanged, IRelayAutoOperations, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, ILogicalDeviceVoltageMeasurementBatteryPid, ILogicalDeviceVoltageMeasurementBattery, ILogicalDeviceVoltageMeasurement, ILogicalDeviceReadVoltageMeasurement, IReadVoltageMeasurement, ILogicalDeviceWithStatus<TRelayHBridgeStatus>, ILogicalDeviceWithStatus, ILogicalDeviceWithStatusUpdate<TRelayHBridgeStatus>, ILogicalDeviceIdsCan, ILogicalDeviceMyRvLink where TRelayHBridgeStatus : ILogicalDeviceRelayHBridgeStatus, new()
	{
	}
}
