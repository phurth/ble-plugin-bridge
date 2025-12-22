using System.ComponentModel;

namespace IDS.Portable.LogicalDevice
{
	public enum PidIgnitionBehaviorType
	{
		[Description("None")]
		None,
		[Description("Requires Ignition On")]
		RequiresIgnitionOn,
		[Description("Requires Ignition Off")]
		RequiresIgnitionOff
	}
}
