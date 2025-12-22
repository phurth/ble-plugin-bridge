using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice.LogicalDeviceSource.ConnectionFailure
{
	public abstract class ConnectionFailureManager<TConnectionFailure> : ConnectionFailureManager, IConnectionFailureBle<TConnectionFailure>, IConnectionFailure, INotifyPropertyChanged
	{
		public virtual TConnectionFailure ActiveConnectionFailure => GetConnectionFailure(base.ActiveException);

		public virtual TConnectionFailure LastConnectionFailure => GetConnectionFailure(base.ActiveException);

		public abstract TConnectionFailure GetConnectionFailure(Exception? lastFailureException);
	}
	public class ConnectionFailureManager : CommonDisposableNotifyPropertyChanged, IConnectionFailure, INotifyPropertyChanged
	{
		public const int TimeRemainingUntilRetryRefreshMs = 1000;

		private DateTime? _lastDateTime;

		private Exception? _lastException;

		private Exception? _activeException;

		private DateTime? _retryTime;

		public DateTime? LastDateTime
		{
			get
			{
				return _lastDateTime;
			}
			private set
			{
				SetBackingField(ref _lastDateTime, value, "LastDateTime");
			}
		}

		public Exception? LastException
		{
			get
			{
				return _lastException;
			}
			private set
			{
				SetBackingField(ref _lastException, value, "LastException");
			}
		}

		public Exception? ActiveException
		{
			get
			{
				return _activeException;
			}
			private set
			{
				SetBackingField(ref _activeException, value, "ActiveException");
			}
		}

		public DateTime? RetryTime
		{
			get
			{
				return _retryTime;
			}
			private set
			{
				SetBackingField(ref _retryTime, value, "RetryTime");
			}
		}

		public void RegisterFailure(Exception? exception, DateTime? retryTime = null)
		{
			if (exception == null)
			{
				Clear();
				return;
			}
			LastDateTime = DateTime.Now;
			LastException = exception;
			ActiveException = exception;
			RetryTime = retryTime;
		}

		public void Clear()
		{
			ActiveException = null;
			RetryTime = null;
		}

		public async Task TryDelayForRetry(TimeSpan delay, Exception? ex, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				RegisterFailure(ex, DateTime.Now);
				return;
			}
			RegisterFailure(ex, DateTime.Now + delay);
			await TaskExtension.TryDelay(delay, cancellationToken);
		}

		public Task TryDelayForRetry(double delayMs, Exception? exception, CancellationToken cancellationToken)
		{
			return TryDelayForRetry(TimeSpan.FromMilliseconds(delayMs), exception, cancellationToken);
		}
	}
}
