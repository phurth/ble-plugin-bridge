using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface IDirectCommandLightDimmable : ILogicalDeviceSourceDirect, ILogicalDeviceSource
	{
		Task<CommandResult> SendDirectCommandLightDimmable(ILogicalDeviceLightDimmable logicalDevice, LogicalDeviceLightDimmableCommand command, CancellationToken cancelToken);
	}
}
