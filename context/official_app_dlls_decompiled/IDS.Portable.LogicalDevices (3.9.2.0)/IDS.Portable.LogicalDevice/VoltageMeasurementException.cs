using System;

namespace IDS.Portable.LogicalDevice
{
	public class VoltageMeasurementException : Exception
	{
		public VoltageMeasurementException(string message, Exception? nested = null)
			: base(message, nested)
		{
		}
	}
}
