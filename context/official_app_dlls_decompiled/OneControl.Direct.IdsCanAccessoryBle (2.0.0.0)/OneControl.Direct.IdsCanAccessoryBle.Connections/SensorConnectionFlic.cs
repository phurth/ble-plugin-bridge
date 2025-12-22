using System;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;

namespace OneControl.Direct.IdsCanAccessoryBle.Connections
{
	[JsonObject(MemberSerialization.OptIn)]
	public class SensorConnectionFlic : SensorConnectionBleBase<SensorConnectionFlic>, ISensorConnectionBle, ISensorConnection, IComparable, IJsonSerializable, IDirectConnectionSerializable, IDirectConnection, IEndPointConnection, IJsonSerializerClass, IEndPointConnectionBle
	{
		[JsonProperty]
		public override string ConnectionNameFriendly { get; }

		[JsonProperty]
		[JsonConverter(typeof(MacJsonHexStringConverter))]
		public MAC AccessoryMac { get; }

		[JsonProperty]
		public string SerialNumber { get; }

		static SensorConnectionFlic()
		{
			SensorConnectionBleBase<SensorConnectionFlic>.RegisterJsonSerializer();
		}

		public SensorConnectionFlic(string connectionNameFriendly, Guid connectionGuid, MAC accessoryMac, string serialNumber)
			: base(connectionGuid)
		{
			ConnectionNameFriendly = connectionNameFriendly;
			AccessoryMac = accessoryMac;
			SerialNumber = serialNumber;
		}
	}
}
