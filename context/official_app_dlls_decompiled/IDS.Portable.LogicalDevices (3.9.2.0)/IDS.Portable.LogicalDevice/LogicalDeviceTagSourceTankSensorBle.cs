using System;
using System.ComponentModel;
using System.Reflection;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;

namespace IDS.Portable.LogicalDevice
{
	[JsonObject(MemberSerialization.OptIn)]
	public class LogicalDeviceTagSourceTankSensorBle : JsonSerializable<LogicalDeviceTagSourceTankSensorBle>, ILogicalDeviceSnapshotTag, ILogicalDeviceTag, IEquatable<ILogicalDeviceTag>, IJsonSerializerClass
	{
		[JsonProperty]
		public virtual string SerializerClass => GetType().Name;

		[JsonProperty]
		public Guid DeviceId { get; }

		[JsonProperty]
		[JsonConverter(typeof(MacJsonHexStringConverter))]
		public MAC AccessoryMacAddress { get; }

		[JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
		[DefaultValue("")]
		public string SoftwarePartNumber { get; }

		[JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
		[DefaultValue("")]
		public string DeviceName { get; }

		public bool Equals(ILogicalDeviceTag other)
		{
			if (other is LogicalDeviceTagSourceTankSensorBle logicalDeviceTagSourceTankSensorBle && DeviceId.Equals(logicalDeviceTagSourceTankSensorBle.DeviceId))
			{
				return AccessoryMacAddress.Equals(logicalDeviceTagSourceTankSensorBle.AccessoryMacAddress);
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is LogicalDeviceTagSourceTankSensorBle logicalDeviceTagSourceTankSensorBle && DeviceId.Equals(logicalDeviceTagSourceTankSensorBle.DeviceId))
			{
				return AccessoryMacAddress.Equals(logicalDeviceTagSourceTankSensorBle.AccessoryMacAddress);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return DeviceId.GetHashCode();
		}

		[JsonConstructor]
		public LogicalDeviceTagSourceTankSensorBle(Guid deviceId, MAC accessoryMacAddress, string softwarePartNumber, string deviceName)
		{
			DeviceId = deviceId;
			AccessoryMacAddress = accessoryMacAddress;
			SoftwarePartNumber = softwarePartNumber;
			DeviceName = deviceName;
		}

		static LogicalDeviceTagSourceTankSensorBle()
		{
			Type declaringType = MethodBase.GetCurrentMethod().DeclaringType;
			TypeRegistry.Register(declaringType.Name, declaringType);
		}

		public override string ToString()
		{
			return $"TankSensorBle({DeviceId}: {AccessoryMacAddress})";
		}
	}
}
