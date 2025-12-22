using System;
using System.Collections.Generic;
using ids.portable.ble.Ble;
using ids.portable.ble.Platforms.Shared.BleScanner;
using ids.portable.ble.ScanResults;
using IDS.Portable.Common;
using Plugin.BLE.Abstractions;

namespace OneControl.Direct.IdsCanAccessoryBle.ScanResults
{
	public abstract class IdsCanAccessoryBleScanResultDeviceIdFactory : CommonDisposable, IBleScanResultFactoryDeviceId, IBleScanResultFactory<Guid>
	{
		private readonly IBleService _bleService;

		private const string LogTag = "IdsCanAccessoryBleScanResultDeviceIdFactory";

		public string BleScanResultFactoryName { get; } = "IdsCanAccessoryScanResult";


		public bool RequiresActiveScan => false;

		public Guid BleScanResultKey { get; }

		protected IdsCanAccessoryBleScanResultDeviceIdFactory(IBleService bleService, Guid deviceId)
		{
			_bleService = bleService;
			BleScanResultKey = deviceId;
		}

		public IBleScanResult? MakeBleScanResult(Guid deviceId, string defaultDeviceName, int rssi, IEnumerable<AdvertisementRecord> advertisementRecords)
		{
			if (deviceId != BleScanResultKey)
			{
				TaggedLog.Error("IdsCanAccessoryBleScanResultDeviceIdFactory", "Failed to produce scan result because device ID does not match.");
				return null;
			}
			return new IdsCanAccessoryScanResult(_bleService, deviceId, defaultDeviceName, rssi, advertisementRecords);
		}
	}
}
