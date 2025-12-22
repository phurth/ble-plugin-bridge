using System;
using System.Collections.Generic;
using ids.portable.ble.Platforms.Shared.BleScanner;
using ids.portable.ble.ScanResults;
using Plugin.BLE.Abstractions;

namespace ids.portable.ble.Platforms.Shared.ScanResults
{
	public class SureShadeGatewayBleScanResultPrimaryServiceFactory : IBleScanResultFactoryPrimaryService, IBleScanResultFactory<Guid>
	{
		public static readonly Guid AccessoryPrimaryService = new Guid("00000040-0200-A58E-E411-AFE28044E62C");

		public string BleScanResultFactoryName { get; } = "SureShadeGatewayBleScanResultPrimaryServiceFactory";


		public bool RequiresActiveScan => false;

		public Guid BleScanResultKey => AccessoryPrimaryService;

		public IBleScanResult? MakeBleScanResult(Guid deviceId, string defaultDeviceName, int rssi, IEnumerable<AdvertisementRecord> advertisementRecords)
		{
			return new SureShadeGatewayScanResult(deviceId, defaultDeviceName, rssi, advertisementRecords);
		}
	}
}
