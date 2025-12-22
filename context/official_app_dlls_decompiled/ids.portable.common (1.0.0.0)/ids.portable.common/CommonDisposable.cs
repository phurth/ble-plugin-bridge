using System;
using System.Threading;

namespace IDS.Portable.Common
{
	public abstract class CommonDisposable : ICommonDisposable, IDisposable
	{
		private int mIsDisposed;

		public bool IsDisposed => mIsDisposed != 0;

		public void TryDispose()
		{
			try
			{
				if (!IsDisposed)
				{
					Dispose();
				}
			}
			catch
			{
			}
		}

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
