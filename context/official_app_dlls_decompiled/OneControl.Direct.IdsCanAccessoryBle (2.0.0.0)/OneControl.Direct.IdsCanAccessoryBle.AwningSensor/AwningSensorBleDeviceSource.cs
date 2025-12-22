using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using ids.portable.ble.Ble;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using IDS.Portable.LogicalDevice.FirmwareUpdate;
using IDS.Portable.LogicalDevice.LogicalDeviceSource;
using OneControl.Devices.AwningSensor;
using OneControl.Direct.IdsCanAccessoryBle.AccessoryTemplate.BleDeviceSource;
using OneControl.Direct.IdsCanAccessoryBle.Connections;

namespace OneControl.Direct.IdsCanAccessoryBle.AwningSensor
{
	public class AwningSensorBleDeviceSource : BleDeviceSourceLocap<AwningSensorBleDeviceDriver, SensorConnectionAwningSensor, ILogicalDeviceAwningSensor>, IAwningSensorBleDeviceSource, IAccessoryBleDeviceSourceLocap<SensorConnectionAwningSensor>, IAccessoryBleDeviceSourceLocap, ILogicalDeviceSourceDirect, ILogicalDeviceSource, IAccessoryBleDeviceSource<SensorConnectionAwningSensor>, IAccessoryBleDeviceSource, ICommonDisposable, IDisposable, ILogicalDeviceSourceDirectIdsAccessory, ILogicalDeviceSourceDirectRename, ILogicalDeviceSourceDirectMetadata, ILogicalDeviceSourceDirectPid, ILogicalDeviceSourceDirectAccessoryHistoryData, IAccessoryBleDeviceSourceDevices<AwningSensorBleDeviceDriver>, ILogicalDeviceSourceDirectFirmwareUpdateDevice, IFirmwareUpdateDevice
	{
		private readonly IBleService _bleService;

		private const string DeviceSourceTokenDefault = "Ids.Accessory.AwningSensor.Default";

		public const int BleConnectionAutoCloseTimeoutMs = 300000;

		public const int BleConnectAttemptMs = 40000;

		public const int BleConnectTimeoutMaxMs = 80000;

		public const int BleConnectionRetryDelayMs = 200;

		protected override string LogTag => "AwningSensorBleDeviceSource";

		public AwningSensorBleDeviceSource(IBleService bleService, ILogicalDeviceService deviceService)
			: base(bleService, deviceService, "Ids.Accessory.AwningSensor.Default", TimeSpan.FromMilliseconds(40000.0))
		{
			_bleService = bleService;
		}

		protected override AwningSensorBleDeviceDriver CreateDeviceBle(SensorConnectionAwningSensor sensorConnection)
		{
			return new AwningSensorBleDeviceDriver(_bleService, this, sensorConnection);
		}

		public override bool RegisterSensor(Guid bleDeviceId, MAC accessoryMacAddress, string softwarePartNumber, string bleDeviceName)
		{
			return RegisterSensor(new SensorConnectionAwningSensor(bleDeviceName, bleDeviceId, accessoryMacAddress, softwarePartNumber));
		}

		protected override ILogicalDeviceTag CreateLogicalDeviceTag(Guid bleDeviceId, MAC accessoryMacAddress, string softwarePartNumber, string bleDeviceName)
		{
			return new LogicalDeviceTagSourceAwningSensorBle(bleDeviceId, accessoryMacAddress, softwarePartNumber, bleDeviceName);
		}

		public async Task<FirmwareUpdateSupport> TryGetFirmwareUpdateSupportAsync(ILogicalDevice logicalDevice, CancellationToken cancelToken)
		{
			AwningSensorBleDeviceDriver? sensorDevice = GetSensorDevice(logicalDevice);
			if (sensorDevice?.AccessoryConnectionManager == null)
			{
				throw new AccessoryConnectionManagerException("AccessoryConnectionManager is null!");
			}
			return await sensorDevice!.AccessoryConnectionManager!.TryGetFirmwareUpdateSupportAsync(logicalDevice, cancelToken);
		}

		public async Task UpdateFirmwareAsync(ILogicalDeviceFirmwareUpdateSession firmwareUpdateSession, IReadOnlyList<byte> data, Func<ILogicalDeviceTransferProgress, bool> progressAck, CancellationToken cancellationToken, IReadOnlyDictionary<FirmwareUpdateOption, object>? options = null)
		{
			AwningSensorBleDeviceDriver? sensorDevice = GetSensorDevice(firmwareUpdateSession.LogicalDevice);
			if (sensorDevice?.AccessoryConnectionManager == null)
			{
				throw new AccessoryConnectionManagerException("AccessoryConnectionManager is null!");
			}
			await sensorDevice!.AccessoryConnectionManager!.UpdateFirmwareAsync(firmwareUpdateSession, data, progressAck, cancellationToken, options);
		}
	}
}
