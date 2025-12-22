using System;
using System.Collections.Generic;

namespace ids.portable.common.Metrics
{
	public class DurationMetric : IDurationMetric
	{
		private readonly Queue<(long delta, long time)> _dataBuffer;

		private long _totalTicks;

		private int _totalMeasurements;

		public TimeSpan DeltaThreshold { get; }

		public TimeSpan SamplingWindow { get; }

		public int Count { get; private set; }

		public TimeSpan Average { get; private set; }

		public DurationMetric(TimeSpan deltaThreshold, TimeSpan samplingWindow)
		{
			DeltaThreshold = deltaThreshold;
			SamplingWindow = samplingWindow;
			_dataBuffer = new Queue<(long, long)>();
			Count = 0;
			Average = TimeSpan.Zero;
		}

		public void Update(TimeSpan delta, TimeSpan timeIndex)
		{
			if (delta > DeltaThreshold)
			{
				Count++;
				_totalTicks += delta.Ticks;
				_totalMeasurements++;
				_dataBuffer.Enqueue((delta.Ticks, timeIndex.Ticks));
			}
			while (_totalMeasurements > 0 && timeIndex.Ticks - _dataBuffer.Peek().time > SamplingWindow.Ticks)
			{
				_totalTicks -= _dataBuffer.Dequeue().delta;
				_totalMeasurements--;
			}
			Average = ((_totalMeasurements > 0) ? TimeSpan.FromTicks(_totalTicks / _totalMeasurements) : TimeSpan.Zero);
		}
	}
}
