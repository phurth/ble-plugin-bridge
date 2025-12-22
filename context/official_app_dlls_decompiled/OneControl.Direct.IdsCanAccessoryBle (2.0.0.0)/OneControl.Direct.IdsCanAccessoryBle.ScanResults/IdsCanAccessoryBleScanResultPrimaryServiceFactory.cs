using System;
using System.Collections.Generic;
using ids.portable.ble.Ble;
using ids.portable.ble.Platforms.Shared.BleScanner;
using ids.portable.ble.ScanResults;
using Plugin.BLE.Abstractions;

namespace OneControl.Direct.IdsCanAccessoryBle.ScanResults
{
	public class IdsCanAccessoryBleScanResultPrimaryServiceFactory : IBleScanResultFactoryPrimaryService, IBleScanResultFactory<Guid>
	{
		private readonly IBleService _bleService;

		public static readonly Guid AccessoryPrimaryService = new Guid("20890000-62e8-4795-9377-b44229c80329");

		public static readonly Guid KeySeedExchangeServiceGuidDefault = AccessoryPrimaryService;

		public static readonly Guid SeedCharacteristicGuidDefault = Guid.Parse("20890007-62e8-4795-9377-b44229c80329");

		public static readonly Guid KeyCharacteristicGuidDefault = Guid.Parse("20890008-62e8-4795-9377-b44229c80329");

		public static readonly Guid PidWriteCharacteristic = Guid.Parse("20890004-62e8-4795-9377-b44229c80329");

		public static readonly Guid ReadHistoryDataCharacteristic = Guid.Parse("20890006-62e8-4795-9377-b44229c80329");

		public static readonly Guid WriteHistoryDataCharacteristic = Guid.Parse("20890005-62e8-4795-9377-b44229c80329");

		public static readonly Guid ReadSoftwarePartNumberCharacteristic = Guid.Parse("20890003-62e8-4795-9377-b44229c80329");

		public static readonly Guid BleOtaService = new Guid("aa950000-f2ac-4b5d-88d4-16d013eac74d");

		public static readonly Guid UnlockGetSeedCharacteristic = Guid.Parse("aa950007-f2ac-4b5d-88d4-16d013eac74d");

		public static readonly Guid UnlockWriteKeyCharacteristic = Guid.Parse("aa950008-f2ac-4b5d-88d4-16d013eac74d");

		public static readonly Guid BlockPropertiesCharacteristic = Guid.Parse("aa950010-f2ac-4b5d-88d4-16d013eac74d");

		public static readonly Guid BlockBeginTransferCharacteristic = Guid.Parse("aa950011-f2ac-4b5d-88d4-16d013eac74d");

		public static readonly Guid WriteBulkTransferDataCharacteristic = Guid.Parse("aa950012-f2ac-4b5d-88d4-16d013eac74d");

		public static readonly Guid EndTransferCrcCheckCharacteristic = Guid.Parse("aa950013-f2ac-4b5d-88d4-16d013eac74d");

		public static readonly Guid GetCrcCharacteristic = Guid.Parse("aa950014-f2ac-4b5d-88d4-16d013eac74d");

		public static readonly Guid OtaRebootCharacteristic = Guid.Parse("aa950015-f2ac-4b5d-88d4-16d013eac74d");

		public string BleScanResultFactoryName { get; } = "IdsCanAccessoryScanResult";


		public bool RequiresActiveScan => false;

		public Guid BleScanResultKey => AccessoryPrimaryService;

		public IBleScanResult? MakeBleScanResult(Guid deviceId, string defaultDeviceName, int rssi, IEnumerable<AdvertisementRecord> advertisementRecords)
		{
			return new IdsCanAccessoryScanResult(_bleService, deviceId, defaultDeviceName, rssi, advertisementRecords);
		}

		public IdsCanAccessoryBleScanResultPrimaryServiceFactory(IBleService bleService)
		{
			_bleService = bleService;
		}
	}
}
