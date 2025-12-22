using System;
using System.Collections.Generic;
using IDS.Core.IDS_CAN;
using ids.portable.ble.Ble;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.TankSensor.Mopeka;
using OneControl.Direct.IdsCanAccessoryBle.Mopeka;
using OneControl.Direct.IdsCanAccessoryBle.ScanResults;

namespace OneControl.Direct.IdsCanAccessoryBle.Connections
{
	public class SensorConnectionMopekaFactory : ISensorConnectionFactory
	{
		public const string LogTag = "SensorConnectionMopekaFactory";

		public DEVICE_TYPE DeviceType => (byte)10;

		public bool IsStandardSource => true;

		public Type SensorConnectionType => typeof(SensorConnectionMopeka);

		public ILogicalDeviceSourceDirect DeviceSource => EchoBrakeDeviceSource;

		protected IMopekaBleDeviceSource EchoBrakeDeviceSource { get; }

		public IEnumerable<ISensorConnection> SensorConnectionsAll => EchoBrakeDeviceSource.SensorConnectionsAll;

		public SensorConnectionMopekaFactory(IBleService bleService, ILogicalDeviceService deviceService, Func<ILPSettingsRepository> lpSettingsRepositoryFactory)
		{
			EchoBrakeDeviceSource = (MopekaBleDeviceSource.SharedExtension = new MopekaBleDeviceSource(deviceService, bleService, lpSettingsRepositoryFactory));
			deviceService.RegisterLogicalDeviceExFactory(MopekaBleDeviceSource.LogicalDeviceExFactory);
			Resolver<IMopekaBleDeviceSource>.LazyConstructAndRegister(() => MopekaBleDeviceSource.SharedExtension);
		}

		public (ISensorConnection? SensorConnection, bool NewRegistration) TryAddSensorConnection(IAccessoryScanResult accessoryScanResult)
		{
			return (null, false);
		}

		public bool TryAddSensorConnection(ISensorConnection sensorConnection)
		{
			if (!(sensorConnection is SensorConnectionMopeka sensorConnection2))
			{
				return false;
			}
			return EchoBrakeDeviceSource.LinkMopekaSensor(sensorConnection2);
		}

		public bool TryRemoveSensorConnection(ISensorConnection sensorConnection)
		{
			if (!(sensorConnection is SensorConnectionMopeka sensorConnectionMopeka))
			{
				return false;
			}
			if (!EchoBrakeDeviceSource.IsMopekaSensorLinked(sensorConnectionMopeka.MacAddress))
			{
				return false;
			}
			EchoBrakeDeviceSource.UnlinkMopekaSensor(sensorConnectionMopeka.MacAddress);
			return true;
		}
	}
}
