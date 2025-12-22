using System.ComponentModel;

namespace IDS.Portable.LogicalDevice
{
	public enum PidLockoutType
	{
		[Description("Not a Hazardous Device")]
		NotHazardous,
		[Description("Hazardous Device on Lockout")]
		Hazardous,
		[Description("Prevents Extend on Lockout")]
		NotHazardousPreventsExtend,
		[Description("Prevents Retract on Lockout")]
		NotHazardousPreventsRetract
	}
}
