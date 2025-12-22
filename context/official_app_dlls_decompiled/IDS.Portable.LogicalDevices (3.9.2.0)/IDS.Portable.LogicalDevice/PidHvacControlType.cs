using System.ComponentModel;

namespace IDS.Portable.LogicalDevice
{
	public enum PidHvacControlType
	{
		[Description("Unknown")]
		Unknown,
		[Description("Coleman")]
		Coleman,
		[Description("Dometic")]
		Dometic
	}
}
