using System;
using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.Common
{
	public sealed class CallbackTimer : CancellationTokenSource
	{
		private const string LogTag = "CallbackTimer";

		public bool TimerFired;

		public CallbackTimer(TimerCallback callback, object state, int msDueTime)
		{
			try
			{
				Task.Delay(msDueTime, base.Token).ContinueWith(delegate(Task t, object? s)
				{
					if (!base.IsCancellationRequested)
					{
						TimerFired = true;
						Tuple<TimerCallback, object> tuple = (Tuple<TimerCallback, object>)s;
						tuple.Item1(tuple.Item2);
					}
				}, Tuple.Create(callback, state), CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
			}
			catch (TaskCanceledException)
			{
			}
			catch (Exception ex2)
			{
				TaggedLog.Error("CallbackTimer", "Exception during timer callback {0}: {1}", ex2.Message, ex2.StackTrace);
			}
		}

		public CallbackTimer(TimerSimpleCallback callback, int msDueTime, bool repeat = false)
		{
			RunCallbackAfterDelay(callback, msDueTime, repeat);
		}

		private void RunCallbackAfterDelay(TimerSimpleCallback callback, int msDueTime, bool repeat)
		{
			try
			{
				Task.Delay(msDueTime, base.Token).ContinueWith(delegate
				{
					if (!base.IsCancellationRequested && callback != null)
					{
						TimerFired = true;
						callback();
						if (repeat)
						{
							RunCallbackAfterDelay(callback, msDueTime, repeat);
						}
					}
				}, null, CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
			}
			catch (TaskCanceledException)
			{
			}
			catch (Exception ex2)
			{
				TaggedLog.Error("CallbackTimer", "CallbackTimer - Exception during timer callback {0}: {1}", ex2.Message, ex2.StackTrace);
			}
		}

		protected override void Dispose(bool disposing)
		{
			try
			{
				Cancel();
			}
			catch
			{
			}
			base.Dispose(disposing);
		}

		public void TryDispose()
		{
			try
			{
				Dispose();
			}
			catch
			{
			}
		}
	}
}
