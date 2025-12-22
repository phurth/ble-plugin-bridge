using System;
using System.Collections.Generic;
using IDS.Core.IDS_CAN;
using ids.portable.ble.Ble;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Direct.IdsCanAccessoryBle.ScanResults;
using OneControl.Direct.IdsCanAccessoryBle.TirePressureMonitor;

namespace OneControl.Direct.IdsCanAccessoryBle.Connections
{
	public class SensorConnectionTirePressureMonitorFactory : ISensorConnectionFactory
	{
		public const string LogTag = "SensorConnectionTirePressureMonitorFactory";

		public DEVICE_TYPE DeviceType => (byte)42;

		public bool IsStandardSource => true;

		public Type SensorConnectionType => typeof(SensorConnectionTirePressureMonitor);

		public ILogicalDeviceSourceDirect DeviceSource => TirePressureMonitorDeviceSource;

		protected DirectTirePressureMonitorBleDeviceDriver TirePressureMonitorDeviceSource { get; }

		public IEnumerable<ISensorConnection> SensorConnectionsAll => TirePressureMonitorDeviceSource.SensorConnectionsAll;

		public SensorConnectionTirePressureMonitorFactory(IBleService bleService, ILogicalDeviceService deviceService)
		{
			TirePressureMonitorDeviceSource = new DirectTirePressureMonitorBleDeviceDriver(bleService, deviceService);
			Resolver<ITirePressureMonitorBleDeviceSource>.LazyConstructAndRegister(() => TirePressureMonitorDeviceSource);
		}

		public (ISensorConnection? SensorConnection, bool NewRegistration) TryAddSensorConnection(IAccessoryScanResult accessoryScanResult)
		{
			MAC accessoryMacAddress = accessoryScanResult.AccessoryMacAddress;
			if ((object)accessoryMacAddress == null)
			{
				return (null, false);
			}
			IdsCanAccessoryStatus? accessoryStatus = accessoryScanResult.GetAccessoryStatus(accessoryMacAddress);
			if (!accessoryStatus.HasValue)
			{
				return (null, false);
			}
			if (accessoryStatus.Value.DeviceType != DeviceType)
			{
				return (null, false);
			}
			SensorConnectionTirePressureMonitor sensorConnectionTirePressureMonitor = new SensorConnectionTirePressureMonitor(accessoryScanResult.DeviceName, accessoryScanResult.DeviceId, accessoryScanResult.AccessoryMacAddress, accessoryScanResult.SoftwarePartNumber ?? string.Empty);
			bool flag = TryAddSensorConnection(sensorConnectionTirePressureMonitor);
			return (sensorConnectionTirePressureMonitor, flag);
		}

		public bool TryAddSensorConnection(ISensorConnection sensorConnection)
		{
			if (!(sensorConnection is SensorConnectionTirePressureMonitor sensorConnection2))
			{
				return false;
			}
			return TirePressureMonitorDeviceSource.RegisterSensor(sensorConnection2);
		}

		public bool TryRemoveSensorConnection(ISensorConnection sensorConnection)
		{
			if (!(sensorConnection is SensorConnectionTirePressureMonitor sensorConnectionTirePressureMonitor))
			{
				return false;
			}
			if (!TirePressureMonitorDeviceSource.IsSensorRegistered(sensorConnectionTirePressureMonitor.ConnectionGuid))
			{
				return false;
			}
			TirePressureMonitorDeviceSource.UnRegisterSensor(sensorConnectionTirePressureMonitor.ConnectionGuid);
			return true;
		}
	}
}
