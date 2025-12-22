using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using ids.portable.ble.Platforms.Shared.ManufacturingData;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;
using Plugin.BLE.Abstractions;

namespace ids.portable.ble.Extensions
{
	public static class AdvertisementRecordExtensions
	{
		public const string LogTag = "AdvertisementRecordExtensions";

		private const int IBeaconIdentifier = 1275068949;

		private const string PrimaryServiceGuidShortPrefix = "-0000-1000-8000-00805F9B34FB";

		public const ushort LippertCompanyIdentifier = 1479;

		public const int CompanyIdSize = 2;

		public const int CompanyIdIndex = 0;

		public static bool IsiBeacon(this AdvertisementRecord? advertisementRecord)
		{
			if (advertisementRecord == null)
			{
				return false;
			}
			if (advertisementRecord!.Type != AdvertisementRecordType.ManufacturerSpecificData)
			{
				return false;
			}
			if (advertisementRecord!.Data.Length != 25)
			{
				return false;
			}
			if (advertisementRecord!.Data.GetValueInt32(0) != 1275068949)
			{
				return false;
			}
			return true;
		}

		public static ushort? GetFirstManufacturerId(this IEnumerable<AdvertisementRecord> advertisementRecords)
		{
			foreach (AdvertisementRecord advertisementRecord in advertisementRecords)
			{
				try
				{
					if (advertisementRecord.Type != AdvertisementRecordType.ManufacturerSpecificData || advertisementRecord.Data.Length < 2 || advertisementRecord.IsiBeacon())
					{
						continue;
					}
					return advertisementRecord.Data.GetValueUInt16(0, ArrayExtension.Endian.Little);
				}
				catch
				{
				}
			}
			return null;
		}

		public static TIdsManufacturerSpecificData GetIdsManufacturerSpecificDataOrDefault<TIdsManufacturerSpecificData>(this byte[] manufacturerSpecificData) where TIdsManufacturerSpecificData : struct, IIdsManufacturerSpecificData
		{
			try
			{
				return Enumerable.First(Enumerable.OfType<TIdsManufacturerSpecificData>(manufacturerSpecificData.ParseLciManufacturerSpecificData()));
			}
			catch
			{
				return default(TIdsManufacturerSpecificData);
			}
		}

		public static TIdsManufacturerSpecificData? TryGetIdsManufacturerSpecificData<TIdsManufacturerSpecificData>(this byte[] manufacturerSpecificData) where TIdsManufacturerSpecificData : struct, IIdsManufacturerSpecificData
		{
			try
			{
				return Enumerable.First(Enumerable.OfType<TIdsManufacturerSpecificData>(manufacturerSpecificData.ParseLciManufacturerSpecificData()));
			}
			catch
			{
				return null;
			}
		}

		public static string? TryGetDeviceName(this AdvertisementRecord advertisementRecord)
		{
			if (advertisementRecord == null)
			{
				return null;
			}
			try
			{
				if (advertisementRecord.Type != AdvertisementRecordType.CompleteLocalName && advertisementRecord.Type != AdvertisementRecordType.ShortLocalName)
				{
					return null;
				}
				return Encoding.UTF8.GetString(advertisementRecord.Data, 0, advertisementRecord.Data.Length) ?? throw new ArgumentNullException();
			}
			catch
			{
				return null;
			}
		}

		public static Guid MakePrimaryServiceGuid(ushort shortServiceId)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 2);
			defaultInterpolatedStringHandler.AppendFormatted(shortServiceId, "X8");
			defaultInterpolatedStringHandler.AppendFormatted("-0000-1000-8000-00805F9B34FB");
			return new Guid(defaultInterpolatedStringHandler.ToStringAndClear());
		}

		public static Guid? TryGetPrimaryServiceGuid(this IEnumerable<AdvertisementRecord> advertisementRecords)
		{
			foreach (AdvertisementRecord advertisementRecord in advertisementRecords)
			{
				try
				{
					switch (advertisementRecord.Type)
					{
					case AdvertisementRecordType.UuidsIncomplete128Bit:
					case AdvertisementRecordType.UuidsComplete128Bit:
						return advertisementRecord.TryGetPrimaryServiceGuid();
					case AdvertisementRecordType.UuidsComplete16Bit:
						if (advertisementRecord.Data.Length != 2)
						{
							break;
						}
						return MakePrimaryServiceGuid(advertisementRecord.Data.GetValueUInt16(0));
					}
				}
				catch (Exception ex)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(50, 3);
					defaultInterpolatedStringHandler.AppendLiteral("Error trying to get primary advertisement guid ");
					defaultInterpolatedStringHandler.AppendFormatted(advertisementRecord.Type);
					defaultInterpolatedStringHandler.AppendLiteral(": ");
					defaultInterpolatedStringHandler.AppendFormatted(advertisementRecord.Data.DebugDump());
					defaultInterpolatedStringHandler.AppendLiteral(" ");
					defaultInterpolatedStringHandler.AppendFormatted(ex.Message);
					TaggedLog.Error("AdvertisementRecordExtensions", defaultInterpolatedStringHandler.ToStringAndClear());
					return null;
				}
			}
			return null;
		}

		public static Guid? TryGetPrimaryServiceGuid(this AdvertisementRecord advertisementRecord)
		{
			if (advertisementRecord == null)
			{
				return null;
			}
			try
			{
				if (advertisementRecord.Type != AdvertisementRecordType.UuidsComplete128Bit && advertisementRecord.Type != AdvertisementRecordType.UuidsIncomplete128Bit)
				{
					return null;
				}
				if (advertisementRecord.Data.Length != 16)
				{
					return null;
				}
				return advertisementRecord.Data.ToGuid(0);
			}
			catch (Exception)
			{
				return null;
			}
		}

		public static bool IsLciManufacturerSpecificData(this byte[] manufacturerSpecificData)
		{
			if (manufacturerSpecificData.Length < 2)
			{
				return false;
			}
			if (manufacturerSpecificData.GetValueUInt16(0, ArrayExtension.Endian.Little) != 1479)
			{
				return false;
			}
			return true;
		}
	}
}
