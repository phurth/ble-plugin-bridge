using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace IDS.Core
{
	public class Timer
	{
		private static readonly long SysStartTicks;

		private static readonly double ScaleFactor_sec;

		private long mStartTicks;

		private long mPausedTicks;

		public static ulong ClockFrequency_hz { get; private set; }

		public static long RawTicks => FreeRunningCounter.Ticks - SysStartTicks;

		public static double RawTicks_sec => (double)RawTicks * ScaleFactor_sec;

		private long StartTicks
		{
			get
			{
				return Interlocked.Read(ref mStartTicks);
			}
			set
			{
				Interlocked.Exchange(ref mStartTicks, value);
			}
		}

		private long PausedTicks
		{
			get
			{
				return Interlocked.Read(ref mPausedTicks);
			}
			set
			{
				Interlocked.Exchange(ref mPausedTicks, value);
			}
		}

		public bool IsRunning { get; private set; }

		private long Ticks
		{
			get
			{
				if (!IsRunning)
				{
					return PausedTicks;
				}
				return RawTicks;
			}
		}

		private long ElapsedTicks
		{
			get
			{
				return Ticks - StartTicks;
			}
			set
			{
				StartTicks = Ticks - value;
			}
		}

		public double ElapsedTime_sec
		{
			get
			{
				return (double)ElapsedTicks * ScaleFactor_sec;
			}
			set
			{
				ElapsedTicks = (long)(value * (double)ClockFrequency_hz);
			}
		}

		public TimeSpan ElapsedTime
		{
			get
			{
				return TimeSpan.FromSeconds(ElapsedTime_sec);
			}
			set
			{
				ElapsedTime_sec = value.TotalSeconds;
			}
		}

		static Timer()
		{
			try
			{
				SysStartTicks = FreeRunningCounter.Ticks;
				ClockFrequency_hz = FreeRunningCounter.ClockFrequency_hz;
				ScaleFactor_sec = 1.0 / (double)ClockFrequency_hz;
			}
			catch
			{
				throw new Exception("Failed to access hardware clock, was one registered to IDS.FreeRunningCounter.Initialize() ?");
			}
		}

		public Timer(bool start = true)
		{
			StartTicks = RawTicks;
			if (start)
			{
				Reset();
			}
		}

		public void Reset()
		{
			StartTicks = RawTicks;
			IsRunning = true;
		}

		public void Start()
		{
			if (!IsRunning)
			{
				StartTicks = RawTicks - ElapsedTicks;
				IsRunning = true;
			}
		}

		public void Stop()
		{
			PausedTicks = Ticks;
			IsRunning = false;
		}

		public TimeSpan GetElapsedTimeAndReset()
		{
			long rawTicks = RawTicks;
			double value = (double)((IsRunning ? rawTicks : PausedTicks) - StartTicks) * ScaleFactor_sec;
			StartTicks = rawTicks;
			IsRunning = true;
			return TimeSpan.FromSeconds(value);
		}

		public override string ToString()
		{
			return FormatString(ElapsedTime);
		}

		public static string FormatString(TimeSpan span)
		{
			int num = (int)span.TotalSeconds;
			int num2 = num / 86400;
			num %= 86400;
			int num3 = num / 3600;
			num %= 3600;
			int num4 = num / 60;
			num %= 60;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler;
			if (num2 > 0)
			{
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(3, 4);
				defaultInterpolatedStringHandler.AppendFormatted(num2);
				defaultInterpolatedStringHandler.AppendLiteral(":");
				defaultInterpolatedStringHandler.AppendFormatted(num3.ToString("00"));
				defaultInterpolatedStringHandler.AppendLiteral(":");
				defaultInterpolatedStringHandler.AppendFormatted(num4.ToString("00"));
				defaultInterpolatedStringHandler.AppendLiteral(":");
				defaultInterpolatedStringHandler.AppendFormatted(num.ToString("00"));
				return defaultInterpolatedStringHandler.ToStringAndClear();
			}
			if (num3 > 0)
			{
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 3);
				defaultInterpolatedStringHandler.AppendFormatted(num3);
				defaultInterpolatedStringHandler.AppendLiteral(":");
				defaultInterpolatedStringHandler.AppendFormatted(num4.ToString("00"));
				defaultInterpolatedStringHandler.AppendLiteral(":");
				defaultInterpolatedStringHandler.AppendFormatted(num.ToString("00"));
				return defaultInterpolatedStringHandler.ToStringAndClear();
			}
			defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(1, 2);
			defaultInterpolatedStringHandler.AppendFormatted(num4);
			defaultInterpolatedStringHandler.AppendLiteral(":");
			defaultInterpolatedStringHandler.AppendFormatted(num.ToString("00"));
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
