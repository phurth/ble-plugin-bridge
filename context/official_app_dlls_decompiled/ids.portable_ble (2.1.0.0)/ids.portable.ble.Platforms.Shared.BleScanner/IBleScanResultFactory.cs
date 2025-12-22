using System;
using System.Collections.Generic;
using ids.portable.ble.ScanResults;
using Plugin.BLE.Abstractions;

namespace ids.portable.ble.Platforms.Shared.BleScanner
{
	public interface IBleScanResultFactory<out TKey> where TKey : notnull
	{
		string BleScanResultFactoryName { get; }

		bool RequiresActiveScan { get; }

		TKey BleScanResultKey { get; }

		IBleScanResult? MakeBleScanResult(Guid deviceId, string defaultDeviceName, int rssi, IEnumerable<AdvertisementRecord> advertisementRecords);
	}
}
