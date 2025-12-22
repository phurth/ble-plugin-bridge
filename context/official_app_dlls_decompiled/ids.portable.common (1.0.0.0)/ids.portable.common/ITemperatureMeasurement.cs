namespace IDS.Portable.Common
{
	public interface ITemperatureMeasurement
	{
		bool IsTemperatureValid { get; }

		float TemperatureCelsius { get; }

		float TemperatureFahrenheit { get; }
	}
}
