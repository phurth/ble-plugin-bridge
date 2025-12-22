using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ids.portable.common.Metrics
{
	public class FrequencyMetrics : IFrequencyMetricsReadonly
	{
		private Stopwatch? _timerFromFirstUpdate;

		private Stopwatch? _timerFromLastUpdate;

		public long Count { get; private set; }

		public long MinTimeMs { get; private set; }

		public long MaxTimeMs { get; private set; }

		public double AverageTimeMs { get; private set; }

		public double TotalTimeMs => ((double?)_timerFromFirstUpdate?.ElapsedMilliseconds) ?? 0.0;

		public double UpdatesPerSecond { get; private set; }

		public void Update()
		{
			if (_timerFromFirstUpdate == null)
			{
				_timerFromFirstUpdate = Stopwatch.StartNew();
			}
			if (_timerFromLastUpdate == null)
			{
				_timerFromLastUpdate = Stopwatch.StartNew();
			}
			Count++;
			long elapsedMilliseconds = _timerFromLastUpdate!.ElapsedMilliseconds;
			MaxTimeMs = ((MaxTimeMs < elapsedMilliseconds) ? elapsedMilliseconds : MaxTimeMs);
			if (elapsedMilliseconds != 0L && elapsedMilliseconds < MinTimeMs)
			{
				MinTimeMs = elapsedMilliseconds;
			}
			AverageTimeMs = (double)_timerFromFirstUpdate!.ElapsedMilliseconds / (double)Count;
			int num = (int)_timerFromFirstUpdate!.Elapsed.TotalSeconds;
			UpdatesPerSecond = ((num == 0) ? 0f : ((float)Count / (float)num));
			_timerFromLastUpdate!.Restart();
		}

		public void Clear()
		{
			_timerFromFirstUpdate?.Reset();
			_timerFromLastUpdate?.Reset();
			Count = 0L;
			MinTimeMs = 0L;
			MaxTimeMs = 0L;
			AverageTimeMs = 0.0;
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(68, 6);
			defaultInterpolatedStringHandler.AppendLiteral("Per Second=");
			defaultInterpolatedStringHandler.AppendFormatted(UpdatesPerSecond, "F0");
			defaultInterpolatedStringHandler.AppendLiteral(" Count=");
			defaultInterpolatedStringHandler.AppendFormatted(Count);
			defaultInterpolatedStringHandler.AppendLiteral(" AverageTime=");
			defaultInterpolatedStringHandler.AppendFormatted((long)AverageTimeMs);
			defaultInterpolatedStringHandler.AppendLiteral("ms MaxTime=");
			defaultInterpolatedStringHandler.AppendFormatted(MaxTimeMs);
			defaultInterpolatedStringHandler.AppendLiteral("ms MinTime=");
			defaultInterpolatedStringHandler.AppendFormatted(MinTimeMs);
			defaultInterpolatedStringHandler.AppendLiteral("ms TotalTime=");
			defaultInterpolatedStringHandler.AppendFormatted((long)TotalTimeMs);
			defaultInterpolatedStringHandler.AppendLiteral("ms");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
