using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common;
using OneControl.Direct.IdsCanAccessoryBle.ScanResults.Statistics;

namespace OneControl.Direct.IdsCanAccessoryBle.ScanResults
{
	public class IdsCanAccessoryScanResultStatisticsByDevice : IIdsCanAccessoryScanResultStatistics
	{
		private readonly ConcurrentDictionary<IdsCanAccessoryMessageType, IdsCanAccessoryStatistics> _statisticsDict = new ConcurrentDictionary<IdsCanAccessoryMessageType, IdsCanAccessoryStatistics>();

		private static string LogTag => "IdsCanAccessoryScanResultStatisticsByDevice";

		public Guid DeviceId { get; }

		public IdsCanAccessoryScanResultStatisticsByDevice(Guid deviceId)
		{
			DeviceId = deviceId;
		}

		public IdsCanAccessoryStatistics? GetStatisticsForMessageType(IdsCanAccessoryMessageType messageType)
		{
			if (_statisticsDict.TryGetValue(messageType, out var result))
			{
				return result;
			}
			return null;
		}

		public TAccessoryStatistics? GetStatisticsForMessageType<TAccessoryStatistics>(IdsCanAccessoryMessageType messageType) where TAccessoryStatistics : IdsCanAccessoryStatistics
		{
			return (GetStatisticsForMessageType(messageType) as TAccessoryStatistics) ?? null;
		}

