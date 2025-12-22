using System;
using IDS.Core.IDS_CAN;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidValueReadException : LogicalDevicePidException
	{
		public LogicalDevicePidValueReadException(PID pid, ILogicalDevice? device, Exception? innerException = null)
			: this(pid, null, device, innerException)
		{
		}

		public LogicalDevicePidValueReadException(PID pid, ILogicalDevice? device, string message)
			: this(pid, null, device, message)
		{
		}

		public LogicalDevicePidValueReadException(PID pid, ushort? pidAddress, ILogicalDevice? device, Exception? innerException = null)
			: base(pid, pidAddress, device, "Unable to READ PID", innerException)
		{
		}

		protected LogicalDevicePidValueReadException(PID pid, ushort? pidAddress, ILogicalDevice? device, string message)
			: base(pid, pidAddress, device, message)
		{
		}
	}
}
