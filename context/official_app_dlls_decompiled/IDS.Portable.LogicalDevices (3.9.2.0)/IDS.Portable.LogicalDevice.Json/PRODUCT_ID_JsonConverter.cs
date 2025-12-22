using System;
using IDS.Core.IDS_CAN;
using Newtonsoft.Json;

namespace IDS.Portable.LogicalDevice.Json
{
	public class PRODUCT_ID_JsonConverter : JsonConverter
	{
		public override void WriteJson(JsonWriter? writer, object? value, JsonSerializer serializer)
		{
			if (value == null)
			{
				throw new ArgumentException("class DEVICE_TYPE_JsonConverter - Function WriteJson null value for writer");
			}
			writer?.WriteValue(((PRODUCT_ID)value).Value);
		}

		public override object ReadJson(JsonReader? reader, Type objectType, object? existingValue, JsonSerializer serializer)
		{
			return (PRODUCT_ID)Convert.ToUInt16(reader?.Value);
		}

		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(PRODUCT_ID);
		}
	}
}
