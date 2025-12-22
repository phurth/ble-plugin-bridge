using System;
using System.Diagnostics;
using System.Threading;

namespace IDS.Portable.Common.Utils
{
	public struct PerformanceTimer : IDisposable
	{
		private static ObjectPool<Stopwatch> _stopwatchObjectPool = ObjectPool<Stopwatch>.MakeObjectPool<Stopwatch>();

		public Func<string> MakeWatchDetailDescription;

		private Stopwatch? _stopwatch;

		public PerformanceTimerOption Option { get; }

		public string LogTag { get; }

		public bool IsConfigured { get; }

		public bool IsRunning => _stopwatch?.IsRunning ?? false;

		private TimeSpan? StopTimeWarning { get; }

		public PerformanceTimer(string logTag, string message, TimeSpan? stopTimeWarning = null, PerformanceTimerOption option = PerformanceTimerOption.Verbose | PerformanceTimerOption.AutoStartOnCreate)
			: this(logTag, (object)message, stopTimeWarning, option)
		{
		}

		public PerformanceTimer(string logTag, object detailObject, TimeSpan? stopTimeWarning = null, PerformanceTimerOption option = PerformanceTimerOption.Verbose | PerformanceTimerOption.AutoStartOnCreate)
		{
			Option = option;
			LogTag = logTag;
			if (detailObject is string)
			{
				MakeWatchDetailDescription = () => (detailObject as string) ?? "Null Reference Error detailObject";
			}
			else if (detailObject != null)
			{
				MakeWatchDetailDescription = () => detailObject.GetType().Name;
			}
			else
			{
				MakeWatchDetailDescription = () => string.Empty;
			}
			_stopwatch = null;
			StopTimeWarning = stopTimeWarning;
			IsConfigured = true;
			if (option.HasFlag(PerformanceTimerOption.AutoStartOnCreate))
			{
				Start();
			}
		}

		public void Start()
		{
			if (IsRunning)
			{
				TaggedLog.Warning(LogTag, "Ignoring Start as timer already started {0}", MakeWatchDetailDescription());
				return;
			}
			_stopwatch = _stopwatchObjectPool.TakeObject();
			_stopwatch!.Restart();
			if (Option.HasFlag(PerformanceTimerOption.OnShowStart))
			{
				TaggedLog.Information(LogTag, "TIMER START {0}", MakeWatchDetailDescription());
			}
		}

		public TimeSpan Stop()
		{
			Stopwatch stopwatch = Interlocked.Exchange(ref _stopwatch, null);
			if (stopwatch == null || !stopwatch.IsRunning)
			{
				return TimeSpan.Zero;
			}
			stopwatch.Stop();
			TimeSpan elapsed = stopwatch.Elapsed;
			stopwatch.Reset();
			_stopwatchObjectPool.PutObject(stopwatch);
			stopwatch = null;
			TimeSpan? stopTimeWarning = StopTimeWarning;
			if (stopTimeWarning.HasValue)
			{
				TimeSpan valueOrDefault = stopTimeWarning.GetValueOrDefault();
				if (elapsed > valueOrDefault)
				{
					TaggedLog.Warning(LogTag, "TIMER STOPPED({0}ms > {1}ms) {2}", elapsed.TotalMilliseconds, valueOrDefault.TotalMilliseconds, MakeWatchDetailDescription());
					goto IL_00ee;
				}
			}
			if (Option.HasFlag(PerformanceTimerOption.OnShowStopTotalTimeInMs))
			{
				TaggedLog.Information(LogTag, "TIMER STOPPED({0}ms {1})", elapsed.TotalMilliseconds, MakeWatchDetailDescription());
			}
			goto IL_00ee;
			IL_00ee:
			return elapsed;
		}

		public void Mark(string message)
		{
			Stopwatch stopwatch = _stopwatch;
			TimeSpan? timeSpan = stopwatch?.Elapsed;
			if (stopwatch != null && stopwatch.IsRunning)
			{
				TaggedLog.Information(LogTag, "TIMER MARK({0} - {1}ms)", message, timeSpan);
			}
		}

		public void Dispose()
		{
			Stop();
		}
	}
}
