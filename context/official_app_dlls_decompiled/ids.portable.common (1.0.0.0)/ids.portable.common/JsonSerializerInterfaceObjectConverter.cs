using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IDS.Portable.Common
{
	public class JsonSerializerInterfaceObjectConverter<TJsonSerializerInterface> : JsonSerializerInterfaceObjectConverter<TJsonSerializerInterface, TJsonSerializerInterface> where TJsonSerializerInterface : IJsonSerializerClass
	{
	}
	public class JsonSerializerInterfaceObjectConverter<TJsonSerializerInterface, TJsonSerializerDefaultClass> : JsonConverter where TJsonSerializerInterface : IJsonSerializerClass where TJsonSerializerDefaultClass : TJsonSerializerInterface
	{
		public const string LogTag = "JsonSerializerInterfaceObjectConverter";

		public override bool CanWrite => false;

		public override bool CanRead => true;

		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(TJsonSerializerInterface);
		}

		public virtual Type? DefaultConstructionType()
		{
			return null;
		}

		public override void WriteJson(JsonWriter writer, object? value, Newtonsoft.Json.JsonSerializer serializer)
		{
			throw new InvalidOperationException("Use default serialization.");
		}

		public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, Newtonsoft.Json.JsonSerializer serializer)
		{
			JObject jObject = JObject.Load(reader);
			Type type = ResolveSerializer(jObject["SerializerClass"]?.Value<string>());
			return jObject.ToObject(type ?? DefaultConstructionType());
		}

		public static Type? ResolveSerializer(string serializerClassName)
		{
			Type type = null;
			try
			{
				if (string.IsNullOrEmpty(serializerClassName))
				{
					throw new ArgumentException("Invalid/unknown serialization class specified");
				}
				type = TypeRegistry.Lookup(serializerClassName);
				if (type == null)
				{
					type = typeof(TJsonSerializerDefaultClass);
					TaggedLog.Error("JsonSerializerInterfaceObjectConverter", "ReadJson unable to resolve serializerClass from TypeRegistry for {0} of type {1}, using default serializer {2} in its place", serializerClassName, typeof(TJsonSerializerInterface), type);
				}
				if (!Enumerable.Contains(type.GetInterfaces(), typeof(TJsonSerializerInterface)))
				{
					type = typeof(TJsonSerializerDefaultClass);
					TaggedLog.Error("JsonSerializerInterfaceObjectConverter", "ReadJson serializerClassName for {0} of type {1}, doesn't implement {2}, using default serializer {3} in its place", serializerClassName, typeof(TJsonSerializerInterface), typeof(TJsonSerializerInterface), type);
					return type;
				}
				return type;
			}
			catch (ArgumentNullException)
			{
				return type;
			}
			catch (Exception ex2)
			{
				TaggedLog.Error("JsonSerializerInterfaceObjectConverter", "Unable to resolve ResolveSerializer for {0}: {1}", serializerClassName, ex2.Message);
				return type;
			}
		}
	}
}
