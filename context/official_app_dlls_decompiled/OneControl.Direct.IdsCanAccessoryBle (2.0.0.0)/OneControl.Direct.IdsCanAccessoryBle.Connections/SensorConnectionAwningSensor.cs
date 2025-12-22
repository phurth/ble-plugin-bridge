using System;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;

namespace OneControl.Direct.IdsCanAccessoryBle.Connections
{
	[JsonObject(MemberSerialization.OptIn)]
	public class SensorConnectionAwningSensor : SensorConnectionBleBase<SensorConnectionAwningSensor>, ISensorConnectionBleLocap, ISensorConnectionBle, ISensorConnection, IComparable, IJsonSerializable, IDirectConnectionSerializable, IDirectConnection, IEndPointConnection, IJsonSerializerClass, IEndPointConnectionBle
	{
		[JsonProperty]
		public override string ConnectionNameFriendly { get; }

		[JsonProperty]
		[JsonConverter(typeof(MacJsonHexStringConverter))]
		public MAC AccessoryMac { get; }

		[JsonProperty]
		public string SoftwarePartNumber { get; }

		static SensorConnectionAwningSensor()
		{
			SensorConnectionBleBase<SensorConnectionAwningSensor>.RegisterJsonSerializer();
		}

		public SensorConnectionAwningSensor(string connectionNameFriendly, Guid connectionGuid, MAC accessoryMac, string softwarePartNumber)
			: base(connectionGuid)
		{
			ConnectionNameFriendly = connectionNameFriendly;
			AccessoryMac = accessoryMac;
			SoftwarePartNumber = softwarePartNumber;
		}
	}
}
