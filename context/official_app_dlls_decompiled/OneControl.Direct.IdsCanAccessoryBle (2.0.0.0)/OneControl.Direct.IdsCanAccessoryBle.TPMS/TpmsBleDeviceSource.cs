using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using ids.portable.ble.Ble;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using IDS.Portable.LogicalDevice.LogicalDeviceSource;
using IDS.Portable.LogicalDevice.Tag;
using OneControl.Devices.TPMS;
using OneControl.Direct.IdsCanAccessoryBle.AccessoryTemplate.BleDeviceSource;
using OneControl.Direct.IdsCanAccessoryBle.Connections;

namespace OneControl.Direct.IdsCanAccessoryBle.TPMS
{
	public class TpmsBleDeviceSource : BleDeviceSourceLocap<TpmsBleDeviceDriver, SensorConnectionTpms, ILogicalDeviceTpms>, ITpmsBleDeviceSource, IAccessoryBleDeviceSourceLocap<SensorConnectionTpms>, IAccessoryBleDeviceSourceLocap, ILogicalDeviceSourceDirect, ILogicalDeviceSource, IAccessoryBleDeviceSource<SensorConnectionTpms>, IAccessoryBleDeviceSource, ICommonDisposable, IDisposable, ILogicalDeviceSourceDirectIdsAccessory, ILogicalDeviceSourceDirectRename, ILogicalDeviceSourceDirectMetadata, ILogicalDeviceSourceDirectPid, ILogicalDeviceSourceDirectAccessoryHistoryData, IAccessoryBleDeviceSourceDevices<TpmsBleDeviceDriver>
	{
		private readonly IBleService _bleService;

		private const string DeviceSourceTokenDefault = "Ids.Accessory.Tpms.Default";

		public const int BleConnectionAutoCloseTimeoutMs = 30000;

		public const int BleConnectAttemptMs = 80000;

		public const int BleConnectTimeoutMaxMs = 160000;

		public const int BleConnectionRetryDelayMs = 200;

		protected override string LogTag => "TpmsBleDeviceSource";

		public TpmsBleDeviceSource(IBleService bleService, ILogicalDeviceService deviceService)
			: base(bleService, deviceService, "Ids.Accessory.Tpms.Default", TimeSpan.FromMilliseconds(80000.0))
		{
			_bleService = bleService;
		}

		protected override TpmsBleDeviceDriver CreateDeviceBle(SensorConnectionTpms sensorConnection)
		{
			return new TpmsBleDeviceDriver(_bleService, this, sensorConnection);
		}

		public override bool RegisterSensor(Guid bleDeviceId, MAC accessoryMacAddress, string softwarePartNumber, string bleDeviceName)
		{
			return RegisterSensor(new SensorConnectionTpms(bleDeviceName, bleDeviceId, accessoryMacAddress, softwarePartNumber));
		}

		protected override ILogicalDeviceTag CreateLogicalDeviceTag(Guid bleDeviceId, MAC accessoryMacAddress, string softwarePartNumber, string bleDeviceName)
		{
			return new LogicalDeviceTagSourceTpmsBle(bleDeviceId, accessoryMacAddress, softwarePartNumber, bleDeviceName);
		}

		public override Task<IReadOnlyList<byte>> GetAccessoryHistoryDataAsync(ILogicalDevice logicalDevice, byte block, byte startIndex = 0, byte dataLength = byte.MaxValue, CancellationToken cancellationToken = default(CancellationToken))
		{
			return ((GetSensorDevice(logicalDevice) ?? throw new LogicalDeviceHistoryDataException("Error retrieving tpms block data."))!.AccessoryConnectionManager ?? throw new LogicalDeviceHistoryDataException("Error retrieving TPMS block data."))!.GetAccessoryHistoryDataAsync(block, startIndex, dataLength, cancellationToken);
		}
	}
}
