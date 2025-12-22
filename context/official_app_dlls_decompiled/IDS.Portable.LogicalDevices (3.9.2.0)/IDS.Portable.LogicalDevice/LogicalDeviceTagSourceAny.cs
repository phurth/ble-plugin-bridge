using System;
using System.Reflection;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;

namespace IDS.Portable.LogicalDevice
{
	[JsonObject(MemberSerialization.OptIn)]
	public class LogicalDeviceTagSourceAny : JsonSerializable<LogicalDeviceTagSourceAny>, ILogicalDeviceTagSourceAny, ILogicalDeviceSnapshotTag, ILogicalDeviceTag, IEquatable<ILogicalDeviceTag>, IJsonSerializerClass
	{
		public static LogicalDeviceTagSourceAny DefaultAnySourceTag;

		[JsonProperty]
		public virtual string SerializerClass => GetType().Name;

		[JsonProperty]
		public string Name { get; }

		public bool Equals(ILogicalDeviceTag other)
		{
			if (other is LogicalDeviceTagSourceAny logicalDeviceTagSourceAny)
			{
				return string.Equals(Name, logicalDeviceTagSourceAny.Name, StringComparison.Ordinal);
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is LogicalDeviceTagSourceAny logicalDeviceTagSourceAny)
			{
				return string.Equals(Name, logicalDeviceTagSourceAny.Name, StringComparison.Ordinal);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return Name?.GetHashCode() ?? 0;
		}

		public LogicalDeviceTagSourceAny()
			: this("Any")
		{
		}

		[JsonConstructor]
		public LogicalDeviceTagSourceAny(string name)
		{
			Name = name;
		}

		static LogicalDeviceTagSourceAny()
		{
			DefaultAnySourceTag = new LogicalDeviceTagSourceAny();
			Type declaringType = MethodBase.GetCurrentMethod().DeclaringType;
			TypeRegistry.Register(declaringType.Name, declaringType);
		}

		public override string ToString()
		{
			return Name ?? "";
		}
	}
}
