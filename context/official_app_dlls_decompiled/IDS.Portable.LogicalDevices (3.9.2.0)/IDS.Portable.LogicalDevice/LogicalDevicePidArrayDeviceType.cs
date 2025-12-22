using System;
using IDS.Core.IDS_CAN;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidArrayDeviceType : LogicalDevicePidArray<DEVICE_TYPE>
	{
		public static ulong FromValue(DEVICE_TYPE value)
		{
			return (byte)value;
		}

		public static DEVICE_TYPE ToValue(ulong rawValue)
		{
			return (byte)rawValue;
		}

		public LogicalDevicePidArrayDeviceType(ILogicalDevice logicalDevice, PID pid, LogicalDeviceSessionType writeAccess, ushort minIndex, ushort maxIndex, Func<ulong, bool>? validityCheckRead, Func<ulong, bool>? validityCheckWrite)
			: base(logicalDevice, pid, writeAccess, minIndex, maxIndex, (Func<DEVICE_TYPE, ulong>)FromValue, (Func<ulong, DEVICE_TYPE>)ToValue, validityCheckRead, validityCheckWrite)
		{
		}

		public LogicalDevicePidArrayDeviceType(ILogicalDevice logicalDevice, PID pid, LogicalDeviceSessionType writeAccess, ushort minIndex, ushort maxIndex, Func<ulong, bool>? validityCheck = null)
			: this(logicalDevice, pid, writeAccess, minIndex, maxIndex, validityCheck, validityCheck)
		{
		}
	}
}
