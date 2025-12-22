using System;
using IDS.Core.IDS_CAN;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidValueWriteException : LogicalDevicePidException
	{
		public LogicalDevicePidValueWriteException(PID pid, ILogicalDevice? device, ulong value, Exception? innerException = null)
			: this(pid, null, device, value, innerException)
		{
		}

		public LogicalDevicePidValueWriteException(PID pid, ILogicalDevice? device, string message)
			: this(pid, null, device, message)
		{
		}

		public LogicalDevicePidValueWriteException(PID pid, ushort? pidAddress, ILogicalDevice? device, ulong value, Exception? innerException = null)
			: this(pid, pidAddress, device, $"Unable to WRITE PID with value 0x{value:X}", innerException)
		{
		}

		protected LogicalDevicePidValueWriteException(PID pid, ushort? pidAddress, ILogicalDevice? device, string message, Exception? innerException = null)
			: base(pid, pidAddress, device, message, innerException)
		{
		}
	}
}
