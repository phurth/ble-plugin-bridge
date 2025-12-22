using System.Collections.Concurrent;
using System.Diagnostics;

namespace IDS.Portable.Common.Utils
{
	public static class Profiler
	{
		public const string LogTag = "Profiler";

		private const string LogMessageTemplate = "Stopwatch {0} took {1}{2}";

		private static readonly ConcurrentDictionary<string, Stopwatch> watches = new ConcurrentDictionary<string, Stopwatch>();

		public static void Start(object view)
		{
			Start(view.GetType().Name);
		}

		public static void Start(string tag, string comment = "")
		{
			if (!string.IsNullOrEmpty(comment))
			{
				comment = " : " + comment;
			}
			TaggedLog.Debug("Profiler", "Starting Stopwatch {0}{1}", tag, comment);
			Stopwatch stopwatch2 = (watches[tag] = new Stopwatch());
			stopwatch2.Start();
		}

		public static void Stop(string tag, string comment = "")
		{
			if (watches.TryRemove(tag, out var stopwatch))
			{
				if (!string.IsNullOrEmpty(comment))
				{
					comment = " : " + comment;
				}
				TaggedLog.Debug("Profiler", "Stopwatch {0} took {1}{2}", tag, stopwatch?.Elapsed, comment);
				stopwatch?.Stop();
			}
		}

		public static void Split(string tag, string comment = "")
		{
			if (watches.TryGetValue(tag, out var stopwatch))
			{
				if (!string.IsNullOrEmpty(comment))
				{
					comment = " : " + comment;
				}
				TaggedLog.Debug("Profiler", "Stopwatch {0} took {1}{2}", tag, stopwatch?.Elapsed, comment);
			}
		}
	}
}
