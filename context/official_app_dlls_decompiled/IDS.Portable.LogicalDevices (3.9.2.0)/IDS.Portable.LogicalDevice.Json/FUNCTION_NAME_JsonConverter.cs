using System;
using IDS.Core.IDS_CAN;
using Newtonsoft.Json;

namespace IDS.Portable.LogicalDevice.Json
{
	public class FUNCTION_NAME_JsonConverter : JsonConverter
	{
		public override void WriteJson(JsonWriter? writer, object? value, JsonSerializer serializer)
		{
			if (value == null)
			{
				throw new ArgumentException("class FUNCTION_NAME_JsonConverter - Function WriteJson null value for writer");
			}
			writer?.WriteValue(((FUNCTION_NAME)value).Value);
		}

		public override object ReadJson(JsonReader? reader, Type objectType, object? existingValue, JsonSerializer serializer)
		{
			return (FUNCTION_NAME)Convert.ToUInt16(reader?.Value);
		}

		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(FUNCTION_NAME);
		}
	}
}
