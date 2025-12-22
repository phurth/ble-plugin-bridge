using ids.portable.common.Metrics;

namespace OneControl.Direct.MyRvLink
{
	public interface IDirectConnectionMyRvLinkMetrics
	{
		IFrequencyMetricsReadonly GetFrequencyMetricForCommandSend(MyRvLinkCommandType commandType);

		IFrequencyMetricsReadonly GetFrequencyMetricForCommandFailure(MyRvLinkCommandType commandType);

		IFrequencyMetricsReadonly GetFrequencyMetricForEvent(MyRvLinkEventType eventType);
	}
}
