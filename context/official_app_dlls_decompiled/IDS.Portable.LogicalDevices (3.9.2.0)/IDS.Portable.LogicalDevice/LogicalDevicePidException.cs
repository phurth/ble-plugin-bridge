using System;
using IDS.Core.IDS_CAN;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidException : LogicalDeviceException
	{
		public LogicalDevicePidException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}

		public static string PidToString(PID pid, ushort? pidAddress)
		{
			return string.Format("{0}{1}", pid, (!pidAddress.HasValue) ? "" : $"[{pidAddress.Value}]");
		}

		public LogicalDevicePidException(PID pid, string message, Exception? innerException = null)
			: base($"{pid} {message}", innerException)
		{
		}

		public LogicalDevicePidException(PID pid, ILogicalDevice? device, string message, Exception? innerException = null)
			: base(string.Format("{0} {1} for {2}", pid, message, device?.ToString() ?? "unknown"), innerException)
		{
		}

		public LogicalDevicePidException(PID pid, ushort? pidAddress, ILogicalDevice? device, string message, Exception? innerException = null)
			: base(PidToString(pid, pidAddress) + " " + message + " for " + (device?.ToString() ?? "unknown"), innerException)
		{
		}

		public LogicalDevicePidException(PID pid, IRemoteDevice? device, string message, Exception? innerException = null)
			: base(string.Format("{0} {1} for {2}", pid, message, device?.ToString() ?? "unknown"), innerException)
		{
		}

		public LogicalDevicePidException(IRemoteDevice? device, string message, Exception? innerException = null)
			: base(message + " for " + (device?.ToString() ?? "unknown"), innerException)
		{
		}
	}
}
