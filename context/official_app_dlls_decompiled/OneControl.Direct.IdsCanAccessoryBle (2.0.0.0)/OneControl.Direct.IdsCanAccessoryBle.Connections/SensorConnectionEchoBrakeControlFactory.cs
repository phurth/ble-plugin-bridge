using System;
using System.Collections.Generic;
using IDS.Core.IDS_CAN;
using ids.portable.ble.Ble;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Direct.IdsCanAccessoryBle.EchoBrakeControl;
using OneControl.Direct.IdsCanAccessoryBle.ScanResults;

namespace OneControl.Direct.IdsCanAccessoryBle.Connections
{
	public class SensorConnectionEchoBrakeControlFactory : ISensorConnectionFactory
	{
		public const string LogTag = "SensorConnectionEchoBrakeControlFactory";

		public DEVICE_TYPE DeviceType => (byte)53;

		public bool IsStandardSource => true;

		public Type SensorConnectionType => typeof(SensorConnectionEchoBrakeControl);

		public ILogicalDeviceSourceDirect DeviceSource => EchoBrakeDeviceSource;

		protected IEchoBrakeControlBleDeviceSource EchoBrakeDeviceSource { get; }

		public IEnumerable<ISensorConnection> SensorConnectionsAll => EchoBrakeDeviceSource.SensorConnectionsAll;

		public SensorConnectionEchoBrakeControlFactory(IBleService bleService, ILogicalDeviceService deviceService)
		{
			EchoBrakeDeviceSource = new EchoBrakeControlBleDeviceSource(bleService, deviceService);
			Resolver<IEchoBrakeControlBleDeviceSource>.LazyConstructAndRegister(() => EchoBrakeDeviceSource);
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
			SensorConnectionEchoBrakeControl sensorConnectionEchoBrakeControl = new SensorConnectionEchoBrakeControl(accessoryScanResult.DeviceName, accessoryScanResult.DeviceId);
			bool flag = TryAddSensorConnection(sensorConnectionEchoBrakeControl);
			return (sensorConnectionEchoBrakeControl, flag);
		}

		public bool TryAddSensorConnection(ISensorConnection sensorConnection)
		{
			if (!(sensorConnection is SensorConnectionEchoBrakeControl sensorConnection2))
			{
				return false;
			}
			return EchoBrakeDeviceSource.RegisterSensor(sensorConnection2);
		}

		public bool TryRemoveSensorConnection(ISensorConnection sensorConnection)
		{
			if (!(sensorConnection is SensorConnectionEchoBrakeControl sensorConnectionEchoBrakeControl))
			{
				return false;
			}
			if (!EchoBrakeDeviceSource.IsSensorRegistered(sensorConnectionEchoBrakeControl.ConnectionGuid))
			{
				return false;
			}
			EchoBrakeDeviceSource.UnRegisterSensor(sensorConnectionEchoBrakeControl.ConnectionGuid);
			return true;
		}
	}
}
