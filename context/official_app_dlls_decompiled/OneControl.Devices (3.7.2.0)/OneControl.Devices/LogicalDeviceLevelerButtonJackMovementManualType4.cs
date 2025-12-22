using System;

namespace OneControl.Devices
{
	[Flags]
	public enum LogicalDeviceLevelerButtonJackMovementManualType4 : uint
	{
		None = 0u,
		JackRightFrontExtend = 1u,
		JackRightFrontRetract = 2u,
		JackLeftFrontExtend = 4u,
		JackLeftFrontRetract = 8u,
		JackRightRearExtend = 0x10u,
		JackRightRearRetract = 0x20u,
		JackLeftRearExtend = 0x40u,
		JackLeftRearRetract = 0x80u,
		JackTongueExtend = 0x100u,
		JackTongueRetract = 0x200u
	}
}
