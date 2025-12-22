using System;

namespace OneControl.Devices
{
	[Flags]
	public enum RelayHBridgeCapabilityFlagType2 : byte
	{
		None = 0,
		SupportsSoftwareConfigurableFuse = 1,
		SupportsAutoCommands = 2,
		SupportsFinePosition = 4,
		PhysicalSwitchMask = 0x18,
		AllLightsGroupBehaviorMask = 0x60,
		SupportsHoming = 0x20,
		SupportsAwningSensor = 0x40
	}
}
