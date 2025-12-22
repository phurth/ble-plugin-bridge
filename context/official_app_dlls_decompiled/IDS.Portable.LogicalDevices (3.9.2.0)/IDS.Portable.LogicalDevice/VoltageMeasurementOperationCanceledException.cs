using System;

namespace IDS.Portable.LogicalDevice
{
	public class VoltageMeasurementOperationCanceledException : OperationCanceledException
	{
		public VoltageMeasurementOperationCanceledException(Exception? nested = null)
			: base("Voltage Measurement Operation Canceled", nested)
		{
		}
	}
}
