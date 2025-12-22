using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceSourceDirectDtc : ILogicalDeviceSourceDirect, ILogicalDeviceSource
	{
		Task<IReadOnlyDictionary<DTC_ID, DtcValue>> GetDtcValuesAsync(ILogicalDevice logicalDevice, LogicalDeviceDtcFilter dtcFilter, DTC_ID startDtcId, DTC_ID endDtcId, CancellationToken cancellationToken);
	}
}
