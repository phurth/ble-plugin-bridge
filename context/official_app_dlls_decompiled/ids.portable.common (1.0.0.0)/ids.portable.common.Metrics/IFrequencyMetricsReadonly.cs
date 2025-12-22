namespace ids.portable.common.Metrics
{
	public interface IFrequencyMetricsReadonly
	{
		long Count { get; }

		long MinTimeMs { get; }

		long MaxTimeMs { get; }

		double AverageTimeMs { get; }

		double TotalTimeMs { get; }

		double UpdatesPerSecond { get; }
	}
}
