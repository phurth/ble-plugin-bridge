using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.Common
{
	public static class SemaphoreSlimExtension
	{
		public static async Task<SemaphoreSlimLock> LockAsync(this SemaphoreSlim semaphore, CancellationToken cancellationToken)
		{
			await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
			return new SemaphoreSlimLock(semaphore, hasLock: true);
		}

		public static async Task<SemaphoreSlimLock> TryLockAsync(this SemaphoreSlim semaphore, CancellationToken cancellationToken)
		{
			try
			{
				return await semaphore.LockAsync(cancellationToken);
			}
			catch
			{
				return new SemaphoreSlimLock(semaphore, hasLock: false);
			}
		}
	}
}
