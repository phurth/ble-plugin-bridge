using System.ComponentModel;

namespace IDS.Portable.LogicalDevice
{
	public enum PidParkBrakeBehaviorType
	{
		[Description("None")]
		None,
		[Description("Requires Parking Break Engaged")]
		RequiresParkingBreakEngaged,
		[Description("Requires Parking Break Released")]
		RequiresParkingBreakReleased
	}
}
