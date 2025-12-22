namespace OneControl.Devices.TPMS
{
	public enum TpmsRepeaterStatus
	{
		Startup = 0,
		Idle = 1,
		Scanning = 2,
		ScanSensorFound = 3,
		ScanNoSensorFound = 4,
		ScanMultipleSensorsFound = 5,
		LearnDone = 6,
		LearnTerminatedOrTimeout = 7,
		LearnCanceled = 8,
		SetIndexAck = 9,
		SetIndexNakOrOutOfRange = 16,
		Unknown = 255
	}
}
