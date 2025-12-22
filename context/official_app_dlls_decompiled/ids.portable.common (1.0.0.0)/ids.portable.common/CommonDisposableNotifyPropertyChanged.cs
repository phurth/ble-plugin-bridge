using System;
using System.Threading;

namespace IDS.Portable.Common
{
	public abstract class CommonDisposableNotifyPropertyChanged : CommonNotifyPropertyChanged, ICommonDisposable, IDisposable
	{
		private int _isDisposed;

		public bool IsDisposed => _isDisposed != 0;

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
			if (!IsDisposed && Interlocked.Exchange(ref _isDisposed, 1) == 0)
			{
				Dispose(disposing: true);
			}
		}

		public virtual void Dispose(bool disposing)
		{
			RemoveAllPropertyChangedEventHandler();
		}
	}
}
