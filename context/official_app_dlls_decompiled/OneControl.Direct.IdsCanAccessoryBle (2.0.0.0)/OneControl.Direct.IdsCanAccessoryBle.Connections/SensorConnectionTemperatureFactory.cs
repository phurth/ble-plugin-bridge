using System;
using IDS.Core.IDS_CAN;
using ids.portable.ble.Ble;
using IDS.Portable.LogicalDevice;
using OneControl.Direct.IdsCanAccessoryBle.TemperatureSensor;

namespace OneControl.Direct.IdsCanAccessoryBle.Connections
{
	public class SensorConnectionTemperatureFactory : SensorConnectionFactoryLocap<SensorConnectionTemperature, ITemperatureSensorBleDeviceSource>
	{
		public override DEVICE_TYPE DeviceType => (byte)25;

		public override bool IsStandardSource => true;

		public SensorConnectionTemperatureFactory(IBleService bleService, ILogicalDeviceService deviceService)
			: base((ITemperatureSensorBleDeviceSource)new TemperatureSensorBleDeviceSource(bleService, deviceService))
		{
		}

		public override SensorConnectionTemperature MakeSensorConnection(string connectionNameFriendly, Guid connectionGuid, MAC accessoryMac, string softwarePartNumber)
		{
			return new SensorConnectionTemperature(connectionNameFriendly, connectionGuid, accessoryMac, softwarePartNumber);
		}
	}
}
