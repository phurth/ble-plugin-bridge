using System;
using System.Linq;
using Newtonsoft.Json;
using Serilog;

namespace IDS.Portable.LogicalDevice.Json
{
	public class CapabilityFeatureIdJsonConverter : JsonConverter
	{
		public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
		{
			if (reader == null)
			{
				Log.Warning("WARNING: CapabilityFeatureIdJsonConverter function $reader is null.");
				return LogicalDeviceCapabilityFeatureId.Unknown;
			}
			if (reader.TokenType == JsonToken.Null)
			{
				Log.Warning("WARNING: A Capability Feature ID with e value of null found during JSON deserialization.");
				return LogicalDeviceCapabilityFeatureId.Unknown;
			}
			if (reader.TokenType != JsonToken.String || !(reader.Value is string text))
			{
				return LogicalDeviceCapabilityFeatureId.Unknown;
			}
			try
			{
				Enum.GetValues(typeof(LogicalDeviceCapabilityFeatureId));
				foreach (LogicalDeviceCapabilityFeatureId item in Enumerable.Cast<LogicalDeviceCapabilityFeatureId>(Enum.GetValues(typeof(LogicalDeviceCapabilityFeatureId))))
				{
					if (item.ToString() == text)
					{
						return item;
					}
					if (item.GetCloudToken() == text)
					{
						return item;
					}
				}
				return LogicalDeviceCapabilityFeatureId.Unknown;
			}
			catch (Exception arg)
			{
				Log.Warning($"WARNING: JSON deserialization issue for Capability Feature ID. {arg}");
				return LogicalDeviceCapabilityFeatureId.Unknown;
			}
		}

		public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
		{
			if (writer == null)
			{
				Log.Warning("WARNING: CapabilityFeatureIdJsonConverter function $writer is null.");
				return;
			}
			if (!(value is LogicalDeviceCapabilityFeatureId capabilityFeatureId))
			{
				writer.WriteNull();
				return;
			}
			try
			{
				writer?.WriteValue(capabilityFeatureId.GetCloudToken() ?? capabilityFeatureId.ToString());
			}
			catch (Exception arg)
			{
				Log.Warning($"WARNING: JSON serialization issue for Capability Feature ID. {arg}");
			}
		}

		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(LogicalDeviceCapabilityFeatureId);
		}
	}
}
