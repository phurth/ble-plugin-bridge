using System;

namespace IDS.Portable.Common
{
	public class TypeIsNotAnEnumException : Exception
	{
		public TypeIsNotAnEnumException(Type type)
			: base("The type " + type.Name + " is not an enum")
		{
		}
	}
}
