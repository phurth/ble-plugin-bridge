using System;
using System.Threading;

namespace IDS.Core.Tasks
{
	public class AsyncOperation : System.IDisposable
	{
		private const int MAX_TIMEOUT_MS = int.MaxValue;

		public static readonly TimeSpan MAX_TIMEOUT = TimeSpan.FromMilliseconds(2147483647.0);

		public readonly TimeSpan Timeout;

		private readonly CancellationTokenSource CTS;

		private readonly Action<float, string> ProgressDelegate;

		private readonly Timer Timer = new Timer();

		public float PercentComplete { get; private set; }

		public string Status { get; private set; }

		public CancellationToken CancellationToken => CTS.Token;

		public bool IsCancellationRequested => CTS.IsCancellationRequested;

		public TimeSpan ElapsedTime => Timer.ElapsedTime;

		public bool ProgressRequested => ProgressDelegate != null;

		public TimeSpan? EstimatedTotalTime
		{
			get
			{
				if (PercentComplete <= 0f)
				{
					return null;
				}
				double totalSeconds = ElapsedTime.TotalSeconds;
				if (totalSeconds <= 1.0)
				{
					return null;
				}
				return TimeSpan.FromSeconds(totalSeconds / (double)(PercentComplete / 100f));
			}
		}

		public TimeSpan? EstimatedRemainingTime => EstimatedTotalTime - ElapsedTime;

		public TimeSpan TimeUntilTimeout => Timeout - ElapsedTime;

		public AsyncOperation(TimeSpan timeout)
		{
			TimeSpan ts = new TimeSpan(0, 0, 10, 0);
			Timeout = timeout.Add(ts);
			CTS = new CancellationTokenSource((int)timeout.TotalMilliseconds);
		}

		public AsyncOperation(TimeSpan timeout, CancellationToken token)
		{
			Timeout = timeout;
			CTS = CancellationTokenSource.CreateLinkedTokenSource(token);
			CTS.CancelAfter((int)timeout.TotalMilliseconds);
		}

		public AsyncOperation(TimeSpan timeout, Action<float, string> handler)
			: this(timeout)
		{
			ProgressDelegate = handler;
		}

		public AsyncOperation(TimeSpan timeout, CancellationToken token, Action<float, string> handler)
			: this(timeout, token)
		{
			ProgressDelegate = handler;
		}

		public void Dispose()
		{
			CTS.Dispose();
		}

		public void Cancel()
		{
			CTS.Cancel();
		}

		public void ThrowIfCancellationRequested()
		{
			CTS.Token.ThrowIfCancellationRequested();
		}

		public void ReportProgress(string status)
		{
			if (status != null)
			{
				Status = status;
			}
			ProgressDelegate?.Invoke(PercentComplete, Status);
		}

		public void ReportProgress(float percent_complete, string status = null)
		{
			PercentComplete = percent_complete;
			if (status != null)
			{
				Status = status;
			}
			ProgressDelegate?.Invoke(PercentComplete, Status);
		}
	}
}
