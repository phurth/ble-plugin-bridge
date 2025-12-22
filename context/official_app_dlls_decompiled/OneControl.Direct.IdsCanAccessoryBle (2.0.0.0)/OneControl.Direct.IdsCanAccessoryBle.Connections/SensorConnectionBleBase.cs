using System;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;

namespace OneControl.Direct.IdsCanAccessoryBle.Connections
{
	[JsonObject(MemberSerialization.OptIn)]
	public abstract class SensorConnectionBleBase<TSerializable> : JsonSerializable<TSerializable>, ISensorConnectionBle, ISensorConnection, IComparable, IJsonSerializable, IDirectConnectionSerializable, IDirectConnection, IEndPointConnection, IJsonSerializerClass, IEndPointConnectionBle where TSerializable : class
	{
		public const string LogTag = "SensorConnectionBleBase";

		[JsonProperty]
		public string SerializerClass => GetType().Name;

		[JsonProperty]
		public abstract string ConnectionNameFriendly { get; }

		[JsonIgnore]
		public string ConnectionId => ConnectionNameFriendly;

		[JsonProperty]
		public Guid ConnectionGuid { get; }

		protected SensorConnectionBleBase(Guid connectionGuid)
		{
			ConnectionGuid = connectionGuid;
		}

		public override string ToString()
		{
			return "'" + ConnectionNameFriendly + "'";
		}

		public override bool Equals(object obj)
		{
			if (this == obj)
			{
				return true;
			}
			if (!(obj is SensorConnectionBleBase<TSerializable> sensorConnectionBleBase))
			{
				return false;
			}
			if (ConnectionGuid == sensorConnectionBleBase.ConnectionGuid)
			{
				return string.Equals(ConnectionId, sensorConnectionBleBase.ConnectionId);
			}
			return false;
		}

		public virtual int CompareTo(object obj)
		{
			if (Equals(obj))
			{
				return 0;
			}
			if (!(obj is SensorConnectionBleBase<TSerializable> sensorConnectionBleBase))
			{
				return 1;
			}
			return string.Compare(ConnectionId, sensorConnectionBleBase.ConnectionId, StringComparison.Ordinal);
		}

		public override int GetHashCode()
		{
			return 17.Hash(ConnectionGuid);
		}

		protected static void RegisterJsonSerializer()
		{
			Type typeFromHandle = typeof(TSerializable);
			TypeRegistry.Register(typeFromHandle.Name, typeFromHandle);
		}
	}
}
