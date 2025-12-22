using System;

namespace OneControl.Devices
{
	[Flags]
	public enum GeneratorGenieCapabilityFlag : byte
	{
		None = 0,
		SupportsAutoStartOnTempDifferental = 1,
		CumminsOnanGeneratorDetected = 2
	}
}
