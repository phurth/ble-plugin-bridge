using System.Collections.Generic;
using System.Text;
using IDS.Portable.Common.Extensions;

namespace IDS.Portable.LogicalDevice
{
	public static class DictionaryExtensions
	{
		public static string DebugDump(this Dictionary<byte, byte[]> dict)
		{
			if (dict == null)
			{
				return string.Empty;
			}
			StringBuilder stringBuilder = new StringBuilder();
			foreach (KeyValuePair<byte, byte[]> item in dict)
			{
				stringBuilder.AppendLine($"[{item.Key}] = {item.Value.DebugDump()}");
			}
			return stringBuilder.ToString();
		}
	}
}
