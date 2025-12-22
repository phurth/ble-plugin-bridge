namespace OneControl.Devices
{
	public enum RelayOperationStatus
	{
		PerformingOperation,
		UnableToPerformOperationNoSession,
		UnableToPerformOperationNoConnection,
		UnableToPerformOperationNoExclusiveOperation,
		UnableToPerformOperationNotSupported
	}
}
