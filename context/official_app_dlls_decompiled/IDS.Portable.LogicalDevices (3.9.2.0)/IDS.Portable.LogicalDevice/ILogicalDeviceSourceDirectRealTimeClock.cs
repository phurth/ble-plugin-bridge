using System;
using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceSourceDirectRealTimeClock : ILogicalDeviceSourceDirect, ILogicalDeviceSource
	{
		DateTime GetRealTimeClockTime { get; }

		Task<bool> SetRealTimeClockTimeAsync(DateTime dateTime, CancellationToken cancellationToken);
	}
}
