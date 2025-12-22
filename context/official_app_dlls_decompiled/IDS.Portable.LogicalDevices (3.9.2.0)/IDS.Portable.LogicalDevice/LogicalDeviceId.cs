using System;
using System.Collections.Concurrent;
using System.Reflection;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;

namespace IDS.Portable.LogicalDevice
{
	[JsonObject(MemberSerialization.OptIn)]
	public class LogicalDeviceId : JsonSerializable<LogicalDeviceId>, ILogicalDeviceId, IJsonSerializable, IJsonSerializerClass, IComparable, IEquatable<ILogicalDeviceId>
	{
		private ConcurrentDictionary<LogicalDeviceIdFormat, string> _cachedToStringResultDict = new ConcurrentDictionary<LogicalDeviceIdFormat, string>();

		private const string IdLdiImmutablePrefix = "LDIS";

		private const string IdLdiImmutablePrefixWithFunctionClass = "LDIL";

		private const string IdLdiPrefix = "LDI";

		[JsonProperty]
		[JsonConverter(typeof(DEVICE_TYPE_JsonConverter))]
		public DEVICE_TYPE DeviceType { get; }

		[JsonProperty]
		public int DeviceInstance { get; }

		[JsonProperty]
		[JsonConverter(typeof(PRODUCT_ID_JsonConverter))]
		public PRODUCT_ID ProductId { get; }

		[JsonProperty]
		[JsonConverter(typeof(MacJsonHexStringConverter))]
		public MAC? ProductMacAddress { get; }

		[JsonProperty]
		[JsonConverter(typeof(FUNCTION_NAME_JsonConverter))]
		public FUNCTION_NAME FunctionName { get; internal set; }

		[JsonProperty]
		public int FunctionInstance { get; internal set; }

		public FUNCTION_CLASS FunctionClass => DeviceType.GetPreferredFunctionClass(FunctionName);

		[JsonProperty]
		public virtual string SerializerClass => GetType().Name;

		[Obsolete("FunctionString is deprecated, please use ToString(LogicalDeviceIdFormat.FunctionNameFull) instead.")]
		public string FunctionString => ToString(LogicalDeviceIdFormat.FunctionNameCommon);

		static LogicalDeviceId()
		{
			Type declaringType = MethodBase.GetCurrentMethod().DeclaringType;
			TypeRegistry.Register(declaringType.Name, declaringType);
		}

		public LogicalDeviceId(LogicalDeviceId logicalDeviceId)
			: this(logicalDeviceId.DeviceType, logicalDeviceId.DeviceInstance, logicalDeviceId.FunctionName, logicalDeviceId.FunctionInstance, logicalDeviceId.ProductId, logicalDeviceId.ProductMacAddress)
		{
		}

		[JsonConstructor]
		public LogicalDeviceId(DEVICE_TYPE deviceType, int deviceInstance, FUNCTION_NAME functionName, int functionInstance, PRODUCT_ID productId, MAC? productMacAddress)
		{
			DeviceType = deviceType ?? ((DEVICE_TYPE)(byte)0);
			ProductId = productId ?? PRODUCT_ID.UNKNOWN;
			DeviceInstance = deviceInstance;
			ProductMacAddress = productMacAddress;
			FunctionName = functionName ?? FUNCTION_NAME.UNKNOWN;
			FunctionInstance = functionInstance;
		}

		public string MakeImmutableUniqueId(bool isFunctionClassChangeable)
		{
			if (!isFunctionClassChangeable)
			{
				return string.Format("{0}{1:X2}.{2:X2}.{3}.{4}", "LDIL", DeviceType.Value, DeviceInstance, FunctionClass, this.MacAsHexString());
			}
			return string.Format("{0}{1:X2}.{2:X2}.{3}", "LDIS", DeviceType.Value, DeviceInstance, this.MacAsHexString());
		}

		public LogicalDeviceId(DEVICE_TYPE deviceType, int deviceInstance, FUNCTION_NAME functionName, int functionInstance)
			: this(deviceType, deviceInstance, functionName, functionInstance, PRODUCT_ID.UNKNOWN, null)
		{
		}

