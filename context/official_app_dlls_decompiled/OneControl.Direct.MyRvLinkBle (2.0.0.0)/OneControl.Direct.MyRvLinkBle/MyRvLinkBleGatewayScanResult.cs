using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ids.portable.ble.Extensions;
using ids.portable.ble.Platforms.Shared.ManufacturingData;
using ids.portable.ble.Platforms.Shared.ScanResults;
using ids.portable.ble.ScanResults;
using Plugin.BLE.Abstractions;

namespace OneControl.Direct.MyRvLinkBle
{
	public class MyRvLinkBleGatewayScanResult : PairableDeviceScanResult, IMyRvLinkBleGatewayScanResult, IPairableDeviceScanResult, IBleScanResultWithIdsManufacturingData, IBleScanResult, IKeySeedDeviceScanResult, IConnectionCountScanResult
	{
		public struct RawAlertData
		{
			public byte AlertSize { get; }

			public byte AlertCommand { get; }

			public byte RvLinkDeviceId { get; }

			public byte RvLinkTableId { get; }

			public byte AlertId { get; }

			public byte AlertData { get; }

			public RawAlertData(byte alertSize, byte alertCommand, byte rvLinkDeviceId, byte rvLinkTableId, byte alertId, byte alertData)
			{
				AlertSize = alertSize;
				AlertCommand = alertCommand;
				RvLinkDeviceId = rvLinkDeviceId;
				RvLinkTableId = rvLinkTableId;
				AlertId = alertId;
				AlertData = alertData;
			}
		}

		public const string AntiLockBrakingNamePrefix = "LCIABS";

		public const string SwayBrakingNamePrefix = "LCISWAY";

		public const uint RvLinkKeySeedCypher = 612643285u;

		private byte[] _lastAlertRawData = Array.Empty<byte>();

		private readonly int AlertByteSize = 8;

		public int AlertSizeIndex = 2;

		public int AlertCommandIndex = 3;

		public int RvLinkDeviceIdIndex = 4;

		public int TableIdIndex = 5;

		public int AlertIdIndex = 6;

		public int AlertDataIndex = 7;

		public uint KeySeedCypher { get; } = 612643285u;


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
					BleConnectionCount? connectionCount = ConnectionCount;
					if (!connectionCount.HasValue || !connectionCount.GetValueOrDefault().IsValid)
					{
						return BleRequiredAdvertisements.NoneExist;
					}
					return BleRequiredAdvertisements.SomeExist;
				}
				default:
				{
					BleConnectionCount? connectionCount = ConnectionCount;
					if (!connectionCount.HasValue || !connectionCount.GetValueOrDefault().IsValid)
					{
						return BleRequiredAdvertisements.SomeExist;
					}
					return BleRequiredAdvertisements.AllExist;
				}
				}
			}
		}

		public BleConnectionCount? ConnectionCount { get; private set; }

		public RvLinkGatewayType GatewayType
		{
			get
			{
				if (!base.HasDeviceName)
				{
					return RvLinkGatewayType.Unknown;
				}
				return GatewayTypeFromDeviceName(base.DeviceName);
			}
		}

		public static RvLinkGatewayType GatewayTypeFromDeviceName(string deviceName)
		{
			if (deviceName.StartsWith("LCIABS", StringComparison.OrdinalIgnoreCase))
			{
				return RvLinkGatewayType.AntiLockBraking;
			}
			if (deviceName.StartsWith("LCISWAY", StringComparison.OrdinalIgnoreCase))
			{
				return RvLinkGatewayType.Sway;
			}
			return RvLinkGatewayType.Gateway;
		}

		public MyRvLinkBleGatewayScanResult(Guid deviceId, string defaultDeviceName, int rssi, IEnumerable<AdvertisementRecord> advertisementRecords)
			: base(deviceId, defaultDeviceName, rssi, advertisementRecords)
		{
		}

		protected override void RawManufacturerSpecificDataUpdated(byte[] manufacturerSpecificData)
		{
			base.RawManufacturerSpecificDataUpdated(manufacturerSpecificData);
			if (manufacturerSpecificData.IsLciManufacturerSpecificData())
			{
				if (manufacturerSpecificData.Length == AlertByteSize)
				{
					_lastAlertRawData = manufacturerSpecificData;
				}
				ConnectionCount = manufacturerSpecificData.TryGetIdsManufacturerSpecificData<BleConnectionCount>();
			}
		}

		public RawAlertData? GetAlertStatus()
		{
			if (_lastAlertRawData.Length == AlertByteSize)
			{
				return new RawAlertData(_lastAlertRawData[AlertSizeIndex], _lastAlertRawData[AlertCommandIndex], _lastAlertRawData[RvLinkDeviceIdIndex], _lastAlertRawData[TableIdIndex], _lastAlertRawData[AlertIdIndex], _lastAlertRawData[AlertDataIndex]);
			}
			return null;
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(18, 2);
			defaultInterpolatedStringHandler.AppendFormatted(base.ToString());
			defaultInterpolatedStringHandler.AppendLiteral(" ConnectionCount: ");
			defaultInterpolatedStringHandler.AppendFormatted(ConnectionCount);
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
