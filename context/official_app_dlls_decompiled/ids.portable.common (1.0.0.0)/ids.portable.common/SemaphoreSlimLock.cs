using System;
using System.Threading;

namespace IDS.Portable.Common
{
	public struct SemaphoreSlimLock : IDisposable
	{
		private readonly SemaphoreSlim _semaphore;

		private int _isDisposed;

		public bool HasLock { get; private set; }

		public bool IsDisposed => _isDisposed != 0;

		public SemaphoreSlimLock(SemaphoreSlim semaphore, bool hasLock)
		{
			_semaphore = semaphore;
			_isDisposed = 0;
			HasLock = hasLock;
		}

		public void Dispose()
		{
			if (!IsDisposed && Interlocked.Exchange(ref _isDisposed, 1) == 0 && HasLock)
			{
				_semaphore.Release();
				HasLock = false;
			}
		}
	}
}
