using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.Common
{
	public class Watchdog : CommonDisposable, IWatchdog, ICommonDisposable, IDisposable
	{
		private const string LogTag = "Watchdog";

		private const double PercentOfTotalTimeToRespond = 1.25;

		public const long PetFailed = -1L;

		private const int MaximumSleepTimeDifferenceMs = 10;

		private readonly object _sync = new object();

		private readonly Action? _triggerCallback;

		private TaskCompletionSource<bool>? _triggerTcs;

		private Task? _monitoringTask;

		private CancellationTokenSource _monitoringTaskCts = new CancellationTokenSource();

		private readonly Stopwatch _lastPetStopwatch = new Stopwatch();

		public bool AutoStartOnFirstPet { get; }

		public TimeSpan PetTimeout { get; }

		public TimeSpan MaxTimeUntilTriggered { get; }

		public bool Triggered { get; private set; }

		public Watchdog(int petTimeoutMs, bool autoStartOnFirstPet = false)
			: this(TimeSpan.FromMilliseconds(petTimeoutMs), null, autoStartOnFirstPet)
		{
		}

		public Watchdog(int petTimeoutMs, Action? triggerCallback, bool autoStartOnFirstPet = false)
			: this(TimeSpan.FromMilliseconds(petTimeoutMs), triggerCallback, autoStartOnFirstPet)
		{
		}

		public Watchdog(int petTimeoutMs, int maxTimeUntilTriggered, Action? triggerCallback, bool autoStartOnFirstPet = false)
			: this(TimeSpan.FromMilliseconds(petTimeoutMs), TimeSpan.FromMilliseconds(maxTimeUntilTriggered), triggerCallback, autoStartOnFirstPet)
		{
		}

		public Watchdog(TimeSpan petTimeout, Action? triggerCallback, bool autoStartOnFirstPet = false)
			: this(petTimeout, TimeSpan.Zero, triggerCallback, autoStartOnFirstPet)
		{
		}

		public Watchdog(TimeSpan petTimeout, TimeSpan maxTimeUntilTriggered, Action? triggerCallback, bool autoStartOnFirstPet)
		{
			AutoStartOnFirstPet = autoStartOnFirstPet;
			PetTimeout = petTimeout;
			MaxTimeUntilTriggered = maxTimeUntilTriggered;
			_triggerCallback = triggerCallback;
		}

		private async Task WatchdogBackgroundOperationAsync(CancellationToken cancellationToken)
		{
			Stopwatch monitorStartedStopwatch = Stopwatch.StartNew();
			bool triggered = false;
			while (true)
			{
				int sleepTime = 0;
				lock (_sync)
				{
					if (base.IsDisposed || cancellationToken.IsCancellationRequested)
					{
						break;
					}
					triggered = Triggered;
					if (triggered)
					{
						break;
					}
					long elapsedMilliseconds = _lastPetStopwatch.ElapsedMilliseconds;
					sleepTime = (int)PetTimeout.TotalMilliseconds - (int)elapsedMilliseconds;
					if (MaxTimeUntilTriggered > TimeSpan.Zero && sleepTime > 0)
					{
						int num = (int)MaxTimeUntilTriggered.TotalMilliseconds - (int)monitorStartedStopwatch.ElapsedMilliseconds;
						if (num < sleepTime)
						{
							sleepTime = num;
						}
					}
					if (sleepTime > 0)
					{
						goto IL_0114;
					}
					bool flag2 = (Triggered = true);
					triggered = flag2;
				}
				break;
				IL_0114:
				Stopwatch monitorTotalDelayTimePassed = Stopwatch.StartNew();
				while (!cancellationToken.IsCancellationRequested)
				{
					int adjustedSleepTime = (int)(sleepTime - monitorTotalDelayTimePassed.ElapsedMilliseconds);
					if (adjustedSleepTime <= 0)
					{
						break;
					}
					await TaskExtension.TryDelay(adjustedSleepTime, cancellationToken).ConfigureAwait(false);
					if (!cancellationToken.IsCancellationRequested)
					{
						if (sleepTime - monitorTotalDelayTimePassed.ElapsedMilliseconds > 10)
						{
							DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(94, 3);
							defaultInterpolatedStringHandler.AppendLiteral("Watchdog delay finished too soon.  Expected ");
							defaultInterpolatedStringHandler.AppendFormatted(sleepTime);
							defaultInterpolatedStringHandler.AppendLiteral("ms but only ");
							defaultInterpolatedStringHandler.AppendFormatted(monitorTotalDelayTimePassed.ElapsedMilliseconds);
							defaultInterpolatedStringHandler.AppendLiteral("ms has passed [using time delay of ");
							defaultInterpolatedStringHandler.AppendFormatted(adjustedSleepTime);
							defaultInterpolatedStringHandler.AppendLiteral("ms]");
							TaggedLog.Warning("Watchdog", defaultInterpolatedStringHandler.ToStringAndClear());
						}
						continue;
					}
					break;
				}
			}
			if (!base.IsDisposed && !cancellationToken.IsCancellationRequested && triggered)
			{
				long elapsedMilliseconds2 = monitorStartedStopwatch.ElapsedMilliseconds;
				if (elapsedMilliseconds2 < (long)PetTimeout.TotalMilliseconds)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(75, 2);
					defaultInterpolatedStringHandler.AppendLiteral("Watchdog was triggered too soon.  Triggered after ");
					defaultInterpolatedStringHandler.AppendFormatted(elapsedMilliseconds2);
					defaultInterpolatedStringHandler.AppendLiteral("ms when Pet Timeout is ");
					defaultInterpolatedStringHandler.AppendFormatted(PetTimeout.TotalMilliseconds);
					defaultInterpolatedStringHandler.AppendLiteral("ms");
					TaggedLog.Warning("Watchdog", defaultInterpolatedStringHandler.ToStringAndClear());
				}
				try
				{
					_triggerCallback?.Invoke();
				}
				catch (Exception ex)
				{
					TaggedLog.Error("Watchdog", "{0} callback failed {1}\n{2}", GetType().FullName, ex.Message, ex.StackTrace);
				}
				_triggerTcs?.TrySetResult(true);
			}
		}

		public void Monitor()
		{
			lock (_sync)
			{
				if (base.IsDisposed)
				{
					throw new ObjectDisposedException(GetType().FullName);
				}
				if (Triggered)
				{
					throw new InvalidOperationException(GetType().FullName + " has already been triggered!");
				}
				if (_monitoringTask != null)
				{
					throw new InvalidOperationException(GetType().FullName + " is already monitoring!");
				}
				_lastPetStopwatch.Restart();
				_monitoringTask = WatchdogBackgroundOperationAsync(_monitoringTaskCts.Token);
			}
		}

		public void Cancel()
		{
			lock (_sync)
			{
				if (base.IsDisposed)
				{
					throw new ObjectDisposedException(GetType().FullName);
				}
				if (_triggerTcs != null && !_triggerTcs!.Task.IsCompleted)
				{
					_triggerTcs!.TrySetException(new OperationCanceledException("Watchdog was canceled"));
				}
				Triggered = false;
				_monitoringTaskCts.TryCancelAndDispose();
				_monitoringTaskCts = new CancellationTokenSource();
				_monitoringTask = null;
			}
		}

		public long Pet(bool autoReset = false)
		{
			lock (_sync)
			{
				if (base.IsDisposed)
				{
					throw new ObjectDisposedException(GetType().FullName);
				}
				if (AutoStartOnFirstPet && _monitoringTask == null)
				{
					Monitor();
				}
				if (Triggered)
				{
					if (!autoReset)
					{
						throw new InvalidOperationException(GetType().FullName + " has already been triggered!");
					}
					Triggered = false;
					TaskCompletionSource<bool>? triggerTcs = _triggerTcs;
					if (triggerTcs != null && triggerTcs!.Task.IsCompleted)
					{
						_triggerTcs = null;
					}
					_monitoringTask = null;
					Monitor();
					return _lastPetStopwatch.ElapsedMilliseconds;
				}
				long elapsedMilliseconds = _lastPetStopwatch.ElapsedMilliseconds;
				_lastPetStopwatch.Restart();
				return elapsedMilliseconds;
			}
		}

		public long TryPet(bool autoReset = false)
		{
			try
			{
				if (base.IsDisposed)
				{
					return -1L;
				}
				return Pet(autoReset);
			}
			catch
			{
				return -1L;
			}
		}

		public Task AsTask()
		{
			lock (_sync)
			{
				if (_triggerTcs == null)
				{
					if (base.IsDisposed)
					{
						return Task.FromException(new ObjectDisposedException(GetType().FullName, "The Watchdog has been disposed so can't get Task from AsTask"));
					}
					if (Triggered)
					{
						return Task.FromResult(true);
					}
					_triggerTcs = new TaskCompletionSource<bool>(TaskContinuationOptions.RunContinuationsAsynchronously);
				}
				return _triggerTcs!.Task;
			}
		}

		public override void Dispose(bool disposing)
		{
			lock (_sync)
			{
				_monitoringTaskCts.TryCancelAndDispose();
				_monitoringTask = null;
				if (_triggerTcs != null && !_triggerTcs!.Task.IsCompleted)
				{
					_triggerTcs!.TrySetException(new ObjectDisposedException(GetType().FullName, "The Watchdog has been disposed"));
				}
			}
		}
	}
}
