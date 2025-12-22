using System;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidMonitorPanelDeviceId : LogicalDevicePid, ILogicalDevicePidMonitorPanelDeviceId, ILogicalDevicePid<PidMonitorPanelDeviceId>, ILogicalDevicePid
	{
		public LogicalDevicePidMonitorPanelDeviceId(ILogicalDevice logicalDevice, PID pid, LogicalDeviceSessionType writeAccess, Func<ulong, bool>? validityCheck = null)
			: base(logicalDevice, pid, writeAccess, validityCheck)
		{
		}

		public Task<PidMonitorPanelDeviceId> ReadAsync(CancellationToken cancellationToken)
		{
			return ReadPidDeviceIdAsync(cancellationToken);
		}

		public async Task<PidMonitorPanelDeviceId> ReadPidDeviceIdAsync(CancellationToken cancellationToken)
		{
			return new PidMonitorPanelDeviceId(await ReadValueAsync(cancellationToken));
		}

		public Task WriteAsync(PidMonitorPanelDeviceId value, CancellationToken cancellationToken)
		{
			return WriteDeviceAsync(value, cancellationToken);
		}

		public Task WriteDeviceAsync(PidMonitorPanelDeviceId pidDeviceId, CancellationToken cancellationToken)
		{
			return WriteValueAsync(pidDeviceId.RawValue, cancellationToken);
		}
	}
}
