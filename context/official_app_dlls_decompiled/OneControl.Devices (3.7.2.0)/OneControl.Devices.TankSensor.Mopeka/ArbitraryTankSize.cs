namespace OneControl.Devices.TankSensor.Mopeka
{
	public class ArbitraryTankSize : ILPTankSize
	{
		public int Id => LPTankSizes.ArbitraryTankSizeId;

		public float TankHeightInMm { get; set; }

		public float Amount { get; }

		public LPTankUnit Unit { get; }

		public LPTankOrientation Orientation { get; }

		public ArbitraryTankSize(float tankHeightInMm)
		{
			TankHeightInMm = tankHeightInMm;
		}

		public ArbitraryTankSize(float tankHeightInMm, float amount, LPTankUnit unit, LPTankOrientation orientation)
		{
			TankHeightInMm = tankHeightInMm;
			Amount = amount;
			Unit = unit;
			Orientation = orientation;
		}
	}
}
