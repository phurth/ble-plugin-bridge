using System.ComponentModel;

namespace IDS.Portable.Common
{
	public enum TemperatureScale
	{
		[Description("°F")]
		Fahrenheit,
		[Description("°C")]
		Celsius,
		[Description("K")]
		Kelvin
	}
}