		public LogicalDeviceId(DEVICE_ID deviceId, MAC productMacAddress)
			: this(deviceId.DeviceType, deviceId.DeviceInstance, deviceId.FunctionName, deviceId.FunctionInstance, deviceId.ProductID, productMacAddress)
		{
		}

		public LogicalDeviceId(DEVICE_ID deviceId)
			: this(deviceId.DeviceType, deviceId.DeviceInstance, deviceId.FunctionName, deviceId.FunctionInstance, deviceId.ProductID, null)
		{
		}

		public DEVICE_ID MakeDeviceId(byte? rawCapability, byte productInstance = 0)
		{
			PRODUCT_ID productId = ProductId;
			DEVICE_TYPE deviceType = DeviceType;
			int deviceInstance = DeviceInstance;
			FUNCTION_NAME functionName = FunctionName;
			int functionInstance = FunctionInstance;
			return new DEVICE_ID(productId, productInstance, deviceType, deviceInstance, functionName, functionInstance, rawCapability);
		}

		public DEVICE_ID MakeDeviceId<TCapability>(TCapability capability, byte productInstance = 0) where TCapability : ILogicalDeviceCapability
		{
			return MakeDeviceId((byte?)capability.GetRawValue(), productInstance);
		}

		public bool IsForDeviceId(DEVICE_ID deviceId, MAC withDeviceMacAddress)
		{
			if (DeviceType != deviceId.DeviceType || FunctionName != deviceId.FunctionName || DeviceInstance != deviceId.DeviceInstance || FunctionInstance != deviceId.FunctionInstance)
			{
				return false;
			}
			if (ProductId != deviceId.ProductID)
			{
				return false;
			}
			MAC? productMacAddress = ProductMacAddress;
			if ((object)productMacAddress != null && productMacAddress!.CompareTo(withDeviceMacAddress) == 0)
			{
				return true;
			}
			if (ProductMacAddress == null && withDeviceMacAddress == null)
			{
				return true;
			}
			return false;
		}

		public bool IsForDeviceIdIgnoringName(DEVICE_ID deviceId, MAC withDeviceMacAddress, bool ignoreFunctionClass)
		{
			if (DeviceType != deviceId.DeviceType)
			{
				return false;
			}
			if (!ignoreFunctionClass)
			{
				FUNCTION_CLASS preferredFunctionClass = deviceId.DeviceType.GetPreferredFunctionClass(deviceId.FunctionName);
				if (FunctionClass != preferredFunctionClass)
				{
					return false;
				}
			}
			if (ProductId != deviceId.ProductID)
			{
				return false;
			}
			if (DeviceInstance != deviceId.DeviceInstance)
			{
				return false;
			}
			MAC? productMacAddress = ProductMacAddress;
			if ((object)productMacAddress != null && productMacAddress!.CompareTo(withDeviceMacAddress) == 0)
			{
				return true;
			}
			if (ProductMacAddress == null && withDeviceMacAddress == null)
			{
				return true;
			}
			return false;
		}

		public bool IsMatchingPhysicalHardware(PRODUCT_ID withProductId, DEVICE_TYPE withDeviceType, int withDeviceInstance, MAC withDeviceMacAddress)
		{
			if (ProductId != withProductId)
			{
				return false;
			}
			if (DeviceType != withDeviceType)
			{
				return false;
			}
			if (DeviceInstance != withDeviceInstance)
			{
				return false;
			}
			MAC? productMacAddress = ProductMacAddress;
			if ((object)productMacAddress != null && productMacAddress!.CompareTo(withDeviceMacAddress) == 0)
			{
				return true;
			}
			if (ProductMacAddress == null && withDeviceMacAddress == null)
			{
				return true;
			}
			return false;
		}

		public bool Rename(FUNCTION_NAME functionName, int functionInstance)
		{
			FunctionName = functionName;
			FunctionInstance = functionInstance;
			_cachedToStringResultDict.Clear();
			return true;
		}

		public bool Equals(ILogicalDeviceId other)
		{
			return Equals((object)other);
		}

