using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.LogicalDevice.LogicalDeviceSource
{
	public interface ILogicalDeviceSourceDirectAccessoryHistoryData : ILogicalDeviceSourceDirect, ILogicalDeviceSource
	{
		Task<IReadOnlyList<byte>> GetAccessoryHistoryDataAsync(ILogicalDevice logicalDevice, byte block, byte startIndex, byte dataLength, CancellationToken cancellationToken);
	}
}
