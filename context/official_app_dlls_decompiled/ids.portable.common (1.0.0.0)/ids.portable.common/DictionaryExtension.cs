using System.Collections.Concurrent;
using System.Collections.Generic;

namespace IDS.Portable.Common
{
	public static class DictionaryExtension
	{
		public static void TryRemove<TKey, TItem>(this IDictionary<TKey, TItem> dictionary, TKey key) where TKey : notnull
		{
			try
			{
				if (dictionary.ContainsKey(key))
				{
					dictionary.Remove(key);
				}
			}
			catch
			{
			}
		}

		public static TItem TryGetValue<TKey, TItem>(this IReadOnlyDictionary<TKey, TItem> dictionary, TKey key) where TKey : notnull
		{
			dictionary.TryGetValue(key, out var result);
			return result;
		}

		public static TItem TryGetValue<TKey, TItem>(this ConcurrentDictionary<TKey, TItem> dictionary, TKey key) where TKey : notnull
		{
			dictionary.TryGetValue(key, out var result);
			return result;
		}

		public static TItem TryGetWithCustomDefaultValue<TKey, TItem>(this IReadOnlyDictionary<TKey, TItem> dictionary, TKey key, TItem defaultValue) where TKey : notnull
		{
			if (!dictionary.TryGetValue(key, out var result))
			{
				return defaultValue;
			}
			return result;
		}

		public static TItem TryGetWithCustomDefaultValue<TKey, TItem>(this ConcurrentDictionary<TKey, TItem> dictionary, TKey key, TItem defaultValue) where TKey : notnull
		{
			if (!dictionary.TryGetValue(key, out var result))
			{
				return defaultValue;
			}
			return result;
		}

		public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> sourceDict)
		{
			Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();
			foreach (KeyValuePair<TKey, TValue> item in sourceDict)
			{
				dictionary.Add(item.Key, item.Value);
			}
			return dictionary;
		}

		public static void AddRange<TKey, TValue>(this Dictionary<TKey, TValue> source, IReadOnlyDictionary<TKey, TValue> collection)
		{
			if (source == null || collection == null || collection.Count <= 0)
			{
				return;
			}
			foreach (KeyValuePair<TKey, TValue> item in collection)
			{
				source[item.Key] = item.Value;
			}
		}
	}
}
