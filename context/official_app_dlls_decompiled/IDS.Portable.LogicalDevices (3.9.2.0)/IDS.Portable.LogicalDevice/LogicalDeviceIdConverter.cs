using System;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceIdConverter : JsonSerializerInterfaceObjectConverter<ILogicalDeviceId>
	{
		public override Type DefaultConstructionType()
		{
			return typeof(LogicalDeviceId);
		}
	}
}
