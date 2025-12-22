using System;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using Newtonsoft.Json;
using Serilog;

namespace IDS.Portable.LogicalDevice.Json
{
	public class MacJsonHexStringConverter : JsonConverter
	{
		public override void WriteJson(JsonWriter? writer, object? value, Newtonsoft.Json.JsonSerializer serializer)
		{
			if (writer == null)
			{
				Log.Warning("WARNING: MacJsonHexStringConverter function $writer is null.");
				return;
			}
			if (!(value is MAC mAC) || mAC.Count <= 0)
			{
				writer!.WriteNull();
				return;
			}
			byte[] array = new byte[mAC.Count];
			for (int i = 0; i < mAC.Count; i++)
			{
				array[i] = mAC[i];
			}
			try
			{
				string value2 = new HexBinaryArray(array);
				writer?.WriteValue(value2);
			}
			catch (Exception arg)
			{
				Log.Warning($"WARNING: JSON serialization issue for MAC Address. {arg}");
			}
		}

		public override object? ReadJson(JsonReader? reader, Type objectType, object? existingValue, Newtonsoft.Json.JsonSerializer serializer)
		{
			if (reader == null)
			{
				Log.Warning("WARNING: MacJsonHexStringConverter function $reader is null.");
				return null;
			}
			if (reader!.TokenType == JsonToken.Null)
			{
				Log.Warning("WARNING: A MAC Address with e value of null found during JSON deserialization.");
				return null;
			}
			if (reader!.TokenType != JsonToken.String || !(reader!.Value is string value))
			{
				return null;
			}
			try
			{
				byte[] buffer = new HexBinaryArray(value);
				new MAC(buffer);
				return new MAC(buffer);
			}
			catch (Exception arg)
			{
				Log.Warning($"WARNING: JSON deserialization issue for MAC Address. {arg}");
				return null;
			}
		}

		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(MAC);
		}
	}
}
