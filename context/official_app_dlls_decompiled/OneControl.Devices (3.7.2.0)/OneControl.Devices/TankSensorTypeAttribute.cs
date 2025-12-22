using System;

namespace OneControl.Devices
{
	[AttributeUsage(AttributeTargets.Field)]
	public class TankSensorTypeAttribute : Attribute
	{
		public string FunctionNameIdentifier { get; }

		public int SortPriority { get; }

		public TankHoldingType HoldingType { get; }

		public TankLevelThresholdType ThresholdType { get; }

		public TankSensorTypeAttribute(string functionNameIdentifier, int sortPriority, TankHoldingType holdingType, TankLevelThresholdType thresholdType)
		{
			FunctionNameIdentifier = functionNameIdentifier.ToUpper();
			SortPriority = sortPriority;
			HoldingType = holdingType;
			ThresholdType = thresholdType;
		}
	}
}
