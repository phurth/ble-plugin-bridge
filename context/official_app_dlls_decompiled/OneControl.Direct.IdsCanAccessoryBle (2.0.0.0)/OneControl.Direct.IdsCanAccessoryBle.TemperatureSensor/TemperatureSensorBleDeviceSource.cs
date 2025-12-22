using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using ids.portable.ble.Ble;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using IDS.Portable.LogicalDevice.LogicalDeviceSource;
using OneControl.Devices.TemperatureSensor;
using OneControl.Direct.IdsCanAccessoryBle.AccessoryTemplate.BleDeviceSource;
using OneControl.Direct.IdsCanAccessoryBle.Connections;

namespace OneControl.Direct.IdsCanAccessoryBle.TemperatureSensor
{
	public class TemperatureSensorBleDeviceSource : BleDeviceSourceLocap<TemperatureSensorBleDeviceDriver, SensorConnectionTemperature, ILogicalDeviceTemperatureSensor>, ITemperatureSensorBleDeviceSource, IAccessoryBleDeviceSourceLocap<SensorConnectionTemperature>, IAccessoryBleDeviceSourceLocap, ILogicalDeviceSourceDirect, ILogicalDeviceSource, IAccessoryBleDeviceSource<SensorConnectionTemperature>, IAccessoryBleDeviceSource, ICommonDisposable, IDisposable, ILogicalDeviceSourceDirectIdsAccessory, ILogicalDeviceSourceDirectRename, ILogicalDeviceSourceDirectMetadata, ILogicalDeviceSourceDirectPid, ILogicalDeviceSourceDirectAccessoryHistoryData, IAccessoryBleDeviceSourceDevices<TemperatureSensorBleDeviceDriver>
	{
		private readonly IBleService _bleService;

		private const string DeviceSourceTokenDefault = "Ids.Accessory.TemperatureSensor.Default";

		public const int BleConnectionAutoCloseTimeoutMs = 30000;

		public const int BleConnectAttemptMs = 80000;

		public const int BleConnectTimeoutMaxMs = 160000;

		public const int BleConnectionRetryDelayMs = 200;

		protected override string LogTag => "TemperatureSensorBleDeviceSource";

		public TemperatureSensorBleDeviceSource(IBleService bleService, ILogicalDeviceService deviceService)
			: base(bleService, deviceService, "Ids.Accessory.TemperatureSensor.Default", TimeSpan.FromMilliseconds(80000.0))
		{
			_bleService = bleService;
		}

		protected override TemperatureSensorBleDeviceDriver CreateDeviceBle(SensorConnectionTemperature sensorConnection)
		{
			return new TemperatureSensorBleDeviceDriver(_bleService, this, sensorConnection);
		}

		public override bool RegisterSensor(Guid bleDeviceId, MAC accessoryMacAddress, string softwarePartNumber, string bleDeviceName)
		{
			return RegisterSensor(new SensorConnectionTemperature(bleDeviceName, bleDeviceId, accessoryMacAddress, softwarePartNumber));
		}

		protected override ILogicalDeviceTag CreateLogicalDeviceTag(Guid bleDeviceId, MAC accessoryMacAddress, string softwarePartNumber, string bleDeviceName)
		{
			return new LogicalDeviceTagSourceTemperatureSensorBle(bleDeviceId, accessoryMacAddress, softwarePartNumber, bleDeviceName);
		}

		public override Task<IReadOnlyList<byte>> GetAccessoryHistoryDataAsync(ILogicalDevice logicalDevice, byte block, byte startIndex = 0, byte dataLength = byte.MaxValue, CancellationToken cancellationToken = default(CancellationToken))
		{
			return ((GetSensorDevice(logicalDevice) ?? throw new LogicalDeviceHistoryDataException("Error retrieving temperature sensor history data."))!.AccessoryConnectionManager ?? throw new LogicalDeviceHistoryDataException("Error retrieving temperature sensor history data."))!.GetAccessoryHistoryDataAsync(block, startIndex, dataLength, cancellationToken);
		}
	}
}
