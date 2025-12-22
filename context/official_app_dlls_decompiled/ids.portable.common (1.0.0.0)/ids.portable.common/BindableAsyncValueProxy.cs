using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.Common
{
	public class BindableAsyncValueProxy<TValue> : CommonNotifyPropertyChanged where TValue : IEquatable<TValue>
	{
		private IBindableAsyncValue<TValue>? _backingStore;

		public IBindableAsyncValue<TValue>? BackingStore
		{
			get
			{
				return _backingStore;
			}
			set
			{
				if (_backingStore != value)
				{
					if (_backingStore != null)
					{
						_backingStore!.PropertyChanged -= BackingStoreOnPropertyChanged;
					}
					_backingStore = value;
					if (_backingStore != null)
					{
						_backingStore!.PropertyChanged += BackingStoreOnPropertyChanged;
					}
					OnPropertyChanged("HasValueBeenLoaded");
					OnPropertyChanged("IsReading");
					OnPropertyChanged("IsWriting");
					OnPropertyChanged("LastValue");
					OnPropertyChanged("Value");
				}
			}
		}

		private IBindableAsyncValue<TValue>? ResolvedBackingStore
		{
			get
			{
				if (_backingStore == null || _backingStore!.IsDisposed)
				{
					return null;
				}
				return _backingStore;
			}
		}

		public bool HasValueBeenLoaded => ResolvedBackingStore?.HasValueBeenLoaded ?? false;

		public bool IsReading => ResolvedBackingStore?.IsReading ?? false;

		public bool IsWriting => ResolvedBackingStore?.IsWriting ?? false;

		public TValue LastValue
		{
			get
			{
				if (ResolvedBackingStore != null)
				{
					return ResolvedBackingStore!.LastValue;
				}
				return default(TValue);
			}
		}

		public TValue Value
		{
			get
			{
				if (ResolvedBackingStore != null)
				{
					return ResolvedBackingStore!.Value;
				}
				return default(TValue);
			}
		}

		~BindableAsyncValueProxy()
		{
			if (_backingStore != null)
			{
				_backingStore!.PropertyChanged -= BackingStoreOnPropertyChanged;
			}
			_backingStore = null;
		}

		public Task LoadAsync(CancellationToken cancellationToken)
		{
			return (ResolvedBackingStore ?? throw new Exception("Bindable Delayed Device Value Proxy Not Bound to Backing Store"))!.LoadAsync(cancellationToken);
		}

		public Task SaveAsync(CancellationToken cancellationToken)
		{
			return (ResolvedBackingStore ?? throw new Exception("Bindable Delayed Device Value Proxy Not Bound to Backing Store"))!.SaveAsync(cancellationToken);
		}

		private void BackingStoreOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
		{
			string propertyName = propertyChangedEventArgs.PropertyName;
			if (!(propertyName != "HasValueBeenLoaded") && !(propertyName != "IsReading") && !(propertyName != "IsWriting") && !(propertyName != "LastValue") && !(propertyName != "Value"))
			{
				OnPropertyChanged(propertyName);
			}
		}
	}
}
