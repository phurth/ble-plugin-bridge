using System;
using IDS.Core.IDS_CAN;
using Newtonsoft.Json;

namespace IDS.Portable.LogicalDevice.Json
{
	public class DEVICE_TYPE_JsonConverter : JsonConverter
	{
		public override void WriteJson(JsonWriter? writer, object? value, JsonSerializer serializer)
		{
			if (value == null)
			{
				throw new ArgumentException("class DEVICE_TYPE_JsonConverter - Function WriteJson null value for writer");
			}
			writer?.WriteValue(((DEVICE_TYPE)value).Value);
		}

		public override object ReadJson(JsonReader? reader, Type objectType, object? existingValue, JsonSerializer serializer)
		{
			return (DEVICE_TYPE)Convert.ToByte(reader?.Value);
		}

		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(DEVICE_TYPE);
		}
	}
}
