using System;
using IDS.Portable.Common;

namespace OneControl.Devices
{
	[AttributeUsage(AttributeTargets.Field)]
	public class LogicalDeviceLevelerStatusIndicatorActiveType3Attribute : Attribute, IAttributeValue<bool>
	{
		public bool Value { get; }

		public LogicalDeviceLevelerStatusIndicatorActiveType3Attribute(bool value)
		{
			Value = value;
		}
	}
}
