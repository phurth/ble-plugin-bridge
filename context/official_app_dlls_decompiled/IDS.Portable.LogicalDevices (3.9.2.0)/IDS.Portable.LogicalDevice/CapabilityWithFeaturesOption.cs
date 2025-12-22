using System;

namespace IDS.Portable.LogicalDevice
{
	[Flags]
	public enum CapabilityWithFeaturesOption : uint
	{
		None = 0u,
		SupportsUserDisabledFeatures = 1u
	}
}
