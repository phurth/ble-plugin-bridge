using System.Runtime.InteropServices;

namespace IDS.Portable.Common
{
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct TemperatureMeasurementUnknown : ITemperatureMeasurement
	{
		public bool IsTemperatureValid => false;

		public float TemperatureFahrenheit => 0f;

		public float TemperatureCelsius => 0f;
	}
}
