using System.Collections.Generic;
using IDS.Portable.LogicalDevice;
using OneControl.Direct.IdsCanAccessoryBle.AwningSensor;
using OneControl.Direct.IdsCanAccessoryBle.BatteryMonitor;
using OneControl.Direct.IdsCanAccessoryBle.Connections;
using OneControl.Direct.IdsCanAccessoryBle.DoorLock;
using OneControl.Direct.IdsCanAccessoryBle.EchoBrakeControl;
using OneControl.Direct.IdsCanAccessoryBle.FlicButton;
using OneControl.Direct.IdsCanAccessoryBle.Mopeka;
using OneControl.Direct.IdsCanAccessoryBle.ScanResults;
using OneControl.Direct.IdsCanAccessoryBle.TankSensor;
using OneControl.Direct.IdsCanAccessoryBle.TemperatureSensor;
using OneControl.Direct.IdsCanAccessoryBle.TirePressureMonitor;
using OneControl.Direct.IdsCanAccessoryBle.TPMS;

namespace OneControl.Direct.IdsCanAccessoryBle
{
	public interface IAccessoryRegistrationManager
	{
		IAwningSensorBleDeviceSource? AwningSensorBleDeviceSource { get; }

		IBatteryMonitorBleDeviceSource? BatteryMonitorBleDeviceSource { get; }

		IDoorLockBleDeviceSource? DoorLockBleDeviceSource { get; }

		IEchoBrakeControlBleDeviceSource? EchoBrakeControlControllerBleDeviceSource { get; }

		ITirePressureMonitorBleDeviceSource? TirePressureMonitorControllerBleDeviceSource { get; }

		IMopekaBleDeviceSource? MopekaBleDeviceSource { get; }

		ITemperatureSensorBleDeviceSource? TemperatureSensorBleDeviceSource { get; }

		ITankSensorBleDeviceSource? TankSensorBleDeviceSource { get; }

		IFlicButtonBleDeviceSource FlicButtonBleDeviceSource { get; internal set; }

		ITpmsBleDeviceSource? TpmsBleDeviceSource { get; }

		IEnumerable<ILogicalDeviceSourceDirect> StandardSharedSensorSources { get; }

		IEnumerable<ISensorConnection> SensorConnectionsAll { get; }

		event SensorConnectionAdded? DoSensorConnectionAdded;

		event SensorConnectionRemoved? DoSensorConnectionRemoved;

		bool RegisterFactory(ISensorConnectionFactory factory);

		bool TryAddSensorConnection(IAccessoryScanResult accessoryScanResult, bool requestSave);

		bool TryAddSensorConnection(ISensorConnection sensorConnection, bool requestSave);

		bool TryRemoveSensorConnection(ISensorConnection sensorConnection, bool requestSave);
	}
}
