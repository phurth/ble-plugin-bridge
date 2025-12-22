using System;
using System.Threading;

namespace IDS.Core
{
	public abstract class Disposable : IDisposable, System.IDisposable
	{
		private int mIsDisposed;

		public bool IsDisposed => mIsDisposed != 0;

		public void Dispose()
		{
			if (!IsDisposed && Interlocked.Exchange(ref mIsDisposed, 1) == 0)
			{
				Dispose(disposing: true);
			}
		}

		public abstract void Dispose(bool disposing);
	}
}
