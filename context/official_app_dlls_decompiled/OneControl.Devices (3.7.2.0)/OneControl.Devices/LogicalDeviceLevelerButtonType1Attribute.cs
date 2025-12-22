using System;

namespace OneControl.Devices
{
	[AttributeUsage(AttributeTargets.Field)]
	public class LogicalDeviceLevelerButtonType1Attribute : Attribute
	{
		public LogicalDeviceLevelerButtonType1 ButtonType { get; }

		public LogicalDeviceLevelerButtonType1Attribute(LogicalDeviceLevelerButtonType1 buttonType)
		{
			ButtonType = buttonType;
		}
	}
}
