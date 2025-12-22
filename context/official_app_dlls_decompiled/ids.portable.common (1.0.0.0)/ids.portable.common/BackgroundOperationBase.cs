using System;
using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.Common
{
	public abstract class BackgroundOperationBase
	{
		private const string LogTag = "BackgroundOperationBase";

		protected readonly object Locker = new object();

		private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

		private bool _restartRequested;

		private object[]? _restartArgs;

		public bool Started { get; private set; }

		public bool StartedOrWillStart
		{
			get
			{
				lock (Locker)
				{
					return Started || _restartRequested;
				}
			}
		}

		protected BackgroundOperationBase()
		{
			Started = false;
		}

		protected abstract Task BackgroundOperationAsync(object[]? args, CancellationToken cancellationToken);

		protected virtual void BackgroundOperationStart(object[]? args)
		{
			object[] args2 = args;
			CancellationToken cancelToken;
			lock (Locker)
			{
				if (Started)
				{
					if (!_restartRequested)
					{
						_restartRequested = true;
						_restartArgs = args2;
					}
					return;
				}
				_restartRequested = false;
				Started = true;
				_cancellationTokenSource.TryCancelAndDispose();
				_cancellationTokenSource = new CancellationTokenSource();
				cancelToken = _cancellationTokenSource.Token;
			}
			Task.Run(async delegate
			{
				try
				{
					if (!cancelToken.IsCancellationRequested)
					{
						await BackgroundOperationAsync(args2, cancelToken);
					}
				}
				catch (OperationCanceledException)
				{
					_cancellationTokenSource.TryCancel();
					TaggedLog.Debug("BackgroundOperationBase", "Background Operation was canceled", string.Empty);
				}
				catch (Exception ex2)
				{
					TaggedLog.Error("BackgroundOperationBase", "Background Operation threw Exception {0}", ex2.Message);
				}
				finally
				{
					lock (Locker)
					{
						Started = false;
						if (cancelToken.IsCancellationRequested && _restartRequested)
						{
							BackgroundOperationStart(_restartArgs);
						}
					}
				}
			}, CancellationToken.None);
		}

		protected virtual void BackgroundOperationStop()
		{
			lock (Locker)
			{
				if (!_restartRequested)
				{
					_restartArgs = null;
				}
				_restartRequested = false;
				_cancellationTokenSource.TryCancelAndDispose();
			}
		}
	}
}
