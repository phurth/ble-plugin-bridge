using System;
using System.Collections.Generic;
using ids.portable.ble.Extensions;
using ids.portable.ble.Platforms.Shared.ManufacturingData;
using ids.portable.ble.ScanResults;
using Plugin.BLE.Abstractions;

namespace ids.portable.ble.Platforms.Shared.ScanResults
{
	public class X180TGatewayScanResult : PairableDeviceScanResult, IX180TGatewayScanResult, IBleGatewayScanResult, IPairableDeviceScanResult, IBleScanResultWithIdsManufacturingData, IBleScanResult, IKeySeedDeviceScanResult
	{
		public const uint X180TKeySeedCypher = 3357376288u;

		private BleCanGatewayProtocolVersion? _gatewayVersion;

		public uint KeySeedCypher { get; } = 3357376288u;


		public override BleRequiredAdvertisements HasRequiredAdvertisements
		{
			get
			{
				if (base.HasRequiredAdvertisements == BleRequiredAdvertisements.AllExist)
				{
					ref BleCanGatewayProtocolVersion? gatewayVersion = ref _gatewayVersion;
					if (!gatewayVersion.HasValue || !gatewayVersion.GetValueOrDefault().IsValid)
					{
						return BleRequiredAdvertisements.NoneExist;
					}
					return BleRequiredAdvertisements.AllExist;
				}
				ref BleCanGatewayProtocolVersion? gatewayVersion2 = ref _gatewayVersion;
				if (!gatewayVersion2.HasValue || !gatewayVersion2.GetValueOrDefault().IsValid)
				{
					return base.HasRequiredAdvertisements;
				}
				return BleRequiredAdvertisements.NoneExist;
			}
		}

		public BleGatewayInfo.GatewayVersion AdvertisedGatewayVersion => _gatewayVersion?.GatewayVersion ?? BleGatewayInfo.GatewayVersion.Unknown;

		public X180TGatewayScanResult(Guid deviceId, string defaultDeviceName, int rssi, IEnumerable<AdvertisementRecord> advertisementRecords)
			: base(deviceId, defaultDeviceName, rssi, advertisementRecords)
		{
		}

		protected override void RawManufacturerSpecificDataUpdated(byte[] manufacturerSpecificData)
		{
			base.RawManufacturerSpecificDataUpdated(manufacturerSpecificData);
			if (manufacturerSpecificData.IsLciManufacturerSpecificData())
			{
				_gatewayVersion = manufacturerSpecificData.TryGetIdsManufacturerSpecificData<BleCanGatewayProtocolVersion>();
			}
		}
	}
}
