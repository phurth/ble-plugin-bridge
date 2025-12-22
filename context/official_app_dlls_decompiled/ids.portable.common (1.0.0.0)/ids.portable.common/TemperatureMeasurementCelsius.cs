namespace IDS.Portable.Common
{
	public struct TemperatureMeasurementCelsius : ITemperatureMeasurement
	{
		public bool IsTemperatureValid => true;

		public float TemperatureFahrenheit => Temperature.ConvertCelsiusToFahrenheit(TemperatureCelsius);

		public float TemperatureCelsius { get; }

		public TemperatureMeasurementCelsius(float celsius)
		{
			TemperatureCelsius = celsius;
		}
	}
}
