using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IDS.Portable.Common
{
	public class JsonSerializerInterfaceListConverter<TJsonSerializerInterface> : JsonSerializerInterfaceListConverter<TJsonSerializerInterface, TJsonSerializerInterface> where TJsonSerializerInterface : IJsonSerializerClass
	{
	}
	public class JsonSerializerInterfaceListConverter<TJsonSerializerInterface, TJsonSerializerDefaultClass> : JsonConverter where TJsonSerializerInterface : IJsonSerializerClass where TJsonSerializerDefaultClass : TJsonSerializerInterface
	{
		public const string LogTag = "JsonSerializerInterfaceListConverter";

		public override bool CanWrite => false;

		public override bool CanRead => true;

		public virtual Type? DefaultConstructionType()
		{
			return null;
		}

		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(List<TJsonSerializerInterface>);
		}

		public override void WriteJson(JsonWriter writer, object? value, Newtonsoft.Json.JsonSerializer serializer)
		{
			throw new InvalidOperationException("Use default serialization.");
		}

		public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, Newtonsoft.Json.JsonSerializer serializer)
		{
			JToken jToken = JToken.Load(reader);
			if (jToken.Type != JTokenType.Array)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(35, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Unexpected JSON format encountered ");
				defaultInterpolatedStringHandler.AppendFormatted(jToken);
				throw new JsonSerializationException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			List<TJsonSerializerInterface> list = new List<TJsonSerializerInterface>();
			foreach (JToken item2 in jToken.Children())
			{
				try
				{
					Type type = JsonSerializerInterfaceObjectConverter<TJsonSerializerInterface, TJsonSerializerDefaultClass>.ResolveSerializer((string?)item2["SerializerClass"]);
					object obj = item2.ToObject(type ?? DefaultConstructionType());
					if (obj is TJsonSerializerInterface)
					{
						TJsonSerializerInterface item = (TJsonSerializerInterface)obj;
						list.Add(item);
					}
				}
				catch (Exception ex)
				{
					TaggedLog.Error("JsonSerializerInterfaceListConverter", "Invalid item token {0}: {1}", item2, ex.Message);
				}
			}
			return list;
		}
	}
}
