using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface IDirectCommandSwitch : ILogicalDeviceSourceDirect, ILogicalDeviceSource
	{
		Task<CommandResult> SendDirectCommandRelayBasicSwitch(ILogicalDeviceSwitchable logicalDevice, bool turnOn, CancellationToken cancelToken);
	}
}
