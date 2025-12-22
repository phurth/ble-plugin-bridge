using System.Collections.Concurrent;
using System.Linq;
using Serilog;

namespace IDS.Portable.Common
{
	public static class TaggedLog
	{
		private static ILogger _defaultLogger = Log.Logger;

		private const int MaxCachedTags = 2048;

		private static readonly ConcurrentDictionary<string, ILogger> _taggedLoggerCache = new ConcurrentDictionary<string, ILogger>();

		public static ILogger Tag(string tag)
		{
			ILogger defaultLogger = _defaultLogger;
			if (defaultLogger != Log.Logger)
			{
				_taggedLoggerCache.Clear();
				_defaultLogger = Log.Logger;
				defaultLogger = _defaultLogger;
			}
			if (string.IsNullOrWhiteSpace(tag))
			{
				return defaultLogger;
			}
			if (_taggedLoggerCache.TryGetValue(tag, out var result))
			{
				return result;
			}
			result = defaultLogger.ForContext("SourceContext", tag);
			_taggedLoggerCache.TryAdd(tag, result);
			return result;
		}

		public static void Debug(string tag, string message, params object[] args)
		{
			if (Enumerable.Any(args))
			{
				Tag(tag).Debug(message, args);
			}
			else
			{
				Tag(tag).Debug(message);
			}
		}

		public static void Information(string tag, string message, params object[] args)
		{
			if (Enumerable.Any(args))
			{
				Tag(tag).Information(message, args);
			}
			else
			{
				Tag(tag).Information(message);
			}
		}

		public static void Warning(string tag, string message, params object[] args)
		{
			if (Enumerable.Any(args))
			{
				Tag(tag).Warning(message, args);
			}
			else
			{
				Tag(tag).Warning(message);
			}
		}

		public static void Error(string tag, string message, params object[] args)
		{
			if (Enumerable.Any(args))
			{
				Tag(tag).Error(message, args);
			}
			else
			{
				Tag(tag).Error(message);
			}
		}
	}
}
