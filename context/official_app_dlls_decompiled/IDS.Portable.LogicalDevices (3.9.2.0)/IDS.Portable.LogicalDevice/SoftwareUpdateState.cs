namespace IDS.Portable.LogicalDevice
{
	public enum SoftwareUpdateState
	{
		None = 0,
		NeedsAuthorization = 1,
		Authorized = 2,
		InProgress = 3,
		Unknown = 255
	}
}
