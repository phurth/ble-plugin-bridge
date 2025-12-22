using System;
using System.Threading;

namespace IDS.Portable.Common
{
	public class BackgroundOperationDisposable : BackgroundOperation, ICommonDisposable, IDisposable
	{
		private const string LogTag = "BackgroundOperationDisposable";

		private int _isDisposed;

		public bool IsDisposed => _isDisposed != 0;

		protected BackgroundOperationDisposable()
		{
		}

		public BackgroundOperationDisposable(BackgroundOperationFunc operation)
			: base(operation)
		{
		}

		public BackgroundOperationDisposable(BackgroundOperationAction action)
			: base(action)
		{
		}

		public override void Start()
		{
			lock (Locker)
			{
				if (IsDisposed)
				{
					Stop();
				}
				else
				{
					base.Start();
				}
			}
		}

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
			Stop();
		}
	}
}
