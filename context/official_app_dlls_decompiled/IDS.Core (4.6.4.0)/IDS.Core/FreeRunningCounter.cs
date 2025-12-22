namespace IDS.Core
{
	public class FreeRunningCounter
	{
		private static IFreeRunningCounter mInstance = null;

		private static object CriticalSection = new object();

		public static IFreeRunningCounter Instance
		{
			get
			{
				return mInstance;
			}
			set
			{
				if (mInstance != null)
				{
					return;
				}
				lock (CriticalSection)
				{
					if (mInstance == null)
					{
						mInstance = value;
					}
				}
			}
		}

		public static ulong ClockFrequency_hz => mInstance.ClockFrequency_hz;

		public static long Ticks => mInstance.Ticks;
	}
}
