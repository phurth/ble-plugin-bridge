using System;
using System.Collections.Generic;
using ids.portable.ble.Platforms.Shared.ScanResults;
using Plugin.BLE.Abstractions;

namespace ids.portable.ble.ScanResults
{
	public interface IBleScanResult
	{
		Guid DeviceId { get; }

		string DeviceName { get; }

		DateTime ScannedTimestamp { get; }

		int Rssi { get; }

		Guid? PrimaryServiceGuid { get; }

		BleRequiredAdvertisements HasRequiredAdvertisements { get; }

		byte[] IBeaconAdvertisement { get; }

		byte[] RawManufacturerSpecificData { get; }

		void UpdateScanResult(int rssi, IEnumerable<AdvertisementRecord> advertisementRecords);
	}
}
