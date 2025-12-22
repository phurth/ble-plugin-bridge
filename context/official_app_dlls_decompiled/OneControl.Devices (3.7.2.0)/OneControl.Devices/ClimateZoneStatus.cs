namespace OneControl.Devices
{
	public enum ClimateZoneStatus : byte
	{
		Off = 0,
		Idle = 1,
		Cooling = 2,
		HeatingWithHeatPump = 3,
		HeatingWithElectric = 4,
		HeatingWithGasFurnace = 5,
		HeatingWithGasOverride = 6,
		DeadTime = 7,
		LoadShedding = 8,
		FailOff = 128,
		FailReserved17 = 129,
		FailReserved18 = 130,
		FailHeatingWithHeatPump = 131,
		FailHeatingWithElectric = 132,
		FailHeatingWithGasFurnace = 133,
		FailHeatingWithGasOverride = 134,
		FailReserved23 = 135,
		FailReserved24 = 136
	}
}
