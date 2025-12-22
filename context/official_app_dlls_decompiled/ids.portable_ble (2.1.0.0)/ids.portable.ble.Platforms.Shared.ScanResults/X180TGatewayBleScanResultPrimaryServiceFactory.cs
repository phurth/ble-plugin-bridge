using System;
using System.Collections.Generic;
using ids.portable.ble.Platforms.Shared.BleScanner;
using ids.portable.ble.ScanResults;
using Plugin.BLE.Abstractions;

namespace ids.portable.ble.Platforms.Shared.ScanResults
{
	public class X180TGatewayBleScanResultPrimaryServiceFactory : IBleScanResultFactoryPrimaryService, IBleScanResultFactory<Guid>
	{
		public static readonly Guid AccessoryPrimaryService = new Guid("0000000F-0200-A58E-E411-AFE28044E62C");

		public string BleScanResultFactoryName { get; } = "X180TGatewayBleScanResultPrimaryServiceFactory";


		public bool RequiresActiveScan => false;

		public Guid BleScanResultKey => AccessoryPrimaryService;

		public IBleScanResult? MakeBleScanResult(Guid deviceId, string defaultDeviceName, int rssi, IEnumerable<AdvertisementRecord> advertisementRecords)
		{
			return new X180TGatewayScanResult(deviceId, defaultDeviceName, rssi, advertisementRecords);
		}
	}
}
