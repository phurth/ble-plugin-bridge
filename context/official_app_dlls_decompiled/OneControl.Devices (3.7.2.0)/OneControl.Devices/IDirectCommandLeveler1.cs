using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface IDirectCommandLeveler1 : ILogicalDeviceSourceDirect, ILogicalDeviceSource
	{
		Task<CommandResult> SendDirectCommandLeveler1(ILogicalDeviceLevelerType1 logicalDevice, LogicalDeviceLevelerCommandType1 command, CancellationToken cancelToken);
	}
}
