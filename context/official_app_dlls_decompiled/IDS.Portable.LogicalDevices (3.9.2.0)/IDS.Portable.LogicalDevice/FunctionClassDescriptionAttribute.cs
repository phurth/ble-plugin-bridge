using System;

namespace IDS.Portable.LogicalDevice
{
	[AttributeUsage(AttributeTargets.Field)]
	public class FunctionClassDescriptionAttribute : Attribute
	{
		public string Description { get; }

		public FunctionClassDescriptionAttribute(string description)
		{
			Description = description;
		}
	}
}
