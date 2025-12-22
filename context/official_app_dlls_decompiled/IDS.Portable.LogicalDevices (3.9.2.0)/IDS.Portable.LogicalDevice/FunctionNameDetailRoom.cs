using System.ComponentModel;

namespace IDS.Portable.LogicalDevice
{
	public enum FunctionNameDetailRoom
	{
		[Description("Unknown")]
		Unknown,
		[Description("Other")]
		Any,
		[Description("Bathroom")]
		Bathroom,
		[Description("Bar")]
		Bar,
		[Description("Bedroom")]
		Bedroom,
		[Description("Bunk")]
		Bunk,
		[Description("Compartment")]
		Compartment,
		[Description("Garage")]
		Garage,
		[Description("Hall")]
		Hall,
		[Description("Kitchen")]
		Kitchen,
		[Description("Living Room")]
		LivingRoom,
		[Description("Loft")]
		Loft,
		[Description("Patio")]
		Patio,
		[Description("Trunk")]
		Trunk,
		[Description("Utility")]
		Utility
	}
}
