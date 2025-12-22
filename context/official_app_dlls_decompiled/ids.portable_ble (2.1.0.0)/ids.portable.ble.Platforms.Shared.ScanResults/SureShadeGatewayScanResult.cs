using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ids.portable.ble.Extensions;
using ids.portable.ble.Platforms.Shared.ManufacturingData;
using ids.portable.ble.ScanResults;
using Plugin.BLE.Abstractions;

namespace ids.portable.ble.Platforms.Shared.ScanResults
{
	public class SureShadeGatewayScanResult : PairableDeviceScanResult, IBleGatewayScanResult, IPairableDeviceScanResult, IBleScanResultWithIdsManufacturingData, IBleScanResult, IKeySeedDeviceScanResult, IConnectionCountScanResult
	{
		public const uint RvLinkKeySeedCypher = 1360062733u;

		private BleCanGatewayProtocolVersion? _gatewayVersion;

		public uint KeySeedCypher { get; } = 1360062733u;


		public override BleRequiredAdvertisements HasRequiredAdvertisements
		{
			get
			{
				switch (base.HasRequiredAdvertisements)
				{
				case BleRequiredAdvertisements.SomeExist:
					return BleRequiredAdvertisements.SomeExist;
				case BleRequiredAdvertisements.NoneExist:
				{
					ref BleCanGatewayProtocolVersion? gatewayVersion2 = ref _gatewayVersion;
					if (!gatewayVersion2.HasValue || !gatewayVersion2.GetValueOrDefault().IsValid)
					{
						BleConnectionCount? connectionCount = ConnectionCount;
						if (!connectionCount.HasValue || !connectionCount.GetValueOrDefault().IsValid)
						{
							return BleRequiredAdvertisements.NoneExist;
						}
					}
					return BleRequiredAdvertisements.SomeExist;
				}
				default:
				{
					ref BleCanGatewayProtocolVersion? gatewayVersion = ref _gatewayVersion;
					if (gatewayVersion.HasValue && gatewayVersion.GetValueOrDefault().IsValid)
					{
						BleConnectionCount? connectionCount = ConnectionCount;
						if (connectionCount.HasValue && connectionCount.GetValueOrDefault().IsValid)
						{
							return BleRequiredAdvertisements.AllExist;
						}
					}
					return BleRequiredAdvertisements.SomeExist;
				}
				}
			}
		}

		public BleGatewayInfo.GatewayVersion AdvertisedGatewayVersion => _gatewayVersion?.GatewayVersion ?? BleGatewayInfo.GatewayVersion.Unknown;

		public BleConnectionCount? ConnectionCount { get; private set; }

		public SureShadeGatewayScanResult(Guid deviceId, string defaultDeviceName, int rssi, IEnumerable<AdvertisementRecord> advertisementRecords)
			: base(deviceId, defaultDeviceName, rssi, advertisementRecords)
		{
		}

		protected override void RawManufacturerSpecificDataUpdated(byte[] manufacturerSpecificData)
		{
			base.RawManufacturerSpecificDataUpdated(manufacturerSpecificData);
			if (manufacturerSpecificData.IsLciManufacturerSpecificData())
			{
				_gatewayVersion = manufacturerSpecificData.TryGetIdsManufacturerSpecificData<BleCanGatewayProtocolVersion>();
				ConnectionCount = manufacturerSpecificData.TryGetIdsManufacturerSpecificData<BleConnectionCount>();
			}
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(35, 3);
			defaultInterpolatedStringHandler.AppendFormatted(base.ToString());
			defaultInterpolatedStringHandler.AppendLiteral(" ConnectionCount: ");
			defaultInterpolatedStringHandler.AppendFormatted(ConnectionCount);
			defaultInterpolatedStringHandler.AppendLiteral(" GatewayVersion: ");
			defaultInterpolatedStringHandler.AppendFormatted(AdvertisedGatewayVersion);
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
