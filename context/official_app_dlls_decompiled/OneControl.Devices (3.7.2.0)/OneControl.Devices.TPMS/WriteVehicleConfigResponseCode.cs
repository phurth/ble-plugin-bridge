namespace OneControl.Devices.TPMS
{
	public enum WriteVehicleConfigResponseCode
	{
		Success = 0,
		Processing = 1,
		RepeaterFull = 2,
		InvalidParameters = 3,
		DefaultState = 165,
		NvsConfiguration = 255
	}
}
