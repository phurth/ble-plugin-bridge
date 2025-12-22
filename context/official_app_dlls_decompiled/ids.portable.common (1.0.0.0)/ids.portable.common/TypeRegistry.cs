using System;
using System.Collections.Generic;
using System.Reflection;

namespace IDS.Portable.Common
{
	public static class TypeRegistry
	{
		public const string LogTag = "TypeRegistry";

		private static readonly Dictionary<string, Type> TypeDict = new Dictionary<string, Type>();

		public static void Register(string typeName, Type type)
		{
			if (type == null)
			{
				TypeDict?.TryRemove(typeName);
			}
			else
			{
				TypeDict[typeName] = type;
			}
		}

		public static Type? Lookup(string typeName, bool autoRegister = true)
		{
			Type type = TypeDict.TryGetValue(typeName);
			if (type == null)
			{
				type = Type.GetType(typeName) ?? FindType(typeName);
				if (type != null)
				{
					TaggedLog.Debug("TypeRegistry", "Type {0} not registered but was auto resolved.", type);
					if (autoRegister)
					{
						TaggedLog.Debug("TypeRegistry", "{0} = {1} is being auto registered.", typeName, type);
						Register(typeName, type);
					}
				}
			}
			return type;
		}

		public static Type? FindType(string typeName)
		{
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (Assembly assembly in assemblies)
			{
				Type type = assembly.GetType(typeName);
				if (type != null)
				{
					TaggedLog.Debug("TypeRegistry", "Found type {0} in assembly {1}", typeName, assembly.GetName().Name);
					return type;
				}
			}
			return null;
		}
	}
}
