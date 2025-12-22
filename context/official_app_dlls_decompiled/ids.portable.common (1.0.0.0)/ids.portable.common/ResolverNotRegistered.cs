using System;

namespace IDS.Portable.Common
{
	public class ResolverNotRegistered : Exception
	{
		public ResolverNotRegistered(Type type)
			: base("No resolver registered for " + type.Name)
		{
		}
	}
}
