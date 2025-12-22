using System;
using System.Threading.Tasks;

namespace IDS.Portable.Common
{
	public static class MainThread
	{
		public enum RunBehavior
		{
			Inline,
			Queue
		}

		private const string LogTag = "MainThread";

		private static int? _mainThreadId;

		public static Func<Func<Action, bool>>? RequestMainThreadActionFactory;

		public static ThreadContext CurrentThreadContext
		{
			get
			{
				if (!_mainThreadId.HasValue)
				{
					return ThreadContext.Unknown;
				}
				if (_mainThreadId.Value != Environment.CurrentManagedThreadId)
				{
					return ThreadContext.Other;
				}
				return ThreadContext.Main;
			}
		}

		public static bool RequestMainThreadAction(Action action, RunBehavior runBehavior = RunBehavior.Inline)
		{
			Func<Action, bool> func = RequestMainThreadActionFactory?.Invoke();
			if (func == null)
			{
				TaggedLog.Warning("MainThread", "WARNING: RequestMainThreadAction is NULL -- executing action in current context.", string.Empty);
				action?.Invoke();
				return false;
			}
			switch (CurrentThreadContext)
			{
			case ThreadContext.Main:
				if (runBehavior == RunBehavior.Inline)
				{
					action?.Invoke();
					return false;
				}
				return func(action);
			default:
				return func(action);
			}
		}

		public static Task<bool> RequestMainThreadActionAsync(Action action)
		{
			TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
			RequestMainThreadAction(delegate
			{
				try
				{
					action?.Invoke();
					tcs.TrySetResult(true);
				}
				catch
				{
					tcs.TrySetResult(false);
				}
			});
			return tcs.Task;
		}

		public static void UpdateMainThreadContext()
		{
			int? mainThreadId = _mainThreadId;
			_mainThreadId = Environment.CurrentManagedThreadId;
			if (mainThreadId.HasValue)
			{
				_ = mainThreadId == _mainThreadId;
			}
		}
	}
}
