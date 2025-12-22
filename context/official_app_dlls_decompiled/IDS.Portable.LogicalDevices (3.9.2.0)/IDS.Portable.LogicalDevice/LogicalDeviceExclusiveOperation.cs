using System;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceExclusiveOperation
	{
		private class CancelableExclusiveOperation : CommonDisposable
		{
			private readonly Action _didStopAction;

			public CancelableExclusiveOperation(Action didStopAction)
			{
				_didStopAction = didStopAction;
			}

			public override void Dispose(bool disposing)
			{
				_didStopAction();
			}
		}

		private readonly object _lock = new object();

		private Action? _requestStopAction;

		public bool IsOperationStarted => _requestStopAction != null;

		internal LogicalDeviceExclusiveOperation()
		{
			_requestStopAction = null;
		}

		public IDisposable? Start(Action stopAction)
		{
			lock (_lock)
			{
				if (IsOperationStarted)
				{
					return null;
				}
				_requestStopAction = stopAction;
				return new CancelableExclusiveOperation(delegate
				{
					_requestStopAction = null;
				});
			}
		}

		public void RequestStop()
		{
			lock (_lock)
			{
				if (!IsOperationStarted)
				{
					return;
				}
			}
			_requestStopAction?.Invoke();
		}
	}
}
