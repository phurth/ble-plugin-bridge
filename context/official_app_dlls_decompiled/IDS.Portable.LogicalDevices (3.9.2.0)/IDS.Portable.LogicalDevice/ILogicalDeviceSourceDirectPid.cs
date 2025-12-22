using System;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.Types;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceSourceDirectPid : ILogicalDeviceSourceDirect, ILogicalDeviceSource
	{
		Task<UInt48> PidReadAsync(ILogicalDevice logicalDevice, Pid pid, Action<float, string> readProgress, CancellationToken cancellationToken);

		Task PidWriteAsync(ILogicalDevice logicalDevice, Pid pid, UInt48 value, LogicalDeviceSessionType pidWriteAccess, Action<float, string> writeProgress, CancellationToken cancellationToken);

		Task<uint> PidReadAsync(ILogicalDevice logicalDevice, Pid pid, ushort address, Action<float, string> readProgress, CancellationToken cancellationToken);

		Task PidWriteAsync(ILogicalDevice logicalDevice, Pid pid, ushort address, uint value, LogicalDeviceSessionType pidWriteAccess, Action<float, string> writeProgress, CancellationToken cancellationToken);
	}
}
