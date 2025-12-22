using System;
using IDS.Core.IDS_CAN;
using ids.portable.ble.Ble;
using IDS.Portable.LogicalDevice;
using OneControl.Direct.IdsCanAccessoryBle.TankSensor;

namespace OneControl.Direct.IdsCanAccessoryBle.Connections
{
	public class SensorConnectionTankSensorFactory : SensorConnectionFactoryLocap<SensorConnectionTankSensor, ITankSensorBleDeviceSource>
	{
		public override DEVICE_TYPE DeviceType => (byte)10;

		public override bool IsStandardSource => true;

		public SensorConnectionTankSensorFactory(IBleService bleService, ILogicalDeviceService deviceService)
			: base((ITankSensorBleDeviceSource)new TankSensorBleDeviceSource(bleService, deviceService))
		{
		}

		public override SensorConnectionTankSensor MakeSensorConnection(string connectionNameFriendly, Guid connectionGuid, MAC accessoryMac, string softwarePartNumber)
		{
			return new SensorConnectionTankSensor(connectionNameFriendly, connectionGuid, accessoryMac, softwarePartNumber);
		}
	}
}
