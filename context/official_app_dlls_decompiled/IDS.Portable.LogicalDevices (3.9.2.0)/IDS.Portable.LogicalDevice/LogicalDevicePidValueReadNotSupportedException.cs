using IDS.Core.IDS_CAN;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidValueReadNotSupportedException : LogicalDevicePidValueReadException
	{
		public LogicalDevicePidValueReadNotSupportedException(PID pid, ushort? pidAddress, ILogicalDevice? device)
			: base(pid, pidAddress, device, "Unable to READ PID it doesn't support reading")
		{
		}

		public LogicalDevicePidValueReadNotSupportedException(PID pid, ILogicalDevice? device)
			: this(pid, null, device)
		{
		}
	}
}
