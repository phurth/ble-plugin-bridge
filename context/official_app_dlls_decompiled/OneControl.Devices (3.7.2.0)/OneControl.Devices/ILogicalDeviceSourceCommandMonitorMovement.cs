using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface ILogicalDeviceSourceCommandMonitorMovement : ILogicalDeviceSourceCommandMonitor
	{
		Task WillSendCommandRelayMomentaryAsync(ILogicalDeviceSource deviceSource, ILogicalDeviceRelayHBridge logicalDevice, HBridgeCommand command, CancellationToken cancelToken);

		Task DidSendCommandRelayMomentaryAsync(ILogicalDeviceSource deviceSource, ILogicalDeviceRelayHBridge logicalDevice, HBridgeCommand command, CommandResult commandResult, CancellationToken cancelToken);
	}
}
