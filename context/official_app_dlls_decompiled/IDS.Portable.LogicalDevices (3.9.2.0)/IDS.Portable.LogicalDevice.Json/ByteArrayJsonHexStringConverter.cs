using System;
using IDS.Portable.Common;
using Newtonsoft.Json;

namespace IDS.Portable.LogicalDevice.Json
{
	public class ByteArrayJsonHexStringConverter : JsonConverter
	{
		public const string LogTag = "ByteArrayJsonHexStringConverter";

		public override void WriteJson(JsonWriter? writer, object? value, Newtonsoft.Json.JsonSerializer serializer)
		{
			if (writer == null)
			{
				TaggedLog.Warning("ByteArrayJsonHexStringConverter", "WARNING: ByteArrayJsonHexStringConverter function $writer is null.");
				return;
			}
			if (!(value is byte[] value2))
			{
				writer!.WriteNull();
				return;
			}
			try
			{
				string value3 = new HexBinaryArray(value2);
				writer!.WriteValue(value3);
			}
			catch (Exception arg)
			{
				TaggedLog.Warning("ByteArrayJsonHexStringConverter", $"WARNING: JSON serialization issue for Byte Array. {arg}.");
			}
		}

		public override object ReadJson(JsonReader? reader, Type objectType, object? existingValue, Newtonsoft.Json.JsonSerializer serializer)
		{
			if (reader == null)
			{
				TaggedLog.Warning("ByteArrayJsonHexStringConverter", "WARNING: ByteArrayJsonHexStringConverter function $reader is null.");
				return Array.Empty<byte>();
			}
			if (reader!.TokenType != JsonToken.String || !(reader!.Value is string value))
			{
				return Array.Empty<byte>();
			}
			try
			{
				return (byte[])new HexBinaryArray(value);
			}
			catch (Exception arg)
			{
				TaggedLog.Warning("ByteArrayJsonHexStringConverter", $"WARNING: JSON deserialization issue for Byte Array. {arg}");
				return Array.Empty<byte>();
			}
		}

		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(byte[]);
		}
	}
}
