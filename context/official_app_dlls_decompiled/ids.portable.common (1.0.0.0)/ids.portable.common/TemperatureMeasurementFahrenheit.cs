namespace IDS.Portable.Common
{
	public struct TemperatureMeasurementFahrenheit : ITemperatureMeasurement
	{
		public bool IsTemperatureValid => true;

		public float TemperatureFahrenheit { get; }

		public float TemperatureCelsius => Temperature.ConvertFahrenheitToCelsius(TemperatureFahrenheit);

		public TemperatureMeasurementFahrenheit(float fahrenheit)
		{
			TemperatureFahrenheit = fahrenheit;
		}
	}
}
