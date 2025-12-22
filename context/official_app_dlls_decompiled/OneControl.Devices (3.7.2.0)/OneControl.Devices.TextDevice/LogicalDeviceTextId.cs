using System;
using System.Reflection;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using Newtonsoft.Json;

namespace OneControl.Devices.TextDevice
{
	[JsonObject(MemberSerialization.OptIn)]
	public class LogicalDeviceTextId : LogicalDeviceId
	{
		public const string LogTag = "LogicalDeviceTextId";

		[JsonProperty]
		public string Title { get; }

		[JsonProperty]
		public string Notes { get; }

		[JsonProperty]
		public TimeSpan Duration { get; }

		[JsonProperty]
		public bool IsReminder { get; }

		[JsonProperty]
		public override string SerializerClass => GetType().Name;

		public LogicalDeviceTextId(string title, string notes, TimeSpan duration, bool isReminder, int deviceInstance, MAC productMacAddress)
			: this((byte)1, deviceInstance, (ushort)262, 0, PRODUCT_ID.LCI_ONECONTROL_ANDROID_MOBILE_APPLICATION, productMacAddress, title, notes, duration, isReminder)
		{
		}

		[JsonConstructor]
		protected LogicalDeviceTextId(DEVICE_TYPE deviceType, int deviceInstance, FUNCTION_NAME functionName, int functionInstance, PRODUCT_ID productId, MAC productMacAddress, string title, string notes, TimeSpan duration, bool isReminder)
			: base(deviceType, deviceInstance, functionName, functionInstance, productId, productMacAddress)
		{
			Title = title;
			Duration = duration;
			Notes = notes;
			IsReminder = isReminder;
		}

		public override ILogicalDeviceId Clone()
		{
			return new LogicalDeviceTextId(base.DeviceType, base.DeviceInstance, base.FunctionName, base.FunctionInstance, base.ProductId, base.ProductMacAddress, Title, Notes, Duration, IsReminder);
		}

		static LogicalDeviceTextId()
		{
			Type declaringType = MethodBase.GetCurrentMethod().DeclaringType;
			if (!(declaringType == null))
			{
				TypeRegistry.Register(declaringType.Name, declaringType);
			}
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (this == obj)
			{
				return true;
			}
			if (!base.Equals(obj))
			{
				return false;
			}
			if (!(obj is LogicalDeviceTextId logicalDeviceTextId))
			{
				return false;
			}
			if (!Equals(this))
			{
				return false;
			}
			if (!string.Equals(Title, logicalDeviceTextId.Title))
			{
				return false;
			}
			if (!string.Equals(Notes, logicalDeviceTextId.Notes))
			{
				return false;
			}
			if (Duration != logicalDeviceTextId.Duration)
			{
				return false;
			}
			if (IsReminder != logicalDeviceTextId.IsReminder)
			{
				return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode().Hash(Title).Hash(Notes)
				.Hash(Duration)
				.Hash(IsReminder);
		}
	}
}
