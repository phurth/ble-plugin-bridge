using System;
using System.Collections.Generic;
using ids.portable.ble.Platforms.Shared.BleScanner;
using ids.portable.ble.ScanResults;
using Plugin.BLE.Abstractions;

namespace OneControl.Direct.MyRvLinkBle
{
	public class MyRvLinkGatewayBleScanResultPrimaryServiceFactory : IBleScanResultFactoryPrimaryService, IBleScanResultFactory<Guid>
	{
		public static readonly Guid AccessoryPrimaryService = new Guid("00000041-0200-A58E-E411-AFE28044E62C");

		public string BleScanResultFactoryName { get; } = "MyRvLinkGatewayBleScanResultPrimaryServiceFactory";


		public bool RequiresActiveScan => false;

		public Guid BleScanResultKey => AccessoryPrimaryService;

		public IBleScanResult? MakeBleScanResult(Guid deviceId, string defaultDeviceName, int rssi, IEnumerable<AdvertisementRecord> advertisementRecords)
		{
			return new MyRvLinkBleGatewayScanResult(deviceId, defaultDeviceName, rssi, advertisementRecords);
		}
	}
}
