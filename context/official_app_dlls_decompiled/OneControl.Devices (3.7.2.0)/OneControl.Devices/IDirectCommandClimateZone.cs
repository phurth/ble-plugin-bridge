using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface IDirectCommandClimateZone : ILogicalDeviceSourceDirect, ILogicalDeviceSource
	{
		Task<CommandResult> SendDirectCommandClimateZoneAsync(ILogicalDeviceClimateZone logicalDevice, LogicalDeviceClimateZoneCommand command, CancellationToken cancelToken);
	}
}
