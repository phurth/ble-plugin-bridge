using IDS.Core;

namespace IDS.Portable.LogicalDevice
{
	public static class TimerExtension
	{
		public static double Elapsed_ms(this Timer timer)
		{
			return timer.ElapsedTime.TotalMilliseconds;
		}

		public static double Elapsed_sec(this Timer timer)
		{
			return timer.ElapsedTime.TotalSeconds;
		}
	}
}
