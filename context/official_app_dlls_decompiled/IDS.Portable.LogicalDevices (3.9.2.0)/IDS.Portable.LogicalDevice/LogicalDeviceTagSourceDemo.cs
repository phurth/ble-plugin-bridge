using System;
using System.Reflection;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;

namespace IDS.Portable.LogicalDevice
{
	[JsonObject(MemberSerialization.OptIn)]
	public class LogicalDeviceTagSourceDemo : JsonSerializable<LogicalDeviceTagSourceDemo>, ILogicalDeviceTagSourceDemo, ILogicalDeviceSnapshotTag, ILogicalDeviceTag, IEquatable<ILogicalDeviceTag>, IJsonSerializerClass
	{
		[JsonProperty]
		public virtual string SerializerClass => GetType().Name;

		[JsonProperty]
		public string Name { get; }

		public bool Equals(ILogicalDeviceTag other)
		{
			if (other is LogicalDeviceTagSourceDemo logicalDeviceTagSourceDemo)
			{
				return string.Equals(Name, logicalDeviceTagSourceDemo.Name, StringComparison.Ordinal);
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is LogicalDeviceTagSourceDemo logicalDeviceTagSourceDemo)
			{
				return string.Equals(Name, logicalDeviceTagSourceDemo.Name, StringComparison.Ordinal);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return Name?.GetHashCode() ?? 0;
		}

		public LogicalDeviceTagSourceDemo()
			: this("DEMO")
		{
		}

		[JsonConstructor]
		public LogicalDeviceTagSourceDemo(string name)
		{
			Name = name;
		}

		static LogicalDeviceTagSourceDemo()
		{
			Type declaringType = MethodBase.GetCurrentMethod().DeclaringType;
			TypeRegistry.Register(declaringType.Name, declaringType);
		}

		public override string ToString()
		{
			return Name ?? "";
		}
	}
}
