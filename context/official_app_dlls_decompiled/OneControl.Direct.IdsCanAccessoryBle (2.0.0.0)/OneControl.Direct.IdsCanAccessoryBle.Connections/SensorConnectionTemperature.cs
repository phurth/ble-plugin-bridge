using System;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;

namespace OneControl.Direct.IdsCanAccessoryBle.Connections
{
	[JsonObject(MemberSerialization.OptIn)]
	public class SensorConnectionTemperature : SensorConnectionBleBase<SensorConnectionTemperature>, ISensorConnectionBleLocap, ISensorConnectionBle, ISensorConnection, IComparable, IJsonSerializable, IDirectConnectionSerializable, IDirectConnection, IEndPointConnection, IJsonSerializerClass, IEndPointConnectionBle
	{
		[JsonProperty]
		public override string ConnectionNameFriendly { get; }

		[JsonProperty]
		[JsonConverter(typeof(MacJsonHexStringConverter))]
		public MAC AccessoryMac { get; }

		[JsonProperty]
		public string SoftwarePartNumber { get; }

		static SensorConnectionTemperature()
		{
			SensorConnectionBleBase<SensorConnectionTemperature>.RegisterJsonSerializer();
		}

		public SensorConnectionTemperature(string connectionNameFriendly, Guid connectionGuid, MAC accessoryMac, string softwarePartNumber)
			: base(connectionGuid)
		{
			ConnectionNameFriendly = connectionNameFriendly;
			AccessoryMac = accessoryMac;
			SoftwarePartNumber = softwarePartNumber;
		}
	}
}
