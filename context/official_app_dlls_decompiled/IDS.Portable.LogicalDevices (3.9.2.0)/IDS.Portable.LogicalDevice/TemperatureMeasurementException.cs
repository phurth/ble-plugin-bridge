using System;

namespace IDS.Portable.LogicalDevice
{
	public class TemperatureMeasurementException : Exception
	{
		public TemperatureMeasurementException(string message, Exception nested)
			: base(message, nested)
		{
		}
	}
}
