using IDS.Core.IDS_CAN;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidValueWriteNotSupportedException : LogicalDevicePidValueWriteException
	{
		public LogicalDevicePidValueWriteNotSupportedException(PID pid, ushort? pidAddress, ILogicalDevice? device)
			: base(pid, pidAddress, device, "Unable to WRITE PID because it doesn't support writing")
		{
		}

		public LogicalDevicePidValueWriteNotSupportedException(PID pid, ILogicalDevice? device)
			: this(pid, null, device)
		{
		}

		public LogicalDevicePidValueWriteNotSupportedException(PID pid)
			: base(pid, null, "Unable to WRITE PID because it doesn't support writing")
		{
		}
	}
}
