using System;

namespace OneControl.Devices
{
	[Flags]
	public enum LogicalDeviceLevelerButtonHomeType4 : uint
	{
		None = 0u,
		AutoLevel = 1u,
		AutoHitch = 2u,
		AutoRetractAllJacks = 4u,
		AutoRetractFrontJacks = 8u,
		AutoRetractRearJacks = 0x10u,
		ManualMode = 0x20u,
		ManualAirSuspension = 0x40u,
		ZeroMode = 0x80u,
		AutoHomeJacks = 0x100u,
		RfConfig = 0x200u
	}
}
