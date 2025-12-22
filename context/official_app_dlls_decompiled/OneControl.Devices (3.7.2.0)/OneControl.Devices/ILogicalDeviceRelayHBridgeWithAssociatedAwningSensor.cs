using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.AwningSensor;

namespace OneControl.Devices
{
	public interface ILogicalDeviceRelayHBridgeWithAssociatedAwningSensor : ILogicalDeviceRelayHBridgeWithAssociatedDevice, ILogicalDeviceRelayHBridge, IRelayHBridge, IDevicesCommon, INotifyPropertyChanged, IRelayAutoOperations, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, ILogicalDeviceVoltageMeasurementBatteryPid, ILogicalDeviceVoltageMeasurementBattery, ILogicalDeviceVoltageMeasurement, ILogicalDeviceReadVoltageMeasurement, IReadVoltageMeasurement
	{
		ILogicalDeviceAwningSensor GetAssociatedAwningSensor();

		DeviceAssociation GetAwningSensorAssociation(ILogicalDeviceAwningSensor awningSensor);

		AwningProtectionState GetAwningAutoRetractProtectionState();

		AwningWindStrength GetAwningWindProtectionLevel();

		Task<AwningWindStrength> TryGetAwningWindProtectionLevelAsync(CancellationToken cancellationToken);

		Task SetAwningAutoRetractWindProtectionLevelAsync(AwningWindStrength windSetting, CancellationToken cancellationToken);
	}
}
