using System;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceCommandControlRunner : ICommonDisposable, IDisposable
	{
		Task<CommandResult> SendCommandAsync(Func<CancellationToken, Task<CommandResult>> command, CancellationToken cancelToken, ILogicalDevice logicalDevice, Func<ILogicalDevice, CommandControl>? cmdControl = null, CommandSendOption options = CommandSendOption.None);
	}
}
