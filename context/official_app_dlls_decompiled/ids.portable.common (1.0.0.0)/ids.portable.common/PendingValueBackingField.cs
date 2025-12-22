using System;

namespace IDS.Portable.Common
{
	public class PendingValueBackingField<TValue> : PendingValue<TValue> where TValue : IEquatable<TValue>
	{
		private const string LogTag = "PendingValueBackingField";

		private TValue _assignedValue;

		protected override TValue AssignedValue => _assignedValue;

		public PendingValueBackingField(TValue value, int maxPendingValueTimeMs, ValueChangedHandler? valueChanged)
			: base(value, maxPendingValueTimeMs, valueChanged)
		{
		}

		public PendingValueBackingField(TValue value, ValueChangedHandler? valueChanged = null)
			: this(value, 5000, valueChanged)
		{
		}

		public static implicit operator TValue(PendingValueBackingField<TValue> pendingValue)
		{
			return pendingValue.Value;
		}

		public void SetAssignedValue(TValue value)
		{
			lock (this)
			{
				_assignedValue = value;
				if (AreEqual(_assignedValue, base.CurrentPendingValue))
				{
					TryCancelPendingValue();
				}
				else
				{
					CalculateValue();
				}
			}
		}
	}
}
