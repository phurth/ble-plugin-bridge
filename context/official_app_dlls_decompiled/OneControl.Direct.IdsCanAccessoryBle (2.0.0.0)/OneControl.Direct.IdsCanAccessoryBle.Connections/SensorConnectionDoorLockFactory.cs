using System;
using IDS.Core.IDS_CAN;
using ids.portable.ble.Ble;
using IDS.Portable.LogicalDevice;
using OneControl.Direct.IdsCanAccessoryBle.DoorLock;

namespace OneControl.Direct.IdsCanAccessoryBle.Connections
{
	public class SensorConnectionDoorLockFactory : SensorConnectionFactoryLocap<SensorConnectionDoorLock, IDoorLockBleDeviceSource>
	{
		public override DEVICE_TYPE DeviceType => (byte)51;

		public override bool IsStandardSource => true;

		public SensorConnectionDoorLockFactory(IBleService bleService, ILogicalDeviceService deviceService)
			: base((IDoorLockBleDeviceSource)new DoorLockBleDeviceSource(bleService, deviceService))
		{
		}

		public override SensorConnectionDoorLock MakeSensorConnection(string connectionNameFriendly, Guid connectionGuid, MAC accessoryMac, string softwarePartNumber)
		{
			return new SensorConnectionDoorLock(connectionNameFriendly, connectionGuid, accessoryMac, softwarePartNumber);
		}
	}
}
