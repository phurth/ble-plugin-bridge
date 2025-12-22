using System;
using IDS.Core.IDS_CAN;
using IDS.Portable.LogicalDevice;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OneControl.Devices.TankSensor.Mopeka;

namespace OneControl.Direct.IdsCanAccessoryBle.Connections
{
	[JsonObject(MemberSerialization.OptIn)]
	public class SensorConnectionMopeka : SensorConnectionBleBase<SensorConnectionMopeka>
	{
		[JsonProperty]
		public override string ConnectionNameFriendly { get; }

		[JsonProperty]
		[JsonConverter(typeof(MacJsonHexStringConverter))]
		public MAC MacAddress { get; }

		[JsonProperty]
		public FunctionName DefaultFunctionName { get; set; }

		[JsonProperty]
		public byte DefaultFunctionInstance { get; }

		[JsonProperty]
		public int DefaultTankSizeId { get; }

		[JsonProperty]
		public float DefaultTankHeightInMm { get; }

		[JsonProperty]
		public bool DefaultIsNotificationEnabled { get; }

		[JsonProperty]
		public int DefaultNotificationThreshold { get; }

		[JsonProperty]
		public float DefaultAccelXOffset { get; }

		[JsonProperty]
		public float DefaultAccelYOffset { get; }

		[JsonProperty]
		[JsonConverter(typeof(StringEnumConverter))]
		public TankHeightUnits DefaultPreferredUnits { get; }

		static SensorConnectionMopeka()
		{
			SensorConnectionBleBase<SensorConnectionMopeka>.RegisterJsonSerializer();
		}

		[JsonConstructor]
		public SensorConnectionMopeka(string connectionNameFriendly, Guid connectionGuid, MAC macAddress, FunctionName defaultFunctionName, byte defaultFunctionInstance, int defaultTankSizeId, float defaultTankHeightInMm, bool defaultIsNotificationEnabled, int defaultNotificationThreshold, float defaultAccelXOffset = 0f, float defaultAccelYOffset = 0f, TankHeightUnits defaultPreferredUnits = TankHeightUnits.Centimeters)
			: base(connectionGuid)
		{
			ConnectionNameFriendly = connectionNameFriendly;
			MacAddress = macAddress;
			DefaultFunctionName = defaultFunctionName;
			DefaultFunctionInstance = defaultFunctionInstance;
			DefaultTankSizeId = defaultTankSizeId;
			DefaultTankHeightInMm = defaultTankHeightInMm;
			DefaultIsNotificationEnabled = defaultIsNotificationEnabled;
			DefaultNotificationThreshold = defaultNotificationThreshold;
			DefaultAccelXOffset = defaultAccelXOffset;
			DefaultAccelYOffset = defaultAccelXOffset;
			DefaultPreferredUnits = defaultPreferredUnits;
		}
	}
}
