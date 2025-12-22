using System;

namespace ids.portable.common.Metrics
{
	public interface IDurationMetric
	{
		int Count { get; }

		TimeSpan Average { get; }

		TimeSpan DeltaThreshold { get; }

		TimeSpan SamplingWindow { get; }
	}
}
