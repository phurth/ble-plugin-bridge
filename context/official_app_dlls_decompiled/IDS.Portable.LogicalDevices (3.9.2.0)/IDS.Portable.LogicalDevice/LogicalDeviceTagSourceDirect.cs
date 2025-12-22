using System;
using System.Reflection;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;

namespace IDS.Portable.LogicalDevice
{
	[JsonObject(MemberSerialization.OptIn)]
	public class LogicalDeviceTagSourceDirect : JsonSerializable<LogicalDeviceTagSourceDirect>, ILogicalDeviceSnapshotTag, ILogicalDeviceTag, IEquatable<ILogicalDeviceTag>, IJsonSerializerClass
	{
		[JsonProperty]
		public virtual string SerializerClass => GetType().Name;

		[JsonProperty]
		public string Name { get; }

		public bool Equals(ILogicalDeviceTag other)
		{
			if (other is LogicalDeviceTagSourceDirect logicalDeviceTagSourceDirect)
			{
				return string.Equals(Name, logicalDeviceTagSourceDirect.Name, StringComparison.Ordinal);
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is LogicalDeviceTagSourceDirect logicalDeviceTagSourceDirect)
			{
				return string.Equals(Name, logicalDeviceTagSourceDirect.Name, StringComparison.Ordinal);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return Name?.GetHashCode() ?? 0;
		}

		public LogicalDeviceTagSourceDirect()
			: this("DIRECT")
		{
		}

		[JsonConstructor]
		public LogicalDeviceTagSourceDirect(string name)
		{
			Name = name;
		}

		static LogicalDeviceTagSourceDirect()
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
