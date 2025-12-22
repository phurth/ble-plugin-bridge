using System.Collections.Generic;
using System.Linq;
using Serilog.Core;
using Serilog.Events;

namespace IDS.Portable.Common
{
	public class TagEnricher : Singleton<TagEnricher>, ILogEventEnricher
	{
		public const string PropertyKey = "Tag";

		private TagEnricher()
		{
		}

		public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
		{
			string text = Enumerable.FirstOrDefault(Enumerable.Select(Enumerable.Where(logEvent.Properties, (KeyValuePair<string, LogEventPropertyValue> kvp) => kvp.Key == "SourceContext"), (KeyValuePair<string, LogEventPropertyValue> kvp) => kvp.Value.ToString("l", null))) ?? "";
			logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Tag", text));
		}
	}
}
