using IDS.Core.IDS_CAN;
using ids.portable.ble.Ble;
using OneControl.Devices.BatteryMonitor;
using OneControl.Direct.IdsCanAccessoryBle.AccessoryTemplate.BleDeviceDriver;
using OneControl.Direct.IdsCanAccessoryBle.Connections;

namespace OneControl.Direct.IdsCanAccessoryBle.BatteryMonitor
{
	public class BatteryMonitorBleDeviceDriver : BleDeviceDriverLoCap<IBatteryMonitorBleDeviceSource, SensorConnectionBatteryMonitor, ILogicalDeviceBatteryMonitor>
	{
		public const string DeviceNamePrefix = "LIP";

		public const int PidWriteRetryDelayMs = 1000;

		public const int PidWriteVerifyDelayMs = 2000;

		public const int PidWriteVerifyRetryCount = 10;

		protected override string LogTag => "BatteryMonitorBleDeviceDriver";

		public override DEVICE_TYPE BleDeviceType => (byte)49;

		protected override int BleConnectionAutoCloseTimeoutMs => 10000;

		protected override int BleConnectionRetryDelayMs => 1000;

		protected override int BleConnectAttemptMs => 10000;

		protected override int BleConnectTimeoutMaxMs => 30000;

		public BatteryMonitorBleDeviceDriver(IBleService bleService, IBatteryMonitorBleDeviceSource sourceDirect, SensorConnectionBatteryMonitor sensorConnection)
			: base(bleService, sourceDirect, sensorConnection)
		{
		}

		protected override void ConfigureAccessoryConnectionManager(AccessoryConnectionManager<ILogicalDeviceBatteryMonitor> accessoryConnectionManager)
		{
			accessoryConnectionManager.PidWriteRetryDelayMs = 1000;
			accessoryConnectionManager.PidWriteVerifyDelayMs = 2000;
			accessoryConnectionManager.PidWriteVerifyRetryCount = 10;
		}
	}
}
