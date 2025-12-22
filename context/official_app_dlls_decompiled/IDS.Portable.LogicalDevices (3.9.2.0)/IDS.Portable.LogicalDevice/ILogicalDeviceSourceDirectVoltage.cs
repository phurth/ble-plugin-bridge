using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceSourceDirectVoltage : ILogicalDeviceSourceDirect, ILogicalDeviceSource
	{
		Task<float?> TryGetVoltageAsync(CancellationToken cancellationToken);
	}
}
