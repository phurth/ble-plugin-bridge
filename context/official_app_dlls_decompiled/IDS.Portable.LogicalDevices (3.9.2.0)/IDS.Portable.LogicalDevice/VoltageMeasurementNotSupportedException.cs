using System;

namespace IDS.Portable.LogicalDevice
{
	public class VoltageMeasurementNotSupportedException : VoltageMeasurementException
	{
		public VoltageMeasurementNotSupportedException(Exception? nested = null)
			: this("Voltage Measurement Not Supported", nested)
		{
		}

		public VoltageMeasurementNotSupportedException(string message, Exception? nested = null)
			: base(message, nested)
		{
		}
	}
}
