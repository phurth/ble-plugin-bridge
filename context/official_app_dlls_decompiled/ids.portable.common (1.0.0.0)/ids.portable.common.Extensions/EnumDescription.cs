using System;

namespace IDS.Portable.Common.Extensions
{
	[Obsolete("EnumDescription is deprecated, please use the system attribute 'Description' for this functionality and use TryGetDescription/Description instead of GetAttributeValue")]
	[AttributeUsage(AttributeTargets.Field)]
	public class EnumDescription : Attribute, IAttributeValue<string>
	{
		public string Description { get; }

		public string Value => Description;

		public EnumDescription(string description)
		{
			Description = description;
		}
	}
}
