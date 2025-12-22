using System;

namespace ids.portable.common.Extensions
{
	public static class TimeSpanExtension
	{
		private const long MicrosecondsToMilliseconds = 1000L;

		public static long ElapsedMicroseconds(this TimeSpan instance)
		{
			return (long)((double)instance.Ticks / 10000.0 * 1000.0);
		}
	}
}
