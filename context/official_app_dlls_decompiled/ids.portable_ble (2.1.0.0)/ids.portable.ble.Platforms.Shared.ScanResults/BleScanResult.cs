using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ids.portable.ble.Extensions;
using ids.portable.ble.ScanResults;
using IDS.Portable.Common.Extensions;
using Plugin.BLE.Abstractions;

namespace ids.portable.ble.Platforms.Shared.ScanResults
{
	public class BleScanResult : IBleScanResult
	{
		private Guid? _primaryServiceGuid;

		private byte[] _rawManufacturerSpecificData;

		private static string LogTag => "BleScanResult";

		public Guid DeviceId { get; }

		public string DeviceName { get; private set; }

		public bool HasDeviceName => !string.IsNullOrEmpty(DeviceName);

		public DateTime ScannedTimestamp { get; private set; }

		public int Rssi { get; private set; }

		public virtual BleRequiredAdvertisements HasRequiredAdvertisements => BleRequiredAdvertisements.Unknown;

		public virtual Guid? PrimaryServiceGuid => _primaryServiceGuid;

		public byte[] IBeaconAdvertisement { get; private set; }

		public byte[] RawManufacturerSpecificData
		{
			get
			{
				return _rawManufacturerSpecificData;
			}
			private set
			{
				_rawManufacturerSpecificData = value;
				RawManufacturerSpecificDataUpdated(value);
			}
		}

		protected virtual void RawManufacturerSpecificDataUpdated(byte[] manufacturerSpecificData)
		{
		}

		public BleScanResult(Guid deviceId, string defaultDeviceName, int rssi, IEnumerable<AdvertisementRecord> advertisementRecords)
		{
			DeviceId = deviceId;
			DeviceName = defaultDeviceName;
			_primaryServiceGuid = null;
			IBeaconAdvertisement = Array.Empty<byte>();
			_rawManufacturerSpecificData = Array.Empty<byte>();
			UpdateScanResult(rssi, advertisementRecords);
		}

		public void UpdateScanResult(int rssi, IEnumerable<AdvertisementRecord> advertisementRecords)
		{
			ScannedTimestamp = DateTime.Now;
			Rssi = rssi;
			foreach (AdvertisementRecord advertisementRecord in advertisementRecords)
			{
				switch (advertisementRecord.Type)
				{
				case AdvertisementRecordType.ManufacturerSpecificData:
					if (advertisementRecord.IsiBeacon())
					{
						IBeaconAdvertisement = advertisementRecord.Data;
					}
					else
					{
						RawManufacturerSpecificData = advertisementRecord.Data;
					}
					break;
				case AdvertisementRecordType.UuidsIncomplete128Bit:
				case AdvertisementRecordType.UuidsComplete128Bit:
				{
					Guid? primaryServiceGuid = _primaryServiceGuid;
					if (!primaryServiceGuid.HasValue)
					{
						_primaryServiceGuid = advertisementRecord.TryGetPrimaryServiceGuid();
					}
					break;
				}
				case AdvertisementRecordType.ShortLocalName:
				case AdvertisementRecordType.CompleteLocalName:
					DeviceName = advertisementRecord.TryGetDeviceName() ?? DeviceName;
					break;
				}
			}
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler;
			if (!PrimaryServiceGuid.HasValue)
			{
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(59, 5);
				defaultInterpolatedStringHandler.AppendFormatted(DeviceName);
				defaultInterpolatedStringHandler.AppendLiteral("(");
				defaultInterpolatedStringHandler.AppendFormatted(DeviceId);
				defaultInterpolatedStringHandler.AppendLiteral(") RSSI:");
				defaultInterpolatedStringHandler.AppendFormatted(Rssi);
				defaultInterpolatedStringHandler.AppendLiteral(" primaryService:None ");
				defaultInterpolatedStringHandler.AppendFormatted(ScannedTimestamp);
				defaultInterpolatedStringHandler.AppendLiteral(" RawManufacturerSpecificData: ");
				defaultInterpolatedStringHandler.AppendFormatted(RawManufacturerSpecificData.DebugDump());
				return defaultInterpolatedStringHandler.ToStringAndClear();
			}
			defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(76, 9);
			defaultInterpolatedStringHandler.AppendFormatted(GetType().Name);
			defaultInterpolatedStringHandler.AppendLiteral(" ");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceName);
			defaultInterpolatedStringHandler.AppendLiteral("(");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceId);
			defaultInterpolatedStringHandler.AppendLiteral(") RSSI:");
			defaultInterpolatedStringHandler.AppendFormatted(Rssi);
			defaultInterpolatedStringHandler.AppendLiteral(" primaryService:");
			defaultInterpolatedStringHandler.AppendFormatted(PrimaryServiceGuid);
			defaultInterpolatedStringHandler.AppendLiteral(" ");
			defaultInterpolatedStringHandler.AppendFormatted(HasRequiredAdvertisements);
			defaultInterpolatedStringHandler.AppendLiteral(" Scanned: ");
			defaultInterpolatedStringHandler.AppendFormatted(ScannedTimestamp);
			defaultInterpolatedStringHandler.AppendLiteral(" RawManufacturerSpecificData: ");
			defaultInterpolatedStringHandler.AppendFormatted(RawManufacturerSpecificData.DebugDump());
			defaultInterpolatedStringHandler.AppendLiteral(" iBeacon: ");
			defaultInterpolatedStringHandler.AppendFormatted(IBeaconAdvertisement.DebugDump());
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
