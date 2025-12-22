using System.ComponentModel;

namespace IDS.Portable.LogicalDevice
{
	public enum PidValueCheck
	{
		[Description("Value Currently Unavailable")]
		NoValue,
		[Description("Has Value")]
		HasValue,
		[Description("Feature Disabled")]
		FeatureDisabled,
		[Description("Invalid/Undefined Value")]
		Undefined
	}
}
