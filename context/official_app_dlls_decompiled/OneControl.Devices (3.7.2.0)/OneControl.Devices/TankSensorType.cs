namespace OneControl.Devices
{
	public enum TankSensorType
	{
		[TankSensorType("Black", 6, TankHoldingType.Waste, TankLevelThresholdType.High)]
		BlackTank,
		[TankSensorType("Grey", 5, TankHoldingType.Waste, TankLevelThresholdType.High)]
		GreyTank,
		[TankSensorType("Fresh", 4, TankHoldingType.Supply, TankLevelThresholdType.Low)]
		FreshTank,
		[TankSensorType("Fuel", 3, TankHoldingType.Supply, TankLevelThresholdType.Low)]
		FuelTank,
		[TankSensorType("LP Tank", 2, TankHoldingType.Supply, TankLevelThresholdType.Low)]
		LpTank,
		[TankSensorType("Unknown", 1, TankHoldingType.Unknown, TankLevelThresholdType.Unknown)]
		UnknownTank
	}
}
