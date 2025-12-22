using System;
using System.Reflection;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;

namespace IDS.Portable.LogicalDevice
{
	[JsonObject(MemberSerialization.OptIn)]
	public class LogicalDeviceTagSourceBle : JsonSerializable<LogicalDeviceTagSourceBle>, ILogicalDeviceSnapshotTag, ILogicalDeviceTag, IEquatable<ILogicalDeviceTag>, IJsonSerializerClass
	{
		[JsonProperty]
		public virtual string SerializerClass => GetType().Name;

		[JsonProperty]
		public string ConnectionId { get; }

		public bool Equals(ILogicalDeviceTag other)
		{
			if (other is LogicalDeviceTagSourceBle logicalDeviceTagSourceBle)
			{
				return string.Equals(ConnectionId, logicalDeviceTagSourceBle.ConnectionId, StringComparison.Ordinal);
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is LogicalDeviceTagSourceBle logicalDeviceTagSourceBle)
			{
				return string.Equals(ConnectionId, logicalDeviceTagSourceBle.ConnectionId, StringComparison.Ordinal);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return ConnectionId?.GetHashCode() ?? 0;
		}

		[JsonConstructor]
		public LogicalDeviceTagSourceBle(string connectionId)
		{
			ConnectionId = connectionId ?? "";
		}

		static LogicalDeviceTagSourceBle()
		{
			Type declaringType = MethodBase.GetCurrentMethod().DeclaringType;
			TypeRegistry.Register(declaringType.Name, declaringType);
		}

		public override string ToString()
		{
			return ConnectionId ?? "";
		}
	}
}
