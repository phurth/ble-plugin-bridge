using System;
using System.Text.Json.Serialization;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;

namespace OneControl.Devices
{
	[JsonObject(MemberSerialization.OptIn)]
	public class RvDetail<TValue> : JsonSerializable<RvDetail<TValue>>, IEquatable<RvDetail<TValue>>
	{
		public const int UnknownLciId = -1;

		[JsonPropertyName("PID")]
		[JsonProperty("PID", NullValueHandling = NullValueHandling.Ignore)]
		public int LciId { get; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public TValue Value { get; }

		[JsonPropertyName("Id")]
		[JsonProperty("Id", NullValueHandling = NullValueHandling.Ignore)]
		public string SalesforceId { get; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string Name { get; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string DisplayName { get; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string Year { get; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string Priority { get; }

		[Newtonsoft.Json.JsonConstructor]
		[System.Text.Json.Serialization.JsonConstructor]
		public RvDetail(int lciId, TValue value = default(TValue), string salesforceId = null, string name = null, string displayName = null, string year = null, string priority = null)
		{
			LciId = lciId;
			Value = value;
			SalesforceId = salesforceId;
			Name = name;
			DisplayName = displayName;
			Year = year;
			Priority = priority;
			if (lciId != 0 && value is int)
			{
				Value = (TValue)(object)LciId;
				DisplayName = LciId.ToString();
			}
			if (lciId == 0 && value is int)
			{
				ref int reference = ref LciId;
				object obj = value;
				int num = (reference = (int)((obj is int) ? obj : null));
			}
		}

		public bool Equals(RvDetail<TValue> other)
		{
			if (other != null && LciId == other.LciId && object.Equals(Value, other.Value) && string.Equals(SalesforceId, other.SalesforceId) && string.Equals(Name, other.Name) && string.Equals(DisplayName, other.DisplayName) && string.Equals(Year, other.Year))
			{
				return string.Equals(Priority, other.Priority);
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (this == obj)
			{
				return true;
			}
			if (obj is RvDetail<TValue> other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return 17.Hash(LciId).Hash(Value).Hash(SalesforceId)
				.Hash(Name)
				.Hash(DisplayName)
				.Hash(Year)
				.Hash(Priority);
		}

		public override string ToString()
		{
			return $"LciId({LciId}), Value({Value}), SalesforceId({SalesforceId}), Name({Name}), DisplayName({DisplayName}), Year({Year}), Priority({Priority})";
		}
	}
}
