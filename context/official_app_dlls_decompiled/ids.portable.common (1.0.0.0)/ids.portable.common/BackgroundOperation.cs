using System;
using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.Common
{
	public class BackgroundOperation : BackgroundOperationBase, IBackgroundOperation
	{
		public delegate Task BackgroundOperationFunc(CancellationToken cancellationToken);

		public delegate void BackgroundOperationAction(CancellationToken cancellationToken);

		private const string LogTag = "BackgroundOperation";

		private readonly BackgroundOperationFunc? _backgroundOperation;

		protected BackgroundOperation()
		{
			_backgroundOperation = null;
		}

		public BackgroundOperation(BackgroundOperationFunc operation)
			: this()
		{
			_backgroundOperation = operation ?? throw new ArgumentNullException("operation");
		}

		public BackgroundOperation(BackgroundOperationAction action)
		{
			BackgroundOperationAction action2 = action;
			this._002Ector();
			if (action2 == null)
			{
				throw new ArgumentNullException("action");
			}
			_backgroundOperation = delegate(CancellationToken cancelToken)
			{
				action2?.Invoke(cancelToken);
				return Task.CompletedTask;
			};
		}

		protected virtual async Task BackgroundOperationAsync(CancellationToken cancellationToken)
		{
			if (_backgroundOperation != null)
			{
				await _backgroundOperation!(cancellationToken);
			}
		}

		protected override Task BackgroundOperationAsync(object[]? args, CancellationToken cancellationToken)
		{
			return BackgroundOperationAsync(cancellationToken);
		}

		public virtual void Start()
		{
			BackgroundOperationStart(null);
		}

		public virtual void Stop()
		{
			BackgroundOperationStop();
		}
	}
	public class BackgroundOperation<TBackgroundArg1> : BackgroundOperationBase, IBackgroundOperation<TBackgroundArg1>
	{
		public delegate Task BackgroundOperationFunc(TBackgroundArg1 arg1, CancellationToken cancellationToken);

		public delegate void BackgroundOperationAction(TBackgroundArg1 arg1, CancellationToken cancellationToken);

		private const string LogTag = "BackgroundOperation";

		private readonly BackgroundOperationFunc? _backgroundOperation;

		protected BackgroundOperation()
		{
			_backgroundOperation = null;
		}

		public BackgroundOperation(BackgroundOperationFunc operation)
			: this()
		{
			_backgroundOperation = operation ?? throw new ArgumentNullException("operation");
		}

		public BackgroundOperation(BackgroundOperationAction action)
			: this()
		{
			if (action == null)
			{
				throw new ArgumentNullException("action");
			}
			_backgroundOperation = delegate(TBackgroundArg1 arg1, CancellationToken cancelToken)
			{
				action?.Invoke(arg1, cancelToken);
				return Task.CompletedTask;
			};
		}

		protected virtual async Task BackgroundOperationAsync(TBackgroundArg1 arg1, CancellationToken cancellationToken)
		{
			if (_backgroundOperation != null)
			{
				await _backgroundOperation!(arg1, cancellationToken);
			}
		}

		protected sealed override Task BackgroundOperationAsync(object[] args, CancellationToken cancellationToken)
		{
			if (args.Length != 1 || !(args[0] is TBackgroundArg1 arg))
			{
				object[] args2 = new string[1] { "TBackgroundArg1" };
				TaggedLog.Error("BackgroundOperation", "Invalid argument, expected 1 parameter of type {0}", args2);
				throw new ArgumentException("Invalid argument, expected 1 parameter of type {nameof(TBackgroundArg1)}", "args");
			}
			return BackgroundOperationAsync(arg, cancellationToken);
		}

		public virtual void Start(TBackgroundArg1 arg1)
		{
			BackgroundOperationStart(new object[1] { arg1 });
		}

		public virtual void Stop()
		{
			BackgroundOperationStop();
		}
	}
	public class BackgroundOperation<TBackgroundArg1, TBackgroundArg2> : BackgroundOperationBase, IBackgroundOperation<TBackgroundArg1, TBackgroundArg2>
	{
		public delegate Task BackgroundOperationFunc(TBackgroundArg1 arg1, TBackgroundArg2 arg2, CancellationToken cancellationToken);

		public delegate void BackgroundOperationAction(TBackgroundArg1 arg1, TBackgroundArg2 arg2, CancellationToken cancellationToken);

		private const string LogTag = "BackgroundOperation";

		private readonly BackgroundOperationFunc? _backgroundOperation;

		protected BackgroundOperation()
		{
			_backgroundOperation = null;
		}

		public BackgroundOperation(BackgroundOperationFunc operation)
			: this()
		{
			_backgroundOperation = operation ?? throw new ArgumentNullException("operation");
		}

		public BackgroundOperation(BackgroundOperationAction action)
			: this()
		{
			if (action == null)
			{
				throw new ArgumentNullException("action");
			}
			_backgroundOperation = delegate(TBackgroundArg1 arg1, TBackgroundArg2 arg2, CancellationToken cancelToken)
			{
				action(arg1, arg2, cancelToken);
				return Task.CompletedTask;
			};
		}

		protected virtual async Task BackgroundOperationAsync(TBackgroundArg1 arg1, TBackgroundArg2 arg2, CancellationToken cancellationToken)
		{
			if (_backgroundOperation != null)
			{
				await _backgroundOperation!(arg1, arg2, cancellationToken);
			}
		}

		protected sealed override Task BackgroundOperationAsync(object[]? args, CancellationToken cancellationToken)
		{
			if (args != null && args!.Length == 2 && args[0] is TBackgroundArg1 arg && args[1] is TBackgroundArg2 arg2)
			{
				return BackgroundOperationAsync(arg, arg2, cancellationToken);
			}
			TaggedLog.Error("BackgroundOperation", "Invalid argument, expected 2 parameters of type {0} and {1}", "TBackgroundArg1", "TBackgroundArg2");
			throw new ArgumentException("Invalid argument, expected 2 parameters of type TBackgroundArg1 and TBackgroundArg2", "args");
		}

		public virtual void Start(TBackgroundArg1 arg1, TBackgroundArg2 arg2)
		{
			BackgroundOperationStart(new object[2] { arg1, arg2 });
		}

		public virtual void Stop()
		{
			BackgroundOperationStop();
		}
	}
}
