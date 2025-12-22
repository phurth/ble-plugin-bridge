using System;
using Newtonsoft.Json;
using Serilog;

namespace IDS.Portable.LogicalDevice.Json
{
	public abstract class JsonSerializable<TObject> : IJsonSerializable where TObject : class
	{
		public bool TryJsonSerialize(out string? json)
		{
			try
			{
				json = JsonConvert.SerializeObject(this, Formatting.Indented);
				return true;
			}
			catch (Exception ex)
			{
				Log.Error("Error trying to JSON serialize: " + ex.Message + "\n" + ex.StackTrace);
				json = null;
				return false;
			}
		}

		public string? TryJsonSerialize()
		{
			if (!TryJsonSerialize(out var json))
			{
				return null;
			}
			return json;
		}

		public static bool TryJsonDeserialize(string json, out TObject? obj)
		{
			try
			{
				obj = JsonConvert.DeserializeObject<TObject>(json);
				return true;
			}
			catch (Exception ex)
			{
				Log.Error("Error trying to JSON deserialize: " + ex.Message + "\n" + ex.StackTrace);
				obj = null;
				return false;
			}
		}

		public static TObject? TryJsonDeserialize(string json)
		{
			if (!TryJsonDeserialize(json, out var obj))
			{
				return null;
			}
			return obj;
		}
	}
}
