using System;
using System.Collections.Generic;
using IDS.Portable.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IDS.Portable.LogicalDevice.Json
{
	public class ByteDictionaryJsonHexStringConverter : JsonConverter
	{
		public const string LogTag = "ByteDictionaryJsonHexStringConverter";

		public const string KeyToken = "k";

		public const string ValueToken = "v";

		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(Dictionary<byte, byte[]>);
		}

		public override void WriteJson(JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
		{
			Dictionary<byte, byte[]> obj = (Dictionary<byte, byte[]>)value;
			JArray jArray = new JArray();
			foreach (KeyValuePair<byte, byte[]> item2 in obj)
			{
				try
				{
					JObject item = new JObject
					{
						{
							"k",
							(int)item2.Key
						},
						{
							"v",
							(item2.Value == null) ? "" : new HexBinaryArray(item2.Value).ToString()
						}
					};
					jArray.Add(item);
				}
				catch (Exception arg)
				{
					TaggedLog.Warning("ByteDictionaryJsonHexStringConverter", $"WARNING: JSON WRITE serialization issue for Byte Array. {arg}.");
				}
			}
			jArray.WriteTo(writer);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
		{
			JArray jArray = JArray.Load(reader);
			Dictionary<byte, byte[]> dictionary = new Dictionary<byte, byte[]>();
			foreach (JObject item in jArray.Children<JObject>())
			{
				try
				{
					byte b = (byte)item["k"].Value<int>();
					string value = item["v"].Value<string>();
					dictionary[b] = new HexBinaryArray(value).HexBytes;
				}
				catch (Exception arg)
				{
					TaggedLog.Warning("ByteDictionaryJsonHexStringConverter", $"WARNING: JSON READ serialization issue for Byte Array. {arg}.");
				}
			}
			return dictionary;
		}
	}
}
