using System;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;

namespace OneControl.Direct.IdsCanAccessoryBle.Connections
{
	[JsonObject(MemberSerialization.OptIn)]
	public class SensorConnectionTankSensor : SensorConnectionBleBase<SensorConnectionTankSensor>, ISensorConnectionBleLocap, ISensorConnectionBle, ISensorConnection, IComparable, IJsonSerializable, IDirectConnectionSerializable, IDirectConnection, IEndPointConnection, IJsonSerializerClass, IEndPointConnectionBle
	{
		[JsonProperty]
		public override string ConnectionNameFriendly { get; }

		[JsonProperty]
		[JsonConverter(typeof(MacJsonHexStringConverter))]
		public MAC AccessoryMac { get; }

		[JsonProperty]
		public string SoftwarePartNumber { get; }

		static SensorConnectionTankSensor()
		{
			SensorConnectionBleBase<SensorConnectionTankSensor>.RegisterJsonSerializer();
		}

		public SensorConnectionTankSensor(string connectionNameFriendly, Guid connectionGuid, MAC accessoryMac, string softwarePartNumber)
			: base(connectionGuid)
		{
			ConnectionNameFriendly = connectionNameFriendly;
			AccessoryMac = accessoryMac;
			SoftwarePartNumber = softwarePartNumber;
		}
	}
}
