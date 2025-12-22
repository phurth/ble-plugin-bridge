using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface ILogicalDeviceRelayHBridgeWithAssociatedDevice : ILogicalDeviceRelayHBridge, IRelayHBridge, IDevicesCommon, INotifyPropertyChanged, IRelayAutoOperations, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, ILogicalDeviceVoltageMeasurementBatteryPid, ILogicalDeviceVoltageMeasurementBattery, ILogicalDeviceVoltageMeasurement, ILogicalDeviceReadVoltageMeasurement, IReadVoltageMeasurement
	{
		Task<LogicalDeviceRelayHBridgeCircuitIdRole> TryGetAssociatedCircuitIdRoleAsync(CancellationToken cancellationToken);

		Task<bool> TrySetAssociatedCircuitIdRoleAsync(LogicalDeviceRelayHBridgeCircuitIdRole circuitRole, CancellationToken cancellationToken);
	}
}
