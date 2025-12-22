using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface IDirectCommandGeneratorGenie : ILogicalDeviceSourceDirect, ILogicalDeviceSource
	{
		Task<CommandResult> SendDirectCommandGeneratorGenie(ILogicalDeviceGeneratorGenie logicalDevice, GeneratorGenieCommand command, CancellationToken cancelToken);
	}
}
