using System;
using System.Reflection;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;

namespace IDS.Portable.LogicalDevice
{
	[JsonObject(MemberSerialization.OptIn)]
	public class LogicalDeviceTagSourceRemote : JsonSerializable<LogicalDeviceTagSourceRemote>, ILogicalDeviceTagSourceRemote, ILogicalDeviceSnapshotTag, ILogicalDeviceTag, IEquatable<ILogicalDeviceTag>, IJsonSerializerClass
	{
		[JsonProperty]
		public virtual string SerializerClass => GetType().Name;

		[JsonProperty]
		public string CoachId { get; }

		[JsonIgnore]
		public LogicalDeviceTagSourceRemoteType RemoteType => LogicalDeviceTagSourceRemoteType.MyRvCloud;

		public bool Equals(ILogicalDeviceTag other)
		{
			if (other is LogicalDeviceTagSourceRemote logicalDeviceTagSourceRemote && string.Equals(CoachId, logicalDeviceTagSourceRemote.CoachId, StringComparison.Ordinal))
			{
				return RemoteType == logicalDeviceTagSourceRemote.RemoteType;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is LogicalDeviceTagSourceRemote logicalDeviceTagSourceRemote && string.Equals(CoachId, logicalDeviceTagSourceRemote.CoachId, StringComparison.Ordinal))
			{
				return RemoteType == logicalDeviceTagSourceRemote.RemoteType;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return 17.Hash(CoachId ?? "").Hash(RemoteType);
		}

		[JsonConstructor]
		public LogicalDeviceTagSourceRemote(string coachId)
		{
			CoachId = coachId;
		}

		static LogicalDeviceTagSourceRemote()
		{
			Type declaringType = MethodBase.GetCurrentMethod().DeclaringType;
			TypeRegistry.Register(declaringType.Name, declaringType);
		}

		public override string ToString()
		{
			return CoachId ?? "";
		}
	}
}
