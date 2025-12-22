using System;
using System.ComponentModel;

namespace IDS.Portable.LogicalDevice
{
	[Flags]
	public enum PidLevelUiSupportedFeaturesType
	{
		[Description("Auto Hitch Supported")]
		AutoHitch = 1,
		[Description("Retract Front Jacks Supported")]
		RetractFrontJacks = 2,
		[Description("Retract Rear Jacks Supported")]
		RetractRearJacks = 4,
		[Description("Manual Air Suspension Control Supported")]
		ManualAirSuspensionControl = 8,
		[Description("RF Remote Pairing Supported")]
		RfRemotePairing = 0x10,
		[Description("Re-home Jack Positions Supported")]
		ReHomeJackPositions = 0x20
	}
}
