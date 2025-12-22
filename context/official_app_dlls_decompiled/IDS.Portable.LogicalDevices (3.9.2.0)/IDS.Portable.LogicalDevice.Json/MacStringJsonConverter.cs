using System;
using System.Linq;
using IDS.Core.IDS_CAN;
using Newtonsoft.Json;
using Serilog;

namespace IDS.Portable.LogicalDevice.Json
{
	public class MacStringJsonConverter : JsonConverter
	{
		public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
		{
			if (reader == null)
			{
				Log.Warning("WARNING: MacStringJsonConverter function $reader is null.");
				return Array.Empty<byte>();
			}
			if (reader.TokenType == JsonToken.Null)
			{
				Log.Warning("WARNING: A MAC Address with e value of null found during JSON deserialization.");
				return null;
			}
			if (reader.TokenType != JsonToken.String || !(reader.Value is string text))
			{
				return null;
			}
			try
			{
				char[] separator = new char[4] { ':', '-', '.', ' ' };
				string[] array = text.Split(separator);
				if (array.Length != 6)
				{
					return null;
				}
				return new MAC(Enumerable.ToArray(Enumerable.Select(array, (string s) => Convert.ToByte(s, 16))));
			}
			catch (Exception arg)
			{
				Log.Warning($"WARNING: JSON deserialization issue for MAC Address. {arg}");
				return null;
			}
		}

		public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
		{
			if (writer == null)
			{
				Log.Warning("WARNING: MacStringJsonConverter function $writer is null.");
				return;
			}
			if (!(value is MAC))
			{
				writer.WriteNull();
				return;
			}
			try
			{
				writer?.WriteValue(value!.ToString());
			}
			catch (Exception arg)
			{
				Log.Warning($"WARNING: JSON serialization issue for MAC Address. {arg}");
			}
		}

		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(MAC);
		}
	}
}
