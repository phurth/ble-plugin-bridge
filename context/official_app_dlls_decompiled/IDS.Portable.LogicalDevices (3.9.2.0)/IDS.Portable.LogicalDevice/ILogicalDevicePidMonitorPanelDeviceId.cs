using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDevicePidMonitorPanelDeviceId : ILogicalDevicePid<PidMonitorPanelDeviceId>, ILogicalDevicePid
	{
		Task<PidMonitorPanelDeviceId> ReadPidDeviceIdAsync(CancellationToken cancellationToken);

		Task WriteDeviceAsync(PidMonitorPanelDeviceId pidDeviceId, CancellationToken cancellationToken);
	}
}
