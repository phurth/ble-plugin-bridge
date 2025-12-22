using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ids.portable.common.Metrics
{
	public class DurationMetrics
	{
		public struct Duration : IDisposable
		{
			private DurationMetrics? _metrics;

			public TimeSpan Began { get; }

			public Duration(DurationMetrics metrics)
			{
				_metrics = metrics;
				Began = _metrics!.Now;
			}

			public void Dispose()
			{
				_metrics?.Update(this);
				_metrics = null;
			}
		}

		private readonly Stopwatch _timer = new Stopwatch();

		public readonly IReadOnlyList<IDurationMetric> Metrics;

		public TimeSpan Now => TimeSpan.FromTicks(_timer.ElapsedTicks);

		public DurationMetrics(IReadOnlyList<IDurationMetric> metrics)
		{
			Metrics = metrics;
			_timer.Restart();
		}

		public Duration StartTimingMetric()
		{
			return new Duration(this);
		}

		private void Update(Duration duration)
		{
			TimeSpan delta = Now - duration.Began;
			foreach (DurationMetric metric in Metrics)
			{
				metric.Update(delta, duration.Began);
			}
		}
	}
}
