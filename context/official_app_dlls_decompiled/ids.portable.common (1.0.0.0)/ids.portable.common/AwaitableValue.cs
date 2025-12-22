using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.Common
{
	public class AwaitableValue<TValue> : CommonDisposable
	{
		private class ValueRequest : CommonDisposable
		{
			private readonly TaskCompletionSource<TValue> _tcs;

			private readonly CancellationTokenSource _cts;

			private readonly CancellationTokenRegistration _ctsCanceledTokenReg;

			public Task<TValue> Task => _tcs.Task;

			public ValueRequest(CancellationToken sourceCancellationToken)
			{
				_tcs = new TaskCompletionSource<TValue>();
				_cts = CancellationTokenSource.CreateLinkedTokenSource(sourceCancellationToken);
				_ctsCanceledTokenReg = _cts.Token.Register(delegate
				{
					_tcs.TrySetCanceled();
				});
				_tcs.Task.ConfigureAwait(false);
			}

			public void SetResult(TValue value)
			{
				_tcs.TrySetResult(value);
			}

			public override void Dispose(bool disposing)
			{
				_ctsCanceledTokenReg.TryDispose();
				_cts.TryCancelAndDispose();
			}
		}

		private bool _valueSet;

		private TValue _value;

		private readonly List<ValueRequest> _pendingRequestList = new List<ValueRequest>();

		private readonly IEnumerable<TValue> _acceptedValues;

		private readonly object _syncSet = new object();

		public AwaitableValue()
		{
		}

		public AwaitableValue(TValue acceptVal)
		{
			_acceptedValues = new List<TValue> { acceptVal };
		}

		public AwaitableValue(IEnumerable<TValue> acceptedValues)
		{
			_acceptedValues = acceptedValues;
		}

		public Task<TValue> GetAsync(int msTimeout)
		{
			return GetAsync(new TimeSpan(0, 0, 0, 0, msTimeout));
		}

		public async Task<TValue> GetAsync(TimeSpan timeout)
		{
			using CancellationTokenSource cts = new CancellationTokenSource(timeout);
			return await GetAsync(cts.Token);
		}

		public Task<TValue> GetAsync(CancellationToken ct)
		{
			lock (_syncSet)
			{
				if (base.IsDisposed)
				{
					throw new ObjectDisposedException("AwaitableValue");
				}
				if (_valueSet)
				{
					return Task.FromResult(_value);
				}
				ValueRequest valueRequest = new ValueRequest(ct);
				_pendingRequestList.Add(valueRequest);
				return valueRequest.Task;
			}
		}

		public void Reset()
		{
			lock (_syncSet)
			{
				if (base.IsDisposed)
				{
					throw new ObjectDisposedException("AwaitableValue");
				}
				_valueSet = false;
			}
		}

		public void Set(TValue value)
		{
			lock (_syncSet)
			{
				if (base.IsDisposed)
				{
					throw new ObjectDisposedException("AwaitableValue");
				}
				IEnumerable<TValue> acceptedValues = _acceptedValues;
				if (acceptedValues != null && !Enumerable.Contains(acceptedValues, value))
				{
					return;
				}
				_valueSet = true;
				_value = value;
				foreach (ValueRequest pendingRequest in _pendingRequestList)
				{
					pendingRequest.SetResult(value);
					pendingRequest.TryDispose();
				}
				_pendingRequestList.Clear();
			}
		}

		public override void Dispose(bool disposing)
		{
			lock (_syncSet)
			{
				foreach (ValueRequest pendingRequest in _pendingRequestList)
				{
					pendingRequest.TryDispose();
				}
				_pendingRequestList.Clear();
			}
		}
	}
}
