using System;
using System.Reflection;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;

namespace IDS.Portable.LogicalDevice
{
	[JsonObject(MemberSerialization.OptIn)]
	public class LogicalDeviceTagSourceMyRvLinkBle : JsonSerializable<LogicalDeviceTagSourceMyRvLinkBle>, ILogicalDeviceSnapshotTag, ILogicalDeviceTag, IEquatable<ILogicalDeviceTag>, IJsonSerializerClass
	{
		[JsonProperty]
		public virtual string SerializerClass => GetType().Name;

		[JsonProperty]
		public string ConnectionId { get; }

		[JsonProperty]
		public Guid ConnectionGuid { get; }

		public bool Equals(ILogicalDeviceTag other)
		{
			if (other is LogicalDeviceTagSourceMyRvLinkBle logicalDeviceTagSourceMyRvLinkBle && ConnectionGuid.Equals(logicalDeviceTagSourceMyRvLinkBle.ConnectionGuid))
			{
				return string.Equals(ConnectionId, logicalDeviceTagSourceMyRvLinkBle.ConnectionId, StringComparison.Ordinal);
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is LogicalDeviceTagSourceMyRvLinkBle logicalDeviceTagSourceMyRvLinkBle && ConnectionGuid.Equals(logicalDeviceTagSourceMyRvLinkBle.ConnectionGuid))
			{
				return string.Equals(ConnectionId, logicalDeviceTagSourceMyRvLinkBle.ConnectionId, StringComparison.Ordinal);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return ConnectionId?.GetHashCode() ?? 0;
		}

		[JsonConstructor]
		public LogicalDeviceTagSourceMyRvLinkBle(string connectionId, Guid connectionGuid)
		{
			ConnectionId = connectionId ?? "";
			ConnectionGuid = connectionGuid;
		}

		static LogicalDeviceTagSourceMyRvLinkBle()
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
