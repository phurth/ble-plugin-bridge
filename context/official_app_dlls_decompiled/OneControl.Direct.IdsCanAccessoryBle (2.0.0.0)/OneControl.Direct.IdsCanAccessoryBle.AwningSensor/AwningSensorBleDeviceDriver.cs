using IDS.Core.IDS_CAN;
using ids.portable.ble.Ble;
using OneControl.Devices.AwningSensor;
using OneControl.Direct.IdsCanAccessoryBle.AccessoryTemplate.BleDeviceDriver;
using OneControl.Direct.IdsCanAccessoryBle.Connections;

namespace OneControl.Direct.IdsCanAccessoryBle.AwningSensor
{
	public class AwningSensorBleDeviceDriver : BleDeviceDriverLoCap<IAwningSensorBleDeviceSource, SensorConnectionAwningSensor, ILogicalDeviceAwningSensor>
	{
		protected override string LogTag => "AwningSensorBleDeviceDriver";

		public override DEVICE_TYPE BleDeviceType => (byte)47;

		protected override int BleConnectionAutoCloseTimeoutMs => 300000;

		protected override int BleConnectionRetryDelayMs => 80000;

		protected override int BleConnectAttemptMs => 40000;

		protected override int BleConnectTimeoutMaxMs => 200;

		public AwningSensorBleDeviceDriver(IBleService bleService, IAwningSensorBleDeviceSource sourceDirect, SensorConnectionAwningSensor sensorConnection)
			: base(bleService, sourceDirect, sensorConnection)
		{
		}
	}
}
