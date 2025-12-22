using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.Common
{
	public static class TaskCompletionSourceExtension
	{
		public static async Task<TResult?> TryWaitAsync<TResult>(this TaskCompletionSource<TResult> tcs, CancellationToken cancelToken, int timeoutMs = -1, bool updateTcs = false, TResult? failureResult = default(TResult?))
		{
			try
			{
				return await tcs.WaitAsync(cancelToken, timeoutMs, updateTcs);
			}
			catch (Exception)
			{
				return failureResult;
			}
		}

		public static async Task<TResult> WaitAsync<TResult>(this TaskCompletionSource<TResult> tcs, CancellationToken cancelToken, int timeoutMs = -1, bool updateTcs = false)
		{
			TaskCompletionSource<TResult> overrideTcs = new TaskCompletionSource<TResult>();
			CancellationTokenSource timeoutCancelTokenSource = ((timeoutMs <= 0 || timeoutMs == -1) ? null : new CancellationTokenSource(timeoutMs));
			try
			{
				CancellationToken cancellationToken = timeoutCancelTokenSource?.Token ?? CancellationToken.None;
				using CancellationTokenSource linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancelToken, cancellationToken);
				using (linkedTokenSource.Token.Register(CancelTcs))
				{
					try
					{
						await Task.WhenAny(tcs.Task, overrideTcs.Task);
					}
					catch
					{
					}
					if (tcs.Task.IsCompleted)
					{
						await tcs.Task;
						return tcs.Task.Result;
					}
					if (timeoutCancelTokenSource?.IsCancellationRequested ?? false)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(28, 1);
						defaultInterpolatedStringHandler.AppendLiteral("WaitAsync timed out after ");
						defaultInterpolatedStringHandler.AppendFormatted(timeoutMs);
						defaultInterpolatedStringHandler.AppendLiteral("ms");
						throw new TimeoutException(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					throw new OperationCanceledException();
				}
			}
			finally
			{
				if (timeoutCancelTokenSource != null)
				{
					((IDisposable)timeoutCancelTokenSource).Dispose();
				}
			}
			void CancelTcs()
			{
				if (updateTcs && !tcs.Task.IsCompleted)
				{
					CancellationTokenSource cancellationTokenSource = timeoutCancelTokenSource;
					if (cancellationTokenSource != null && cancellationTokenSource.IsCancellationRequested)
					{
						TaskCompletionSource<TResult> taskCompletionSource = tcs;
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(28, 1);
						defaultInterpolatedStringHandler2.AppendLiteral("WaitAsync timed out after ");
						defaultInterpolatedStringHandler2.AppendFormatted(timeoutMs);
						defaultInterpolatedStringHandler2.AppendLiteral("ms");
						taskCompletionSource.TrySetException(new TimeoutException(defaultInterpolatedStringHandler2.ToStringAndClear()));
					}
					else
					{
						tcs.TrySetCanceled();
					}
				}
				overrideTcs.TrySetResult(default(TResult));
			}
		}
	}
}
