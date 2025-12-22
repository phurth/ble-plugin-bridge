using System;
using System.Collections.Generic;
using ids.portable.ble.Platforms.Shared.ScanResults;
using ids.portable.ble.ScanResults;
using Plugin.BLE.Abstractions;

namespace OneControl.Direct.IdsCanAccessoryBle.EchoBrakeControl
{
	public class EchoBrakeControlScanResult : BleScanResult, IEchoBrakeControlScanResult, IBleScanResult
	{
		public EchoBrakeControlScanResult(Guid deviceId, string defaultDeviceName, int rssi, IEnumerable<AdvertisementRecord> advertisementRecords)
			: base(deviceId, defaultDeviceName, rssi, advertisementRecords)
		{
		}
	}
}
