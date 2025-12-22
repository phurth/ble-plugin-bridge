using IDS.Core.IDS_CAN;
using ids.portable.ble.Ble;
using OneControl.Devices.DoorLock;
using OneControl.Direct.IdsCanAccessoryBle.AccessoryTemplate.BleDeviceDriver;
using OneControl.Direct.IdsCanAccessoryBle.Connections;

namespace OneControl.Direct.IdsCanAccessoryBle.DoorLock
{
	public class DoorLockBleDeviceDriver : BleDeviceDriverLoCap<IDoorLockBleDeviceSource, SensorConnectionDoorLock, ILogicalDeviceDoorLock>
	{
		protected override string LogTag => "DoorLockBleDeviceDriver";

		public override DEVICE_TYPE BleDeviceType => (byte)51;

		protected override int BleConnectionAutoCloseTimeoutMs => 30000;

		protected override int BleConnectionRetryDelayMs => 200;

		protected override int BleConnectAttemptMs => 40000;

		protected override int BleConnectTimeoutMaxMs => 80000;

		public DoorLockBleDeviceDriver(IBleService bleService, IDoorLockBleDeviceSource sourceDirect, SensorConnectionDoorLock sensorConnection)
			: base(bleService, sourceDirect, sensorConnection)
		{
		}
	}
}
