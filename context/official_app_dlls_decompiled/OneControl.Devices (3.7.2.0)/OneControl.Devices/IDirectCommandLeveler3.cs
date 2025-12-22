using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface IDirectCommandLeveler3 : ILogicalDeviceSourceDirect, ILogicalDeviceSource
	{
		Task<CommandResult> SendDirectCommandLeveler3(ILogicalDeviceLevelerType3 logicalDevice, LogicalDeviceLevelerCommandType3 command, CancellationToken cancelToken);
	}
}
