using System;
using IDS.Core.IDS_CAN;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;

namespace OneControl.Direct.IdsCanAccessoryBle.Connections
{
	[JsonObject(MemberSerialization.OptIn)]
	public class SensorConnectionTirePressureMonitor : SensorConnectionBleBase<SensorConnectionTirePressureMonitor>
	{
		public const int TpmsSoftwarePartNumberPrefix = 25254;

		public const int TpmsSoftwarePartNumberPrefixCurt = 30232;

		public static PRODUCT_ID TpmsProductIdDefault;

		public static PRODUCT_ID TpmsProductIdCurt;

		[JsonProperty]
		public override string ConnectionNameFriendly { get; }

		[JsonProperty]
		[JsonConverter(typeof(MacJsonHexStringConverter))]
		public MAC AccessoryMac { get; }

		[JsonProperty]
		public string SoftwarePartNumber { get; }

		static SensorConnectionTirePressureMonitor()
		{
			TpmsProductIdDefault = PRODUCT_ID.TPMS_TIRE_LINC;
			TpmsProductIdCurt = PRODUCT_ID.CURT_TPMS_TIRE_LINC_AUTO;
			SensorConnectionBleBase<SensorConnectionTirePressureMonitor>.RegisterJsonSerializer();
		}

		[JsonConstructor]
		public SensorConnectionTirePressureMonitor(string connectionNameFriendly, Guid connectionGuid, MAC accessoryMac, string softwarePartNumber)
			: base(connectionGuid)
		{
			ConnectionNameFriendly = connectionNameFriendly;
			AccessoryMac = accessoryMac;
			SoftwarePartNumber = softwarePartNumber;
		}
	}
}
