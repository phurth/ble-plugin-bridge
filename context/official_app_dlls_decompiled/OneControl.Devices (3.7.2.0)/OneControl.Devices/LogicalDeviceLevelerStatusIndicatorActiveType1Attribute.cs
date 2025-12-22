using System;
using IDS.Portable.Common;

namespace OneControl.Devices
{
	[AttributeUsage(AttributeTargets.Field)]
	public class LogicalDeviceLevelerStatusIndicatorActiveType1Attribute : Attribute, IAttributeValue<bool>
	{
		public bool Value { get; }

		public LogicalDeviceLevelerStatusIndicatorActiveType1Attribute(bool value)
		{
			Value = value;
		}
	}
}
