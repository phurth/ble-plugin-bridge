using System;
using System.Collections.Generic;
using IDS.Core.IDS_CAN;
using Newtonsoft.Json;
using Serilog;

namespace IDS.Portable.LogicalDevice.Json
{
	public class MacJsonArrayConverter : JsonConverter
	{
		public override void WriteJson(JsonWriter? writer, object? value, JsonSerializer serializer)
		{
			if (writer == null)
			{
				Log.Warning("WARNING: MacJsonArrayConverter function $writer is null.");
				return;
			}
			if (!(value is MAC))
			{
				writer!.WriteNull();
				return;
			}
			writer!.WriteStartArray();
			foreach (byte item in (MAC)value)
			{
				writer!.WriteValue(item);
			}
			writer!.WriteEndArray();
		}

		public override object? ReadJson(JsonReader? reader, Type objectType, object? existingValue, JsonSerializer serializer)
		{
			if (reader == null)
			{
				Log.Warning("WARNING: MacJsonArrayConverter function $reader is null.");
				return null;
			}
			if (reader!.TokenType == JsonToken.Null)
			{
				Log.Warning("WARNING: A MAC Address with e value of null found during JSON deserialization.");
				return null;
			}
			if (reader!.TokenType != JsonToken.StartArray)
			{
				throw new FormatException($"Unexpected token parsing MAC. Expected StartArray, got {reader!.TokenType}.");
			}
			List<byte> list = new List<byte>();
			while (reader!.Read())
			{
				switch (reader!.TokenType)
				{
				case JsonToken.Integer:
					list.Add(Convert.ToByte(reader!.Value));
					break;
				case JsonToken.EndArray:
					return new MAC(list.ToArray());
				default:
					throw new FormatException($"Unexpected token while reading MAC: {reader!.TokenType}");
				case JsonToken.Comment:
					break;
				}
			}
			throw new FormatException("Unexpected end while reading MAC.");
		}

		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(MAC);
		}
	}
}
