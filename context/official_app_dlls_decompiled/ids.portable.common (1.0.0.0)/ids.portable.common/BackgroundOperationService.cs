using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.Common
{
	public abstract class BackgroundOperationService<TSingleton> : Singleton<TSingleton>, IBackgroundOperation where TSingleton : class
	{
		private readonly BackgroundOperation _backgroundOperation;

		public bool Started => _backgroundOperation.Started;

		public bool StartedOrWillStart => _backgroundOperation.StartedOrWillStart;

		protected BackgroundOperationService()
		{
			_backgroundOperation = new BackgroundOperationDisposable((BackgroundOperation.BackgroundOperationFunc)BackgroundOperationAsync);
		}

		public virtual void Start()
		{
			_backgroundOperation.Start();
		}

		public virtual void Stop()
		{
			_backgroundOperation.Stop();
		}

		protected abstract Task BackgroundOperationAsync(CancellationToken cancellationToken);
	}
	public abstract class BackgroundOperationService<TSingleton, TArg1> : Singleton<TSingleton>, IBackgroundOperation<TArg1> where TSingleton : class
	{
		private readonly BackgroundOperation<TArg1> _backgroundOperation;

		public bool Started => _backgroundOperation.Started;

		public bool StartedOrWillStart => _backgroundOperation.StartedOrWillStart;

		protected BackgroundOperationService()
		{
			_backgroundOperation = new BackgroundOperation<TArg1>((BackgroundOperation<TArg1>.BackgroundOperationFunc)BackgroundOperationAsync);
		}

		public virtual void Start(TArg1 arg1)
		{
			_backgroundOperation.Start(arg1);
		}

		public virtual void Stop()
		{
			_backgroundOperation.Stop();
		}

		protected abstract Task BackgroundOperationAsync(TArg1 arg1, CancellationToken cancellationToken);
	}
	public abstract class BackgroundOperationService<TSingleton, TArg1, TArg2> : Singleton<TSingleton>, IBackgroundOperation<TArg1, TArg2> where TSingleton : class
	{
		private readonly BackgroundOperation<TArg1, TArg2> _backgroundOperation;

		public bool Started => _backgroundOperation.Started;

		public bool StartedOrWillStart => _backgroundOperation.StartedOrWillStart;

		protected BackgroundOperationService()
		{
			_backgroundOperation = new BackgroundOperation<TArg1, TArg2>((BackgroundOperation<TArg1, TArg2>.BackgroundOperationFunc)BackgroundOperationAsync);
		}

		public virtual void Start(TArg1 arg1, TArg2 arg2)
		{
			_backgroundOperation.Start(arg1, arg2);
		}

		public virtual void Stop()
		{
			_backgroundOperation.Stop();
		}

		protected abstract Task BackgroundOperationAsync(TArg1 arg1, TArg2 arg2, CancellationToken cancellationToken);
	}
}
