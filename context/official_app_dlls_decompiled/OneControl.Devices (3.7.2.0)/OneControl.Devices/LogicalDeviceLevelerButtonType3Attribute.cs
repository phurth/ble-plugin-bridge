using System;

namespace OneControl.Devices
{
	[AttributeUsage(AttributeTargets.Field)]
	public class LogicalDeviceLevelerButtonType3Attribute : Attribute
	{
		public LogicalDeviceLevelerButtonType3 ButtonType { get; }

		public LogicalDeviceLevelerButtonType3Attribute(LogicalDeviceLevelerButtonType3 buttonType)
		{
			ButtonType = buttonType;
		}
	}
}
