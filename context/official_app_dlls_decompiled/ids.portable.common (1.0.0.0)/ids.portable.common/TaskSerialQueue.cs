using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common.Extensions;

namespace IDS.Portable.Common
{
	public class TaskSerialQueue
	{
		public class TaskSerialLock : CommonDisposable
		{
			private readonly TaskSerialQueue _taskSerialQueue;

			private readonly CancellationTokenSource _cts;

			private readonly TaskCompletionSource<bool> _lockGrantedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

			internal CancellationToken CancelToken { get; }

			internal Task WaitForLockAsync()
			{
				return _lockGrantedTcs.WaitAsync(CancelToken, -1, updateTcs: true);
			}

			internal void GrantLock()
			{
				_lockGrantedTcs.TrySetResult(true);
			}

			internal TaskSerialLock(TaskSerialQueue taskSerialQueue, CancellationToken cancelToken, TimeSpan timeout)
			{
				_taskSerialQueue = taskSerialQueue;
				_cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
				if (timeout < TimeSpan.MaxValue)
				{
					_cts.CancelAfter(timeout);
				}
				CancelToken = _cts.Token;
			}

			public override void Dispose(bool disposing)
			{
				_taskSerialQueue.LockNoLongerNeeded(this);
				_cts.TryCancelAndDispose();
			}
		}

		private const string LogTag = "TaskSerialQueue";

		private readonly List<TaskSerialLock> _taskQueue = new List<TaskSerialLock>();

		private TaskSerialLock? _currentLock;

		public int MaxQueueSize { get; }

		public TaskSerialQueue(int maxQueueSize = int.MaxValue)
		{
			MaxQueueSize = maxQueueSize;
		}

		[Obsolete("GetLock is deprecated, please use GetLockAsync instead.")]
		public Task<TaskSerialLock> GetLock(CancellationToken cancelToken)
		{
			return GetLockAsync(cancelToken, TimeSpan.MaxValue);
		}

		[Obsolete("GetLock is deprecated, please use GetLockAsync instead.")]
		public Task<TaskSerialLock> GetLock(CancellationToken cancelToken, TimeSpan timeout)
		{
			return GetLockAsync(cancelToken, timeout);
		}

		public Task<TaskSerialLock> GetLockAsync(CancellationToken cancelToken)
		{
			return GetLockAsync(cancelToken, TimeSpan.MaxValue);
		}

		public async Task<TaskSerialLock> GetLockAsync(CancellationToken cancelToken, TimeSpan timeout)
		{
			TaskSerialLock serialLock = new TaskSerialLock(this, cancelToken, timeout);
			lock (_taskQueue)
			{
				if (_taskQueue.Count > MaxQueueSize)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(81, 1);
					defaultInterpolatedStringHandler.AppendLiteral("TaskSerialQueue: Unable to run queued task because maximum queue Size of ");
					defaultInterpolatedStringHandler.AppendFormatted(MaxQueueSize);
					defaultInterpolatedStringHandler.AppendLiteral(" reached");
					IndexOutOfRangeException ex = new IndexOutOfRangeException(defaultInterpolatedStringHandler.ToStringAndClear());
					TaggedLog.Warning("TaskSerialQueue", ex.Message, string.Empty);
					throw ex;
				}
				if (_currentLock == null)
				{
					_currentLock = serialLock;
					_currentLock!.GrantLock();
					return serialLock;
				}
				_taskQueue.Add(serialLock);
			}
			try
			{
				await serialLock.WaitForLockAsync();
				return serialLock;
			}
			catch
			{
				serialLock.TryDispose();
				throw;
			}
		}

		internal void LockNoLongerNeeded(TaskSerialLock serialLock)
		{
			lock (_taskQueue)
			{
				if (_currentLock != serialLock)
				{
					_taskQueue.TryRemove(serialLock);
					return;
				}
				_currentLock = null;
				TaskSerialLock item;
				while (_taskQueue.TryTakeFirst(out item))
				{
					if (item.IsDisposed || item.CancelToken.IsCancellationRequested)
					{
						TaggedLog.Debug("TaskSerialQueue", "TaskSerialQueue: Removed queued lock request because it was canceled ({0})", _taskQueue.Count);
						continue;
					}
					_currentLock = item;
					item.GrantLock();
					break;
				}
			}
		}
	}
}
