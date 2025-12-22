using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace OneControl.Direct.IdsCanAccessoryBle.ScanResults
{
	public class IdsCanAccessoryScanResultStatisticsByDeviceTracker : IAccessoryScanResultStatisticsTracker
	{
		private readonly ConcurrentDictionary<Guid, IIdsCanAccessoryScanResultStatistics> _statisticsForDeviceDict = new ConcurrentDictionary<Guid, IIdsCanAccessoryScanResultStatistics>();

		public IEnumerable<IIdsCanAccessoryScanResultStatistics> StatisticsByDevice => _statisticsForDeviceDict.Values;

		public IIdsCanAccessoryScanResultStatistics GetStatisticsForDeviceId(Guid deviceId)
		{
			if (!_statisticsForDeviceDict.TryGetValue(deviceId, out var result))
			{
				result = (_statisticsForDeviceDict[deviceId] = new IdsCanAccessoryScanResultStatisticsByDevice(deviceId));
			}
			return result;
		}

		public void Clear()
		{
			foreach (IIdsCanAccessoryScanResultStatistics item in StatisticsByDevice)
			{
				item.Clear();
			}
		}
	}
}
