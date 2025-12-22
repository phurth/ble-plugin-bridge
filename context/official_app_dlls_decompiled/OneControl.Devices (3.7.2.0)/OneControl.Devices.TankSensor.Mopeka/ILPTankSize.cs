namespace OneControl.Devices.TankSensor.Mopeka
{
	public interface ILPTankSize
	{
		int Id { get; }

		float TankHeightInMm { get; }

		float Amount { get; }

		LPTankUnit Unit { get; }

		LPTankOrientation Orientation { get; }
	}
}
