using System;
using System.ComponentModel;
using System.Linq;

namespace IDS.Portable.Common.Extensions
{
	public static class Default<TValue> where TValue : struct, IConvertible
	{
		public static TValue Value;

		public static bool HasCustomDefaultValue;

		static Default()
		{
			Type typeFromHandle = typeof(TValue);
			object[] customAttributes = typeFromHandle.GetCustomAttributes(typeof(DefaultValueAttribute), true);
			if (((customAttributes != null) ? Enumerable.FirstOrDefault(customAttributes) : null) is DefaultValueAttribute defaultValueAttribute)
			{
				if (defaultValueAttribute.Value is TValue value)
				{
					Value = value;
					HasCustomDefaultValue = true;
				}
				else
				{
					TaggedLog.Error("Default", "Default value isn't of correct type expected '{0}' using system default", typeFromHandle.Name);
				}
			}
		}
	}
}
