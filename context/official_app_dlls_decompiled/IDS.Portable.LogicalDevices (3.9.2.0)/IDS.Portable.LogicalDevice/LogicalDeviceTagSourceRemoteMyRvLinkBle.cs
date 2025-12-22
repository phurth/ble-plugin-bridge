using System;
using System.Reflection;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;

namespace IDS.Portable.LogicalDevice
{
	[JsonObject(MemberSerialization.OptIn)]
	public class LogicalDeviceTagSourceRemoteMyRvLinkBle : JsonSerializable<LogicalDeviceTagSourceRemote>, ILogicalDeviceTagSourceRemoteMyRvLink, ILogicalDeviceTagSourceRemote, ILogicalDeviceSnapshotTag, ILogicalDeviceTag, IEquatable<ILogicalDeviceTag>, IJsonSerializerClass
	{
		[JsonIgnore]
		public LogicalDeviceTagSourceRemoteType RemoteType => LogicalDeviceTagSourceRemoteType.MyRvLink;

		[JsonProperty]
		public virtual string SerializerClass => GetType().Name;

		[JsonProperty]
		public string ConnectionId { get; }

		public bool Equals(ILogicalDeviceTag other)
		{
			if (other is LogicalDeviceTagSourceRemoteMyRvLinkBle logicalDeviceTagSourceRemoteMyRvLinkBle)
			{
				return string.Equals(ConnectionId, logicalDeviceTagSourceRemoteMyRvLinkBle.ConnectionId, StringComparison.Ordinal);
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is LogicalDeviceTagSourceRemoteMyRvLinkBle logicalDeviceTagSourceRemoteMyRvLinkBle)
			{
				return string.Equals(ConnectionId, logicalDeviceTagSourceRemoteMyRvLinkBle.ConnectionId, StringComparison.Ordinal);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return ConnectionId?.GetHashCode() ?? 0;
		}

		[JsonConstructor]
		public LogicalDeviceTagSourceRemoteMyRvLinkBle(string connectionId)
		{
			ConnectionId = connectionId ?? "";
		}

		static LogicalDeviceTagSourceRemoteMyRvLinkBle()
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
