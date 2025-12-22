using System;
using System.Reflection;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;

namespace IDS.Portable.LogicalDevice
{
	[JsonObject(MemberSerialization.OptIn)]
	public class LogicalDeviceTagFavorite : JsonSerializable<LogicalDeviceTagFavorite>, ILogicalDeviceTagFavorite, ILogicalDeviceSnapshotTag, ILogicalDeviceTag, IEquatable<ILogicalDeviceTag>, IJsonSerializerClass
	{
		public static LogicalDeviceTagFavorite DefaultFavoriteTag;

		[JsonProperty]
		public virtual string SerializerClass => GetType().Name;

		[JsonProperty]
		public string Name { get; }

		public bool Equals(ILogicalDeviceTag other)
		{
			if (other is LogicalDeviceTagFavorite logicalDeviceTagFavorite)
			{
				return string.Equals(Name, logicalDeviceTagFavorite.Name);
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is LogicalDeviceTagFavorite logicalDeviceTagFavorite)
			{
				return string.Equals(Name, logicalDeviceTagFavorite.Name);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return Name?.GetHashCode() ?? 0;
		}

		public LogicalDeviceTagFavorite()
			: this("FAVORITE")
		{
		}

		[JsonConstructor]
		public LogicalDeviceTagFavorite(string name)
		{
			Name = name;
		}

		static LogicalDeviceTagFavorite()
		{
			DefaultFavoriteTag = new LogicalDeviceTagFavorite();
			Type declaringType = MethodBase.GetCurrentMethod().DeclaringType;
			TypeRegistry.Register(declaringType.Name, declaringType);
		}

		public override string ToString()
		{
			return Name ?? "";
		}
	}
}