		public override bool Equals(object obj)
		{
			if (this == obj)
			{
				return true;
			}
			if (obj == null)
			{
				return false;
			}
			if (obj.GetType() != GetType())
			{
				return false;
			}
			ILogicalDeviceId logicalDeviceId = (ILogicalDeviceId)obj;
			if (DeviceType != logicalDeviceId.DeviceType)
			{
				return false;
			}
			if (FunctionClass != logicalDeviceId.FunctionClass)
			{
				return false;
			}
			if (FunctionName != logicalDeviceId.FunctionName)
			{
				return false;
			}
			if (FunctionInstance != logicalDeviceId.FunctionInstance)
			{
				return false;
			}
			if (DeviceInstance != logicalDeviceId.DeviceInstance)
			{
				return false;
			}
			if (ProductId != logicalDeviceId.ProductId)
			{
				return false;
			}
			MAC? productMacAddress = ProductMacAddress;
			if ((object)productMacAddress != null && productMacAddress!.CompareTo(logicalDeviceId.ProductMacAddress) == 0)
			{
				return true;
			}
			if (ProductMacAddress == null && logicalDeviceId.ProductMacAddress == null)
			{
				return true;
			}
			return false;
		}

		public override int GetHashCode()
		{
			int num = 27;
			num = num * 21 + DeviceType.GetHashCode();
			num = num * 21 + DeviceInstance.GetHashCode();
			num = num * 21 + ProductId.GetHashCode();
			if (ProductMacAddress != null)
			{
				num = num * 21 + ProductMacAddress!.GetHashCode();
			}
			return num;
		}

		public static bool operator ==(LogicalDeviceId s1, LogicalDeviceId s2)
		{
			return s1?.Equals(s2) ?? ((object)s2 == null);
		}

		public static bool operator !=(LogicalDeviceId s1, LogicalDeviceId s2)
		{
			return !(s1 == s2);
		}

		public int CompareTo(object obj)
		{
			if (obj == null)
			{
				return 1;
			}
			if (obj is ILogicalDeviceId logicalDeviceId)
			{
				int num = string.Compare(FunctionClass.GetName(), logicalDeviceId.FunctionClass.GetName(), StringComparison.Ordinal);
				if (num == 0)
				{
					num = string.Compare(FunctionName.ToString(), logicalDeviceId.FunctionName.ToString(), StringComparison.Ordinal);
				}
				if (num == 0)
				{
					int num2 = ((FunctionInstance != 15) ? FunctionInstance : 0);
					int value = ((logicalDeviceId.FunctionInstance != 15) ? logicalDeviceId.FunctionInstance : 0);
					num = num2.CompareTo(value);
				}
				if (num == 0)
				{
					num = DeviceInstance.CompareTo(logicalDeviceId.DeviceInstance);
				}
				if (num == 0)
				{
					num = ((!(ProductMacAddress != null) || !(logicalDeviceId.ProductMacAddress != null)) ? ((!(ProductMacAddress == null)) ? 1 : (-1)) : ProductMacAddress!.CompareTo(logicalDeviceId.ProductMacAddress));
				}
				return num;
			}
			throw new ArgumentException("Object is not an ILogicalDeviceId");
		}

		public string ToString(LogicalDeviceIdFormat format)
		{
			string text = _cachedToStringResultDict.TryGetValue(format);
			if (text == null)
			{
				text = format.FormatLogicalId(this);
				_cachedToStringResultDict[format] = text;
			}
			return text;
		}

		public virtual string ToLdiStringEncoding()
		{
			return string.Format("{0}{1:X2}.{2:X2}.{3}.{4:X4}.{5:X4}.{6:X2}", "LDI", DeviceType.Value, DeviceInstance, this.MacAsHexString(), ProductId.Value, FunctionName.Value, FunctionInstance);
		}

		public override string ToString()
		{
			return ToString(LogicalDeviceIdFormat.Debug);
		}

		public virtual ILogicalDeviceId Clone()
		{
			return new LogicalDeviceId(this);
		}
	}
}
