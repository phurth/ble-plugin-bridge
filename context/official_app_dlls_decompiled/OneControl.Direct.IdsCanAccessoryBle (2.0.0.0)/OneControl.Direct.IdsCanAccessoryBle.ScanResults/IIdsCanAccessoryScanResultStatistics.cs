using System;
using System.Collections.Generic;
using OneControl.Direct.IdsCanAccessoryBle.ScanResults.Statistics;

namespace OneControl.Direct.IdsCanAccessoryBle.ScanResults
{
	public interface IIdsCanAccessoryScanResultStatistics
	{
		Guid DeviceId { get; }

		IdsCanAccessoryStatistics? GetStatisticsForMessageType(IdsCanAccessoryMessageType messageType);

		TAccessoryStatistics? GetStatisticsForMessageType<TAccessoryStatistics>(IdsCanAccessoryMessageType messageType) where TAccessoryStatistics : IdsCanAccessoryStatistics;

		void UpdateAccessoryStatus(IReadOnlyList<byte> manufacturerSpecificData, IdsCanAccessoryScanResult accessoryScanResult);

		void UpdateAccessoryConfigStatus(IReadOnlyList<byte> manufacturerSpecificData, IdsCanAccessoryScanResult accessoryScanResult);

		void UpdateAccessoryId(IReadOnlyList<byte> manufacturerSpecificData, IdsCanAccessoryScanResult accessoryScanResult);

		void UpdateExtendedStatus(IReadOnlyList<byte> manufacturerSpecificData, IdsCanAccessoryScanResult accessoryScanResult);

		void UpdateExtendedStatusWithEnhancedByte(IReadOnlyList<byte> manufacturerSpecificData, IdsCanAccessoryScanResult accessoryScanResult);

		void UpdateAccessoryAbridgedStatus(IReadOnlyList<byte> manufacturerSpecificData, IdsCanAccessoryScanResult accessoryScanResult);

		void UpdateAccessoryVersion(IReadOnlyList<byte> manufacturerSpecificData, IdsCanAccessoryScanResult accessoryScanResult);

		void UpdateAccessoryInvalid(IReadOnlyList<byte> manufacturerSpecificData, IdsCanAccessoryScanResult accessoryScanResult);

		void DebugDumpAllStatistics();

		void Clear();
	}
}
