using IDS.Core.IDS_CAN;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidNotSupportedException : LogicalDevicePidException
	{
		public LogicalDevicePidNotSupportedException(PID pid, ILogicalDevice? device)
			: base(pid, device, "not supported")
		{
		}

		public LogicalDevicePidNotSupportedException(PID pid, IRemoteDevice? device)
			: base(pid, device, "not supported")
		{
		}

		public LogicalDevicePidNotSupportedException(IRemoteDevice? device)
			: base(device, "PIDs are not supported")
		{
		}
	}
}
