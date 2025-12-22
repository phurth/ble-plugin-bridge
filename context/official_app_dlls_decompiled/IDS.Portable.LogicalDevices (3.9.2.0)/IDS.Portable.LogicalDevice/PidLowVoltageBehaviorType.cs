using System.ComponentModel;

namespace IDS.Portable.LogicalDevice
{
	public enum PidLowVoltageBehaviorType
	{
		[Description("None")]
		None,
		[Description("> 10 V")]
		OperatesAbove10V,
		[Description("> 10.5 V")]
		OperatesAbove10Point5V,
		[Description("> 11 V")]
		OperatesAbove11V,
		[Description("> 11.5 V")]
		OperatesAbove11Point5V,
		[Description("> 12 V")]
		OperatesAbove12V,
		[Description("> 12.5 V")]
		OperatesAbove12Point5V,
		[Description("> 13 V")]
		OperatesAbove13V,
		[Description("> 13.5 V")]
		OperatesAbove14Point5V
	}
}
