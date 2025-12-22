using System.Runtime.InteropServices;

namespace IDS.Portable.LogicalDevice
{
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct LogicalDeviceSessionConstants
	{
		public const uint SessionNoKeepAliveTime = 0u;

		public const uint SessionKeepAliveTime = 15000u;

		public const uint SessionGetTimeout = 3000u;
	}
}
