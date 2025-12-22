namespace IDS.Core
{
	public interface IFreeRunningCounter
	{
		ulong ClockFrequency_hz { get; }

		long Ticks { get; }
	}
}
