using System;
using System.Globalization;
using Newtonsoft.Json;
using Serilog;

namespace IDS.Portable.LogicalDevice.Json
{
	public class JsonStringConverter<TValue> : JsonConverter where TValue : IConvertible
	{
		public override void WriteJson(JsonWriter? writer, object? value, JsonSerializer serializer)
		{
			if (writer == null)
			{
				Log.Warning("WARNING: JsonStringConverter function $writer is null.");
				return;
			}
			if (value is IConvertible convertible)
			{
				try
				{
					string value2 = convertible.ToString(CultureInfo.InvariantCulture);
					writer!.WriteValue(value2);
					return;
				}
				catch (Exception arg)
				{
					Log.Warning($"WARNING: JSON serialization issue for MAC Address. {arg}");
					return;
				}
			}
			Log.Warning("WARNING: JsonStringConverter don't know how to convert value to string.");
			writer!.WriteNull();
		}

		public override object? ReadJson(JsonReader? reader, Type objectType, object? existingValue, JsonSerializer serializer)
		{
			if (reader == null)
			{
				Log.Warning("WARNING: JsonStringConverter function $reader is null.");
				return null;
			}
			if (reader!.TokenType == JsonToken.Null)
			{
				return null;
			}
			if (reader!.TokenType != JsonToken.String || !(reader!.Value is string text))
			{
				Log.Warning(string.Format("WARNING: {0} expected a string during JSON deserialization, but got {1}.", "JsonStringConverter", reader!.TokenType));
				return null;
			}
			try
			{
				return Convert.ChangeType(text, typeof(TValue));
			}
			catch (Exception ex)
			{
				Log.Warning($"WARNING: JSON deserialization unable to converter {text} to {typeof(TValue)}: {ex.Message}");
				return null;
			}
		}

		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(TValue);
		}
	}
}
