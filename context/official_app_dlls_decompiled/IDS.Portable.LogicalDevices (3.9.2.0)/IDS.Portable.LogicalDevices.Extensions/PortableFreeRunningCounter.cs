using System.Diagnostics;
using IDS.Core;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevices.Extensions
{
	public class PortableFreeRunningCounter : Singleton<PortableFreeRunningCounter>, IFreeRunningCounter
	{
		private readonly ulong _frequencyHz = (ulong)Stopwatch.Frequency;

		public ulong ClockFrequency_hz => _frequencyHz;

		public long Ticks => Stopwatch.GetTimestamp();

		private PortableFreeRunningCounter()
		{
		}
	}
}
