using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceSourceDirectRemoveOfflineDevices : ILogicalDeviceSourceDirect, ILogicalDeviceSource
	{
		Task RemoveOfflineDevicesAsync(CancellationToken cancellationToken);
	}
}
