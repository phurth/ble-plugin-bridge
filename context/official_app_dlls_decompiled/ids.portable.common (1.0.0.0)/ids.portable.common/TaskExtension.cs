using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.Common
{
	public static class TaskExtension
	{
		public static readonly Task CompletedTask = Task.FromResult(false);

		public static Task Delay(TimeSpan timeSpan, CancellationToken cancelToken)
		{
			return Task.Delay((int)timeSpan.TotalMilliseconds, cancelToken);
		}

		public static async Task<bool> TryDelay(int millisecondsDelay, CancellationToken cancelToken)
		{
			try
			{
				await Task.Delay(millisecondsDelay, cancelToken);
				return true;
			}
			catch
			{
			}
			return false;
		}

		public static async Task<bool> TryDelay(TimeSpan timespan, CancellationToken cancelToken)
		{
			try
			{
				await Task.Delay(timespan, cancelToken);
				return true;
			}
			catch
			{
			}
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task OnSuccess(this Task thisTask, Action<Task> action, CancellationToken token)
		{
			thisTask.ContinueWith(action, token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
			return thisTask;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task OnSuccess(this Task thisTask, Action<Task> action)
		{
			return thisTask.OnSuccess(action, CancellationToken.None);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task OnFailure(this Task thisTask, Action<Task> action, CancellationToken token)
		{
			thisTask.ContinueWith(action, token, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
			return thisTask;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task OnFailure(this Task thisTask, Action<Task> action)
		{
			return thisTask.OnFailure(action, CancellationToken.None);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task OnFailureLog(this Task thisTask, string tag, string message, CancellationToken token)
		{
			thisTask.OnFailure(delegate
			{
				TaggedLog.Error(tag, message, string.Empty);
			}, token);
			return thisTask;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task OnFailureLog(this Task thisTask, string tag, string message)
		{
			return thisTask.OnFailureLog(tag, message, CancellationToken.None);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task OnCancellation(this Task thisTask, Action<Task> action, CancellationToken token)
		{
			thisTask.ContinueWith(action, token, TaskContinuationOptions.OnlyOnCanceled, TaskScheduler.Default);
			return thisTask;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task OnCancellation(this Task thisTask, Action<Task> action)
		{
			return thisTask.OnCancellation(action, CancellationToken.None);
		}

		public static async Task<bool> TryAwaitAsync(this Task instance, bool configureAwait = true)
		{
			try
			{
				await instance.ConfigureAwait(configureAwait);
				return true;
			}
			catch
			{
				return false;
			}
		}

		public static async Task<TValue> TryAwaitAsync<TValue>(this Task<TValue> instance, TValue @default = default(TValue), bool configureAwait = true)
		{
			try
			{
				return await instance.ConfigureAwait(configureAwait);
			}
			catch
			{
				return @default;
			}
		}

		public static Task<TResult> ContinueWith<TResult>(this Task instance, Func<TResult> onCompletion, Func<TResult> onCancellation, Func<AggregateException, TResult> onException, TaskCreationOptions taskCreationOptions = TaskCreationOptions.None)
		{
			TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>(taskCreationOptions);
			if (instance == null)
			{
				throw new ArgumentException("instance cannot be null!");
			}
			if (onCompletion == null)
			{
				throw new ArgumentException("onCompletion cannot be null!");
			}
			if (onCancellation == null)
			{
				throw new ArgumentException("onCancellation cannot be null!");
			}
			if (onException == null)
			{
				throw new ArgumentException("onException cannot be null!");
			}
			TResult result;
			instance.ContinueWith(delegate(Task task, object? obj)
			{
				try
				{
					if (task.IsFaulted)
					{
						result = onException(task.Exception);
					}
					else if (task.IsCanceled)
					{
						result = onCancellation();
					}
					else
					{
						result = onCompletion();
					}
					tcs.TrySetResult(result);
				}
				catch (Exception ex)
				{
					tcs.TrySetException(new InvalidOperationException("Exception was not handled in callback method!", ex));
				}
			}, TaskContinuationOptions.None, TaskScheduler.Default);
			return tcs.Task;
		}

		public static Task<TResult> ContinueWith<TResult>(this Task<TResult> instance, Func<TResult> onCancellation, Func<AggregateException, TResult> onException, TaskCreationOptions taskCreationOptions = TaskCreationOptions.None)
		{
			TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>(taskCreationOptions);
			if (instance == null)
			{
				throw new ArgumentException("instance cannot be null!");
			}
			if (onCancellation == null)
			{
				throw new ArgumentException("onCancellation cannot be null!");
			}
			if (onException == null)
			{
				throw new ArgumentException("onException cannot be null!");
			}
			TResult result;
			instance.ContinueWith(delegate(Task<TResult> task, object? obj)
			{
				try
				{
					if (task.IsFaulted)
					{
						result = onException(task.Exception);
					}
					else if (task.IsCanceled)
					{
						result = onCancellation();
					}
					else
					{
						result = task.Result;
					}
					tcs.TrySetResult(result);
				}
				catch (Exception ex)
				{
					tcs.TrySetException(new InvalidOperationException("Exception was not handled in callback method!", ex));
				}
			}, TaskContinuationOptions.None, TaskScheduler.Default);
			return tcs.Task;
		}

		public static async Task WhenCanceled(this CancellationToken cancellationToken)
		{
			TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
			using (cancellationToken.Register((Action<object>)delegate
			{
				tcs.SetResult(true);
			}, (object)null))
			{
				try
				{
					await tcs.Task;
				}
				catch
				{
				}
			}
		}
	}
}
