using System;
using System.Collections.Generic;
using ids.portable.ble.Extensions;
using ids.portable.ble.Platforms.Shared.BleScanner;
using ids.portable.ble.ScanResults;
using Plugin.BLE.Abstractions;

namespace OneControl.Direct.IdsCanAccessoryBle.EchoBrakeControl
{
	public class EchoBrakeControlScanResultFactory : IBleScanResultFactoryPrimaryService, IBleScanResultFactory<Guid>
	{
		public const ushort BleShortPrimaryServiceId = 6154;

		public string BleScanResultFactoryName => "EchoBrakeControlScanResultFactory";

		public bool RequiresActiveScan => false;

		public Guid BleScanResultKey { get; } = AdvertisementRecordExtensions.MakePrimaryServiceGuid(6154);


		public IBleScanResult? MakeBleScanResult(Guid deviceId, string defaultDeviceName, int rssi, IEnumerable<AdvertisementRecord> advertisementRecords)
		{
			return new EchoBrakeControlScanResult(deviceId, defaultDeviceName, rssi, advertisementRecords);
		}
	}
}
