using System.Diagnostics;
using ids.portable.common;

namespace IDS.Portable.Common
{
	public class AsyncValueCached<TValue> : CommonDisposableNotifyPropertyChanged, IAsyncValueCached<TValue>
	{
		private Stopwatch? _valueExpireTrackingTimer;

		private readonly object _lock = new object();

		private TValue _value;

		private AsyncValueCachedOperation<TValue>? _asyncUpdateOperationActive;

		public int ValueGoodForMs { get; }

		public TValue Value
		{
			get
			{
				lock (_lock)
				{
					return (_asyncUpdateOperationActive != null) ? _asyncUpdateOperationActive!.ValueNew : _value;
				}
			}
			set
			{
				lock (_lock)
				{
					if (_asyncUpdateOperationActive != null)
					{
						_asyncUpdateOperationActive!.ValueToRevertTo = value;
					}
					else
					{
						SetRealOrPendingValue(value);
					}
				}
			}
		}

		public (TValue Value, AsyncValueCachedState State) ValueAndState
		{
			get
			{
				lock (_lock)
				{
					return (_value, State);
				}
			}
		}

		public bool HasValue => _valueExpireTrackingTimer != null;

		public bool NeedsUpdate
		{
			get
			{
				lock (_lock)
				{
					return !HasValue || _valueExpireTrackingTimer == null || ValueGoodForMs == 0 || _valueExpireTrackingTimer.IsStopped() || _valueExpireTrackingTimer?.ElapsedMilliseconds > ValueGoodForMs;
				}
			}
		}

		public bool IsAsyncUpdating => _asyncUpdateOperationActive != null;

		public AsyncValueCachedState State
		{
			get
			{
				if (!HasValue)
				{
					return AsyncValueCachedState.NoValue;
				}
				if (IsAsyncUpdating)
				{
					return AsyncValueCachedState.HasValueUpdating;
				}
				if (NeedsUpdate)
				{
					return AsyncValueCachedState.HasValueNeedsUpdate;
				}
				return AsyncValueCachedState.HasValue;
			}
		}

		private void SetRealOrPendingValue(TValue value, bool revertingValue = false)
		{
			lock (_lock)
			{
				if (!revertingValue)
				{
					if (_valueExpireTrackingTimer == null)
					{
						_valueExpireTrackingTimer = new Stopwatch();
					}
					_valueExpireTrackingTimer!.Restart();
				}
				SetBackingField(ref _value, value, "Value", "HasValue", "NeedsUpdate", "IsAsyncUpdating", "State");
			}
		}

		public void InvalidateCache()
		{
			_valueExpireTrackingTimer?.Stop();
		}

		public AsyncValueCached(int valueGoodForMs)
		{
			ValueGoodForMs = valueGoodForMs;
		}

		public AsyncValueCached(TValue initialValue, int valueGoodForMs)
			: this(valueGoodForMs)
		{
			Value = initialValue;
		}

		public AsyncValueCachedOperation<TValue> AsyncUpdateStart(TValue value)
		{
			lock (_lock)
			{
				_asyncUpdateOperationActive = new AsyncValueCachedOperation<TValue>(Value, value);
				SetRealOrPendingValue(value);
				return _asyncUpdateOperationActive;
			}
		}

		public void AsyncUpdateComplete(AsyncValueCachedOperation<TValue>? asyncUpdateOperation)
		{
			if (asyncUpdateOperation == null)
			{
				return;
			}
			lock (_lock)
			{
				if (_asyncUpdateOperationActive == asyncUpdateOperation)
				{
					_asyncUpdateOperationActive = null;
					SetRealOrPendingValue(asyncUpdateOperation!.ValueNew);
				}
			}
		}

		public void AsyncUpdateFailed(AsyncValueCachedOperation<TValue>? asyncUpdateOperation)
		{
			if (asyncUpdateOperation == null)
			{
				return;
			}
			lock (_lock)
			{
				if (_asyncUpdateOperationActive == asyncUpdateOperation)
				{
					_asyncUpdateOperationActive = null;
					SetRealOrPendingValue(asyncUpdateOperation!.ValueToRevertTo, revertingValue: true);
				}
			}
		}
	}
}
