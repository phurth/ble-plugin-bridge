using System;

namespace IDS.Portable.LogicalDevice
{
	[Flags]
	public enum LogicalDeviceServiceOptions : ulong
	{
		None = 0uL,
		AllowExperimentalFeatures = 1uL,
		AutoInTransitClear = 2uL,
		AllowHazardousOperationAtLockoutLevel1 = 4uL,
		AllowFastPids = 8uL,
		AutoFavoriteAccessoryDevices = 0x10uL,
		AutoRegisterReactiveOnlineChangedExtension = 0x20uL,
		AutoRegisterReactiveStatusChangedExtension = 0x40uL,
		AutoRegisterReactiveAlertChangedExtension = 0x80uL,
		SingletonMode = 0x100uL
	}
}
