using System;

namespace IDS.Portable.LogicalDevice
{
	[Flags]
	public enum PidAccess
	{
		Unknown = 0,
		Readable = 1,
		Writable = 2,
		NonVolatile = 4,
		Reserved = 0xF8
	}
}
