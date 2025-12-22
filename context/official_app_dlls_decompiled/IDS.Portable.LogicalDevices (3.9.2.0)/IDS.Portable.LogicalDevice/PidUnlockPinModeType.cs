using System.ComponentModel;

namespace IDS.Portable.LogicalDevice
{
	public enum PidUnlockPinModeType
	{
		[Description("Disabled")]
		Disabled,
		[Description("Enabled For All Features")]
		EnabledAllFeatures,
		[Description("Enabled For Safety Features")]
		EnabledForSafetyFeatures
	}
}
