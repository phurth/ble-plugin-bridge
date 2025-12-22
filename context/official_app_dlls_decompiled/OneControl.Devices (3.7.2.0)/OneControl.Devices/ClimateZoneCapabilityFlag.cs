using System;

namespace OneControl.Devices
{
	[Flags]
	public enum ClimateZoneCapabilityFlag : byte
	{
		None = 0,
		GasFurnace = 1,
		AirConditioner = 2,
		HeatPump = 4,
		MultiSpeedFan = 8,
		Reserved0 = 0x10,
		Reserved1 = 0x20,
		Reserved2 = 0x40,
		Reserved3 = 0x80
	}
}
