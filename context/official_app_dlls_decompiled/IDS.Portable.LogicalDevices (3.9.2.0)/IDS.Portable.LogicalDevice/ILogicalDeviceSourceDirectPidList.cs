using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceSourceDirectPidList : ILogicalDeviceSourceDirect, ILogicalDeviceSource
	{
		Task<IReadOnlyDictionary<Pid, PidAccess>> GetDevicePidListAsync(ILogicalDevice logicalDevice, CancellationToken cancellationToken, Pid startPidId = Pid.Unknown, Pid endPidId = (Pid)65535);
	}
}
