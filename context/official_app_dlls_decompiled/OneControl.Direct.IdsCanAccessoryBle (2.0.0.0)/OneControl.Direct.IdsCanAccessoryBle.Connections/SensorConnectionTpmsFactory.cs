using System;
using IDS.Core.IDS_CAN;
using ids.portable.ble.Ble;
using IDS.Portable.LogicalDevice;
using OneControl.Direct.IdsCanAccessoryBle.TPMS;

namespace OneControl.Direct.IdsCanAccessoryBle.Connections
{
	public class SensorConnectionTpmsFactory : SensorConnectionFactoryLocap<SensorConnectionTpms, ITpmsBleDeviceSource>
	{
		public override DEVICE_TYPE DeviceType => (byte)42;

		public override bool IsStandardSource => true;

		public SensorConnectionTpmsFactory(IBleService bleService, ILogicalDeviceService deviceService)
			: base((ITpmsBleDeviceSource)new TpmsBleDeviceSource(bleService, deviceService))
		{
		}

		public override SensorConnectionTpms MakeSensorConnection(string connectionNameFriendly, Guid connectionGuid, MAC accessoryMac, string softwarePartNumber)
		{
			return new SensorConnectionTpms(connectionNameFriendly, connectionGuid, accessoryMac, softwarePartNumber);
		}
	}
}
