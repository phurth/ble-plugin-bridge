using System;
using IDS.Core.IDS_CAN;
using ids.portable.ble.Ble;
using OneControl.Devices;
using OneControl.Direct.IdsCanAccessoryBle.AccessoryTemplate.BleDeviceDriver;
using OneControl.Direct.IdsCanAccessoryBle.Connections;

namespace OneControl.Direct.IdsCanAccessoryBle.TankSensor
{
	public class TankSensorBleDeviceDriver : BleDeviceDriverLoCap<ITankSensorBleDeviceSource, SensorConnectionTankSensor, ILogicalDeviceTankSensor>
	{
		protected override string LogTag => "TankSensorBleDeviceDriver";

		public override DEVICE_TYPE BleDeviceType => (byte)10;

		protected override TimeSpan MinimumConnectionWindowTimeSpan => TimeSpan.FromMinutes(1.0);

		protected override int BleConnectionAutoCloseTimeoutMs => 30000;

		protected override int BleConnectionRetryDelayMs => 30000;

		protected override int BleConnectAttemptMs => 10000;

		protected override int BleConnectTimeoutMaxMs => 1000;

		public TankSensorBleDeviceDriver(IBleService bleService, ITankSensorBleDeviceSource sourceDirect, SensorConnectionTankSensor sensorConnection)
			: base(bleService, sourceDirect, sensorConnection)
		{
		}
	}
}
