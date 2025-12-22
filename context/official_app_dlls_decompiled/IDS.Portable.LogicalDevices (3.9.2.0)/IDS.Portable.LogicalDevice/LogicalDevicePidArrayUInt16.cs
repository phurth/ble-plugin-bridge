using System;
using IDS.Core.IDS_CAN;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidArrayUInt16 : LogicalDevicePidArray<ushort>
	{
		public static ulong FromValue(ushort value)
		{
			return value;
		}

		public static ushort ToValue(ulong rawValue)
		{
			return (ushort)rawValue;
		}

		public LogicalDevicePidArrayUInt16(ILogicalDevice logicalDevice, PID pid, LogicalDeviceSessionType writeAccess, ushort minIndex, ushort maxIndex, Func<ulong, bool>? validityCheckRead, Func<ulong, bool>? validityCheckWrite)
			: base(logicalDevice, pid, writeAccess, minIndex, maxIndex, (Func<ushort, ulong>)FromValue, (Func<ulong, ushort>)ToValue, validityCheckRead, validityCheckWrite)
		{
		}

		public LogicalDevicePidArrayUInt16(ILogicalDevice logicalDevice, PID pid, LogicalDeviceSessionType writeAccess, ushort minIndex, ushort maxIndex, Func<ulong, bool>? validityCheck = null)
			: this(logicalDevice, pid, writeAccess, minIndex, maxIndex, validityCheck, validityCheck)
		{
		}
	}
}
