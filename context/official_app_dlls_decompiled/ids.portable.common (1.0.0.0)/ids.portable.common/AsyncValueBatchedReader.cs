using System;
using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.Common
{
	public class AsyncValueBatchedReader<TValue> : IAsyncValueBatchedReader<TValue>
	{
		public delegate Task<TValue> ReadValueBlockedAsyncHandler(CancellationToken cancellationToken);

		private const string LogTag = "AsyncValueBatchedReader";

		public const int DefaultReadTimeoutMs = 3000;

		private TaskCompletionSource<TValue>? _readTcs;

		private readonly ReadValueBlockedAsyncHandler? _readValueBlockedAsync;

		public IAsyncValueCached<TValue>? ReadCachedValue { get; private set; }

		public int ReadValueTimeoutMs { get; set; }

		public AsyncValueBatchedReader(ReadValueBlockedAsyncHandler readValueAsync, IAsyncValueCached<TValue>? readCachedValue = null, int readTimeoutMs = 3000)
			: this(readCachedValue, readTimeoutMs)
		{
			_readValueBlockedAsync = readValueAsync ?? throw new ArgumentNullException("readValueAsync");
		}

		protected AsyncValueBatchedReader(IAsyncValueCached<TValue>? readCachedValue = null, int readTimeoutMs = 3000)
		{
			ReadValueTimeoutMs = readTimeoutMs;
			ReadCachedValue = readCachedValue;
		}

		public virtual async Task<TValue> ReadValueAsync(CancellationToken cancellationToken, bool forceUpdate = false)
		{
			IAsyncValueCached<TValue> cacheValue = ReadCachedValue;
			if (!forceUpdate && cacheValue != null && cacheValue.HasValue && !cacheValue.NeedsUpdate)
			{
				return cacheValue.Value;
			}
			TaskCompletionSource<TValue> taskCompletionSource = new TaskCompletionSource<TValue>();
			TaskCompletionSource<TValue> tcs2 = Interlocked.CompareExchange(ref _readTcs, taskCompletionSource, null);
			if (tcs2 != null)
			{
				return await tcs2.WaitAsync(cancellationToken, ReadValueTimeoutMs);
			}
			tcs2 = taskCompletionSource;
			try
			{
				TValue val = await ReadValueImplAsync(cancellationToken);
				tcs2.SetResult(val);
				if (cacheValue != null)
				{
					cacheValue.Value = val;
				}
				return val;
			}
			catch (Exception ex)
			{
				tcs2.TrySetException(ex);
				throw;
			}
			finally
			{
				_readTcs = null;
			}
		}

		protected virtual Task<TValue> ReadValueImplAsync(CancellationToken cancellationToken)
		{
			return (_readValueBlockedAsync ?? throw new NotImplementedException("AsyncValueBatchedReader - Derived classes should not call the base implementation"))!(cancellationToken);
		}
	}
}
