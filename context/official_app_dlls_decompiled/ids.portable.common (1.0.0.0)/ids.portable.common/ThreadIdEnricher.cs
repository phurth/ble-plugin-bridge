using System.Threading;
using Serilog.Core;
using Serilog.Events;

namespace IDS.Portable.Common
{
	public class ThreadIdEnricher : Singleton<ThreadIdEnricher>, ILogEventEnricher
	{
		public const string PropertyKey = "ThreadId";

		private ThreadIdEnricher()
		{
		}

		public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
		{
			logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ThreadId", Thread.CurrentThread.ManagedThreadId));
		}
	}
}
