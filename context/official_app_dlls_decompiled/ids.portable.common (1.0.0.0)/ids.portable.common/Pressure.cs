using System;

namespace IDS.Portable.Common
{
	public static class Pressure
	{
		private static readonly double ConversionFactor = 6.89476;

		public static decimal ConvertPsiTokPA(decimal psi)
		{
			return Convert.ToDecimal((double)psi * ConversionFactor);
		}

		public static double ConvertPsiTokPA(double psi)
		{
			return psi * ConversionFactor;
		}
	}
}
