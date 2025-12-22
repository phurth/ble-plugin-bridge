using System.Collections.Generic;
using System.Linq;

namespace OneControl.Devices.TankSensor.Mopeka
{
	public static class LPTankSizes
	{
		public static readonly int ArbitraryTankSizeId = 0;

		public static readonly LPTankSize Lp20lbVertical = new LPTankSize(1, 20f, LPTankUnit.Lb, LPTankOrientation.Vertical, 254f);

		public static readonly LPTankSize Lp30lbVertical = new LPTankSize(2, 30f, LPTankUnit.Lb, LPTankOrientation.Vertical, 381f);

		public static readonly LPTankSize Lp40lbVertical = new LPTankSize(3, 40f, LPTankUnit.Lb, LPTankOrientation.Vertical, 508f);

		public static readonly LPTankSize Lp100lbVertical = new LPTankSize(4, 100f, LPTankUnit.Lb, LPTankOrientation.Vertical, 812.8f);

		public static readonly LPTankSize Lp120galVertical = new LPTankSize(5, 120f, LPTankUnit.Gal, LPTankOrientation.Vertical, 975.36f);

		public static readonly LPTankSize Lp120galHorizontal = new LPTankSize(6, 120f, LPTankUnit.Gal, LPTankOrientation.Horizontal, 609.6f);

		public static readonly LPTankSize Lp150galHorizontal = new LPTankSize(7, 150f, LPTankUnit.Gal, LPTankOrientation.Horizontal, 609.6f);

		public static readonly LPTankSize Lp250galHorizontal = new LPTankSize(8, 250f, LPTankUnit.Gal, LPTankOrientation.Horizontal, 762f);

		public static readonly LPTankSize Lp500galHorizontal = new LPTankSize(9, 500f, LPTankUnit.Gal, LPTankOrientation.Horizontal, 939.8f);

		public static readonly LPTankSize Lp1000galHorizontal = new LPTankSize(10, 1000f, LPTankUnit.Gal, LPTankOrientation.Horizontal, 1041.4f);

		public static readonly LPTankSize Lp3_7kgVertical = new LPTankSize(11, 3.7f, LPTankUnit.Kilogram, LPTankOrientation.Vertical, 235f);

		public static readonly LPTankSize Lp8_5kgVertical = new LPTankSize(12, 8.5f, LPTankUnit.Kilogram, LPTankOrientation.Vertical, 342f);

		public static IEnumerable<LPTankSize> KnownSizes { get; } = new List<LPTankSize>
		{
			Lp20lbVertical, Lp30lbVertical, Lp40lbVertical, Lp100lbVertical, Lp120galVertical, Lp120galHorizontal, Lp150galHorizontal, Lp250galHorizontal, Lp500galHorizontal, Lp1000galHorizontal,
			Lp3_7kgVertical, Lp8_5kgVertical
		};


		public static LPTankSize GetById(int id)
		{
			return Enumerable.First(KnownSizes, (LPTankSize x) => x.Id == id);
		}
	}
}
