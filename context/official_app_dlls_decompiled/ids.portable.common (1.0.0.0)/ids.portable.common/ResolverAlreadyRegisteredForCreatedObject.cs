using System;

namespace IDS.Portable.Common
{
	public class ResolverAlreadyRegisteredForCreatedObject : Exception
	{
		public ResolverAlreadyRegisteredForCreatedObject(Type type)
			: base("Can't register a new resolver for " + type.Name + " because instance already created!")
		{
		}
	}
}
