using System;
using System.Runtime.CompilerServices;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OneControl.Devices.TankSensor.Mopeka;

namespace OneControl.Direct.IdsCanAccessoryBle.Mopeka
{
	[JsonObject(MemberSerialization.OptIn)]
	public class MopekaLogicalDeviceTag : JsonSerializable<MopekaLogicalDeviceTag>, ILogicalDeviceSnapshotTag, ILogicalDeviceTag, IEquatable<ILogicalDeviceTag>, IJsonSerializerClass
	{
		[JsonProperty]
		[JsonConverter(typeof(StringEnumConverter))]
		public TankHeightUnits PreferredUnits;

		[JsonProperty]
		public virtual string SerializerClass => GetType().Name;

		[JsonProperty]
		[JsonConverter(typeof(MacJsonHexStringConverter))]
		public MAC MacAddress { get; }

		[JsonProperty]
		public int TankSizeId { get; }

		[JsonProperty]
		public float TankHeightInMm { get; }

		[JsonProperty]
		public bool IsNotificationEnabled { get; }

		[JsonProperty]
		public int NotificationThreshold { get; }

		[JsonProperty]
		public float AccelXOffset { get; }

		[JsonProperty]
		public float AccelYOffset { get; }

		[JsonProperty("IsFaulted")]
		public bool IsTankLevelFaulted { get; }

		[JsonProperty]
		public bool IsBatteryLevelFaulted { get; }

		public bool Equals(ILogicalDeviceTag other)
		{
			if (other is MopekaLogicalDeviceTag mopekaLogicalDeviceTag)
			{
				return (MacAddress, TankSizeId, TankHeightInMm, IsNotificationEnabled, NotificationThreshold, AccelXOffset, AccelYOffset, IsTankLevelFaulted, IsBatteryLevelFaulted, PreferredUnits).Equals((mopekaLogicalDeviceTag.MacAddress, mopekaLogicalDeviceTag.TankSizeId, mopekaLogicalDeviceTag.TankHeightInMm, mopekaLogicalDeviceTag.IsNotificationEnabled, mopekaLogicalDeviceTag.NotificationThreshold, mopekaLogicalDeviceTag.AccelXOffset, mopekaLogicalDeviceTag.AccelYOffset, mopekaLogicalDeviceTag.IsTankLevelFaulted, mopekaLogicalDeviceTag.IsBatteryLevelFaulted, mopekaLogicalDeviceTag.PreferredUnits));
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is MopekaLogicalDeviceTag other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return MacAddress.GetHashCode();
		}

		[JsonConstructor]
		public MopekaLogicalDeviceTag(MAC macAddress, int tankSizeId, float tankHeightInMm, bool isNotificationEnabled, int notificationThreshold, float accelXOffset, float accelYOffset, bool isTankLevelFaulted, bool isBatteryLevelFaulted, TankHeightUnits preferredUnits)
		{
			MacAddress = macAddress;
			TankSizeId = tankSizeId;
			TankHeightInMm = tankHeightInMm;
			IsNotificationEnabled = isNotificationEnabled;
			NotificationThreshold = notificationThreshold;
			AccelXOffset = accelXOffset;
			AccelYOffset = accelYOffset;
			IsTankLevelFaulted = isTankLevelFaulted;
			IsBatteryLevelFaulted = isBatteryLevelFaulted;
			PreferredUnits = preferredUnits;
		}

		static MopekaLogicalDeviceTag()
		{
			TypeRegistry.Register("MopekaLogicalDeviceTag", typeof(MopekaLogicalDeviceTag));
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(163, 10);
			defaultInterpolatedStringHandler.AppendLiteral("MAC=");
			defaultInterpolatedStringHandler.AppendFormatted(MacAddress);
			defaultInterpolatedStringHandler.AppendLiteral(", TankSizeId=");
			defaultInterpolatedStringHandler.AppendFormatted(TankSizeId);
			defaultInterpolatedStringHandler.AppendLiteral(", TankHeightInMm=");
			defaultInterpolatedStringHandler.AppendFormatted(TankHeightInMm);
			defaultInterpolatedStringHandler.AppendLiteral(", IsNotificationEnabled=");
			defaultInterpolatedStringHandler.AppendFormatted(IsNotificationEnabled);
			defaultInterpolatedStringHandler.AppendLiteral(", NotificationThreshold=");
			defaultInterpolatedStringHandler.AppendFormatted(NotificationThreshold);
			defaultInterpolatedStringHandler.AppendLiteral("%, AccelOffset=(");
			defaultInterpolatedStringHandler.AppendFormatted(AccelXOffset);
			defaultInterpolatedStringHandler.AppendLiteral(", ");
			defaultInterpolatedStringHandler.AppendFormatted(AccelYOffset);
			defaultInterpolatedStringHandler.AppendLiteral("), IsTankLevelFaulted=");
			defaultInterpolatedStringHandler.AppendFormatted(IsTankLevelFaulted);
			defaultInterpolatedStringHandler.AppendLiteral(", IsBatteryLevelFaulted=");
			defaultInterpolatedStringHandler.AppendFormatted(IsBatteryLevelFaulted);
			defaultInterpolatedStringHandler.AppendLiteral(", PreferredUnits=");
			defaultInterpolatedStringHandler.AppendFormatted(PreferredUnits);
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
