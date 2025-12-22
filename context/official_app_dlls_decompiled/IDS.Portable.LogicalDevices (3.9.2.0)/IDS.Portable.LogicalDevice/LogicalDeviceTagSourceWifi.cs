using System;
using System.Reflection;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;

namespace IDS.Portable.LogicalDevice
{
	[JsonObject(MemberSerialization.OptIn)]
	public class LogicalDeviceTagSourceWifi : JsonSerializable<LogicalDeviceTagSourceWifi>, ILogicalDeviceSnapshotTag, ILogicalDeviceTag, IEquatable<ILogicalDeviceTag>, IJsonSerializerClass
	{
		[JsonProperty]
		public virtual string SerializerClass => GetType().Name;

		[JsonProperty]
		public string Ssid { get; }

		public bool Equals(ILogicalDeviceTag other)
		{
			if (other is LogicalDeviceTagSourceWifi logicalDeviceTagSourceWifi)
			{
				return string.Equals(Ssid, logicalDeviceTagSourceWifi.Ssid, StringComparison.Ordinal);
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is LogicalDeviceTagSourceWifi logicalDeviceTagSourceWifi)
			{
				return string.Equals(Ssid, logicalDeviceTagSourceWifi.Ssid, StringComparison.Ordinal);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return Ssid?.GetHashCode() ?? 0;
		}

		[JsonConstructor]
		public LogicalDeviceTagSourceWifi(string ssid)
		{
			Ssid = ssid;
		}

		static LogicalDeviceTagSourceWifi()
		{
			Type declaringType = MethodBase.GetCurrentMethod().DeclaringType;
			TypeRegistry.Register(declaringType.Name, declaringType);
		}

		public override string ToString()
		{
			return Ssid ?? "";
		}
	}
}
