using System;

namespace OneControl.Devices
{
	[Flags]
	public enum RelayCapabilityFlagType1 : byte
	{
		None = 0,
		SupportsSoftwareConfigurableFuse = 1
	}
}
