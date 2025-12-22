using System;
using IDS.Core.IDS_CAN;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidInvalidValueException : LogicalDevicePidException
	{
		public LogicalDevicePidInvalidValueException(PID pid, ulong value)
			: this(pid, null, null, value)
		{
		}

		public LogicalDevicePidInvalidValueException(PID pid, ILogicalDevice? device, ulong value)
			: this(pid, null, device, value)
		{
		}

		public LogicalDevicePidInvalidValueException(PID pid, ILogicalDevice? device, string value)
			: this(pid, null, device, value)
		{
		}

		public LogicalDevicePidInvalidValueException(PID pid, ILogicalDevice? device, object? value, Exception? innerException = null)
			: this(pid, null, device, value, innerException)
		{
		}

		public LogicalDevicePidInvalidValueException(PID pid, ushort? pidAddress, ILogicalDevice? device, ulong value)
			: base(pid, pidAddress, device, $"invalid value of {value:X}")
		{
		}

		public LogicalDevicePidInvalidValueException(PID pid, ushort? pidAddress, ILogicalDevice? device, string value)
			: base(pid, pidAddress, device, "invalid value of " + value)
		{
		}

		public LogicalDevicePidInvalidValueException(PID pid, ushort? pidAddress, ILogicalDevice? device, object? value, Exception? innerException = null)
			: base(pid, pidAddress, device, $"invalid value of {value}", innerException)
		{
		}

		public LogicalDevicePidInvalidValueException(PID pid, object value)
			: base(pid, $"invalid value of {value}")
		{
		}
	}
	public class LogicalDevicePidInvalidValueException<TValue> : LogicalDevicePidInvalidValueException
	{
		public TValue InvalidValue { get; }

		public LogicalDevicePidInvalidValueException(PID pid, ILogicalDevice device, TValue value, Exception? innerException = null)
			: base(pid, device, value)
		{
			InvalidValue = value;
		}
	}
}
