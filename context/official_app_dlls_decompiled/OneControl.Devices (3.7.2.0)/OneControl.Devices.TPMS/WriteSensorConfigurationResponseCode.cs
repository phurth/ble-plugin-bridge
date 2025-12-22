namespace OneControl.Devices.TPMS
{
	public enum WriteSensorConfigurationResponseCode
	{
		Success = 0,
		Processing = 1,
		UnknownInvalidSensor = 2,
		InvalidTempLimit = 3,
		InvalidPressureLimit = 4,
		UnknownError = 15
	}
}
