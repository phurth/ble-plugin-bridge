using System.ComponentModel;

namespace IDS.Portable.LogicalDevice
{
	[DefaultValue(LogicalDeviceCapabilityFeatureId.Unknown)]
	public enum LogicalDeviceCapabilityFeatureId : ushort
	{
		Unknown,
		Abs,
		Odometer,
		[LogicalDeviceFeatureIdCloud("ABSSway")]
		Sway,
		BrakePanicAssist,
		BrakeAntiChucking
	}
}