		public void DebugDumpAllStatistics()
		{
			foreach (IdsCanAccessoryStatistics value in _statisticsDict.Values)
			{
				string logTag = LogTag;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(11, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Stats(");
				defaultInterpolatedStringHandler.AppendFormatted(DeviceId);
				defaultInterpolatedStringHandler.AppendLiteral(") => ");
				defaultInterpolatedStringHandler.AppendFormatted(value.ToString());
				TaggedLog.Debug(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
			}
		}

		public void Clear()
		{
			foreach (IdsCanAccessoryStatistics value in _statisticsDict.Values)
			{
				value.Clear();
			}
		}

		public void UpdateAccessoryStatus(IReadOnlyList<byte> manufacturerSpecificData, IdsCanAccessoryScanResult accessoryScanResult)
		{
			IdsCanAccessoryMessageType idsCanAccessoryMessageType = IdsCanAccessoryMessageType.AccessoryStatus;
			IdsCanAccessoryStatisticsStatus idsCanAccessoryStatisticsStatus = GetStatisticsForMessageType<IdsCanAccessoryStatisticsStatus>(idsCanAccessoryMessageType);
			if (idsCanAccessoryStatisticsStatus == null)
			{
				idsCanAccessoryStatisticsStatus = (IdsCanAccessoryStatisticsStatus)(_statisticsDict[idsCanAccessoryMessageType] = new IdsCanAccessoryStatisticsStatus());
			}
			idsCanAccessoryStatisticsStatus.UpdateScanResultMetadata(manufacturerSpecificData, accessoryScanResult);
			string logTag = LogTag;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(21, 3);
			defaultInterpolatedStringHandler.AppendLiteral("Accessory Stats [");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceId);
			defaultInterpolatedStringHandler.AppendLiteral("/");
			defaultInterpolatedStringHandler.AppendFormatted(accessoryScanResult.AccessoryMacAddress);
			defaultInterpolatedStringHandler.AppendLiteral("]: ");
			defaultInterpolatedStringHandler.AppendFormatted(idsCanAccessoryStatisticsStatus.ToString());
			TaggedLog.Debug(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
		}

		public void UpdateAccessoryConfigStatus(IReadOnlyList<byte> manufacturerSpecificData, IdsCanAccessoryScanResult accessoryScanResult)
		{
			IdsCanAccessoryMessageType idsCanAccessoryMessageType = IdsCanAccessoryMessageType.AccessoryConfigStatus;
			IdsCanAccessoryStatisticsConfigStatus idsCanAccessoryStatisticsConfigStatus = GetStatisticsForMessageType<IdsCanAccessoryStatisticsConfigStatus>(idsCanAccessoryMessageType);
			if (idsCanAccessoryStatisticsConfigStatus == null)
			{
				idsCanAccessoryStatisticsConfigStatus = (IdsCanAccessoryStatisticsConfigStatus)(_statisticsDict[idsCanAccessoryMessageType] = new IdsCanAccessoryStatisticsConfigStatus());
			}
			idsCanAccessoryStatisticsConfigStatus.UpdateScanResultMetadata(manufacturerSpecificData, accessoryScanResult);
			string logTag = LogTag;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(29, 3);
			defaultInterpolatedStringHandler.AppendLiteral("Accessory Config Status [");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceId);
			defaultInterpolatedStringHandler.AppendLiteral("/");
			defaultInterpolatedStringHandler.AppendFormatted(accessoryScanResult.AccessoryMacAddress);
			defaultInterpolatedStringHandler.AppendLiteral("]: ");
			defaultInterpolatedStringHandler.AppendFormatted(idsCanAccessoryStatisticsConfigStatus.ToString());
			TaggedLog.Debug(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
		}

		public void UpdateAccessoryId(IReadOnlyList<byte> manufacturerSpecificData, IdsCanAccessoryScanResult accessoryScanResult)
		{
			IdsCanAccessoryMessageType idsCanAccessoryMessageType = IdsCanAccessoryMessageType.AccessoryId;
			IdsCanAccessoryStatisticsId idsCanAccessoryStatisticsId = GetStatisticsForMessageType<IdsCanAccessoryStatisticsId>(idsCanAccessoryMessageType);
			if (idsCanAccessoryStatisticsId == null)
			{
				idsCanAccessoryStatisticsId = (IdsCanAccessoryStatisticsId)(_statisticsDict[idsCanAccessoryMessageType] = new IdsCanAccessoryStatisticsId());
			}
			idsCanAccessoryStatisticsId.UpdateScanResultMetadata(manufacturerSpecificData, accessoryScanResult);
			string logTag = LogTag;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(18, 3);
			defaultInterpolatedStringHandler.AppendLiteral("Accessory Id [");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceId);
			defaultInterpolatedStringHandler.AppendLiteral("/");
			defaultInterpolatedStringHandler.AppendFormatted(accessoryScanResult.AccessoryMacAddress);
			defaultInterpolatedStringHandler.AppendLiteral("]: ");
			defaultInterpolatedStringHandler.AppendFormatted(idsCanAccessoryStatisticsId.ToString());
			TaggedLog.Debug(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
		}

		public void UpdateExtendedStatus(IReadOnlyList<byte> manufacturerSpecificData, IdsCanAccessoryScanResult accessoryScanResult)
		{
			IdsCanAccessoryMessageType idsCanAccessoryMessageType = IdsCanAccessoryMessageType.IdsCanExtendedStatus;
			IdsCanAccessoryStatisticsExtendedStatus idsCanAccessoryStatisticsExtendedStatus = GetStatisticsForMessageType<IdsCanAccessoryStatisticsExtendedStatus>(idsCanAccessoryMessageType);
			if (idsCanAccessoryStatisticsExtendedStatus == null)
			{
				idsCanAccessoryStatisticsExtendedStatus = (IdsCanAccessoryStatisticsExtendedStatus)(_statisticsDict[idsCanAccessoryMessageType] = new IdsCanAccessoryStatisticsExtendedStatus());
			}
			idsCanAccessoryStatisticsExtendedStatus.UpdateScanResultMetadata(manufacturerSpecificData, accessoryScanResult);
			string logTag = LogTag;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(30, 3);
			defaultInterpolatedStringHandler.AppendLiteral("Accessory Extended Stats [");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceId);
			defaultInterpolatedStringHandler.AppendLiteral("/");
			defaultInterpolatedStringHandler.AppendFormatted(accessoryScanResult.AccessoryMacAddress);
			defaultInterpolatedStringHandler.AppendLiteral("]: ");
			defaultInterpolatedStringHandler.AppendFormatted(idsCanAccessoryStatisticsExtendedStatus.ToString());
			TaggedLog.Debug(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
		}

		public void UpdateExtendedStatusWithEnhancedByte(IReadOnlyList<byte> manufacturerSpecificData, IdsCanAccessoryScanResult accessoryScanResult)
		{
			IdsCanAccessoryMessageType idsCanAccessoryMessageType = IdsCanAccessoryMessageType.ExtendedStatusWithEnhancedByte;
			IdsCanAccessoryStatisticsExtendedStatusEnhanced idsCanAccessoryStatisticsExtendedStatusEnhanced = GetStatisticsForMessageType<IdsCanAccessoryStatisticsExtendedStatusEnhanced>(idsCanAccessoryMessageType);
			if (idsCanAccessoryStatisticsExtendedStatusEnhanced == null)
			{
				idsCanAccessoryStatisticsExtendedStatusEnhanced = (IdsCanAccessoryStatisticsExtendedStatusEnhanced)(_statisticsDict[idsCanAccessoryMessageType] = new IdsCanAccessoryStatisticsExtendedStatusEnhanced());
			}
			idsCanAccessoryStatisticsExtendedStatusEnhanced.UpdateScanResultMetadata(manufacturerSpecificData, accessoryScanResult);
			string logTag = LogTag;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(39, 3);
			defaultInterpolatedStringHandler.AppendLiteral("Accessory Extended Enhanced Stats [");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceId);
			defaultInterpolatedStringHandler.AppendLiteral("/");
			defaultInterpolatedStringHandler.AppendFormatted(accessoryScanResult.AccessoryMacAddress);
			defaultInterpolatedStringHandler.AppendLiteral("]: ");
			defaultInterpolatedStringHandler.AppendFormatted(idsCanAccessoryStatisticsExtendedStatusEnhanced.ToString());
			TaggedLog.Debug(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
		}

		public void UpdateAccessoryAbridgedStatus(IReadOnlyList<byte> manufacturerSpecificData, IdsCanAccessoryScanResult accessoryScanResult)
		{
			IdsCanAccessoryMessageType idsCanAccessoryMessageType = IdsCanAccessoryMessageType.AccessoryAbridgedStatus;
			IdsCanAccessoryStatisticsAbridgedStatus idsCanAccessoryStatisticsAbridgedStatus = GetStatisticsForMessageType<IdsCanAccessoryStatisticsAbridgedStatus>(idsCanAccessoryMessageType);
			if (idsCanAccessoryStatisticsAbridgedStatus == null)
			{
				idsCanAccessoryStatisticsAbridgedStatus = (IdsCanAccessoryStatisticsAbridgedStatus)(_statisticsDict[idsCanAccessoryMessageType] = new IdsCanAccessoryStatisticsAbridgedStatus());
			}
			idsCanAccessoryStatisticsAbridgedStatus.UpdateScanResultMetadata(manufacturerSpecificData, accessoryScanResult);
			string logTag = LogTag;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(31, 3);
			defaultInterpolatedStringHandler.AppendLiteral("Accessory Abridged Status [");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceId);
			defaultInterpolatedStringHandler.AppendLiteral("/");
			defaultInterpolatedStringHandler.AppendFormatted(accessoryScanResult.AccessoryMacAddress);
			defaultInterpolatedStringHandler.AppendLiteral("]: ");
			defaultInterpolatedStringHandler.AppendFormatted(idsCanAccessoryStatisticsAbridgedStatus.ToString());
			TaggedLog.Debug(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
		}

		public void UpdateAccessoryVersion(IReadOnlyList<byte> manufacturerSpecificData, IdsCanAccessoryScanResult accessoryScanResult)
		{
			IdsCanAccessoryMessageType idsCanAccessoryMessageType = IdsCanAccessoryMessageType.VersionInfo;
			IdsCanAccessoryStatisticsVersion idsCanAccessoryStatisticsVersion = GetStatisticsForMessageType<IdsCanAccessoryStatisticsVersion>(idsCanAccessoryMessageType);
			if (idsCanAccessoryStatisticsVersion == null)
			{
				idsCanAccessoryStatisticsVersion = (IdsCanAccessoryStatisticsVersion)(_statisticsDict[idsCanAccessoryMessageType] = new IdsCanAccessoryStatisticsVersion());
			}
			idsCanAccessoryStatisticsVersion.UpdateScanResultMetadata(manufacturerSpecificData, accessoryScanResult);
			string logTag = LogTag;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(23, 3);
			defaultInterpolatedStringHandler.AppendLiteral("Accessory Version [");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceId);
			defaultInterpolatedStringHandler.AppendLiteral("/");
			defaultInterpolatedStringHandler.AppendFormatted(accessoryScanResult.AccessoryMacAddress);
			defaultInterpolatedStringHandler.AppendLiteral("]: ");
			defaultInterpolatedStringHandler.AppendFormatted(idsCanAccessoryStatisticsVersion.ToString());
			TaggedLog.Debug(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
		}

		public void UpdateAccessoryInvalid(IReadOnlyList<byte> manufacturerSpecificData, IdsCanAccessoryScanResult accessoryScanResult)
		{
			IdsCanAccessoryMessageType idsCanAccessoryMessageType = IdsCanAccessoryMessageType.Invalid;
			IdsCanAccessoryStatisticsInvalid idsCanAccessoryStatisticsInvalid = GetStatisticsForMessageType<IdsCanAccessoryStatisticsInvalid>(idsCanAccessoryMessageType);
			if (idsCanAccessoryStatisticsInvalid == null)
			{
				idsCanAccessoryStatisticsInvalid = (IdsCanAccessoryStatisticsInvalid)(_statisticsDict[idsCanAccessoryMessageType] = new IdsCanAccessoryStatisticsInvalid());
			}
			idsCanAccessoryStatisticsInvalid.UpdateScanResultMetadata(manufacturerSpecificData, accessoryScanResult);
			string logTag = LogTag;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(23, 3);
			defaultInterpolatedStringHandler.AppendLiteral("Accessory Version [");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceId);
			defaultInterpolatedStringHandler.AppendLiteral("/");
			defaultInterpolatedStringHandler.AppendFormatted(accessoryScanResult.AccessoryMacAddress);
			defaultInterpolatedStringHandler.AppendLiteral("]: ");
			defaultInterpolatedStringHandler.AppendFormatted(idsCanAccessoryStatisticsInvalid.ToString());
			TaggedLog.Debug(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
		}
	}
}
