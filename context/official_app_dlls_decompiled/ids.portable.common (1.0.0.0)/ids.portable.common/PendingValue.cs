using System;

namespace IDS.Portable.Common
{
	public abstract class PendingValue<TValue> : CommonDisposable, IPendingValue<TValue>
	{
		public delegate void ValueChangedHandler(TValue oldValue, TValue newValue);

		private const string LogTag = "PendingValue";

		public const int DefaultMaxPendingValueTimeMs = 5000;

		private Watchdog? _resetActiveValueWatchdog;

		private bool _pendingValueEnabled;

		private ValueChangedHandler? _valueChangedHandler;

		private readonly object _locker = new object();

		private TValue _lastKnownValue;

		protected virtual int MaxPendingValueTimeMs { get; }

		public TValue Value
		{
			get
			{
				return CalculateValue().Value;
			}
			set
			{
				SetPendingValue(value);
			}
		}

		protected abstract TValue AssignedValue { get; }

		protected TValue CurrentPendingValue { get; private set; }

		public bool IsValuePending
		{
			get
			{
				if (_pendingValueEnabled)
				{
					return !AreEqual(_lastKnownValue, AssignedValue);
				}
				return false;
			}
		}

		public event ValueChangedHandler? ValueChanged;

		public event ValueChangedHandler? PendingValueChanged;

		protected PendingValue(TValue value, int maxPendingValueTimeMs, ValueChangedHandler? valueChanged)
		{
			MaxPendingValueTimeMs = maxPendingValueTimeMs;
			CurrentPendingValue = value;
			_pendingValueEnabled = false;
			_lastKnownValue = value;
			_valueChangedHandler = valueChanged;
			if (valueChanged != null)
			{
				ValueChanged += valueChanged;
			}
		}

		public static implicit operator TValue(PendingValue<TValue> pendingValue)
		{
			return pendingValue.Value;
		}

		protected bool AreEqual(TValue first, TValue second)
		{
			return first?.Equals(second) ?? (second == null);
		}

		protected (TValue Value, bool Changed) CalculateValue()
		{
			lock (_locker)
			{
				TValue val = (_pendingValueEnabled ? CurrentPendingValue : AssignedValue);
				if (AreEqual(_lastKnownValue, val))
				{
					return (_lastKnownValue, false);
				}
				TValue lastKnownValue = _lastKnownValue;
				_lastKnownValue = val;
				if (!base.IsDisposed)
				{
					OnValueChanged(lastKnownValue, _lastKnownValue);
				}
				return (_lastKnownValue, true);
			}
		}

		protected virtual void OnValueChanged(TValue oldValue, TValue newValue)
		{
			try
			{
				this.ValueChanged?.Invoke(oldValue, newValue);
			}
			catch (Exception ex)
			{
				TaggedLog.Error("PendingValue", "Invoking value changed handler for pending value {0}", ex.Message);
			}
		}

		public void SetPendingValue(TValue value)
		{
			lock (_locker)
			{
				if (base.IsDisposed)
				{
					TaggedLog.Debug("PendingValue", "Unable to set bending value {0} because object has been disposed", value);
					return;
				}
				CurrentPendingValue = value;
				if (AreEqual(AssignedValue, value))
				{
					return;
				}
				_pendingValueEnabled = true;
				CalculateValue();
				if (_resetActiveValueWatchdog == null)
				{
					_resetActiveValueWatchdog = new Watchdog(MaxPendingValueTimeMs, CancelPendingValue, autoStartOnFirstPet: true);
				}
				_resetActiveValueWatchdog!.TryPet(autoReset: true);
				try
				{
					this.PendingValueChanged?.Invoke(AssignedValue, value);
				}
				catch (Exception ex)
				{
					TaggedLog.Error("PendingValue", "Invoking pending value changed handler for value {0} {1} => {2}: {3}", value, AssignedValue, value, ex.Message);
				}
			}
		}

		private void CancelPendingValue()
		{
			lock (_locker)
			{
				_pendingValueEnabled = false;
				CalculateValue();
			}
		}

		public void TryCancelPendingValue()
		{
			lock (_locker)
			{
				_resetActiveValueWatchdog?.TryDispose();
				_resetActiveValueWatchdog = null;
				CancelPendingValue();
			}
		}

		public override void Dispose(bool disposing)
		{
			lock (_locker)
			{
				TryCancelPendingValue();
				if (_valueChangedHandler != null)
				{
					try
					{
						ValueChanged -= _valueChangedHandler;
					}
					catch
					{
					}
					_valueChangedHandler = null;
				}
				this.ValueChanged = null;
				this.PendingValueChanged = null;
			}
		}
	}
}
