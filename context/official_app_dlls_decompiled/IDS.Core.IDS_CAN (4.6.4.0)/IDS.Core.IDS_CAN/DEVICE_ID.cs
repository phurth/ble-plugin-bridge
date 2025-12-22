using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IDS.Core.IDS_CAN
{
	[JsonConverter(typeof(DeviceIdConverter))]
	public struct DEVICE_ID
	{
		private class DeviceIdConverter : JsonConverter
		{
			public override bool CanConvert(Type objectType)
			{
				return objectType == typeof(DEVICE_ID);
			}

			public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
			{
				JToken jToken = JToken.Load(reader);
				if (jToken.Type == JTokenType.Null)
				{
					return null;
				}
				return new DEVICE_ID(jToken.ToString());
			}

			public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
			{
				if (value is DEVICE_ID dEVICE_ID)
				{
					JToken.FromObject(dEVICE_ID.JsonString).WriteTo(writer);
					return;
				}
				throw new ArgumentException();
			}
		}

		public PRODUCT_ID ProductID;

		public DEVICE_TYPE DeviceType;

		public FUNCTION_NAME FunctionName;

		private byte mDeviceInstance;

		private byte mFunctionInstance;

		public byte ProductInstance { get; set; }

		public int DeviceInstance
		{
			get
			{
				return mDeviceInstance;
			}
			set
			{
				mDeviceInstance = (byte)((uint)value & 0xFu);
			}
		}

		public int FunctionInstance
		{
			get
			{
				return mFunctionInstance;
			}
			set
			{
				mFunctionInstance = (byte)((uint)value & 0xFu);
			}
		}

		public byte? DeviceCapabilities { get; set; }

		private string JsonString
		{
			get
			{
				ulong num = (ushort)ProductID;
				num <<= 8;
				num |= ProductInstance;
				num <<= 8;
				num |= (byte)DeviceType;
				num <<= 4;
				num |= (byte)DeviceInstance;
				num <<= 16;
				num |= (ushort)FunctionName;
				num <<= 4;
				num |= (byte)FunctionInstance;
				if (DeviceCapabilities.HasValue)
				{
					num <<= 8;
					return (num | DeviceCapabilities.Value).ToString("X16");
				}
				return num.ToString("X14");
			}
		}

		public bool IsValid
		{
			get
			{
				if (ProductID.IsValid)
				{
					return DeviceType.IsValid;
				}
				return false;
			}
		}

		public string ProductString
		{
			get
			{
				if (ProductID == null)
				{
					ProductID = PRODUCT_ID.UNKNOWN;
				}
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(3, 2);
				defaultInterpolatedStringHandler.AppendFormatted(ProductID);
				defaultInterpolatedStringHandler.AppendLiteral(" @");
				defaultInterpolatedStringHandler.AppendFormatted(ProductInstance.HexString());
				defaultInterpolatedStringHandler.AppendLiteral("h");
				return defaultInterpolatedStringHandler.ToStringAndClear();
			}
		}

		public string DeviceString
		{
			get
			{
				if (DeviceType == null)
				{
					DeviceType = (byte)0;
				}
				if (DeviceInstance > 0)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 2);
					defaultInterpolatedStringHandler.AppendFormatted(DeviceType);
					defaultInterpolatedStringHandler.AppendLiteral(" #");
					defaultInterpolatedStringHandler.AppendFormatted(DeviceInstance);
					return defaultInterpolatedStringHandler.ToStringAndClear();
				}
				return DeviceType.ToString();
			}
		}

		public string FunctionString
		{
			get
			{
				if (FunctionName == null)
				{
					FunctionName = FUNCTION_NAME.UNKNOWN;
				}
				if (FunctionInstance > 0)
				{
					if (FunctionName.Name.Contains("{0}"))
					{
						return FunctionName.Name.Replace("{0}", FunctionInstance.ToString()) ?? "";
					}
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(1, 2);
					defaultInterpolatedStringHandler.AppendFormatted(FunctionName);
					defaultInterpolatedStringHandler.AppendLiteral(" ");
					defaultInterpolatedStringHandler.AppendFormatted(FunctionInstance);
					return defaultInterpolatedStringHandler.ToStringAndClear();
				}
				return FunctionName.ToString();
			}
		}

		public ICON Icon
		{
			get
			{
				if (!DeviceType.IsValid)
				{
					return ICON.UNKNOWN;
				}
				if (FunctionName.IsValid)
				{
					return FunctionName.Icon;
				}
				return DeviceType.Icon;
			}
		}

		public DEVICE_ID(PRODUCT_ID product_id, byte product_instance, DEVICE_TYPE device_type, int device_instance, FUNCTION_NAME function_name, int function_instance, byte? device_capabilities)
		{
			ProductID = product_id;
			ProductInstance = product_instance;
			DeviceType = device_type;
			mDeviceInstance = (byte)((uint)device_instance & 0xFu);
			FunctionName = function_name;
			mFunctionInstance = (byte)((uint)function_instance & 0xFu);
			DeviceCapabilities = device_capabilities;
		}

		private DEVICE_ID(string json)
		{
			ulong num = ulong.Parse(json, NumberStyles.HexNumber);
			switch (json.Length)
			{
			default:
				throw new ArgumentException();
			case 16:
				DeviceCapabilities = (byte)num;
				num >>= 8;
				break;
			case 14:
				DeviceCapabilities = null;
				break;
			}
			mFunctionInstance = (byte)(num & 0xF);
			num >>= 4;
			FunctionName = (ushort)num;
			num >>= 16;
			mDeviceInstance = (byte)(num & 0xF);
			num >>= 4;
			DeviceType = (byte)num;
			num >>= 8;
			ProductInstance = (byte)num;
			num >>= 8;
			ProductID = (ushort)num;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is DEVICE_ID dEVICE_ID))
			{
				return false;
			}
			if (ProductID != dEVICE_ID.ProductID)
			{
				return false;
			}
			if (ProductInstance != dEVICE_ID.ProductInstance)
			{
				return false;
			}
			if (DeviceType != dEVICE_ID.DeviceType)
			{
				return false;
			}
			if (DeviceInstance != dEVICE_ID.DeviceInstance)
			{
				return false;
			}
			if (FunctionName != dEVICE_ID.FunctionName)
			{
				return false;
			}
			if (FunctionInstance != dEVICE_ID.FunctionInstance)
			{
				return false;
			}
			if (DeviceCapabilities != dEVICE_ID.DeviceCapabilities)
			{
				return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			return ((((((17 * 23 + ProductID.GetHashCode()) * 23 + ProductInstance.GetHashCode()) * 23 + DeviceType.GetHashCode()) * 23 + DeviceInstance.GetHashCode()) * 23 + FunctionName.GetHashCode()) * 23 + FunctionInstance.GetHashCode()) * 23 + DeviceCapabilities.GetHashCode();
		}

		public static bool operator ==(DEVICE_ID s1, DEVICE_ID s2)
		{
			return s1.Equals(s2);
		}

		public static bool operator !=(DEVICE_ID s1, DEVICE_ID s2)
		{
			return !(s1 == s2);
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(4, 3);
			defaultInterpolatedStringHandler.AppendFormatted(FunctionString);
			defaultInterpolatedStringHandler.AppendLiteral(", ");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceString);
			defaultInterpolatedStringHandler.AppendLiteral(", ");
			defaultInterpolatedStringHandler.AppendFormatted(ProductString);
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		public void Clear()
		{
			ProductID = PRODUCT_ID.UNKNOWN;
			ProductInstance = 0;
			DeviceType = (byte)0;
			DeviceInstance = 0;
			FunctionName = FUNCTION_NAME.UNKNOWN;
			FunctionInstance = 0;
			DeviceCapabilities = null;
		}
	}
}
