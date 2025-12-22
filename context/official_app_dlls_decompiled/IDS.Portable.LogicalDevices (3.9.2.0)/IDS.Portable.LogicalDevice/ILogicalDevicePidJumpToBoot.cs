using System;
using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDevicePidJumpToBoot : ILogicalDevicePid
	{
		Task<LogicalDeviceJumpToBootState> ReadJumpToBootStateAsync(CancellationToken cancellationToken);

		Task WriteRequestJumpToBoot(TimeSpan holdTime, CancellationToken cancellationToken);
	}
}
