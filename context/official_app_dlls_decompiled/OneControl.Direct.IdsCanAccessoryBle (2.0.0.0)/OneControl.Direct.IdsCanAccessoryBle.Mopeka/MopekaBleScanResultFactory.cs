using System;
using System.Collections.Generic;
using ids.portable.ble.Platforms.Shared.BleScanner;
using ids.portable.ble.ScanResults;
using Plugin.BLE.Abstractions;

namespace OneControl.Direct.IdsCanAccessoryBle.Mopeka
{
	public class MopekaBleScanResultFactory : IBleScanResultFactoryManufacturerId, IBleScanResultFactory<ushort>
	{
		public string BleScanResultFactoryName => "MopekaBleScanResultFactory";

		public bool RequiresActiveScan => false;

		public ushort BleScanResultKey => 89;

		IEnumerable<Guid>? IBleScanResultFactoryManufacturerId.PrimaryServiceUuids => new Guid[2]
		{
			new Guid("0000fee5-0000-1000-8000-00805f9b34fb"),
			new Guid("0000ada0-0000-1000-8000-00805f9b34fb")
		};

		public IBleScanResult? MakeBleScanResult(Guid deviceId, string defaultDeviceName, int rssi, IEnumerable<AdvertisementRecord> advertisementRecords)
		{
			return new MopekaScanResult(deviceId, defaultDeviceName, rssi, advertisementRecords);
		}
	}
}
