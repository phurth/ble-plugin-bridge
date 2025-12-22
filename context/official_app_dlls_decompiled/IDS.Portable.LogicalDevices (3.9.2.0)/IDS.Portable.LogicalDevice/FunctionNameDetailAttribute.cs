using System;

namespace IDS.Portable.LogicalDevice
{
	[AttributeUsage(AttributeTargets.Field)]
	internal class FunctionNameDetailAttribute : Attribute
	{
		public FunctionNameDetail Detail { get; }

		public FunctionNameDetailAttribute(FunctionNameDetailLocation location, FunctionNameDetailPosition position, FunctionNameDetailRoom room, FunctionNameDetailUse use = FunctionNameDetailUse.Unknown)
		{
			Detail = new FunctionNameDetail(location, position, room, use);
		}
	}
}
