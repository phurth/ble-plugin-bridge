using System;

namespace IDS.Portable.LogicalDevice
{
	public class VoltageMeasurementTimeoutException : TimeoutException
	{
		public VoltageMeasurementTimeoutException(Exception? nested = null)
			: base("Voltage Measurement Timeout", nested)
		{
		}
	}
}
