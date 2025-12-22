using System;

namespace IDS.Core.IDS_CAN
{
	[Flags]
	public enum LOCAL_DEVICE_OPTIONS : uint
	{
		NONE = 0u,
		IGNORE_IN_MOTION_LOCKOUT = 1u
	}
}
