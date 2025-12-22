using System;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;

namespace OneControl.Direct.IdsCanAccessoryBle.Connections
{
	[JsonObject(MemberSerialization.OptIn)]
	public class SensorConnectionEchoBrakeControl : SensorConnectionBleBase<SensorConnectionEchoBrakeControl>, ISensorConnection, IComparable, IJsonSerializable, IDirectConnectionSerializable, IDirectConnection, IEndPointConnection, IJsonSerializerClass
	{
		public const int MacSize = 6;

		private MAC? _accessoryMac;

		[JsonProperty]
		public override string ConnectionNameFriendly { get; }

		[JsonIgnore]
		public MAC AccessoryMacAddress => _accessoryMac ?? (_accessoryMac = MakeMac());

		static SensorConnectionEchoBrakeControl()
		{
			SensorConnectionBleBase<SensorConnectionEchoBrakeControl>.RegisterJsonSerializer();
		}

		private MAC MakeMac()
		{
			byte[] array = base.ConnectionGuid.ToByteArray();
			return new MAC((UInt48)array.GetValueUInt48(array.Length - 6));
		}

		public SensorConnectionEchoBrakeControl(string connectionNameFriendly, Guid connectionGuid)
			: base(connectionGuid)
		{
			ConnectionNameFriendly = connectionNameFriendly;
		}
	}
}
