using System;
using IDS.Core.IDS_CAN;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidArrayUInt32 : LogicalDevicePidArray<uint>
	{
		public static ulong FromValue(uint value)
		{
			return value;
		}

		public static uint ToValue(ulong rawValue)
		{
			return (uint)rawValue;
		}

		public LogicalDevicePidArrayUInt32(ILogicalDevice logicalDevice, PID pid, LogicalDeviceSessionType writeAccess, ushort minIndex, ushort maxIndex, Func<ulong, bool>? validityCheckRead, Func<ulong, bool>? validityCheckWrite)
			: base(logicalDevice, pid, writeAccess, minIndex, maxIndex, (Func<uint, ulong>)FromValue, (Func<ulong, uint>)ToValue, validityCheckRead, validityCheckWrite)
		{
		}

		public LogicalDevicePidArrayUInt32(ILogicalDevice logicalDevice, PID pid, LogicalDeviceSessionType writeAccess, ushort minIndex, ushort maxIndex, Func<ulong, bool>? validityCheck = null)
			: this(logicalDevice, pid, writeAccess, minIndex, maxIndex, validityCheck, validityCheck)
		{
		}
	}
}
