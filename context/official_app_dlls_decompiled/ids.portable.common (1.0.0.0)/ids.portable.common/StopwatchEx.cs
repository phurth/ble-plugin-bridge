using System.Diagnostics;

namespace ids.portable.common
{
	public static class StopwatchEx
	{
		public static bool IsStopped(this Stopwatch? stopwatch)
		{
			if (stopwatch == null)
			{
				return true;
			}
			return !stopwatch!.IsRunning;
		}
	}
}
