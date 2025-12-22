using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface IDirectCommandLeveler4 : ILogicalDeviceSourceDirect, ILogicalDeviceSource
	{
		Task<CommandResult> SendDirectCommandLeveler4(ILogicalDeviceLevelerType4 logicalDevice, ILogicalDeviceLevelerCommandType4 command, CancellationToken cancelToken);
	}
}
