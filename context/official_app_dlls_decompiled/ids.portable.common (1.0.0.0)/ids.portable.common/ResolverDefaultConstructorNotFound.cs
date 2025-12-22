using System;

namespace IDS.Portable.Common
{
	public class ResolverDefaultConstructorNotFound : Exception
	{
		public ResolverDefaultConstructorNotFound(Type type)
			: base("Default constructor not found for " + type.Name)
		{
		}
	}
}
