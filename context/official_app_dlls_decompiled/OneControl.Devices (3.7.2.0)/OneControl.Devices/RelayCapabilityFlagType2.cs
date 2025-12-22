using System;

namespace OneControl.Devices
{
	[Flags]
	public enum RelayCapabilityFlagType2 : byte
	{
		None = 0,
		SupportsSoftwareConfigurableFuse = 1,
		SupportsCoarsePosition = 2,
		SupportsFinePosition = 4,
		PhysicalSwitchMask = 0x18,
		AllLightsGroupBehaviorMask = 0x60
	}
}
