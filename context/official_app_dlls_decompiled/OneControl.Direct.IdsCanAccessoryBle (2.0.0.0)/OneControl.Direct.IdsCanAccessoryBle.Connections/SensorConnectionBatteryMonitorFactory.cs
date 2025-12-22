using System;
using IDS.Core.IDS_CAN;
using ids.portable.ble.Ble;
using IDS.Portable.LogicalDevice;
using OneControl.Direct.IdsCanAccessoryBle.BatteryMonitor;

namespace OneControl.Direct.IdsCanAccessoryBle.Connections
{
	public class SensorConnectionBatteryMonitorFactory : SensorConnectionFactoryLocap<SensorConnectionBatteryMonitor, IBatteryMonitorBleDeviceSource>
	{
		public override DEVICE_TYPE DeviceType => (byte)49;

		public override bool IsStandardSource => true;

		public SensorConnectionBatteryMonitorFactory(IBleService bleService, ILogicalDeviceService deviceService)
			: base((IBatteryMonitorBleDeviceSource)new BatteryMonitorBleDeviceSource(bleService, deviceService))
		{
		}

		public override SensorConnectionBatteryMonitor MakeSensorConnection(string connectionNameFriendly, Guid connectionGuid, MAC accessoryMac, string softwarePartNumber)
		{
			return new SensorConnectionBatteryMonitor(connectionNameFriendly, connectionGuid, accessoryMac, softwarePartNumber);
		}
	}
}
