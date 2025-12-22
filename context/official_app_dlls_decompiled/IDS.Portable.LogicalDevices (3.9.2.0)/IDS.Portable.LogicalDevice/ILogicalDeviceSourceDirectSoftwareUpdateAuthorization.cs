using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceSourceDirectSoftwareUpdateAuthorization : ILogicalDeviceSourceDirect, ILogicalDeviceSource
	{
		Task<CommandResult> SendSoftwareUpdateAuthorizationAsync(ILogicalDevice logicalDevice, CancellationToken cancellationToken);
	}
}
