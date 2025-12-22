using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace IDS.Portable.Common.Manifest
{
	public class ManifestLogSerializer
	{
		public static string MakeJSONFromWebLog(IEnumerable<IManifestLogEntry> webLog)
		{
			return JsonConvert.SerializeObject(webLog, Formatting.Indented);
		}

		public static List<IManifestLogEntry> MakeWebLogFromJSON(string json)
		{
			return Enumerable.ToList(Enumerable.Cast<IManifestLogEntry>(JsonConvert.DeserializeObject<List<ManifestLogEntry>>(json)));
		}
	}
}
