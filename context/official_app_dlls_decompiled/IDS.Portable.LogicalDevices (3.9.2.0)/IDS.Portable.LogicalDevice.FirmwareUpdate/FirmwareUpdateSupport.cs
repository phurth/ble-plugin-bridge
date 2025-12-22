namespace IDS.Portable.LogicalDevice.FirmwareUpdate
{
	public enum FirmwareUpdateSupport
	{
		Unknown,
		SupportedViaDevice,
		SupportedViaBootloader,
		DeviceOffline,
		NotSupported
	}
}
