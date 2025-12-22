using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IDS.Core.Tasks
{
	public class PeriodicTask : Disposable
	{
		public enum Type
		{
			FixedDelay,
			FixedRate
		}

		private class TimeMeasurer
		{
			private static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1.0);

			private Timer Timer = new Timer();

			private Timer DumpTime = new Timer();

			private int Count;

			private TimeSpan TotalTime = TimeSpan.Zero;

			private TimeSpan TotalSleep = TimeSpan.Zero;

			private int TaskCount;

			public void Reset()
			{
				TotalTime = TimeSpan.Zero;
				TotalSleep = TimeSpan.Zero;
				Count = 0;
				DumpTime.Reset();
			}

			public void Start(int task_count)
			{
				TaskCount = task_count;
				Timer.Reset();
			}

			public void Stop(TimeSpan sleep_time)
			{
				TotalTime += Timer.ElapsedTime;
				if (sleep_time.Ticks > 0)
				{
					TotalSleep += sleep_time;
				}
				Count++;
				if (DumpTime.ElapsedTime > OneSecond)
				{
					_ = TotalTime.TotalSeconds / (double)Count;
					_ = TotalSleep.TotalMilliseconds / (double)Count;
					Reset();
				}
			}
		}

		private class ActionScheduler
		{
			private readonly Action Action;

			private readonly TimeSpan Period;

			private readonly Type TaskType;

			private Timer Timer;

			public ActionScheduler(Action action, TimeSpan period, Type tasktype)
			{
				Action = action;
				Period = period;
				TaskType = tasktype;
			}

			public void Resume()
			{
				if (Timer != null && Timer.ElapsedTime > TimeSpan.Zero)
				{
					Timer.ElapsedTime = TimeSpan.Zero;
				}
			}

			public TimeSpan Invoke()
			{
				Action();
				if (TaskType == Type.FixedRate)
				{
					if (Timer == null)
					{
						Timer = new Timer();
					}
					Timer.ElapsedTime -= Period;
					long ticks = Timer.ElapsedTime.Ticks;
					if (ticks < 0)
					{
						return TimeSpan.FromTicks(-ticks);
					}
					return TimeSpan.Zero;
				}
				return Period;
			}
		}

		private static readonly TimeSpan DEFAULT_DELAY = TimeSpan.FromSeconds(0.1);

		private static readonly object CriticalSection = new object();

		private static readonly LinkedList<PeriodicTask> TaskList = new LinkedList<PeriodicTask>();

		private static int MasterBackgroundTaskRunning = 0;

		private static readonly AutoResetEvent WakeMasterBackgroundTaskSignal = new AutoResetEvent(true);

		protected readonly Func<TimeSpan> UserAction;

		private readonly CancellableTask PrivateTask;

		private TimeSpan Delay = TimeSpan.Zero;

		public bool IsRunning { get; private set; }

		public bool IsSynchronous => PrivateTask == null;

		public PeriodicTask(Action action, TimeSpan period, Type tasktype, bool synchronous = true)
			: this(action, period, TimeSpan.Zero, tasktype, synchronous)
		{
		}

		public PeriodicTask(Action action, TimeSpan period, TimeSpan delay, Type tasktype, bool synchronous = true)
			: this(new ActionScheduler(action, period, tasktype).Invoke, delay, synchronous)
		{
		}

		public PeriodicTask(Func<TimeSpan> action, bool synchronous = true)
			: this(action, TimeSpan.Zero, synchronous)
		{
		}

		public PeriodicTask(Func<TimeSpan> action, TimeSpan delay, bool synchronous = true)
		{
			UserAction = action;
			Delay = delay;
			if (Delay.Ticks < 0)
			{
				Delay = TimeSpan.Zero;
			}
			if (!synchronous)
			{
				PrivateTask = CancellableTask.Run(PrivateTaskAsync);
			}
			else
			{
				lock (TaskList)
				{
					TaskList.AddLast(this);
				}
				if (Interlocked.Exchange(ref MasterBackgroundTaskRunning, 1) == 0)
				{
					Task.Run(async delegate
					{
						MasterBackgroundTask(await GetTimerAsync());
					});
				}
			}
			IsRunning = true;
			if (IsSynchronous)
			{
				WakeMasterBackgroundTaskSignal.Set();
			}
		}

		public override void Dispose(bool disposing)
		{
			if (disposing)
			{
				Pause();
				PrivateTask?.Dispose();
			}
		}

		public void Pause()
		{
			IsRunning = false;
		}

		public void Resume()
		{
			if (!IsRunning)
			{
				(UserAction.Target as ActionScheduler)?.Resume();
				IsRunning = true;
				if (IsSynchronous)
				{
					WakeMasterBackgroundTaskSignal.Set();
				}
			}
		}

		private TimeSpan ScheduleInvoke(TimeSpan delta)
		{
			Delay -= delta;
			if (base.IsDisposed || !IsRunning)
			{
				return DEFAULT_DELAY;
			}
			if (Delay.Ticks <= 0)
			{
				try
				{
					Delay = UserAction();
				}
				catch
				{
					Delay = DEFAULT_DELAY;
				}
			}
			if (Delay.Ticks > 0)
			{
				return Delay;
			}
			return TimeSpan.Zero;
		}

		private async Task<Timer> GetTimerAsync()
		{
			do
			{
				await Task.Delay(50).ConfigureAwait(false);
			}
			while (FreeRunningCounter.Instance == null);
			return new Timer();
		}

		private async Task PrivateTaskAsync(CancellationToken token)
		{
			Timer timer = await GetTimerAsync();
			while (!base.IsDisposed && !token.IsCancellationRequested)
			{
				TimeSpan elapsedTimeAndReset = timer.GetElapsedTimeAndReset();
				int num = (int)ScheduleInvoke(elapsedTimeAndReset).TotalMilliseconds;
				if (num >= 20)
				{
					await Task.Delay(num, token).ConfigureAwait(true);
				}
				else if (num >= 0)
				{
					token.WaitHandle.WaitOne(num);
				}
			}
		}

		private static void MasterBackgroundTask(Timer t)
		{
			_ = TaskScheduler.Current.Id;
			while (!Environment.HasShutdownStarted)
			{
				TimeSpan timeSpan = DEFAULT_DELAY;
				TimeSpan elapsedTimeAndReset = t.GetElapsedTimeAndReset();
				LinkedListNode<PeriodicTask> linkedListNode;
				lock (TaskList)
				{
					linkedListNode = TaskList.First;
				}
				if (linkedListNode != null)
				{
					do
					{
						if (!linkedListNode.Value.IsDisposed)
						{
							TimeSpan timeSpan2 = linkedListNode.Value.ScheduleInvoke(elapsedTimeAndReset);
							if (timeSpan > timeSpan2)
							{
								timeSpan = timeSpan2;
							}
						}
						lock (TaskList)
						{
							LinkedListNode<PeriodicTask> next = linkedListNode.Next;
							if (linkedListNode.Value.IsDisposed)
							{
								TaskList.Remove(linkedListNode);
							}
							linkedListNode = next;
						}
					}
					while (linkedListNode != null);
				}
				int num = (int)timeSpan.TotalMilliseconds;
				if (num < 1)
				{
					num = 1;
				}
				WakeMasterBackgroundTaskSignal.WaitOne(num);
			}
		}
	}
}
