namespace OneControl.Direct.MyRvLink
{
	public enum MyRvLinkCommandResponseFailureCode : byte
	{
		InvalidResponse = 0,
		Success = 1,
		DeviceTableIdOld = 2,
		InvalidDevice = 3,
		Offline = 4,
		CommandFailed = 5,
		DeviceTooFar = 6,
		DeviceInUse = 7,
		CommandTimeout = 8,
		CommandNotSupported = 9,
		TooManyCommandsRunning = 10,
		OptionNotSupported = 11,
		CommandAborted = 12,
		DeviceHazardousToOperate = 13,
		InvalidCommand = 14,
		CommandAlreadyRunning = 15,
		OperationReturnedNoResults = 16,
		SessionTimeout = 17,
		CantOpenSession = 18,
		Other = byte.MaxValue
	}
}
