using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ids.portable.ble.Extensions;
using ids.portable.ble.Platforms.Shared.ScanResults;
using Plugin.BLE.Abstractions;

namespace ids.portable.ble.ScanResults
{
	public class BleGatewayScanResult : BleScanResult, IBleGatewayScanResult, IPairableDeviceScanResult, IBleScanResultWithIdsManufacturingData, IBleScanResult
	{
		private static readonly byte[] LippertCompanyId = new byte[2] { 199, 5 };

		private const int ManufacturingAdvertisementLippertCompanyIdStartIndex = 0;

		private const int ManufacturingAdvertisementPartNumberStartIndex = 2;

		private const int ManufacturingAdvertisementMinimumSize = 6;

		public PairingMethod PairingMethod { get; } = PairingMethod.None;


		public bool PairingEnabled { get; }

		public BleGatewayInfo.GatewayVersion AdvertisedGatewayVersion { get; private set; }

		public BleGatewayScanResult(Guid deviceId, string defaultDeviceName, int rssi, IEnumerable<AdvertisementRecord> advertisementRecords)
			: base(deviceId, defaultDeviceName, rssi, advertisementRecords)
		{
		}

		protected override void RawManufacturerSpecificDataUpdated(byte[] manufacturerSpecificData)
		{
			base.RawManufacturerSpecificDataUpdated(manufacturerSpecificData);
			AdvertisedGatewayVersion = GetGatewayVersionViaAdvertisement(manufacturerSpecificData);
		}

		public BleGatewayInfo.GatewayVersion GetGatewayVersionViaAdvertisement(byte[] manufacturerSpecificData)
		{
			if (manufacturerSpecificData.Length < 6)
			{
				return BleGatewayInfo.GatewayVersion.Unknown;
			}
			if (manufacturerSpecificData[0] != LippertCompanyId[0] || manufacturerSpecificData[1] != LippertCompanyId[1])
			{
				return BleGatewayInfo.GatewayVersion.Unknown;
			}
			int partNumber = (manufacturerSpecificData[2] & 0xF) * 10000 + ((manufacturerSpecificData[3] & 0xF0) >> 4) * 1000 + (manufacturerSpecificData[3] & 0xF) * 100 + ((manufacturerSpecificData[4] & 0xF0) >> 4) * 10 + (manufacturerSpecificData[4] & 0xF);
			char rev = (char)manufacturerSpecificData[5];
			return partNumber.GetGatewayVersion(rev);
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(1, 2);
			defaultInterpolatedStringHandler.AppendFormatted(base.ToString());
			defaultInterpolatedStringHandler.AppendLiteral(" ");
			defaultInterpolatedStringHandler.AppendFormatted(AdvertisedGatewayVersion);
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
