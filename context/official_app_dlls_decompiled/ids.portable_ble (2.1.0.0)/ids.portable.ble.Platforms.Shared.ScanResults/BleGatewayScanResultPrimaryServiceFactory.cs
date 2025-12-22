using System;
using System.Collections.Generic;
using ids.portable.ble.Platforms.Shared.BleScanner;
using ids.portable.ble.ScanResults;
using Plugin.BLE.Abstractions;

namespace ids.portable.ble.Platforms.Shared.ScanResults
{
	public class BleGatewayScanResultPrimaryServiceFactory : IBleScanResultFactoryPrimaryService, IBleScanResultFactory<Guid>
	{
		public static readonly Guid AccessoryPrimaryService = new Guid("00000000-0200-A58E-E411-AFE28044E62C");

		public string BleScanResultFactoryName { get; } = "BleGatewayScanResultPrimaryServiceFactory";


		public bool RequiresActiveScan => false;

		public Guid BleScanResultKey => AccessoryPrimaryService;

		public IBleScanResult? MakeBleScanResult(Guid deviceId, string defaultDeviceName, int rssi, IEnumerable<AdvertisementRecord> advertisementRecords)
		{
			return new BleGatewayScanResult(deviceId, defaultDeviceName, rssi, advertisementRecords);
		}
	}
}
