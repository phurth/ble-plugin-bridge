using System;

namespace OneControl.Devices
{
	[Flags]
	public enum ClimateZoneCommandOptions
	{
		None = 0,
		AutoAdjustToSupportedConfiguration = 1
	}
}
