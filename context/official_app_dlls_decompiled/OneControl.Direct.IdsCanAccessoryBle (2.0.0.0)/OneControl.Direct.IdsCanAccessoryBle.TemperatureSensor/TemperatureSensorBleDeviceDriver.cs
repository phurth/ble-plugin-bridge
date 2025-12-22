using System;
using IDS.Core.IDS_CAN;
using ids.portable.ble.Ble;
using OneControl.Devices.TemperatureSensor;
using OneControl.Direct.IdsCanAccessoryBle.AccessoryTemplate.BleDeviceDriver;
using OneControl.Direct.IdsCanAccessoryBle.Connections;

namespace OneControl.Direct.IdsCanAccessoryBle.TemperatureSensor
{
	public class TemperatureSensorBleDeviceDriver : BleDeviceDriverLoCap<ITemperatureSensorBleDeviceSource, SensorConnectionTemperature, ILogicalDeviceTemperatureSensor>
	{
		public const string DeviceNamePrefix = "LIP";

		protected override string LogTag => "TemperatureSensorBleDeviceDriver";

		public override DEVICE_TYPE BleDeviceType => (byte)25;

		protected override TimeSpan MinimumConnectionWindowTimeSpan => TimeSpan.FromSeconds(72.0);

		protected override int BleConnectionAutoCloseTimeoutMs => 30000;

		protected override int BleConnectionRetryDelayMs => 160000;

		protected override int BleConnectAttemptMs => 80000;

		protected override int BleConnectTimeoutMaxMs => 200;

		protected override int LogWarningTimeMs => 120000;

		public TemperatureSensorBleDeviceDriver(IBleService bleService, ITemperatureSensorBleDeviceSource sourceDirect, SensorConnectionTemperature sensorConnection)
			: base(bleService, sourceDirect, sensorConnection)
		{
		}
	}
}
