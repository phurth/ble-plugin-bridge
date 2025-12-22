using System;

namespace OneControl.Direct.IdsCanAccessoryBle.ScanResults
{
	public interface IAccessoryScanResultStatisticsTracker
	{
		IIdsCanAccessoryScanResultStatistics GetStatisticsForDeviceId(Guid deviceId);
	}
}
