using System.ComponentModel;

namespace IDS.Portable.LogicalDevice
{
	public enum PidInputSwitchType
	{
		[Description("No Physical Switch")]
		NoPhysicalSwitch,
		[Description("Dimmable Switch")]
		DimmableSwitch,
		[Description("Toggle Switch")]
		ToggleSwitch,
		[Description("Momentary Switch")]
		MomentarySwitch
	}
}
