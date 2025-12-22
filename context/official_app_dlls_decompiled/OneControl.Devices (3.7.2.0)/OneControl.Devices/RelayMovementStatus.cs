namespace OneControl.Devices
{
	public enum RelayMovementStatus
	{
		AbleToMove,
		UnableToMoveNoSession,
		UnableToMoveNoConnection,
		UnableToMoveNoExclusiveOperation,
		UnableToMoveFaulted
	}
}
