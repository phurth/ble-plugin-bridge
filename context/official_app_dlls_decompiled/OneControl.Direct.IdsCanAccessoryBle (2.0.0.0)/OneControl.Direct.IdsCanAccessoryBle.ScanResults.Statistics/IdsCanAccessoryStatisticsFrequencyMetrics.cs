using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace OneControl.Direct.IdsCanAccessoryBle.ScanResults.Statistics
{
	public class IdsCanAccessoryStatisticsFrequencyMetrics
	{
		public const int BurstWindowSizeMs = 100;

		private Stopwatch? _timerFromFirstUpdate;

		private double _lastMessageReceivedTimeMs;

		private double _currentBurstWindowStartTimeMs;

		private double _currentBurstWindowEndTimeMs;

		private double _lastBurstWindowEndTimeMs;

		private long _messagesReceivedInBurstWindow;

		public long MessagesReceived { get; private set; }

		public long BurstCount { get; private set; }

		public long BurstWindowDurationMs { get; private set; }

		public long TimeBetweenBurstsMs { get; private set; }

		public long MessagesReceivedInBurst { get; private set; }

		public long MinTimeMs { get; private set; }

		public long MaxTimeMs { get; private set; }

		public double AverageTimeMs { get; private set; }

		public double TotalTimeMs => ((double?)_timerFromFirstUpdate?.ElapsedMilliseconds) ?? 0.0;

		public double UpdatesPerSecond { get; private set; }

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(70, 9);
			defaultInterpolatedStringHandler.AppendLiteral("Count:");
			defaultInterpolatedStringHandler.AppendFormatted(MessagesReceived);
			defaultInterpolatedStringHandler.AppendLiteral(" Min:");
			defaultInterpolatedStringHandler.AppendFormatted(MinTimeMs);
			defaultInterpolatedStringHandler.AppendLiteral("ms Max:");
			defaultInterpolatedStringHandler.AppendFormatted(MaxTimeMs);
			defaultInterpolatedStringHandler.AppendLiteral("ms Avg:");
			defaultInterpolatedStringHandler.AppendFormatted((long)AverageTimeMs);
			defaultInterpolatedStringHandler.AppendLiteral("ms PerSecond:");
			defaultInterpolatedStringHandler.AppendFormatted(UpdatesPerSecond, "F2");
			defaultInterpolatedStringHandler.AppendLiteral(" Burst(");
			defaultInterpolatedStringHandler.AppendFormatted(BurstCount);
			defaultInterpolatedStringHandler.AppendLiteral(")[Duration:");
			defaultInterpolatedStringHandler.AppendFormatted(BurstWindowDurationMs);
			defaultInterpolatedStringHandler.AppendLiteral("ms Gap:");
			defaultInterpolatedStringHandler.AppendFormatted(TimeBetweenBurstsMs);
			defaultInterpolatedStringHandler.AppendLiteral(" Msgs:");
			defaultInterpolatedStringHandler.AppendFormatted(MessagesReceivedInBurst);
			defaultInterpolatedStringHandler.AppendLiteral("]");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		public void Update()
		{
			MessagesReceived++;
			if (_timerFromFirstUpdate == null)
			{
				_timerFromFirstUpdate = Stopwatch.StartNew();
				return;
			}
			long elapsedMilliseconds = _timerFromFirstUpdate!.ElapsedMilliseconds;
			double num = (double)elapsedMilliseconds - _lastMessageReceivedTimeMs;
			_lastMessageReceivedTimeMs = elapsedMilliseconds;
			if (_currentBurstWindowStartTimeMs == 0.0)
			{
				_currentBurstWindowStartTimeMs = elapsedMilliseconds;
				_currentBurstWindowEndTimeMs = elapsedMilliseconds;
				_messagesReceivedInBurstWindow++;
			}
			else if ((double)elapsedMilliseconds - _currentBurstWindowStartTimeMs <= 100.0)
			{
				_currentBurstWindowEndTimeMs = elapsedMilliseconds;
				_messagesReceivedInBurstWindow++;
			}
			else
			{
				BurstCount++;
				if (_lastBurstWindowEndTimeMs > 0.0)
				{
					TimeBetweenBurstsMs = (long)((double)elapsedMilliseconds - _lastBurstWindowEndTimeMs);
				}
				BurstWindowDurationMs = (long)(_currentBurstWindowEndTimeMs - _currentBurstWindowStartTimeMs);
				if (BurstWindowDurationMs == 0L)
				{
					BurstWindowDurationMs = 100L;
				}
				MessagesReceivedInBurst = _messagesReceivedInBurstWindow;
				_currentBurstWindowStartTimeMs = elapsedMilliseconds;
				_currentBurstWindowEndTimeMs = elapsedMilliseconds;
				_lastBurstWindowEndTimeMs = elapsedMilliseconds;
				_messagesReceivedInBurstWindow = 1L;
			}
			MaxTimeMs = (((double)MaxTimeMs < num) ? ((long)num) : MaxTimeMs);
			if (num != 0.0 && num < (double)MinTimeMs)
			{
				MinTimeMs = (long)num;
			}
			AverageTimeMs = (double)_timerFromFirstUpdate!.ElapsedMilliseconds / (double)MessagesReceived;
			int num2 = (int)_timerFromFirstUpdate!.Elapsed.TotalSeconds;
			UpdatesPerSecond = ((num2 == 0) ? 0f : ((float)MessagesReceived / (float)num2));
		}

		public void Clear()
		{
			_timerFromFirstUpdate?.Restart();
			MessagesReceived = 0L;
			BurstCount = 0L;
			BurstWindowDurationMs = 0L;
			TimeBetweenBurstsMs = 0L;
			MessagesReceivedInBurst = 0L;
			MinTimeMs = 0L;
			MaxTimeMs = 0L;
			AverageTimeMs = 0.0;
			UpdatesPerSecond = 0.0;
		}
	}
}
