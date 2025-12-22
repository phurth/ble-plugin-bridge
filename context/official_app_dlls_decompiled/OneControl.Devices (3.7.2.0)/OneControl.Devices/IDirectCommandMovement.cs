using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface IDirectCommandMovement : ILogicalDeviceSourceDirect, ILogicalDeviceSource
	{
		Task<CommandResult> SendDirectCommandRelayMomentary(ILogicalDeviceRelayHBridge logicalDevice, HBridgeCommand command, CancellationToken cancelToken);
	}
}
