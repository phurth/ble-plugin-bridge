using System;
using IDS.Core.Types;
using IDS.Portable.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace IDS.Portable.LogicalDevice
{
	[JsonObject(MemberSerialization.OptIn)]
	public readonly struct LogicalDevicePidSnapshot : IEquatable<LogicalDevicePidSnapshot>
	{
		[JsonProperty("Value")]
		private readonly ulong _value;

		[JsonProperty]
		[JsonConverter(typeof(StringEnumConverter))]
		public Pid Id { get; }

		[JsonIgnore]
		public UInt48 Value => (UInt48)_value;

		[JsonConstructor]
		public LogicalDevicePidSnapshot(Pid id, UInt48 value)
		{
			Id = id;
			_value = value;
		}

		public bool Equals(LogicalDevicePidSnapshot other)
		{
			if (Id == other.Id)
			{
				return Value.Equals(other.Value);
			}
			return false;
		}

		public override bool Equals(object? obj)
		{
			if (obj is LogicalDevicePidSnapshot other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return 17.Hash(Id).Hash(Value);
		}
	}
}
