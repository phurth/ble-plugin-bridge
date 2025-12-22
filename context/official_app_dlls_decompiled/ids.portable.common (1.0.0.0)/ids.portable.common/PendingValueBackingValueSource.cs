using System;

namespace IDS.Portable.Common
{
	public class PendingValueBackingValueSource<TValue> : PendingValue<TValue>
	{
		private const string LogTag = "PendingValueBackingValueSource";

		private Func<TValue>? _assignedValueSource;

		protected override TValue AssignedValue
		{
			get
			{
				try
				{
					if (base.IsDisposed)
					{
						throw new Exception("PendingValueBackingValueSource has been disposed");
					}
					if (_assignedValueSource == null)
					{
						throw new Exception("value source is null");
					}
					return _assignedValueSource!();
				}
				catch (Exception ex)
				{
					TaggedLog.Debug("PendingValueBackingValueSource", "Error getting property value from source: {0}", ex.Message);
					return default(TValue);
				}
			}
		}

		public PendingValueBackingValueSource(Func<TValue> assignedValueSource, int maxPendingValueTimeMs, ValueChangedHandler valueChanged)
			: base(assignedValueSource(), maxPendingValueTimeMs, valueChanged)
		{
			_assignedValueSource = assignedValueSource;
		}

		public PendingValueBackingValueSource(Func<TValue> assignedValueSource, ValueChangedHandler? valueChanged = null)
			: this(assignedValueSource, 5000, valueChanged)
		{
		}

		public PendingValueBackingValueSource(Func<TValue> assignedValueSource, ProxyOnPropertyChanged destinationOnPropertyChanged, string destinationPropertyName, ValueChangedHandler? valueChanged = null)
			: this(assignedValueSource, 5000, MakeValueChangeHandlerForPropertyChanged(destinationOnPropertyChanged, destinationPropertyName, valueChanged))
		{
		}

		private static ValueChangedHandler MakeValueChangeHandlerForPropertyChanged(ProxyOnPropertyChanged destinationOnPropertyChanged, string destinationPropertyName, ValueChangedHandler? valueChanged)
		{
			ValueChangedHandler valueChanged2 = valueChanged;
			return delegate(TValue oldValue, TValue newValue)
			{
				valueChanged2?.Invoke(oldValue, newValue);
				destinationOnPropertyChanged?.Invoke(destinationPropertyName);
			};
		}

		public static implicit operator TValue(PendingValueBackingValueSource<TValue> pendingValue)
		{
			return pendingValue.Value;
		}

		public override void Dispose(bool disposing)
		{
			_assignedValueSource = null;
			base.Dispose(disposing);
		}
	}
}
