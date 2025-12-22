using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;

namespace OneControl.Direct.MyRvLink
{
	internal class MyRvLinkCommandTracker : CommonDisposable
	{
		private const string LogTag = "MyRvLinkCommandTracker";

		public readonly IMyRvLinkCommand Command;

		private readonly TaskCompletionSource<IMyRvLinkCommandResponse> _waitingForCommandCompletedTcs;

		private TaskCompletionSource<IMyRvLinkCommandResponse>? _waitingForAnyResponseTcs;

		private readonly CancellationTokenSource _cts;

		private readonly CancellationTokenRegistration _cancellationTokenRegistration;

		private Action<IMyRvLinkCommandResponse>? _responseCallback;

		private readonly int _timeoutMs;

		private TaskCompletionSource<IMyRvLinkCommandResponseFailure?>? _waitingForFailureTcs;

		private IMyRvLinkCommandResponse? _lastResponseReceivedWithNoOneWaiting;

		private readonly object _lock = new object();

		public bool IsCompleted => _waitingForCommandCompletedTcs.Task.IsCompleted;

		public MyRvLinkCommandTracker(IMyRvLinkCommand command, CancellationToken cancelToken, int timeoutMs, Action<IMyRvLinkCommandResponse>? responseCallback)
		{
			MyRvLinkCommandTracker myRvLinkCommandTracker = this;
			Command = command;
			_timeoutMs = timeoutMs;
			_waitingForCommandCompletedTcs = new TaskCompletionSource<IMyRvLinkCommandResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
			_responseCallback = responseCallback;
			_cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
			_cancellationTokenRegistration = _cts.Token.Register(delegate
			{
				if (!myRvLinkCommandTracker.IsCompleted)
				{
					myRvLinkCommandTracker.TrySetFailure(cancelToken.IsCancellationRequested ? MyRvLinkCommandResponseFailureCode.CommandAborted : MyRvLinkCommandResponseFailureCode.CommandTimeout);
				}
			});
			ResetTimer();
		}

		public override void Dispose(bool disposing)
		{
			if (!_waitingForCommandCompletedTcs.Task.IsCompleted)
			{
				MyRvLinkCommandResponseFailure commandResponse = new MyRvLinkCommandResponseFailure(Command.ClientCommandId, MyRvLinkCommandResponseFailureCode.CommandAborted);
				ProcessResponse(commandResponse, forceCompleteCommand: true);
			}
			_cancellationTokenRegistration.TryDispose();
			_cts.TryDispose();
			_waitingForFailureTcs?.TrySetCanceled();
			_responseCallback = null;
		}

		public void ResetTimer()
		{
			_cts.CancelAfter(_timeoutMs);
		}

		public Task<IMyRvLinkCommandResponse> WaitAsync()
		{
			return _waitingForCommandCompletedTcs.Task;
		}

		public Task<IMyRvLinkCommandResponse> WaitForAnyResponse()
		{
			lock (_lock)
			{
				if (base.IsDisposed)
				{
					throw new ObjectDisposedException("MyRvLinkCommandTracker");
				}
				try
				{
					if (IsCompleted)
					{
						return _waitingForCommandCompletedTcs.Task;
					}
					TaskCompletionSource<IMyRvLinkCommandResponse> waitingForAnyResponseTcs = _waitingForAnyResponseTcs;
					if (waitingForAnyResponseTcs != null && !waitingForAnyResponseTcs.Task.IsCompleted)
					{
						return waitingForAnyResponseTcs.Task;
					}
					_waitingForAnyResponseTcs = new TaskCompletionSource<IMyRvLinkCommandResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
					if (_lastResponseReceivedWithNoOneWaiting != null)
					{
						_waitingForAnyResponseTcs!.SetResult(_lastResponseReceivedWithNoOneWaiting);
					}
					return _waitingForAnyResponseTcs!.Task;
				}
				finally
				{
					_lastResponseReceivedWithNoOneWaiting = null;
				}
			}
		}

		public Task<IMyRvLinkCommandResponseFailure?> TryWaitForAnyFailure(TimeSpan timeout, CancellationToken cancellationToken)
		{
			TaskCompletionSource<IMyRvLinkCommandResponseFailure> tcs;
			lock (_lock)
			{
				if (_waitingForFailureTcs != null && !_waitingForFailureTcs!.Task.IsCompleted)
				{
					return _waitingForFailureTcs!.Task;
				}
				tcs = (_waitingForFailureTcs = new TaskCompletionSource<IMyRvLinkCommandResponseFailure>(TaskCreationOptions.RunContinuationsAsynchronously));
			}
			return tcs.TryWaitAsync(cancellationToken, (int)timeout.TotalMilliseconds, updateTcs: true);
		}

		public void ProcessResponse(IMyRvLinkCommandResponse commandResponse, bool forceCompleteCommand)
		{
			lock (_lock)
			{
				if (!base.IsDisposed)
				{
					_responseCallback?.Invoke(commandResponse);
				}
				if (forceCompleteCommand || commandResponse.IsCommandCompleted)
				{
					_waitingForCommandCompletedTcs.TrySetResult(commandResponse);
					_lastResponseReceivedWithNoOneWaiting = null;
				}
				_waitingForAnyResponseTcs?.TrySetResult(commandResponse);
				if (_waitingForAnyResponseTcs == null || _waitingForAnyResponseTcs!.Task.IsCompleted)
				{
					_lastResponseReceivedWithNoOneWaiting = commandResponse;
				}
				if (!(commandResponse is IMyRvLinkCommandResponseSuccess))
				{
					if (commandResponse is IMyRvLinkCommandResponseFailure myRvLinkCommandResponseFailure)
					{
						_waitingForFailureTcs?.TrySetResult(myRvLinkCommandResponseFailure);
					}
				}
				else
				{
					_waitingForFailureTcs?.TrySetCanceled();
				}
			}
		}

		public IMyRvLinkCommandResponseFailure TrySetFailure(MyRvLinkCommandResponseFailureCode failureCode, IReadOnlyList<byte>? extendedData = null)
		{
			MyRvLinkCommandResponseFailure myRvLinkCommandResponseFailure = new MyRvLinkCommandResponseFailure(Command.ClientCommandId, failureCode, extendedData);
			ProcessResponse(myRvLinkCommandResponseFailure, forceCompleteCommand: true);
			return myRvLinkCommandResponseFailure;
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(31, 4);
			defaultInterpolatedStringHandler.AppendLiteral("[0x");
			defaultInterpolatedStringHandler.AppendFormatted(Command.ClientCommandId, "X4");
			defaultInterpolatedStringHandler.AppendLiteral("] IsComplete: ");
			defaultInterpolatedStringHandler.AppendFormatted(IsCompleted);
			defaultInterpolatedStringHandler.AppendLiteral(" IsDisposed: ");
			defaultInterpolatedStringHandler.AppendFormatted(base.IsDisposed);
			defaultInterpolatedStringHandler.AppendLiteral(" ");
			defaultInterpolatedStringHandler.AppendFormatted(Command);
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
