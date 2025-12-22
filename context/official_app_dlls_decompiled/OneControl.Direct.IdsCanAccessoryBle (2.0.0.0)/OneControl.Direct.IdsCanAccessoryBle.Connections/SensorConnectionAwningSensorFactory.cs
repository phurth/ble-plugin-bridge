using System;
using IDS.Core.IDS_CAN;
using ids.portable.ble.Ble;
using IDS.Portable.LogicalDevice;
using OneControl.Direct.IdsCanAccessoryBle.AwningSensor;

namespace OneControl.Direct.IdsCanAccessoryBle.Connections
{
	public class SensorConnectionAwningSensorFactory : SensorConnectionFactoryLocap<SensorConnectionAwningSensor, IAwningSensorBleDeviceSource>
	{
		public override DEVICE_TYPE DeviceType => (byte)47;

		public override bool IsStandardSource => true;

		public SensorConnectionAwningSensorFactory(IBleService bleService, ILogicalDeviceService deviceService)
			: base((IAwningSensorBleDeviceSource)new AwningSensorBleDeviceSource(bleService, deviceService))
		{
		}

		public override SensorConnectionAwningSensor MakeSensorConnection(string connectionNameFriendly, Guid connectionGuid, MAC accessoryMac, string softwarePartNumber)
		{
			return new SensorConnectionAwningSensor(connectionNameFriendly, connectionGuid, accessoryMac, softwarePartNumber);
		}
	}
}
