using System.ComponentModel;

namespace IDS.Portable.LogicalDevice
{
	public enum PidFanSpeedControlType
	{
		[Description("None")]
		None,
		[Description("Force Dual Gang")]
		ForceDualGang,
		[Description("Force Single Gang")]
		ForceSingleGang
	}
}
