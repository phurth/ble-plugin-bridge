using System;

namespace IDS.Portable.LogicalDevice
{
	public class VoltageMeasurementUnavailableException : VoltageMeasurementException
	{
		public VoltageMeasurementUnavailableException()
			: this("Voltage Measurement Unavailable")
		{
		}

		public VoltageMeasurementUnavailableException(string message, Exception? nested = null)
			: base(message, nested)
		{
		}
	}
}
