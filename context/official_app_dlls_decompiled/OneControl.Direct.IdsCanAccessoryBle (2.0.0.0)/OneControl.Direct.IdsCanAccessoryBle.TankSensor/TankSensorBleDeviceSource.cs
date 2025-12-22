using System;
using IDS.Core.IDS_CAN;
using ids.portable.ble.Ble;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using IDS.Portable.LogicalDevice.LogicalDeviceSource;
using OneControl.Devices;
using OneControl.Direct.IdsCanAccessoryBle.AccessoryTemplate.BleDeviceSource;
using OneControl.Direct.IdsCanAccessoryBle.Connections;

namespace OneControl.Direct.IdsCanAccessoryBle.TankSensor
{
	public class TankSensorBleDeviceSource : BleDeviceSourceLocap<TankSensorBleDeviceDriver, SensorConnectionTankSensor, ILogicalDeviceTankSensor>, ITankSensorBleDeviceSource, IAccessoryBleDeviceSourceLocap<SensorConnectionTankSensor>, IAccessoryBleDeviceSourceLocap, ILogicalDeviceSourceDirect, ILogicalDeviceSource, IAccessoryBleDeviceSource<SensorConnectionTankSensor>, IAccessoryBleDeviceSource, ICommonDisposable, IDisposable, ILogicalDeviceSourceDirectIdsAccessory, ILogicalDeviceSourceDirectRename, ILogicalDeviceSourceDirectMetadata, ILogicalDeviceSourceDirectPid, ILogicalDeviceSourceDirectAccessoryHistoryData, IAccessoryBleDeviceSourceDevices<TankSensorBleDeviceDriver>
	{
		private readonly IBleService _bleService;

		private const string DeviceSourceTokenDefault = "Ids.Accessory.TankSensor.Default";

		public const int BleConnectionAutoCloseTimeoutMs = 30000;

		public const int BleConnectionRetryDelayMs = 1000;

		public const int BleConnectAttemptMs = 10000;

		public const int BleConnectTimeoutMaxMs = 30000;

		protected override string LogTag => "TankSensorBleDeviceSource";

		public TankSensorBleDeviceSource(IBleService bleService, ILogicalDeviceService deviceService)
			: base(bleService, deviceService, "Ids.Accessory.TankSensor.Default", TimeSpan.FromMilliseconds(10000.0))
		{
			_bleService = bleService;
		}

		protected override TankSensorBleDeviceDriver CreateDeviceBle(SensorConnectionTankSensor sensorConnection)
		{
			return new TankSensorBleDeviceDriver(_bleService, this, sensorConnection);
		}

		public override bool RegisterSensor(Guid bleDeviceId, MAC accessoryMacAddress, string softwarePartNumber, string bleDeviceName)
		{
			return RegisterSensor(new SensorConnectionTankSensor(bleDeviceName, bleDeviceId, accessoryMacAddress, softwarePartNumber));
		}

		protected override ILogicalDeviceTag CreateLogicalDeviceTag(Guid bleDeviceId, MAC accessoryMacAddress, string softwarePartNumber, string bleDeviceName)
		{
			return new LogicalDeviceTagSourceTankSensorBle(bleDeviceId, accessoryMacAddress, softwarePartNumber, bleDeviceName);
		}
	}
}
