namespace OneControl.Devices.TPMS
{
	public enum LearnSensorResponseCode
	{
		Success = 0,
		Processing = 1,
		UnknownInvalidSensor = 2,
		MaximumSensorsReached = 3,
		LearnTimeout = 4,
		MultipleSensorsFound = 5,
		DefaultState = 165,
		UnknownError = 255
	}
}
