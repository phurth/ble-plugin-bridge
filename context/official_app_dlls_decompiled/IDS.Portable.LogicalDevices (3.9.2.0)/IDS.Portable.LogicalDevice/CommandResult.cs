namespace IDS.Portable.LogicalDevice
{
	public enum CommandResult
	{
		Completed,
		Canceled,
		CanceledWithSameCommand,
		ErrorQueueingCommand,
		ErrorSessionTimeout,
		ErrorCommandTimeout,
		ErrorDeviceOffline,
		ErrorOther,
		ErrorNoSession,
		ErrorRemoteNotAvailable,
		ErrorRemoteOperationNotSupported,
		ErrorCommandNotAllowed,
		ErrorAssumed,
		ErrorDeviceNotFound,
		ErrorInvalidDeviceId
	}
}
