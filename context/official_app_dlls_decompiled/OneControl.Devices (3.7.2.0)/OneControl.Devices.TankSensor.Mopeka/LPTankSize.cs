namespace OneControl.Devices.TankSensor.Mopeka
{
	public class LPTankSize : ILPTankSize
	{
		public int Id { get; }

		public float Amount { get; }

		public LPTankUnit Unit { get; }

		public LPTankOrientation Orientation { get; }

		public float TankHeightInMm { get; }

		internal LPTankSize(int id, float amount, LPTankUnit unit, LPTankOrientation orientation, float tankHeightInMm)
		{
			Id = id;
			Amount = amount;
			Unit = unit;
			Orientation = orientation;
			TankHeightInMm = tankHeightInMm;
		}
	}
}
