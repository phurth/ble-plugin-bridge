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
	public class LogicalDeviceTagSourceBatteryMonitorBle : JsonSerializable<LogicalDeviceTagSourceBatteryMonitorBle>, ILogicalDeviceSnapshotTag, ILogicalDeviceTag, IEquatable<ILogicalDeviceTag>, IJsonSerializerClass
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
			if (other is LogicalDeviceTagSourceBatteryMonitorBle logicalDeviceTagSourceBatteryMonitorBle && DeviceId.Equals(logicalDeviceTagSourceBatteryMonitorBle.DeviceId))
			{
				return AccessoryMacAddress.Equals(logicalDeviceTagSourceBatteryMonitorBle.AccessoryMacAddress);
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is LogicalDeviceTagSourceBatteryMonitorBle logicalDeviceTagSourceBatteryMonitorBle && DeviceId.Equals(logicalDeviceTagSourceBatteryMonitorBle.DeviceId))
			{
				return AccessoryMacAddress.Equals(logicalDeviceTagSourceBatteryMonitorBle.AccessoryMacAddress);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return DeviceId.GetHashCode();
		}

		[JsonConstructor]
		public LogicalDeviceTagSourceBatteryMonitorBle(Guid deviceId, MAC accessoryMacAddress, string softwarePartNumber, string deviceName)
		{
			DeviceId = deviceId;
			AccessoryMacAddress = accessoryMacAddress;
			SoftwarePartNumber = softwarePartNumber;
			DeviceName = deviceName;
		}

		static LogicalDeviceTagSourceBatteryMonitorBle()
		{
			Type declaringType = MethodBase.GetCurrentMethod().DeclaringType;
			TypeRegistry.Register(declaringType.Name, declaringType);
		}

		public override string ToString()
		{
			return $"BatteryMonitorBle({DeviceId}: {AccessoryMacAddress})";
		}
	}
}
