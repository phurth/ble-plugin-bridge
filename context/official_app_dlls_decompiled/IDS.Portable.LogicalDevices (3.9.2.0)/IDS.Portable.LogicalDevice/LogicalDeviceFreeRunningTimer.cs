using System.Diagnostics;

namespace IDS.Portable.LogicalDevice
{
	public static class LogicalDeviceFreeRunningTimer
	{
		private static Stopwatch Stopwatch { get; } = System.Diagnostics.Stopwatch.StartNew();


		public static long ElapsedMilliseconds => Stopwatch.ElapsedMilliseconds;
	}
}
