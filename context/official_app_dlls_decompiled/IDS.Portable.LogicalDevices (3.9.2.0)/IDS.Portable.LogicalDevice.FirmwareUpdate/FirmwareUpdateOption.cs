using System.ComponentModel;

namespace IDS.Portable.LogicalDevice.FirmwareUpdate
{
	[DefaultValue(FirmwareUpdateOption.None)]
	public enum FirmwareUpdateOption : ushort
	{
		None,
		StartAddress,
		DeviceAuthorizationRequired,
		JumpToBootHoldTimeMs
	}
}
