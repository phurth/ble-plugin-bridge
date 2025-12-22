using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.Common
{
	public abstract class BindableAsyncValue<TValue> : CommonDisposableNotifyPropertyChanged, IBindableAsyncValue<TValue>, INotifyPropertyChanged, ICommonDisposable, IDisposable where TValue : IEquatable<TValue>
	{
		private bool _hasValueBeenLoaded;

		private bool _isValueInvalid = true;

		private bool _isReading;

		private bool _isWriting;

		private TValue _lastValue;

		private TValue _value;

		public bool HasValueBeenLoaded
		{
			get
			{
				return _hasValueBeenLoaded;
			}
			protected set
			{
				SetBackingField(ref _hasValueBeenLoaded, value, "HasValueBeenLoaded", "IsReady");
			}
		}

		public bool IsValueInvalid
		{
			get
			{
				return _isValueInvalid;
			}
			protected set
			{
				SetBackingField(ref _isValueInvalid, value, "IsValueInvalid");
			}
		}

		public bool IsReading
		{
			get
			{
				return _isReading;
			}
			protected set
			{
				SetBackingField(ref _isReading, value, "IsReading");
			}
		}

		public bool IsWriting
		{
			get
			{
				return _isWriting;
			}
			protected set
			{
				SetBackingField(ref _isWriting, value, "IsWriting", "IsReady");
			}
		}

		public bool IsReady
		{
			get
			{
				if (HasValueBeenLoaded)
				{
					return !IsWriting;
				}
				return false;
			}
		}

		public TValue LastValue
		{
			get
			{
				return _lastValue;
			}
			protected set
			{
				SetBackingField(ref _lastValue, value, "LastValue");
			}
		}

		public virtual TValue Value
		{
			get
			{
				return _value;
			}
			set
			{
				SetBackingField(ref _value, value, "Value");
			}
		}

		public abstract Task LoadAsync(CancellationToken cancellationToken);

		public abstract Task SaveAsync(CancellationToken cancellationToken);
	}
}
